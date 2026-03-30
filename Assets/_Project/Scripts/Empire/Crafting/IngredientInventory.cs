using UnityEngine;
using System;
using System.Collections.Generic;

namespace Clout.Empire.Crafting
{
    /// <summary>
    /// Raw material inventory — tracks ingredient stacks separately from products.
    ///
    /// Key differences from ProductInventory:
    /// - Ingredients have shelf life (degrade over time)
    /// - No quality tiers — ingredients have fixed potency
    /// - Stacks by ingredient ID (simple matching)
    /// - Feeds into CraftingStation for production
    ///
    /// Phase 2: MonoBehaviour (singleplayer)
    /// Phase 4: NetworkBehaviour with SyncList for multiplayer
    /// </summary>
    public class IngredientInventory : MonoBehaviour
    {
        [Header("Config")]
        public int maxSlots = 30;
        public float maxWeight = 50f;   // kg — ingredients are bulkier than product

        // Runtime
        private List<IngredientStack> _stacks = new List<IngredientStack>();
        private float _currentWeight;

        public IReadOnlyList<IngredientStack> Stacks => _stacks;
        public float CurrentWeight => _currentWeight;
        public int SlotCount => _stacks.Count;
        public bool HasSpace => _stacks.Count < maxSlots;

        public event Action OnInventoryChanged;

        // ─── Core Operations ─────────────────────────────────────

