using UnityEngine;
using Clout.Core;

namespace Clout.Empire.Crafting
{
    /// <summary>
    /// Base ingredient item — raw materials for crafting.
    /// Can be purchased, grown, stolen, or found.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Empire/Ingredient")]
    public class IngredientDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string ingredientName;
        [TextArea(2, 3)]
        public string description;
        public Sprite icon;

        [Header("Category")]
        public IngredientCategory category;

        [Header("Properties")]
        public float potency = 1f;              // Affects output quality
        public float toxicity = 0f;             // Risk factor
        public float shelf_life = 168f;         // Hours before degradation

        [Header("Acquisition")]
        public float basePurchasePrice;         // From suppliers
        public float baseStreetPrice;           // Black market value
        public bool isLegal = false;            // Can be openly carried
        public bool requiresGrowth = false;     // Needs cultivation time

        [Header("Visual")]
        public Color tintColor = Color.white;
        public GameObject worldModelPrefab;

        [Header("Effects When Added")]
        public AdditiveEffect[] effects;        // What this does when mixed in
    }

    public enum IngredientCategory
    {
        BaseChemical,       // Core component
        Additive,           // Modifies properties
        Catalyst,           // Speeds reaction
        Cutting,            // Stretches quantity (reduces quality)
        Coloring,           // Visual only
        Flavoring,          // Affects consumer preference
        Agricultural        // Seeds, soil, nutrients
    }

    [System.Serializable]
    public struct AdditiveEffect
    {
        public string effectName;
        [TextArea(1, 2)]
        public string effectDescription;
        public float qualityModifier;       // +/- quality
        public float valueModifier;         // +/- street value
        public float addictionModifier;     // +/- how addictive
        public float toxicityModifier;      // +/- health risk
    }
}
