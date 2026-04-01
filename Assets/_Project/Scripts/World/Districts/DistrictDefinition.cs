using UnityEngine;
using Clout.Core;

namespace Clout.World.Districts
{
    /// <summary>
    /// ScriptableObject defining a city district.
    ///
    /// Spec v2.0 Step 8: Each district has unique economic profile, police presence,
    /// zone composition, and preferred product mix. Modeled after Bay Area neighborhoods.
    ///
    /// Districts drive:
    ///   - NPC spawn rates + types (customers, police, civilians)
    ///   - Per-product demand curves (EconomyManager.InitializeMarket per zone)
    ///   - Property availability (which PropertyTypes can appear)
    ///   - Territory control thresholds
    ///   - Ambient atmosphere (building density, wealth indicator, zone mix)
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/World/District")]
    public class DistrictDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string districtId;
        public string districtName;
        [TextArea(2, 4)]
        public string description;

        [Header("Geography")]
        [Tooltip("Center position in world space.")]
        public Vector3 worldCenter;
        [Tooltip("Half-extents of the district bounds (width/2, height/2, depth/2).")]
        public Vector3 halfExtents = new Vector3(120f, 50f, 120f);
        [Tooltip("Rotation of the district layout in degrees around Y.")]
        public float layoutRotation = 0f;

        [Header("Zone Composition (should sum to ~1.0)")]
        [Range(0f, 1f)] public float residentialRatio = 0.4f;
        [Range(0f, 1f)] public float commercialRatio = 0.3f;
        [Range(0f, 1f)] public float industrialRatio = 0.2f;
        [Range(0f, 1f)] public float waterfrontRatio = 0.1f;

        [Header("Wealth & Atmosphere")]
        [Tooltip("0 = impoverished, 1 = affluent. Affects building quality, NPC appearance, prices.")]
        [Range(0f, 1f)] public float wealthLevel = 0.5f;
        [Tooltip("0 = suburban sprawl, 1 = dense urban core.")]
        [Range(0f, 1f)] public float density = 0.5f;
        [Tooltip("Visual grime factor. 0 = pristine, 1 = run-down.")]
        [Range(0f, 1f)] public float grimeLevel = 0.3f;

        [Header("Population")]
        [Tooltip("Maximum customers active simultaneously in this district.")]
        public int maxCustomers = 12;
        [Tooltip("Maximum ambient civilians (non-buying NPCs).")]
        public int maxCivilians = 20;
        [Tooltip("Seconds between NPC spawn checks.")]
        public float npcSpawnInterval = 15f;

        [Header("Law Enforcement")]
        [Tooltip("Base police patrol count at Clean heat level.")]
        public int basePolicePatrols = 2;
        [Tooltip("0 = lawless, 1 = heavy police presence. Multiplier on HeatResponseManager brackets.")]
        [Range(0f, 1.5f)] public float policePresenceMultiplier = 1f;
        [Tooltip("How quickly police respond to crimes. Lower = faster.")]
        public float policeResponseTime = 30f;

        [Header("Economy")]
        [Tooltip("Products in demand in this district, with base demand weight.")]
        public ProductDemand[] productDemands;
        [Tooltip("Global price multiplier for this district. Wealthy districts pay more.")]
        public float priceMultiplier = 1f;

        [Header("Properties")]
        [Tooltip("Property types that can appear in this district.")]
        public PropertyType[] availablePropertyTypes;
        [Tooltip("Maximum number of player-ownable properties.")]
        public int maxPropertySlots = 6;

        [Header("Rivals")]
        [Tooltip("Rival threat level. 0 = none, 1 = heavily contested.")]
        [Range(0f, 1f)] public float rivalThreat = 0.3f;
        [Tooltip("Name of the rival crew operating here.")]
        public string rivalCrewName;

        [Header("Road Network")]
        [Tooltip("Number of major N-S streets.")]
        public int majorStreetsNS = 3;
        [Tooltip("Number of major E-W streets.")]
        public int majorStreetsEW = 3;
        [Tooltip("Street width in meters.")]
        public float streetWidth = 10f;
        [Tooltip("Sidewalk width in meters.")]
        public float sidewalkWidth = 2.5f;

        // ─── Computed ──────────────────────────────────────────────

        public Bounds WorldBounds => new Bounds(worldCenter, halfExtents * 2f);

        public float TotalArea => halfExtents.x * 2f * halfExtents.z * 2f;
    }

    [System.Serializable]
    public struct ProductDemand
    {
        public ProductType product;
        [Tooltip("Base demand units per cycle.")]
        public float baseDemand;
        [Tooltip("Base street price in this district.")]
        public float basePrice;
        [Tooltip("Price elasticity. 0 = inelastic (staple), 1 = very elastic (luxury).")]
        [Range(0f, 1f)] public float elasticity;
    }

    /// <summary>
    /// Zone type enum for building placement logic.
    /// </summary>
    public enum DistrictZoneType
    {
        Residential,
        Commercial,
        Industrial,
        Waterfront,
        Park,
        Mixed
    }
}
