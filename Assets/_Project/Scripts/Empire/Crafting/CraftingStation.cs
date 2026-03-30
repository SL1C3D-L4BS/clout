using UnityEngine;
using Clout.Core;
using Clout.Empire.Dealing;
using Clout.Player;
using Clout.Utils;
using Clout.World.Police;
using System;
using System.Collections.Generic;

namespace Clout.Empire.Crafting
{
    /// <summary>
    /// Interactive crafting station — the heart of the production loop.
    ///
    /// Player approaches → selects recipe → ingredients consumed → timer runs →
    /// quality calculated from base + skill + additives → product deposited into
    /// player's ProductInventory → EventBus fires ProductCookedEvent.
    ///
    /// Supports:
    /// - Multiple concurrent batches (upgradeable)
    /// - Additive-based quality modification (cutting = more qty, less quality)
    /// - Risk events: lab explosions, fume detection → police heat
    /// - Speed multiplier from equipment upgrades
    /// - Automation via employee assignment (Phase 2+)
    ///
    /// Implements IInteractable for the player interaction system.
    /// </summary>
    public class CraftingStation : MonoBehaviour, IInteractable
    {
        [Header("Station Config")]
        public string stationName = "Lab Table";
        public string stationId;                        // Unique ID for save/event tracking
        public StationType stationType = StationType.BasicLab;
        public RecipeDefinition[] availableRecipes;

        [Header("Capacity")]
        public int maxConcurrentBatches = 1;
        public float speedMultiplier = 1f;              // Upgraded equipment cooks faster
        public float qualityBonus = 0f;                 // Equipment quality bonus (0-0.2)

        [Header("Automation")]
        public bool isAutomated = false;
        public EmployeeRole requiredEmployee = EmployeeRole.Cook;

        [Header("Risk")]
        [Range(0f, 0.5f)]
        public float explosionRiskMultiplier = 1f;      // Station safety rating
        [Range(0f, 0.5f)]
        public float fumeLeakMultiplier = 1f;           // Ventilation quality

        [Header("Visual")]
        public Transform productSpawnPoint;             // Where finished product appears
        public GameObject cookingVFX;                   // Particle effect while active
        public GameObject completionVFX;                // Particle effect on batch done

        // ─── Runtime State ───────────────────────────────────────

        private List<CraftingBatch> _activeBatches = new List<CraftingBatch>();
        private PlayerStateManager _lastInteractingPlayer;
        private bool _uiOpen;

        public IReadOnlyList<CraftingBatch> ActiveBatches => _activeBatches;
        public bool HasActiveBatch => _activeBatches.Count > 0;
        public bool IsFull => _activeBatches.Count >= maxConcurrentBatches;

        // ─── Events ──────────────────────────────────────────────

        /// <summary>Fired when a batch finishes cooking. Subscribers get the result.</summary>
        public event Action<CraftingResult> OnBatchComplete;

        /// <summary>Fired when a risk event triggers (explosion, fume detection).</summary>
        public event Action<RiskEvent> OnRiskEvent;

        /// <summary>Fired when the station UI should open/close.</summary>
        public event Action<CraftingStation, PlayerStateManager> OnStationOpened;
        public event Action OnStationClosed;

        // ─── IInteractable ───────────────────────────────────────

        public string InteractionPrompt
        {
            get
            {
                if (IsFull)
                    return $"{stationName} [BUSY — {_activeBatches.Count}/{maxConcurrentBatches}]";
                if (HasActiveBatch)
                    return $"Use {stationName} [{_activeBatches.Count}/{maxConcurrentBatches} active]";
                return $"Use {stationName}";
            }
        }

        public bool CanInteract(CharacterStateManager character)
        {
            // Can always interact to check progress, even if full
            return !_uiOpen;
        }

        public void OnInteract(CharacterStateManager character)
        {
            PlayerStateManager player = character as PlayerStateManager;
            if (player == null) return;

            _lastInteractingPlayer = player;
            _uiOpen = true;
            OnStationOpened?.Invoke(this, player);
            Debug.Log($"[Crafting] {stationName} opened by {player.name}");
        }

        public void CloseStation()
        {
            _uiOpen = false;
            _lastInteractingPlayer = null;
            OnStationClosed?.Invoke();
        }

        // ─── Production ──────────────────────────────────────────

