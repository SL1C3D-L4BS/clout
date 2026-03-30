using UnityEngine;
using Clout.Core;

namespace Clout.Inventory
{
    /// <summary>
    /// Base ScriptableObject for all items in Clout.
    /// Everything the player can carry, use, equip, sell, or store.
    ///
    /// Architecture: polymorphic SO hierarchy.
    /// ItemDefinition → WeaponDefinition, ConsumableDefinition, ProductItem, IngredientItem, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Items/Base Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string itemName;
        public string itemId;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public ItemCategory category;

        [Header("Physical")]
        public float weight = 0.1f;             // kg — affects carrying capacity
        public bool isStackable = true;
        public int maxStackSize = 99;
        public bool isIllegal = false;           // Detectable by police

        [Header("Economics")]
        public float baseValue = 10f;            // Base sell price
        public float baseBuyPrice = 15f;         // Base purchase price

        [Header("World")]
        public GameObject worldModelPrefab;       // Dropped in world
        public GameObject equippedModelPrefab;    // Held/worn by character
    }

    public enum ItemCategory
    {
        Weapon,
        Ammo,
        Consumable,
        Ingredient,
        Product,
        Equipment,
        Key,
        Clothing,
        Vehicle,
        Misc
    }
}
