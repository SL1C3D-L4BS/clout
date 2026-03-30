using UnityEngine;
using Clout.Core;

namespace Clout.Empire.Properties
{
    /// <summary>
    /// ScriptableObject defining a purchasable property.
    /// Properties are the physical infrastructure of your empire.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Empire/Property")]
    public class PropertyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string propertyName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public Sprite exteriorPhoto;

        [Header("Type")]
        public PropertyType propertyType;

        [Header("Economics")]
        public float purchasePrice;
        public float dailyUpkeep;
        public float dailyRevenue;               // Passive income (laundering fronts)
        public float maxStorage;                  // Units of product storable
        public int maxEmployeeSlots = 3;

        [Header("Upgrades")]
        public PropertyUpgrade[] availableUpgrades;

        [Header("Risk")]
        [Range(0f, 1f)]
        public float policeVisibility = 0.2f;    // How suspicious this looks
        [Range(0f, 1f)]
        public float raidChance = 0.05f;          // Daily chance of raid (modified by heat)
        public bool requiresCoverBusiness = false;

        [Header("Location")]
        public string districtName;
        public Vector3 worldPosition;             // Set at runtime by property manager
    }

    [System.Serializable]
    public struct PropertyUpgrade
    {
        public string upgradeName;
        public string description;
        public float cost;
        public float constructionTime;            // Hours
        public PropertyUpgradeEffect[] effects;
    }

    [System.Serializable]
    public struct PropertyUpgradeEffect
    {
        public PropertyUpgradeType type;
        public float value;
    }

    public enum PropertyUpgradeType
    {
        StorageCapacity,
        EmployeeSlots,
        CraftingSpeed,
        CraftingQuality,
        SecurityLevel,
        PoliceVisibility,   // Negative = less visible
        DailyRevenue,
        UpkeepReduction
    }
}
