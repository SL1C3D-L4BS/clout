using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Crafting;
using Clout.Forensics;

namespace Clout.Empire.Dealing
{
    /// <summary>
    /// Product-specific inventory — tracks drug stacks separately from item inventory.
    /// Each stack has quality and potency metadata that affects sale price.
    ///
    /// Works alongside InventoryManager (for weapons/consumables/items)
    /// but keeps product tracking cleaner with quality-aware stacking.
    ///
    /// Phase 2: MonoBehaviour (singleplayer)
    /// Phase 4: NetworkBehaviour with SyncList for multiplayer
    /// </summary>
    public class ProductInventory : MonoBehaviour
    {
        [Header("Config")]
        public int maxProductSlots = 20;
        public float maxProductWeight = 30f;    // kg — separate from item weight

        // Runtime
        private List<ProductStack> _products = new List<ProductStack>();
        private float _currentProductWeight;

        public IReadOnlyList<ProductStack> Products => _products;
        public float CurrentWeight => _currentProductWeight;
        public int SlotCount => _products.Count;
        public bool HasSpace => _products.Count < maxProductSlots;

        // ── Step 12: Forensic Signature Tracking ─────────────
        // Maps productId+qualityTier → most recent BatchSignature for that stack.
        // Signatures propagate from CraftingStation → ProductInventory → DealManager.
        private readonly Dictionary<string, BatchSignature> _signatureMap = new();

        public event Action OnProductsChanged;

        /// <summary>
        /// Get the forensic signature associated with a product stack (if any).
        /// Returns null if product was acquired without signature tracking (legacy/pre-Step 12).
        /// </summary>
        public BatchSignature GetSignature(string productId, float quality)
        {
            string key = productId + "_" + GetQualityTierIdByName(productId, quality);
            return _signatureMap.TryGetValue(key, out var sig) ? sig : null;
        }

        /// <summary>
        /// Get the signature key for a product stack.
        /// </summary>
        private string GetSignatureKey(ProductDefinition product, float quality)
        {
            return product.productName + "_" + GetQualityTierId(product, quality);
        }

        private string GetQualityTierIdByName(string productId, float quality)
        {
            // Search existing stacks for the product definition
            foreach (var stack in _products)
            {
                if (stack.productId == productId && stack.product != null)
                    return GetQualityTierId(stack.product, quality);
            }
            return "default";
        }

        /// <summary>
        /// Add product with forensic signature (Step 12).
        /// Signature is tracked alongside the stack for downstream propagation.
        /// </summary>
        public int AddProduct(ProductDefinition product, int quantity, float quality,
            BatchSignature signature)
        {
            int added = AddProduct(product, quantity, quality);
            if (added > 0 && signature != null)
            {
                string key = GetSignatureKey(product, quality);
                _signatureMap[key] = signature;
            }
            return added;
        }

        /// <summary>
        /// Add product to inventory. Returns actual quantity added.
        /// Stacks by productId + quality tier (different quality = different stack).
        /// </summary>
        public int AddProduct(ProductDefinition product, int quantity, float quality)
        {
            if (product == null || quantity <= 0) return 0;

            float weightToAdd = product.weight * quantity;
            if (_currentProductWeight + weightToAdd > maxProductWeight * 1.5f)
                return 0;

            // Try stacking with matching product + quality tier
            string tierId = GetQualityTierId(product, quality);
            for (int i = 0; i < _products.Count; i++)
            {
                if (_products[i].productId == product.productName &&
                    GetQualityTierId(product, _products[i].quality) == tierId)
                {
                    var stack = _products[i];
                    stack.quantity += quantity;
                    // Blend quality when stacking
                    stack.quality = Mathf.Lerp(stack.quality, quality,
                        (float)quantity / stack.quantity);
                    _products[i] = stack;
                    _currentProductWeight += product.weight * quantity;
                    OnProductsChanged?.Invoke();
                    return quantity;
                }
            }

            // New stack
            if (_products.Count >= maxProductSlots) return 0;

            _products.Add(new ProductStack
            {
                productId = product.productName,
                product = product,
                quantity = quantity,
                quality = quality
            });
            _currentProductWeight += product.weight * quantity;
            OnProductsChanged?.Invoke();
            return quantity;
        }