        /// <summary>
        /// Start a crafting batch. Consumes ingredients from player's IngredientInventory.
        /// Returns the batch on success, null on failure.
        /// </summary>
        public CraftingBatch StartBatch(
            RecipeDefinition recipe,
            PlayerStateManager player,
            bool useAdditives = true)
        {
            if (recipe == null || player == null) return null;
            if (_activeBatches.Count >= maxConcurrentBatches)
            {
                Debug.LogWarning($"[Crafting] {stationName} is full — cannot start batch.");
                return null;
            }

            // Get ingredient inventory
            IngredientInventory ingredientInv = player.GetComponent<IngredientInventory>();
            if (ingredientInv == null)
            {
                Debug.LogWarning("[Crafting] Player has no IngredientInventory.");
                return null;
            }

            // Check and consume required ingredients
            if (!ingredientInv.HasIngredientsForRecipe(recipe))
            {
                Debug.Log("[Crafting] Insufficient ingredients for recipe.");
                return null;
            }

            // Consume required ingredients
            ingredientInv.ConsumeIngredientsForRecipe(recipe);

            // Consume optional additives (if available and opted in)
            List<IngredientDefinition> usedAdditives = new List<IngredientDefinition>();
            if (useAdditives)
                usedAdditives = ingredientInv.ConsumeAvailableAdditives(recipe);

            // Calculate quality from ingredients + additives + skill + equipment
            float quality = CalculateQuality(recipe, usedAdditives, player);

            // Calculate output quantity (additives can modify)
            int outputQuantity = CalculateOutputQuantity(recipe, usedAdditives);

            // Calculate duration
            float duration = recipe.craftingTime / speedMultiplier;

            // Create batch
            var batch = new CraftingBatch
            {
                recipe = recipe,
                player = player,
                startTime = Time.time,
                duration = duration,
                quality = quality,
                outputQuantity = outputQuantity,
                usedAdditives = usedAdditives,
                isComplete = false
            };

            _activeBatches.Add(batch);

            // Start cooking VFX
            if (cookingVFX != null)
                cookingVFX.SetActive(true);

            Debug.Log($"[Crafting] Batch started: {recipe.recipeName} " +
                      $"(Quality: {quality:P0}, Output: {outputQuantity}x, Duration: {duration:F1}s)");

            // Check for immediate risk events
            CheckRiskEvents(recipe, player);

            return batch;
        }

        // ─── Quality Calculation ─────────────────────────────────

        /// <summary>
        /// Quality = base + skill contribution + additive modifiers + equipment bonus.
        /// Range: 0.0 to 1.0
        ///
        /// Design philosophy: cooking skill matters most, but additives create
        /// meaningful choices (cutting agent = more product, less quality).
        /// </summary>
        private float CalculateQuality(
            RecipeDefinition recipe,
            List<IngredientDefinition> additives,
            PlayerStateManager player)
        {
            // Base quality from recipe
            float quality = recipe.baseQuality;

            // Player skill contribution (0-15% bonus)
            // TODO: Replace with actual crafting skill when skill system is built
            float playerSkill = 0.3f; // Placeholder — mid-level cook
            quality += playerSkill * 0.15f;

            // Equipment quality bonus (from station upgrades)
            quality += qualityBonus;

            // Additive effects — the key strategic layer
            if (additives != null)
            {
                foreach (var additive in additives)
                {
                    if (additive.effects == null) continue;
                    foreach (var effect in additive.effects)
                    {
                        quality += effect.qualityModifier;
                    }

                    // Potency from ingredient itself
                    quality += (additive.potency - 1f) * 0.05f;
                }
            }

            // Ingredient potency average from required ingredients
            if (recipe.ingredients != null)
            {
                float totalPotency = 0f;
                int count = 0;
                foreach (var slot in recipe.ingredients)
                {
                    if (slot.ingredient != null)
                    {
                        totalPotency += slot.ingredient.potency;
                        count++;
                    }
                }
                if (count > 0)
                {
                    float avgPotency = totalPotency / count;
                    quality += (avgPotency - 1f) * 0.1f; // High potency ingredients boost quality
                }
            }

            return Mathf.Clamp01(quality);
        }

