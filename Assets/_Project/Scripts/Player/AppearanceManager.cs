using System.Collections.Generic;
using UnityEngine;

namespace Clout.Player
{
    /// <summary>
    /// Character appearance system — ported from Sharp Accent's ClothManager + ClothItem.
    /// Upgraded for Synty POLYGON Modular City Characters.
    ///
    /// Synty modular characters work by enabling/disabling mesh renderers
    /// on body part slots (head, hair, torso, legs, feet, accessories).
    /// This manager controls which mesh is active per slot.
    ///
    /// Used for: player customization, NPC procedural generation, disguises (heat reduction),
    /// gang uniform identification, undercover police visual distinction.
    /// </summary>
    public class AppearanceManager : MonoBehaviour
    {
        [Header("Appearance Slots")]
        public AppearanceSlot[] slots;

        private Dictionary<AppearanceSlotType, AppearanceSlot> _slotDict
            = new Dictionary<AppearanceSlotType, AppearanceSlot>();

        public void Init()
        {
            _slotDict.Clear();
            if (slots == null) return;

            foreach (var slot in slots)
            {
                if (!_slotDict.ContainsKey(slot.slotType))
                    _slotDict[slot.slotType] = slot;
                slot.Init();
            }
        }

        /// <summary>Equip an appearance item to its corresponding slot.</summary>
        public void Equip(AppearanceItem item)
        {
            if (item == null) return;
            if (_slotDict.TryGetValue(item.slotType, out var slot))
                slot.SetItem(item);
        }

        /// <summary>Clear a slot back to default.</summary>
        public void Unequip(AppearanceSlotType slotType)
        {
            if (_slotDict.TryGetValue(slotType, out var slot))
                slot.ClearItem();
        }

        /// <summary>Load a full outfit (for NPC generation or save/load).</summary>
        public void LoadOutfit(AppearanceItem[] items)
        {
            // Clear all first
            foreach (var slot in _slotDict.Values)
                slot.ClearItem();

            // Equip each piece
            if (items != null)
            {
                foreach (var item in items)
                    Equip(item);
            }
        }

        /// <summary>Set material color tint (Synty palette recoloring).</summary>
        public void SetColorVariant(int paletteIndex)
        {
            // Synty assets use a texture atlas with 4 color rows
            // Shift UV.y by paletteIndex * 0.25 to select color variant
            foreach (var slot in _slotDict.Values)
                slot.SetPaletteIndex(paletteIndex);
        }
    }

    public enum AppearanceSlotType
    {
        Head,
        Hair,
        FacialHair,
        Torso,
        Arms,
        Hands,
        Legs,
        Feet,
        Hat,
        Glasses,
        Accessory1,
        Accessory2
    }

    [System.Serializable]
    public class AppearanceSlot
    {
        public AppearanceSlotType slotType;

        [Tooltip("All possible mesh renderers for this slot (disabled by default)")]
        public SkinnedMeshRenderer[] meshOptions;

        [Tooltip("Default mesh index to show when no item equipped")]
        public int defaultMeshIndex = -1; // -1 = none

        private int _activeMeshIndex = -1;

        public void Init()
        {
            // Disable all meshes
            if (meshOptions != null)
                foreach (var mesh in meshOptions)
                    if (mesh != null) mesh.enabled = false;

            // Enable default
            if (defaultMeshIndex >= 0 && defaultMeshIndex < meshOptions.Length)
            {
                meshOptions[defaultMeshIndex].enabled = true;
                _activeMeshIndex = defaultMeshIndex;
            }
        }

        public void SetItem(AppearanceItem item)
        {
            // Disable current
            if (_activeMeshIndex >= 0 && _activeMeshIndex < meshOptions.Length)
                meshOptions[_activeMeshIndex].enabled = false;

            // Enable target
            if (item.meshIndex >= 0 && item.meshIndex < meshOptions.Length)
            {
                meshOptions[item.meshIndex].enabled = true;
                _activeMeshIndex = item.meshIndex;

                // Apply material override if specified
                if (item.materialOverride != null)
                    meshOptions[item.meshIndex].material = item.materialOverride;
            }
        }

        public void ClearItem()
        {
            if (_activeMeshIndex >= 0 && _activeMeshIndex < meshOptions.Length)
                meshOptions[_activeMeshIndex].enabled = false;

            _activeMeshIndex = defaultMeshIndex;
            if (_activeMeshIndex >= 0 && _activeMeshIndex < meshOptions.Length)
                meshOptions[_activeMeshIndex].enabled = true;
        }

        public void SetPaletteIndex(int index)
        {
            if (_activeMeshIndex < 0 || _activeMeshIndex >= meshOptions.Length) return;
            var renderer = meshOptions[_activeMeshIndex];
            if (renderer != null && renderer.material != null)
            {
                // Synty palette offset — shift UV row
                renderer.material.SetFloat("_PaletteIndex", index);
            }
        }
    }

    /// <summary>ScriptableObject defining an appearance piece.</summary>
    [CreateAssetMenu(menuName = "CLOUT/Appearance/Appearance Item")]
    public class AppearanceItem : ScriptableObject
    {
        public string itemName;
        public AppearanceSlotType slotType;
        public int meshIndex; // Index into the slot's meshOptions array
        public Material materialOverride; // Optional color/material swap
        public Sprite icon;

        [Header("Gameplay")]
        public bool isDisguise; // Reduces heat when worn
        public float disguiseEffectiveness; // 0-1
    }
}
