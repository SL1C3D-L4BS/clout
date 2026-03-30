using UnityEngine;
using Clout.Core;
using Clout.Empire.Crafting;

namespace Clout.Empire.Dealing
{
    /// <summary>
    /// Defines a product supplier — the connect.
    /// Players buy wholesale product from suppliers, then sell retail to customers.
    ///
    /// Supplier tiers:
    /// - Street Connect: cheap, unreliable, low quality, small quantity
    /// - Mid-Level: moderate price, decent quality, medium quantity
    /// - Cartel Direct: expensive, premium quality, bulk quantity, requires CLOUT
    ///
    /// Each supplier has a restock timer and limited inventory per cycle.
    /// Higher-tier suppliers require higher CLOUT rank to unlock.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Empire/Supplier")]
    public class SupplierDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string supplierName;
        [TextArea(2, 4)]
        public string description;
        public Sprite portrait;

        [Header("Product Catalog")]
        public SupplierProduct[] catalog;

        [Header("Requirements")]
        [Tooltip("Minimum CLOUT rank to access this supplier")]
        public int requiredCloutRank = 0;      // 0=Nobody can access, 5=Legend-only
        [Tooltip("Minimum reputation with suppliers faction")]
        [Range(0f, 1f)]
        public float requiredSupplierRep = 0f;

        [Header("Reliability")]
        [Range(0f, 1f)]
        public float reliability = 0.8f;        // Chance they actually show up
        public float restockInterval = 600f;    // Seconds between restocks
        [Range(0f, 1f)]
        public float bustRisk = 0.05f;          // Chance of deal being a police sting

        [Header("Location")]
        public string preferredDistrict;         // Which district they hang around
    }

    [System.Serializable]
    public struct SupplierProduct
    {
        public ProductDefinition product;
        public int maxQuantityPerRestock;
        public float wholesalePrice;             // Per unit — player sells at retail
        [Range(0f, 1f)]
        public float qualityFloor;               // Minimum quality this supplier provides
        [Range(0f, 1f)]
        public float qualityCeiling;             // Maximum quality
    }
}
