using UnityEngine;
using Clout.Core;
using Clout.Utils;

namespace Clout.World.Interactables
{
    /// <summary>
    /// Pickup interaction — ported from Sharp Accent's PickableHook.
    /// Handles: weapon pickups, product pickups, cash pickups, ammo pickups, key items.
    ///
    /// Publishes events on pickup for UI feedback and save tracking.
    /// </summary>
    public class PickupInteractable : MonoBehaviour, IInteractable, IPickupable
    {
        [Header("Item")]
        public ScriptableObject itemData; // WeaponItem, ProductDefinition, etc.
        public int quantity = 1;

        [Header("Display")]
        public string overridePrompt;
        public bool destroyOnPickup = true;
        public bool playPickupAnimation = true;
        public string pickupAnimation = "pickup_ground";

        [Header("Spawn")]
        [Tooltip("If true, respawns after delay (for testing / persistent world items)")]
        public bool respawn;
        public float respawnDelay = 60f;

        private bool _pickedUp;

        private void Awake()
        {
            gameObject.layer = 15; // Interactable layer
        }

        public string InteractionPrompt
        {
            get
            {
                if (!string.IsNullOrEmpty(overridePrompt)) return overridePrompt;
                if (itemData != null) return $"Pick up {itemData.name}";
                return "Pick up";
            }
        }

        public bool CanInteract(CharacterStateManager interactor) => !_pickedUp;

        public void OnInteract(CharacterStateManager interactor)
        {
            if (_pickedUp) return;

            if (playPickupAnimation && !string.IsNullOrEmpty(pickupAnimation))
                interactor.PlayTargetAnimation(pickupAnimation, true);

            OnPickedUp(interactor);
        }

        public ScriptableObject GetItemData() => itemData;
        public int GetQuantity() => quantity;

        public void OnPickedUp(CharacterStateManager picker)
        {
            _pickedUp = true;

            // TODO: Add to picker's inventory based on itemData type
            // if (itemData is WeaponItem weapon) → picker.inventoryManager.LoadWeapon(weapon)
            // if (itemData is ProductDefinition product) → picker.productInventory.Add(product, quantity)
            // if (itemData is AmmoDefinition ammo) → picker.ammoCacheManager.AddAmmo(ammo, quantity)

            Debug.Log($"[Pickup] {picker.name} picked up {itemData?.name ?? "item"} x{quantity}");

            if (destroyOnPickup)
            {
                if (respawn)
                {
                    gameObject.SetActive(false);
                    Invoke(nameof(Respawn), respawnDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        private void Respawn()
        {
            _pickedUp = false;
            gameObject.SetActive(true);
        }
    }
}
