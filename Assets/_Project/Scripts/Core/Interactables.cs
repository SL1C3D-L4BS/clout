using UnityEngine;

namespace Clout.Core
{
    /// <summary>
    /// Extended interaction types and interfaces for CLOUT's crime empire systems.
    /// Supplements the base interfaces in Interfaces.cs (IInteractable, IShootable, IDamageable, ILockable).
    ///
    /// Ported patterns from Sharp Accent's DoorHook, PickableHook, ShootableHook, DestructiblePropObject
    /// and upgraded for empire gameplay: dealing, property entry, production, stash access.
    /// </summary>

    public enum InteractionType
    {
        None,
        Pickup,         // Pick up item from ground
        Door,           // Open/close door (property entry)
        Talk,           // NPC conversation / deal initiation
        Shop,           // Open shop UI
        MixingStation,  // Open production UI
        Stash,          // Open stash/storage UI
        Vehicle,        // Enter vehicle
        PropertySign,   // Purchase property
        Loot,           // Loot container (after kill, crate)
        Checkpoint,     // Save point
        Custom          // Anything else
    }

    /// <summary>Objects that can be destroyed (props, cover, vehicles).</summary>
    public interface IDestructible
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        void TakeDamage(float amount, Vector3 hitDirection);
        void OnDestroyed();
    }

    /// <summary>Objects the player can pick up.</summary>
    public interface IPickupable
    {
        ScriptableObject GetItemData();
        int GetQuantity();
        void OnPickedUp(CharacterStateManager picker);
    }
}
