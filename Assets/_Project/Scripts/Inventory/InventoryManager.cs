using UnityEngine;
using FishNet.Object;
using System;
using System.Collections.Generic;

namespace Clout.Inventory
{
    /// <summary>
    /// Per-player inventory — server-authoritative item management.
    ///
    /// Supports:
    /// - Slot-based inventory with weight limit
    /// - Stacking for consumables/ammo/ingredients
    /// - Quick slots for equipped items
    /// - Separate stash storage at properties
    /// - Illegal item flagging (police searches)
    /// </summary>
    public class InventoryManager : NetworkBehaviour
    {
        [Header("Config")]
        public int maxSlots = 30;
        public float maxCarryWeight = 50f;       // kg

        [Header("Quick Slots")]
        public int weaponSlots = 4;              // D-pad weapon switching
        public int consumableSlots = 4;

        // Runtime
        private List<InventorySlot> _items = new List<InventorySlot>();
        private float _currentWeight;

        public event Action OnInventoryChanged;
        public event Action<InventorySlot> OnItemAdded;
        public event Action<InventorySlot> OnItemRemoved;

        public IReadOnlyList<InventorySlot> Items => _items;
        public float CurrentWeight => _currentWeight;
        public float RemainingCapacity => maxCarryWeight - _currentWeight;
        public bool IsOverweight => _currentWeight > maxCarryWeight;

        /// <summary>
        /// Try to add an item. Returns actual quantity added.
        /// </summary>
        public int AddItem(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return 0;

            float weightToAdd = item.weight * quantity;
            if (_currentWeight + weightToAdd > maxCarryWeight * 1.5f) // Allow 50% over (but with speed penalty)
                return 0;

            int added = 0;

            // Try stacking first
            if (item.isStackable)
            {
                for (int i = 0; i < _items.Count && added < quantity; i++)
                {
                    if (_items[i].item == item && _items[i].quantity < item.maxStackSize)
                    {
                        int canAdd = Mathf.Min(quantity - added, item.maxStackSize - _items[i].quantity);
                        _items[i] = new InventorySlot(_items[i].item, _items[i].quantity + canAdd);
                        added += canAdd;
                    }
                }
            }

            // Add new slots for remainder
            while (added < quantity && _items.Count < maxSlots)
            {
                int toAdd = item.isStackable
                    ? Mathf.Min(quantity - added, item.maxStackSize)
                    : 1;

                var slot = new InventorySlot(item, toAdd);
                _items.Add(slot);
                added += toAdd;
                OnItemAdded?.Invoke(slot);
            }

            _currentWeight += item.weight * added;
            OnInventoryChanged?.Invoke();
            return added;
        }

        /// <summary>
        /// Remove items. Returns actual quantity removed.
        /// </summary>
        public int RemoveItem(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return 0;

            int removed = 0;

            for (int i = _items.Count - 1; i >= 0 && removed < quantity; i--)
            {
                if (_items[i].item != item) continue;

                int canRemove = Mathf.Min(quantity - removed, _items[i].quantity);
                int remaining = _items[i].quantity - canRemove;

                if (remaining <= 0)
                {
                    OnItemRemoved?.Invoke(_items[i]);
                    _items.RemoveAt(i);
                }
                else
                {
                    _items[i] = new InventorySlot(_items[i].item, remaining);
                }

                removed += canRemove;
            }

            _currentWeight -= item.weight * removed;
            _currentWeight = Mathf.Max(0, _currentWeight);
            OnInventoryChanged?.Invoke();
            return removed;
        }

        /// <summary>
        /// Check if player has enough of an item.
        /// </summary>
        public bool HasItem(ItemDefinition item, int quantity = 1)
        {
            int count = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].item == item)
                    count += _items[i].quantity;
            }
            return count >= quantity;
        }

        /// <summary>
        /// Get total count of a specific item.
        /// </summary>
        public int GetItemCount(ItemDefinition item)
        {
            int count = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].item == item)
                    count += _items[i].quantity;
            }
            return count;
        }

        /// <summary>
        /// Get all illegal items — used by police search system.
        /// </summary>
        public List<InventorySlot> GetIllegalItems()
        {
            var illegal = new List<InventorySlot>();
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].item != null && _items[i].item.isIllegal)
                    illegal.Add(_items[i]);
            }
            return illegal;
        }
    }

    [System.Serializable]
    public struct InventorySlot
    {
        public ItemDefinition item;
        public int quantity;

        public InventorySlot(ItemDefinition item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public bool IsEmpty => item == null || quantity <= 0;
        public float TotalWeight => item != null ? item.weight * quantity : 0f;
    }
}
