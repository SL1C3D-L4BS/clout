using UnityEngine;
using Clout.Player;
using Clout.Utils;
using System;
using System.Collections.Generic;

namespace Clout.Empire.Crafting
{
    /// <summary>
    /// Central production orchestrator — tracks all crafting activity across all stations.
    ///
    /// Singleton that provides:
    /// - Global view of all active batches across all stations
    /// - Production statistics (total cooked, quality averages, revenue estimates)
    /// - Risk management (total fume output, explosion history)
    /// - Ingredient shop integration (knows what player needs to buy)
    ///
    /// Parallel to DealManager (dealing orchestrator) — ProductionManager owns the
    /// cook-side of the loop while DealManager owns the sell-side.
    /// </summary>
    public class ProductionManager : MonoBehaviour
    {
        public static ProductionManager Instance { get; private set; }

        [Header("Config")]
        public float degradationCheckInterval = 60f;    // Check ingredient shelf life every N seconds

        // ─── Tracking ────────────────────────────────────────────

        // All registered stations in the world
        private List<CraftingStation> _registeredStations = new List<CraftingStation>();

        // Production stats
        private int _totalBatchesCompleted;
        private int _totalUnitsProduced;
        private float _totalRevenueEstimate;
        private float _averageQuality;
        private int _explosionCount;
        private int _fumeDetectionCount;

        // Degradation timer
        private float _lastDegradationCheck;

        // ─── Events ──────────────────────────────────────────────

        public event Action<CraftingResult> OnAnyBatchComplete;
        public event Action<RiskEvent> OnAnyRiskEvent;

        // ─── Properties ──────────────────────────────────────────

        public int TotalBatchesCompleted => _totalBatchesCompleted;
        public int TotalUnitsProduced => _totalUnitsProduced;
        public float TotalRevenueEstimate => _totalRevenueEstimate;
        public float AverageQuality => _averageQuality;
        public int ActiveBatchCount
        {
            get
            {
                int count = 0;
                foreach (var station in _registeredStations)
                {
                    if (station != null)
                        count += station.ActiveBatches.Count;
                }
                return count;
            }
        }

        // ─── Lifecycle ───────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // Periodic ingredient degradation check
            if (Time.time - _lastDegradationCheck >= degradationCheckInterval)
            {
                _lastDegradationCheck = Time.time;
                ProcessAllDegradation();
            }
        }

        // ─── Station Registration ────────────────────────────────

        /// <summary>
        /// Register a crafting station with the production manager.
        /// Called by stations on Awake/Start.
        /// </summary>
        public void RegisterStation(CraftingStation station)
        {
            if (station == null || _registeredStations.Contains(station)) return;

            _registeredStations.Add(station);
            station.OnBatchComplete += HandleBatchComplete;
            station.OnRiskEvent += HandleRiskEvent;

            Debug.Log($"[Production] Station registered: {station.stationName} " +
                      $"(Total stations: {_registeredStations.Count})");
        }

        /// <summary>
        /// Unregister a station (when destroyed or sold).
        /// </summary>
        public void UnregisterStation(CraftingStation station)
        {
            if (station == null) return;

            station.OnBatchComplete -= HandleBatchComplete;
            station.OnRiskEvent -= HandleRiskEvent;
            _registeredStations.Remove(station);
        }

        // ─── Event Handlers ──────────────────────────────────────

        private void HandleBatchComplete(CraftingResult result)
        {
            _totalBatchesCompleted++;
            _totalUnitsProduced += result.quantity;

            // Estimate revenue from this batch
            if (result.product != null)
            {
                float qualityMult = Dealing.ProductInventory.GetQualityMultiplier(
                    result.product, result.quality);
                float batchRevenue = result.product.baseStreetValue * qualityMult * result.quantity;
                _totalRevenueEstimate += batchRevenue;
            }

            // Running average quality
            _averageQuality = ((_averageQuality * (_totalBatchesCompleted - 1)) + result.quality)
                              / _totalBatchesCompleted;

            OnAnyBatchComplete?.Invoke(result);

            Debug.Log($"[Production] Stats: {_totalBatchesCompleted} batches, " +
                      $"{_totalUnitsProduced} units, " +
                      $"avg quality {_averageQuality:P0}, " +
                      $"est. revenue ${_totalRevenueEstimate:F0}");
        }

