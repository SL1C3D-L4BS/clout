using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Economy;
using Clout.Empire.Crafting;

namespace Clout.World.NPCs
{
    /// <summary>
    /// Fixed-price shop NPC — sells ingredients, buys product at wholesale.
    /// Each shop has a defined inventory with restock timers.
    ///
    /// Types:
    /// - IngredientSupplier: Sells raw materials for cooking
    /// - FenceShop: Buys product at wholesale (50-70% street value)
    /// - GeneralStore: Sells consumables, tools, equipment
    ///
    /// Wire to IInteractable or call OpenShop() from DealInteraction.
    /// </summary>
    public class ShopKeeper : MonoBehaviour, IInteractable
    {
        [Header("Shop Identity")]
        public string shopName = "Corner Store";
        public ShopType shopType = ShopType.IngredientSupplier;

        [Header("Inventory")]
        public ShopListing[] listings;

        [Header("Restock")]
        public float restockInterval = 300f; // 5 minutes
        private float _restockTimer;

        [Header("Fence Settings")]
        [Tooltip("Multiplier on street value when buying product from player (0.5 = 50%)")]
        [Range(0.3f, 0.9f)]
        public float fenceBuyRate = 0.5f;

        // Runtime stock tracking
        private Dictionary<string, int> _currentStock = new Dictionary<string, int>();

        // Events
        public event Action<ShopKeeper, CharacterStateManager> OnShopOpened;
        public event Action OnShopClosed;

        public string InteractionPrompt => $"Shop: {shopName}";

        private void Start()
        {
            InitializeStock();
            _restockTimer = restockInterval;
        }

        private void Update()
        {
            _restockTimer -= Time.deltaTime;
            if (_restockTimer <= 0)
            {
                RestockAll();
                _restockTimer = restockInterval;
            }
        }

        // ─── IInteractable ──────────────────────────────────

        public bool CanInteract(CharacterStateManager character)
        {
            return !character.isDead;
        }

        public void OnInteract(CharacterStateManager character)
        {
            OnShopOpened?.Invoke(this, character);
            Debug.Log($"[Shop] {shopName} opened by {character.gameObject.name}");
        }

        // ─── Stock Management ────────────────────────────────

        private void InitializeStock()
        {
            if (listings == null) return;
            foreach (var listing in listings)
            {
                _currentStock[listing.itemId] = listing.maxStock;
            }
        }

        private void RestockAll()
        {
            if (listings == null) return;
            foreach (var listing in listings)
            {
                if (!_currentStock.ContainsKey(listing.itemId))
                    _currentStock[listing.itemId] = 0;

                int current = _currentStock[listing.itemId];
                int restock = Mathf.CeilToInt(listing.maxStock * listing.restockRate);
                _currentStock[listing.itemId] = Mathf.Min(current + restock, listing.maxStock);
            }
        }

        public int GetStock(string itemId)
        {
            return _currentStock.TryGetValue(itemId, out int stock) ? stock : 0;
        }

        // ─── Transactions ────────────────────────────────────

        /// <summary>
        /// Buy an item from this shop. Returns true if successful.
        /// </summary>
        public bool BuyFromShop(string itemId, int quantity, CharacterStateManager buyer)
        {
            ShopListing listing = FindListing(itemId);
            if (listing.itemId == null) return false;

            int available = GetStock(itemId);
            if (available < quantity) return false;

            float totalCost = listing.price * quantity;

            CashManager cash = CashManager.Instance;
            if (cash == null || !cash.CanAfford(totalCost)) return false;

            // Process purchase
            cash.Spend(totalCost, $"Shop: {shopName} ({listing.displayName} x{quantity})");
            _currentStock[itemId] -= quantity;

            // Add to player's ingredient inventory if buying ingredients
            if (shopType == ShopType.IngredientSupplier)
            {
                IngredientInventory inv = buyer.GetComponent<IngredientInventory>();
                if (inv != null)
                    inv.AddIngredientById(itemId, quantity);
            }

            Debug.Log($"[Shop] {buyer.gameObject.name} bought {listing.displayName} x{quantity} " +
                      $"for ${totalCost:F0} from {shopName}");
            return true;
        }

        /// <summary>
        /// Sell product to a fence shop. Returns cash received.
        /// </summary>
        public float SellToFence(string productId, int quantity, float quality, CharacterStateManager seller)
        {
            if (shopType != ShopType.FenceShop) return 0;

            // Calculate wholesale price based on quality and fence rate
            float baseValue = 50f; // Default — EconomyManager can override
            EconomyManager econ = FindAnyObjectByType<EconomyManager>();
            if (econ != null)
                baseValue = econ.GetStreetPrice(productId, "default");

            float qualityMultiplier = 0.5f + quality * 1.5f; // 0.5x at Q0, 2.0x at Q1
            float fencePrice = baseValue * qualityMultiplier * fenceBuyRate * quantity;

            CashManager cash = CashManager.Instance;
            if (cash != null)
                cash.EarnDirty(fencePrice, $"Fence: sold {productId} x{quantity}");

            Debug.Log($"[Shop] {seller.gameObject.name} sold {productId} x{quantity} (Q:{quality:P0}) " +
                      $"to fence for ${fencePrice:F0}");
            return fencePrice;
        }

        /// <summary>
        /// Get all current listings with stock info.
        /// </summary>
        public List<ShopListingState> GetCurrentListings()
        {
            var result = new List<ShopListingState>();
            if (listings == null) return result;

            foreach (var listing in listings)
            {
                result.Add(new ShopListingState
                {
                    listing = listing,
                    currentStock = GetStock(listing.itemId)
                });
            }
            return result;
        }

        private ShopListing FindListing(string itemId)
        {
            if (listings == null) return default;
            foreach (var l in listings)
            {
                if (l.itemId == itemId) return l;
            }
            return default;
        }

        public void CloseShop()
        {
            OnShopClosed?.Invoke();
        }
    }

    // ─── Data Structures ─────────────────────────────────────

    public enum ShopType
    {
        IngredientSupplier,
        FenceShop,
        GeneralStore,
        WeaponDealer,
        BlackMarket
    }

    [System.Serializable]
    public struct ShopListing
    {
        public string itemId;
        public string displayName;
        public float price;
        public int maxStock;
        [Range(0f, 1f)]
        public float restockRate; // Fraction of maxStock restored per restock cycle
        public string description;
    }

    public struct ShopListingState
    {
        public ShopListing listing;
        public int currentStock;
    }
}
