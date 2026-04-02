using UnityEngine;
using Clout.Core;
using Clout.Player;
using Clout.Empire.Dealing;
using Clout.Empire.Economy;
using Clout.Empire.Reputation;
using Clout.World.Police;

namespace Clout.World.NPCs
{
    /// <summary>
    /// Supplier NPC — the connect. Player buys product wholesale here.
    ///
    /// Suppliers are stationary NPCs at specific locations.
    /// They restock on a timer and have limited inventory per cycle.
    /// Higher-tier suppliers require higher CLOUT rank.
    ///
    /// Flow:
    /// 1. Player approaches → sees "Buy Product" prompt
    /// 2. Interact → SupplierUI opens with product catalog
    /// 3. Player selects product + quantity → pays cash
    /// 4. Product added to player's ProductInventory
    /// </summary>
    public class SupplierNPC : MonoBehaviour, IInteractable
    {
        [Header("Supplier Data")]
        public SupplierDefinition supplierData;

        [Header("Inventory State")]
        public int[] currentStock;               // Per-catalog-entry stock remaining
        private float _restockTimer;

        // UI callback — SupplierUI subscribes
        public System.Action<SupplierContext> OnSupplierOpened;
        public System.Action OnSupplierClosed;

        private bool _isOpen;

        public string InteractionPrompt => supplierData != null
            ? $"Buy from {supplierData.supplierName}"
            : "Buy Product";

        private void Start()
        {
            if (supplierData != null)
                RestockAll();
        }

        private void Update()
        {
            if (supplierData == null) return;

            _restockTimer -= Time.deltaTime;
            if (_restockTimer <= 0)
            {
                RestockAll();
                _restockTimer = supplierData.restockInterval;
            }
        }

        public bool CanInteract(CharacterStateManager character)
        {
            if (supplierData == null) return false;
            if (_isOpen) return false;

            // Check CLOUT rank requirement
            PlayerStateManager player = character as PlayerStateManager;
            if (player == null) return false;

            ReputationManager rep = player.reputationManager;
            if (rep != null && rep.CurrentRank < supplierData.requiredCloutRank)
                return false;

            return true;
        }

        public void OnInteract(CharacterStateManager character)
        {
            PlayerStateManager player = character as PlayerStateManager;
            if (player == null || !CanInteract(character)) return;

            _isOpen = true;

            // Lock player
            player.isInteracting = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SupplierContext context = new SupplierContext
            {
                player = player,
                supplier = supplierData,
                currentStock = currentStock,
                npc = this
            };

            OnSupplierOpened?.Invoke(context);
        }

        /// <summary>
        /// Execute a purchase from this supplier.
        /// Called by SupplierUI when player confirms purchase.
        /// </summary>
        public PurchaseResult BuyProduct(PlayerStateManager player, int catalogIndex, int quantity)
        {
            if (supplierData == null || catalogIndex < 0 || catalogIndex >= supplierData.catalog.Length)
                return new PurchaseResult { success = false, message = "Invalid product." };

            var entry = supplierData.catalog[catalogIndex];

            // Stock check
            if (currentStock[catalogIndex] < quantity)
                return new PurchaseResult { success = false, message = "Not enough in stock." };

            // Cash check
            float totalCost = entry.wholesalePrice * quantity;
            CashManager cash = CashManager.Instance;
            float availableCash = cash != null ? cash.TotalCash : player.cash;
            if (availableCash < totalCost)
                return new PurchaseResult { success = false, message = $"Need ${totalCost:F0}. You have ${availableCash:F0}." };

            // Bust check
            if (Random.value < supplierData.bustRisk)
            {
                // Police sting! Add massive heat
                if (player.wantedSystem != null)
                    player.wantedSystem.AddHeat(WantedSystem.HeatValues.DealingNearPolice * 2f, "supplier sting!");
                return new PurchaseResult { success = false, message = "IT'S A SETUP! RUN!" };
            }

            // Execute purchase — spend through CashManager (buying supplies = dirty money)
            if (cash != null)
                cash.Spend(totalCost, $"Supplier: {supplierData.supplierName} ({entry.product.productName} x{quantity})");
            else
                player.cash -= totalCost;

            // Generate quality within supplier range
            float quality = Random.Range(entry.qualityFloor, entry.qualityCeiling);

            // Add to product inventory
            ProductInventory productInv = player.GetComponent<ProductInventory>();
            if (productInv == null)
            {
                productInv = player.gameObject.AddComponent<ProductInventory>();
            }

            int added = productInv.AddProduct(entry.product, quantity, quality);
            currentStock[catalogIndex] -= added;

            // Publish money event
            Utils.EventBus.Publish(new Utils.MoneyChangedEvent
            {
                totalCash = cash != null ? cash.TotalCash : player.cash,
                dirtyCash = cash != null ? cash.DirtyCash : player.cash,
                cleanCash = cash != null ? cash.CleanCash : 0,
                changeAmount = -totalCost,
                source = $"Supplier: {entry.product.productName} x{added}"
            });

            string tierName = GetQualityTierName(entry.product, quality);
            return new PurchaseResult
            {
                success = true,
                quantityBought = added,
                totalCost = totalCost,
                quality = quality,
                message = $"Bought {added}x {entry.product.productName} ({tierName}) for ${totalCost:F0}"
            };
        }

        public void CloseSupplier(PlayerStateManager player)
        {
            _isOpen = false;
            if (player != null)
            {
                player.isInteracting = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            OnSupplierClosed?.Invoke();
        }

        private void RestockAll()
        {
            if (supplierData == null) return;
            currentStock = new int[supplierData.catalog.Length];
            for (int i = 0; i < supplierData.catalog.Length; i++)
            {
                // Reliability affects stock — unreliable suppliers may not have everything
                if (Random.value < supplierData.reliability)
                    currentStock[i] = supplierData.catalog[i].maxQuantityPerRestock;
                else
                    currentStock[i] = Random.Range(0, supplierData.catalog[i].maxQuantityPerRestock / 2);
            }
        }

        private string GetQualityTierName(Clout.Empire.Crafting.ProductDefinition product, float quality)
        {
            if (product.qualityTiers == null || product.qualityTiers.Length == 0)
                return "Unknown";
            for (int i = product.qualityTiers.Length - 1; i >= 0; i--)
            {
                if (quality >= product.qualityTiers[i].minQuality)
                    return product.qualityTiers[i].tierName;
            }
            return product.qualityTiers[0].tierName;
        }
    }

    public struct SupplierContext
    {
        public PlayerStateManager player;
        public SupplierDefinition supplier;
        public int[] currentStock;
        public SupplierNPC npc;
    }

    public struct PurchaseResult
    {
        public bool success;
        public int quantityBought;
        public float totalCost;
        public float quality;
        public string message;
    }
}