        /// <summary>
        /// Add ingredients. Returns actual quantity added.
        /// </summary>
        public int AddIngredient(IngredientDefinition ingredient, int quantity)
        {
            if (ingredient == null || quantity <= 0) return 0;

            float unitWeight = EstimateUnitWeight(ingredient);
            float weightToAdd = unitWeight * quantity;

            // Soft cap — allow slight overflow but not ridiculous amounts
            if (_currentWeight + weightToAdd > maxWeight * 1.2f)
            {
                int maxCanAdd = Mathf.FloorToInt((maxWeight * 1.2f - _currentWeight) / unitWeight);
                if (maxCanAdd <= 0) return 0;
                quantity = maxCanAdd;
                weightToAdd = unitWeight * quantity;
            }

            // Try stacking with existing
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i].ingredientId == ingredient.ingredientName)
                {
                    var stack = _stacks[i];
                    stack.quantity += quantity;
                    _stacks[i] = stack;
                    _currentWeight += weightToAdd;
                    OnInventoryChanged?.Invoke();
                    return quantity;
                }
            }

            // New stack
            if (_stacks.Count >= maxSlots) return 0;

            _stacks.Add(new IngredientStack
            {
                ingredientId = ingredient.ingredientName,
                ingredient = ingredient,
                quantity = quantity,
                acquiredTime = Time.time
            });

            _currentWeight += weightToAdd;
            OnInventoryChanged?.Invoke();
            return quantity;
        }

        /// <summary>
        /// Remove ingredients. Returns actual quantity removed.
        /// Prefers oldest stacks first (FIFO — use freshest last).
        /// </summary>
        public int RemoveIngredient(string ingredientId, int quantity)
        {
            int removed = 0;

            for (int i = 0; i < _stacks.Count && removed < quantity; i++)
            {
                if (_stacks[i].ingredientId != ingredientId) continue;

                int canRemove = Mathf.Min(quantity - removed, _stacks[i].quantity);
                var stack = _stacks[i];
                stack.quantity -= canRemove;

                float unitWeight = EstimateUnitWeight(stack.ingredient);
                _currentWeight -= unitWeight * canRemove;

                if (stack.quantity <= 0)
                {
                    _stacks.RemoveAt(i);
                    i--;    // Adjust index after removal
                }
                else
                {
                    _stacks[i] = stack;
                }

                removed += canRemove;
            }

            _currentWeight = Mathf.Max(0f, _currentWeight);
            if (removed > 0) OnInventoryChanged?.Invoke();
            return removed;
        }

        /// <summary>
        /// Check if we have enough of a specific ingredient.
        /// </summary>
        public bool HasIngredient(string ingredientId, int quantity)
        {
            return GetCount(ingredientId) >= quantity;
        }

        /// <summary>
        /// Check if we have all ingredients for a recipe.
        /// </summary>
        public bool HasIngredientsForRecipe(RecipeDefinition recipe)
        {
            if (recipe.ingredients == null) return true;

            foreach (var slot in recipe.ingredients)
            {
                if (slot.ingredient == null) continue;
                if (GetCount(slot.ingredient.ingredientName) < slot.quantity)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Consume all ingredients for a recipe. Returns false if insufficient.
        /// </summary>
        public bool ConsumeIngredientsForRecipe(RecipeDefinition recipe)
        {
            if (!HasIngredientsForRecipe(recipe)) return false;

            foreach (var slot in recipe.ingredients)
            {
                if (slot.ingredient == null) continue;
                RemoveIngredient(slot.ingredient.ingredientName, slot.quantity);
            }
            return true;
        }

        /// <summary>
        /// Consume optional additives for a recipe. Returns which were used.
        /// </summary>
        public List<IngredientDefinition> ConsumeAvailableAdditives(RecipeDefinition recipe)
        {
            var used = new List<IngredientDefinition>();
            if (recipe.optionalAdditives == null) return used;

            foreach (var slot in recipe.optionalAdditives)
            {
                if (slot.ingredient == null) continue;
                if (HasIngredient(slot.ingredient.ingredientName, slot.quantity))
                {
                    RemoveIngredient(slot.ingredient.ingredientName, slot.quantity);
                    used.Add(slot.ingredient);
                }
            }
            return used;
        }

        // ─── Queries ─────────────────────────────────────────────

        public int GetCount(string ingredientId)
        {
            int total = 0;
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i].ingredientId == ingredientId)
                    total += _stacks[i].quantity;
            }
            return total;
        }

        public IngredientStack? GetStack(string ingredientId)
        {
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i].ingredientId == ingredientId)
                    return _stacks[i];
            }
            return null;
        }

        public float GetTotalPurchaseValue()
        {
            float total = 0f;
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i].ingredient != null)
                    total += _stacks[i].ingredient.basePurchasePrice * _stacks[i].quantity;
            }
            return total;
        }

        // ─── Shelf Life & Degradation ────────────────────────────

        /// <summary>
        /// Called from Update or on a timer. Degrades expired ingredients.
        /// </summary>
        public void ProcessDegradation()
        {
            bool changed = false;

            for (int i = _stacks.Count - 1; i >= 0; i--)
            {
                var stack = _stacks[i];
                if (stack.ingredient == null) continue;

                // shelf_life is in hours; convert to seconds
                float shelfLifeSeconds = stack.ingredient.shelf_life * 3600f;
                float age = Time.time - stack.acquiredTime;

                if (age > shelfLifeSeconds)
                {
                    // Expired — lose 1 unit per check cycle
                    stack.quantity--;
                    if (stack.quantity <= 0)
                    {
                        float unitWeight = EstimateUnitWeight(stack.ingredient);
                        _currentWeight -= unitWeight;
                        _stacks.RemoveAt(i);
                    }
                    else
                    {
                        _stacks[i] = stack;
                    }
                    changed = true;
                }
            }

            if (changed)
            {
                _currentWeight = Mathf.Max(0f, _currentWeight);
                OnInventoryChanged?.Invoke();
            }
        }

        // ─── Helpers ─────────────────────────────────────────────

        private float EstimateUnitWeight(IngredientDefinition ingredient)
        {
            // Default 0.2kg per unit if no weight defined
            return ingredient != null ? Mathf.Max(0.01f, 0.2f) : 0.2f;
        }
    }

    [System.Serializable]
    public struct IngredientStack
    {
        public string ingredientId;
        public IngredientDefinition ingredient;
        public int quantity;
        public float acquiredTime;      // Time.time when acquired — for shelf life

        public bool IsEmpty => ingredient == null || quantity <= 0;
    }
}
