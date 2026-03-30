using UnityEngine;
using Clout.Core;

namespace Clout.Empire.Crafting
{
    /// <summary>
    /// ScriptableObject defining a crafting recipe — the backbone of the empire economy.
    ///
    /// Recipes define how base products + additives combine to create sellable goods.
    /// Each recipe has quality tiers, skill requirements, and market value modifiers.
    ///
    /// Architecture: SO-driven for easy balancing and modding support.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Empire/Recipe")]
    public class RecipeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string recipeName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public ProductType productType;

        [Header("Ingredients")]
        public IngredientSlot[] ingredients;
        public IngredientSlot[] optionalAdditives;  // Modify effects/quality/value

        [Header("Output")]
        public ProductDefinition outputProduct;
        public int outputQuantity = 1;
        public float baseQuality = 0.5f;            // 0-1, modified by skill + additives

        [Header("Requirements")]
        public float craftingTime = 10f;             // Seconds to cook
        public int requiredSkillLevel = 0;           // Player crafting skill
        public PropertyType requiredFacility = PropertyType.Lab;
        public EquipmentRequirement[] requiredEquipment;

        [Header("Market Effects")]
        public float marketValueMultiplier = 1f;     // How additives affect street value
        public string[] addedEffects;                // Visual/gameplay effects on consumers

        [Header("Risk")]
        [Range(0f, 1f)]
        public float explosionRisk = 0f;             // Chance of lab accident
        [Range(0f, 1f)]
        public float fumeDetectionRisk = 0f;         // Chance neighbors call police
    }

    [System.Serializable]
    public struct IngredientSlot
    {
        public IngredientDefinition ingredient;
        public int quantity;
    }

    [System.Serializable]
    public struct EquipmentRequirement
    {
        public string equipmentId;
        public string equipmentName;
    }
}