        private void HandleRiskEvent(RiskEvent evt)
        {
            switch (evt.type)
            {
                case RiskEventType.Explosion:
                    _explosionCount++;
                    break;
                case RiskEventType.FumeDetection:
                    _fumeDetectionCount++;
                    break;
            }

            OnAnyRiskEvent?.Invoke(evt);
        }

        // ─── Degradation ─────────────────────────────────────────

        private void ProcessAllDegradation()
        {
            // Find all IngredientInventories in the scene and process degradation
            var inventories = FindObjectsByType<IngredientInventory>(FindObjectsSortMode.None);
            foreach (var inv in inventories)
            {
                inv.ProcessDegradation();
            }
        }

        // ─── Queries ─────────────────────────────────────────────

        /// <summary>
        /// Get all active batches across all stations.
        /// </summary>
        public List<CraftingBatch> GetAllActiveBatches()
        {
            var batches = new List<CraftingBatch>();
            foreach (var station in _registeredStations)
            {
                if (station != null)
                    batches.AddRange(station.ActiveBatches);
            }
            return batches;
        }

        /// <summary>
        /// Get all registered stations.
        /// </summary>
        public IReadOnlyList<CraftingStation> GetAllStations() => _registeredStations;

        /// <summary>
        /// Get stations that have available capacity.
        /// </summary>
        public List<CraftingStation> GetAvailableStations()
        {
            var available = new List<CraftingStation>();
            foreach (var station in _registeredStations)
            {
                if (station != null && !station.IsFull)
                    available.Add(station);
            }
            return available;
        }

        /// <summary>
        /// Get a shopping list of ingredients the player needs to craft a recipe.
        /// Returns ingredient name → quantity needed (minus what player already has).
        /// </summary>
        public Dictionary<string, int> GetShoppingList(
            RecipeDefinition recipe,
            IngredientInventory playerIngredients)
        {
            var needed = new Dictionary<string, int>();
            if (recipe.ingredients == null) return needed;

            foreach (var slot in recipe.ingredients)
            {
                if (slot.ingredient == null) continue;

                string id = slot.ingredient.ingredientName;
                int has = playerIngredients != null ? playerIngredients.GetCount(id) : 0;
                int need = slot.quantity - has;

                if (need > 0)
                    needed[id] = need;
            }

            return needed;
        }

        /// <summary>
        /// Estimate total ingredient cost for a recipe.
        /// </summary>
        public float EstimateIngredientCost(RecipeDefinition recipe)
        {
            float cost = 0f;
            if (recipe.ingredients != null)
            {
                foreach (var slot in recipe.ingredients)
                {
                    if (slot.ingredient != null)
                        cost += slot.ingredient.basePurchasePrice * slot.quantity;
                }
            }
            if (recipe.optionalAdditives != null)
            {
                foreach (var slot in recipe.optionalAdditives)
                {
                    if (slot.ingredient != null)
                        cost += slot.ingredient.basePurchasePrice * slot.quantity;
                }
            }
            return cost;
        }

        /// <summary>
        /// Estimate profit margin for a recipe (sell price - ingredient cost).
        /// </summary>
        public float EstimateProfitMargin(RecipeDefinition recipe, float expectedQuality)
        {
            if (recipe.outputProduct == null) return 0f;

            float sellPrice = recipe.outputProduct.baseStreetValue
                              * Dealing.ProductInventory.GetQualityMultiplier(
                                  recipe.outputProduct, expectedQuality)
                              * recipe.outputQuantity;

            float ingredientCost = EstimateIngredientCost(recipe);
            return sellPrice - ingredientCost;
        }

        private void OnDestroy()
        {
            // Unsubscribe from all stations
            foreach (var station in _registeredStations)
            {
                if (station != null)
                {
                    station.OnBatchComplete -= HandleBatchComplete;
                    station.OnRiskEvent -= HandleRiskEvent;
                }
            }
            _registeredStations.Clear();

            if (Instance == this) Instance = null;
        }
    }
}