        /// <summary>
        /// Output quantity = base + cutting agents. Cutting increases quantity but
        /// the quality penalty from CalculateQuality balances the trade-off.
        /// </summary>
        private int CalculateOutputQuantity(
            RecipeDefinition recipe,
            List<IngredientDefinition> additives)
        {
            float quantity = recipe.outputQuantity;

            if (additives != null)
            {
                foreach (var additive in additives)
                {
                    // Cutting agents increase output
                    if (additive.category == IngredientCategory.Cutting)
                        quantity *= 1.5f;  // 50% more product from cutting

                    // Check additive effects for value modifiers
                    if (additive.effects != null)
                    {
                        foreach (var effect in additive.effects)
                        {
                            // Value modifier > 0 means premium additive (less qty, better price)
                            // Value modifier < 0 means cutting (more qty, worse quality)
                            if (effect.valueModifier < 0)
                                quantity *= 1.2f;
                        }
                    }
                }
            }

            return Mathf.Max(1, Mathf.RoundToInt(quantity));
        }

        // ─── Risk Events ─────────────────────────────────────────

        /// <summary>
        /// Check for lab accidents and fume detection.
        /// Called when a batch starts — risk is front-loaded.
        /// </summary>
        private void CheckRiskEvents(RecipeDefinition recipe, PlayerStateManager player)
        {
            // Explosion check
            float explosionChance = recipe.explosionRisk * explosionRiskMultiplier;
            if (explosionChance > 0 && UnityEngine.Random.value < explosionChance)
            {
                var riskEvt = new RiskEvent
                {
                    type = RiskEventType.Explosion,
                    stationId = stationId,
                    severity = UnityEngine.Random.Range(0.3f, 1f),
                    message = $"Lab explosion at {stationName}!"
                };

                Debug.LogWarning($"[Crafting] EXPLOSION at {stationName}! Severity: {riskEvt.severity:P0}");

                // Damage player
                if (player.runtimeStats != null)
                {
                    int damage = Mathf.RoundToInt(riskEvt.severity * 40f);
                    player.runtimeStats.TakeDamage(damage);
                }

                // Generate heat from explosion
                WantedSystem wanted = player.wantedSystem;
                if (wanted != null)
                    wanted.AddHeat(riskEvt.severity * 80f, "Lab explosion");

                // Cancel active batches on severe explosion
                if (riskEvt.severity > 0.7f)
                {
                    _activeBatches.Clear();
                    if (cookingVFX != null) cookingVFX.SetActive(false);
                }

                OnRiskEvent?.Invoke(riskEvt);
            }

            // Fume detection check — neighbors notice and may call police
            float fumeChance = recipe.fumeDetectionRisk * fumeLeakMultiplier;
            if (fumeChance > 0 && UnityEngine.Random.value < fumeChance)
            {
                var riskEvt = new RiskEvent
                {
                    type = RiskEventType.FumeDetection,
                    stationId = stationId,
                    severity = UnityEngine.Random.Range(0.2f, 0.8f),
                    message = $"Fumes detected from {stationName}! Neighbors getting suspicious."
                };

                Debug.LogWarning($"[Crafting] Fume leak at {stationName}!");

                // Generate police heat
                WantedSystem wanted = player.wantedSystem;
                if (wanted != null)
                    wanted.AddHeat(riskEvt.severity * 40f, "Fume detection");

                OnRiskEvent?.Invoke(riskEvt);
            }
        }

        // ─── Update Loop ─────────────────────────────────────────

        private void Update()
        {
            if (_activeBatches.Count == 0) return;

            for (int i = _activeBatches.Count - 1; i >= 0; i--)
            {
                var batch = _activeBatches[i];
                if (!batch.isComplete && Time.time - batch.startTime >= batch.duration)
                {
                    CompleteBatch(batch);
                    _activeBatches.RemoveAt(i);
                }
            }

            // Turn off cooking VFX when no batches active
            if (_activeBatches.Count == 0 && cookingVFX != null)
                cookingVFX.SetActive(false);
        }