        /// <summary>
        /// Remove product. Prefers removing from lowest quality stack first.
        /// Returns actual quantity removed.
        /// </summary>
        public int RemoveProduct(string productId, int quantity)
        {
            int removed = 0;

            // Sort by quality ascending — sell worst first (player can override in UI)
            for (int i = _products.Count - 1; i >= 0 && removed < quantity; i--)
            {
                if (_products[i].productId != productId) continue;

                int canRemove = Mathf.Min(quantity - removed, _products[i].quantity);
                var stack = _products[i];
                stack.quantity -= canRemove;

                float unitWeight = stack.product != null ? stack.product.weight : 0.1f;
                _currentProductWeight -= unitWeight * canRemove;

                if (stack.quantity <= 0)
                    _products.RemoveAt(i);
                else
                    _products[i] = stack;

                removed += canRemove;
            }

            _currentProductWeight = Mathf.Max(0, _currentProductWeight);
            if (removed > 0) OnProductsChanged?.Invoke();
            return removed;
        }

        /// <summary>
        /// Get total quantity of a specific product across all quality tiers.
        /// </summary>
        public int GetProductCount(string productId)
        {
            int total = 0;
            for (int i = 0; i < _products.Count; i++)
            {
                if (_products[i].productId == productId)
                    total += _products[i].quantity;
            }
            return total;
        }

        /// <summary>
        /// Get all stacks of a specific product.
        /// </summary>
        public List<ProductStack> GetProductStacks(string productId)
        {
            var result = new List<ProductStack>();
            for (int i = 0; i < _products.Count; i++)
            {
                if (_products[i].productId == productId)
                    result.Add(_products[i]);
            }
            return result;
        }

        /// <summary>
        /// Get the best quality stack of a product.
        /// </summary>
        public ProductStack? GetBestStack(string productId)
        {
            ProductStack? best = null;
            for (int i = 0; i < _products.Count; i++)
            {
                if (_products[i].productId == productId)
                {
                    if (!best.HasValue || _products[i].quality > best.Value.quality)
                        best = _products[i];
                }
            }
            return best;
        }

        /// <summary>
        /// Get total estimated street value of all held product.
        /// </summary>
        public float GetTotalValue()
        {
            float total = 0;
            for (int i = 0; i < _products.Count; i++)
            {
                var stack = _products[i];
                if (stack.product != null)
                {
                    float tierMult = GetQualityMultiplier(stack.product, stack.quality);
                    total += stack.product.baseStreetValue * tierMult * stack.quantity;
                }
            }
            return total;
        }

        /// <summary>
        /// Get all products — used by deal UI for product selection.
        /// </summary>
        public List<ProductStack> GetAllProducts()
        {
            return new List<ProductStack>(_products);
        }

        // ─── Helpers ──────────────────────────────────────────

        private string GetQualityTierId(ProductDefinition product, float quality)
        {
            if (product.qualityTiers == null || product.qualityTiers.Length == 0)
                return "default";

            for (int i = product.qualityTiers.Length - 1; i >= 0; i--)
            {
                if (quality >= product.qualityTiers[i].minQuality)
                    return product.qualityTiers[i].tierName;
            }
            return product.qualityTiers[0].tierName;
        }

        public static float GetQualityMultiplier(ProductDefinition product, float quality)
        {
            if (product.qualityTiers == null || product.qualityTiers.Length == 0)
                return 1f;

            for (int i = product.qualityTiers.Length - 1; i >= 0; i--)
            {
                if (quality >= product.qualityTiers[i].minQuality)
                    return product.qualityTiers[i].priceMultiplier;
            }
            return product.qualityTiers[0].priceMultiplier;
        }
    }

    [System.Serializable]
    public struct ProductStack
    {
        public string productId;
        public ProductDefinition product;
        public int quantity;
        [Range(0f, 1f)]
        public float quality;

        public bool IsEmpty => product == null || quantity <= 0;
    }
}
