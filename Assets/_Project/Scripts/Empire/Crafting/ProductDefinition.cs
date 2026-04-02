using UnityEngine;
using Clout.Core;

namespace Clout.Empire.Crafting
{
    /// <summary>
    /// A finished product ready for distribution and sale.
    /// Created by combining ingredients via RecipeDefinition.
    ///
    /// Products have dynamic market value based on:
    /// - Quality (crafting skill + additives)
    /// - Supply/demand (territory economics)
    /// - Effects (consumer preference)
    /// - Heat (police attention reduces buyers)
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Empire/Product")]
    public class ProductDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string productName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public ProductType productType;

        [Header("Base Economics")]
        public float baseStreetValue = 50f;
        public float baseBulkValue = 30f;
        public float productionCost = 15f;

        [Header("Consumer Properties")]
        [Range(0f, 1f)]
        public float addictionRate = 0.3f;      // How quickly consumers come back
        [Range(0f, 1f)]
        public float satisfactionRate = 0.5f;   // Consumer happiness
        public float effectDuration = 60f;       // Seconds of effect

        [Header("Risk Profile")]
        [Range(0f, 1f)]
        public float detectability = 0.3f;       // Chance of detection during transport
        public float weight = 0.1f;              // kg per unit (affects carrying)
        public float odor = 0f;                  // Affects police dog detection

        [Header("Quality Tiers")]
        public QualityTier[] qualityTiers;

        [Header("Visual")]
        public Color productColor = Color.white;
        public GameObject worldModelPrefab;
        public GameObject baggedPrefab;          // How it looks packaged
    }

    [System.Serializable]
    public struct QualityTier
    {
        public string tierName;                  // "Street", "Mid", "Fire", "Pure"
        [Range(0f, 1f)]
        public float minQuality;
        public float priceMultiplier;
        public Color tierColor;
    }
}
