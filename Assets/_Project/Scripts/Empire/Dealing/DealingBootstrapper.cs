using UnityEngine;
using Clout.Player;
using Clout.Empire.Crafting;

namespace Clout.Empire.Dealing
{
    /// <summary>
    /// Runtime bootstrapper for the dealing system.
    /// Gives the player starting product and cash when the scene loads.
    ///
    /// Attach to DealManager or any persistent GameObject.
    /// Remove or disable for production — this is a testing aid.
    /// </summary>
    public class DealingBootstrapper : MonoBehaviour
    {
        [Header("Starting Inventory")]
        public ProductDefinition[] startingProducts;
        public int[] startingQuantities;
        public float[] startingQualities;
        public float startingCash = 500f;

        [Header("Config")]
        public bool giveProductOnStart = true;

        private bool _initialized;

        private void Start()
        {
            if (!giveProductOnStart) return;

            // Delay one frame to ensure all components are initialized
            Invoke(nameof(Initialize), 0.1f);
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            PlayerStateManager player = FindAnyObjectByType<PlayerStateManager>();
            if (player == null)
            {
                Debug.LogWarning("[DealingBootstrapper] No player found.");
                return;
            }

            // Ensure ProductInventory
            ProductInventory inv = player.GetComponent<ProductInventory>();
            if (inv == null)
                inv = player.gameObject.AddComponent<ProductInventory>();

            // Give starting cash
            player.cash = startingCash;

            // Give starting products
            if (startingProducts != null)
            {
                for (int i = 0; i < startingProducts.Length; i++)
                {
                    if (startingProducts[i] == null) continue;
                    int qty = (startingQuantities != null && i < startingQuantities.Length)
                        ? startingQuantities[i] : 5;
                    float qual = (startingQualities != null && i < startingQualities.Length)
                        ? startingQualities[i] : 0.4f;

                    inv.AddProduct(startingProducts[i], qty, qual);
                }
            }

            Debug.Log($"[DealingBootstrapper] Player initialized: ${player.cash:F0} cash, " +
                      $"{inv.Products.Count} product stacks.");
        }
    }
}
