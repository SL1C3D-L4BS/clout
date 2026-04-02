using UnityEngine;
using System;

namespace Clout.Empire.Economy
{
    /// <summary>
    /// Step 13 — Market event types that disrupt supply/demand equilibrium.
    ///
    /// Events are stochastic shocks to the economy: droughts cut supply,
    /// festivals spike demand, police crackdowns compress both.
    /// Each event has a duration, product scope, and modifier profile.
    ///
    /// Architecture: ScriptableObject definitions + runtime ActiveMarketEvent wrappers.
    /// MarketSimulator rolls for events daily and stacks active modifiers into price.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Economy/Market Event")]
    public class MarketEvent : ScriptableObject
    {
        [Header("Identity")]
        public string eventName;
        [TextArea(2, 4)]
        public string description;
        public MarketEventType eventType;

        [Header("Scope")]
        [Tooltip("Which product types are affected. Empty = all products.")]
        public Core.ProductType[] affectedProducts;
        [Tooltip("Which district IDs are affected. Empty = all districts.")]
        public string[] affectedDistricts;

        [Header("Duration")]
        [Tooltip("Duration in game days.")]
        public int durationDays = 7;

        [Header("Modifiers")]
        [Tooltip("Multiplier on current price (1.0 = no change).")]
        public float priceMultiplier = 1f;
        [Tooltip("Multiplier on demand curve (1.0 = no change).")]
        public float demandMultiplier = 1f;
        [Tooltip("Multiplier on supply availability (1.0 = no change).")]
        public float supplyMultiplier = 1f;
        [Tooltip("Additional heat generated per deal during this event.")]
        public float bonusHeatPerDeal = 0f;

        [Header("Trigger")]
        [Tooltip("Daily probability of this event firing (0-1). 0 = manually triggered only.")]
        [Range(0f, 0.1f)]
        public float dailyProbability = 0.02f;
        [Tooltip("Minimum game day before this event can fire.")]
        public int minimumDay = 0;
        [Tooltip("Cooldown in days after this event ends before it can fire again.")]
        public int cooldownDays = 14;

        [Header("Severity Scaling")]
        [Tooltip("If true, modifiers intensify toward the midpoint then decay.")]
        public bool bellCurveIntensity = false;

        /// <summary>
        /// Get the effective modifier strength at a given progress through the event (0-1).
        /// Bell-curve events peak at 0.5 progress; flat events are constant.
        /// </summary>
        public float GetIntensity(float progress01)
        {
            if (!bellCurveIntensity) return 1f;
            // Sine-based bell curve: peaks at 0.5, zero at 0 and 1
            return Mathf.Sin(progress01 * Mathf.PI);
        }

        /// <summary>
        /// Check if this event affects a given product type.
        /// </summary>
        public bool AffectsProduct(Core.ProductType product)
        {
            if (affectedProducts == null || affectedProducts.Length == 0) return true;
            foreach (var p in affectedProducts)
                if (p == product) return true;
            return false;
        }

        /// <summary>
        /// Check if this event affects a given district.
        /// </summary>
        public bool AffectsDistrict(string districtId)
        {
            if (affectedDistricts == null || affectedDistricts.Length == 0) return true;
            foreach (var d in affectedDistricts)
                if (d == districtId) return true;
            return false;
        }
    }

    // ─── Event Type Enum ────────────────────────────────────────────

    public enum MarketEventType
    {
        Drought,            // Supply crunch — precursor shortage
        Festival,           // Demand spike — party season
        PortStrike,         // Import disruption — long supply drought
        PoliceCrackdown,    // Both compressed — heat + reduced activity
        RivalBust,          // Competitor removed — supply gap + opportunity
        MediaExpose,        // Negative press — demand drops, scrutiny rises
        CelebrityDeath,     // Spike then crash — volatile whipsaw
        SupplyRouteCut      // Commodity-level disruption — affects production costs
    }

    // ─── Runtime Active Event Wrapper ───────────────────────────────

    /// <summary>
    /// Runtime state for an active market event. Created by MarketSimulator
    /// when an event triggers, ticked daily, removed on expiry.
    /// </summary>
    [Serializable]
    public class ActiveMarketEvent
    {
        public MarketEvent definition;
        public int startDay;
        public int remainingDays;
        public int lastCooldownDay;       // Day this event last ended (for cooldown)

        /// <summary>Progress through the event, 0 = just started, 1 = about to end.</summary>
        public float Progress => definition.durationDays > 0
            ? 1f - (float)remainingDays / definition.durationDays
            : 1f;

        /// <summary>Current intensity-scaled price multiplier.</summary>
        public float EffectivePriceMultiplier
        {
            get
            {
                float intensity = definition.GetIntensity(Progress);
                // Lerp between 1.0 (no effect) and the target multiplier, scaled by intensity
                return Mathf.Lerp(1f, definition.priceMultiplier, intensity);
            }
        }

        /// <summary>Current intensity-scaled demand multiplier.</summary>
        public float EffectiveDemandMultiplier
        {
            get
            {
                float intensity = definition.GetIntensity(Progress);
                return Mathf.Lerp(1f, definition.demandMultiplier, intensity);
            }
        }

        /// <summary>Current intensity-scaled supply multiplier.</summary>
        public float EffectiveSupplyMultiplier
        {
            get
            {
                float intensity = definition.GetIntensity(Progress);
                return Mathf.Lerp(1f, definition.supplyMultiplier, intensity);
            }
        }

        public bool IsExpired => remainingDays <= 0;

        public void TickDay()
        {
            remainingDays--;
        }
    }
}
