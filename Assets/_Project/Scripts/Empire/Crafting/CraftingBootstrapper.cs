using UnityEngine;
using Clout.Player;

namespace Clout.Empire.Crafting
{
    /// <summary>
    /// Runtime bootstrapper for the production system.
    /// Gives the player starting ingredients when the scene loads.
    ///
    /// Also registers crafting stations with the ProductionManager.
    ///
    /// Attach to ProductionManager GameObject.
    /// Remove or disable for production — this is a testing aid.
    /// </summary>
    public class CraftingBootstrapper : MonoBehaviour
    {
        [Header("Starting Ingredients")]
        public IngredientDefinition[] startingIngredients;
        public int[] startingQuantities;

        [Header("Config")]
        public bool giveIngredientsOnStart = true;

        private bool _initialized;
        private int _retryCount;

        private void Start()
        {
            if (!giveIngredientsOnStart) return;
            Invoke(nameof(Initialize), 1f); // Delay to ensure player is ready
        }

        private void Initialize()
        {
            if (_initialized) return;

            PlayerStateManager player = FindAnyObjectByType<PlayerStateManager>();
            if (player == null)
            {
                _retryCount++;
                if (_retryCount < 5)
                {
                    Invoke(nameof(Initialize), 0.5f);
                    return;
                }
                Debug.LogWarning("[CraftingBootstrapper] No player found after retries.");
                return;
            }
            _initialized = true;

            // Ensure IngredientInventory
            IngredientInventory inv = player.GetComponent<IngredientInventory>();
            if (inv == null)
                inv = player.gameObject.AddComponent<IngredientInventory>();

            // Give starting ingredients
            if (startingIngredients != null)
            {
                for (int i = 0; i < startingIngredients.Length; i++)
                {
                    if (startingIngredients[i] == null) continue;
                    int qty = (startingQuantities != null && i < startingQuantities.Length)
                        ? startingQuantities[i] : 5;
                    inv.AddIngredient(startingIngredients[i], qty);
                }
            }
            else
            {
                // Auto-load ingredients from Resources or ScriptableObjects
                GiveDefaultIngredients(inv);
            }

            // Register all crafting stations with ProductionManager
            RegisterAllStations();

            Debug.Log($"[CraftingBootstrapper] Player initialized with {inv.SlotCount} ingredient stacks.");
        }

        private void GiveDefaultIngredients(IngredientInventory inv)
        {
            // Try loading from known asset paths
            string basePath = "Assets/_Project/ScriptableObjects/Ingredients/";
            string[] defaultIngredients = {
                "ING_Cannabis Seeds",
                "ING_Pseudoephedrine",
                "ING_Iodine",
                "ING_Nutrient Solution",
                "ING_Baking Soda"
            };
            int[] defaultQuantities = { 5, 6, 3, 3, 4 };

#if UNITY_EDITOR
            for (int i = 0; i < defaultIngredients.Length; i++)
            {
                var ing = UnityEditor.AssetDatabase.LoadAssetAtPath<IngredientDefinition>(
                    basePath + defaultIngredients[i] + ".asset");
                if (ing != null)
                    inv.AddIngredient(ing, defaultQuantities[i]);
            }
#endif
        }

        private void RegisterAllStations()
        {
            ProductionManager pm = ProductionManager.Instance;
            if (pm == null) return;

            var stations = FindObjectsByType<CraftingStation>(FindObjectsSortMode.None);
            foreach (var station in stations)
            {
                pm.RegisterStation(station);
            }

            Debug.Log($"[CraftingBootstrapper] Registered {stations.Length} crafting stations.");
        }
    }
}