        private void CompleteBatch(CraftingBatch batch)
        {
            batch.isComplete = true;

            // Deposit product into player's ProductInventory
            PlayerStateManager player = batch.player;
            if (player == null)
            {
                // Player disconnected — product lost
                Debug.LogWarning("[Crafting] Batch completed but player is gone. Product lost.");
                return;
            }

            ProductInventory productInv = player.GetComponent<ProductInventory>();
            if (productInv == null)
                productInv = player.gameObject.AddComponent<ProductInventory>();

            ProductDefinition outputProduct = batch.recipe.outputProduct;
            if (outputProduct != null)
            {
                int added = productInv.AddProduct(outputProduct, batch.outputQuantity, batch.quality);

                Debug.Log($"[Crafting] BATCH COMPLETE: {batch.recipe.recipeName} → " +
                          $"{added}x {outputProduct.productName} (Quality: {batch.quality:P0})");

                // Build result
                var result = new CraftingResult
                {
                    recipe = batch.recipe,
                    product = outputProduct,
                    quantity = added,
                    quality = batch.quality,
                    usedAdditives = batch.usedAdditives,
                    stationId = stationId
                };

                // Fire local event
                OnBatchComplete?.Invoke(result);

                // Fire global EventBus event
                string qualityTier = GetQualityTierName(outputProduct, batch.quality);
                EventBus.Publish(new ProductCookedEvent
                {
                    productId = outputProduct.productName,
                    quantity = added,
                    qualityTier = GetQualityTierIndex(outputProduct, batch.quality),
                    stationId = stationId
                });
            }

            // Completion VFX
            if (completionVFX != null)
            {
                completionVFX.SetActive(true);
                // Auto-disable after 2 seconds
                Invoke(nameof(DisableCompletionVFX), 2f);
            }
        }

        private void DisableCompletionVFX()
        {
            if (completionVFX != null)
                completionVFX.SetActive(false);
        }

        // ─── Query Helpers ───────────────────────────────────────

        /// <summary>
        /// Get recipes that the player has ingredients for.
        /// </summary>
        public List<RecipeDefinition> GetCraftableRecipes(IngredientInventory ingredientInv)
        {
            var craftable = new List<RecipeDefinition>();
            if (availableRecipes == null) return craftable;

            foreach (var recipe in availableRecipes)
            {
                if (recipe != null && ingredientInv.HasIngredientsForRecipe(recipe))
                    craftable.Add(recipe);
            }
            return craftable;
        }

        /// <summary>
        /// Preview quality for a recipe without actually crafting.
        /// </summary>
        public float PreviewQuality(RecipeDefinition recipe, List<IngredientDefinition> additives)
        {
            return CalculateQuality(recipe, additives, null);
        }

        /// <summary>
        /// Preview output quantity for a recipe.
        /// </summary>
        public int PreviewOutputQuantity(RecipeDefinition recipe, List<IngredientDefinition> additives)
        {
            return CalculateOutputQuantity(recipe, additives);
        }

        private string GetQualityTierName(ProductDefinition product, float quality)
        {
            if (product.qualityTiers == null || product.qualityTiers.Length == 0)
                return "Standard";

            for (int i = product.qualityTiers.Length - 1; i >= 0; i--)
            {
                if (quality >= product.qualityTiers[i].minQuality)
                    return product.qualityTiers[i].tierName;
            }
            return product.qualityTiers[0].tierName;
        }

        private int GetQualityTierIndex(ProductDefinition product, float quality)
        {
            if (product.qualityTiers == null) return 0;

            for (int i = product.qualityTiers.Length - 1; i >= 0; i--)
            {
                if (quality >= product.qualityTiers[i].minQuality)
                    return i;
            }
            return 0;
        }

        private void OnDestroy()
        {
            _activeBatches.Clear();
        }
    }

    // ─── Data Structures ─────────────────────────────────────

    public enum StationType
    {
        BasicLab,           // Starter — cook basic product
        AdvancedLab,        // Mid-tier — higher quality, more batches
        IndustrialLab,      // High-end — max output, automation
        GrowRoom,           // Cannabis cultivation
        ExtractorLab,       // Concentrated product
        PackagingStation    // Repackage for distribution
    }

    [System.Serializable]
    public class CraftingBatch
    {
        public RecipeDefinition recipe;
        public PlayerStateManager player;
        public float startTime;
        public float duration;
        [Range(0f, 1f)]
        public float quality;
        public int outputQuantity;
        public List<IngredientDefinition> usedAdditives;
        public bool isComplete;

        public float Progress => duration > 0 ? Mathf.Clamp01((Time.time - startTime) / duration) : 1f;
        public float TimeRemaining => Mathf.Max(0f, duration - (Time.time - startTime));
    }

    public struct CraftingResult
    {
        public RecipeDefinition recipe;
        public ProductDefinition product;
        public int quantity;
        public float quality;
        public List<IngredientDefinition> usedAdditives;
        public string stationId;
    }

    public enum RiskEventType
    {
        Explosion,
        FumeDetection,
        EquipmentFailure,
        ContaminatedBatch
    }

    public struct RiskEvent
    {
        public RiskEventType type;
        public string stationId;
        public float severity;          // 0-1
        public string message;
    }
}
