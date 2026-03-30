using UnityEngine;
using Clout.Core;
using System;
using System.Collections.Generic;

namespace Clout.Empire.Crafting
{
    /// <summary>
    /// Interactive crafting station — player approaches, opens crafting UI,
    /// selects recipe, inserts ingredients, and begins cooking.
    ///
    /// Supports automation: upgraded stations can auto-craft with employees.
    /// </summary>
    public class CraftingStation : MonoBehaviour, IInteractable
    {
        [Header("Station Config")]
        public string stationName = "Lab Table";
        public PropertyType stationType = PropertyType.Lab;
        public RecipeDefinition[] availableRecipes;

        [Header("Capacity")]
        public int maxConcurrentBatches = 1;
        public float speedMultiplier = 1f;          // Upgradeable

        [Header("Automation")]
        public bool isAutomated = false;
        public EmployeeRole requiredEmployee = EmployeeRole.Cook;

        // Runtime state
        private List<CraftingBatch> _activeBatches = new List<CraftingBatch>();

        public string InteractionPrompt => isAutomated ? $"Manage {stationName}" : $"Use {stationName}";

        public bool CanInteract(CharacterStateManager character) =>
            _activeBatches.Count < maxConcurrentBatches || isAutomated;

        public void OnInteract(CharacterStateManager character)
        {
            // Open crafting UI — handled by UIManager
            Debug.Log($"[Crafting] Opening {stationName} for {character.name}");
        }

        /// <summary>
        /// Start a crafting batch. Returns false if station is full.
        /// </summary>
        public bool StartBatch(RecipeDefinition recipe, float playerSkill)
        {
            if (_activeBatches.Count >= maxConcurrentBatches) return false;

            float quality = CalculateQuality(recipe, playerSkill);
            float duration = recipe.craftingTime / speedMultiplier;

            var batch = new CraftingBatch
            {
                recipe = recipe,
                startTime = Time.time,
                duration = duration,
                quality = quality,
                isComplete = false
            };

            _activeBatches.Add(batch);
            return true;
        }

        private float CalculateQuality(RecipeDefinition recipe, float playerSkill)
        {
            float quality = recipe.baseQuality;
            quality += playerSkill * 0.1f;    // Skill adds up to ~10% quality
            quality += speedMultiplier > 1f ? 0.05f : 0f; // Better equipment = better quality
            return Mathf.Clamp01(quality);
        }

        private void Update()
        {
            for (int i = _activeBatches.Count - 1; i >= 0; i--)
            {
                if (Time.time - _activeBatches[i].startTime >= _activeBatches[i].duration)
                {
                    CompleteBatch(_activeBatches[i]);
                    _activeBatches.RemoveAt(i);
                }
            }
        }

        private void CompleteBatch(CraftingBatch batch)
        {
            Debug.Log($"[Crafting] Batch complete: {batch.recipe.recipeName} (Quality: {batch.quality:P0})");
            // TODO: Create product in inventory, check for accidents, trigger events
        }

        public event Action<CraftingBatch> OnBatchComplete;
    }

    [System.Serializable]
    public class CraftingBatch
    {
        public RecipeDefinition recipe;
        public float startTime;
        public float duration;
        public float quality;
        public bool isComplete;

        public float Progress => Mathf.Clamp01((Time.time - startTime) / duration);
    }
}
