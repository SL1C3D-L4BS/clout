using UnityEngine;
using Clout.Core;

namespace Clout.Empire.Employees
{
    /// <summary>
    /// Hireable NPC employee template — ScriptableObject defining a worker's base profile.
    ///
    /// Spec v2.0 Section 13: Every NPC is a node in a directed weighted graph with edges
    /// representing trust, debt, information flow, and loyalty. This SO defines the base
    /// personality that feeds into utility calculations and the betrayal formula:
    ///
    ///   P(betray) = (Greed + Fear - Loyalty + ExternalOfferValue) / CompartmentalizationFactor
    ///
    /// where CompartmentalizationFactor = how much the NPC actually knows about your operations.
    /// An NPC who only knows their direct handler is less dangerous than one who knows the org chart.
    ///
    /// Stats use logarithmic growth at runtime — these are BASE values that WorkerInstance
    /// will track as mutable state.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Empire/Employee Template")]
    public class EmployeeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string employeeName;
        [TextArea(2, 3)]
        public string backstory;
        public Sprite portrait;

        [Header("Role")]
        public EmployeeRole role;

        // ─── Core Stats (Spec v2.0 Section 13) ──────────────────────
        // These are the stats that feed into AI decision-making, betrayal probability,
        // and work output calculations. All 0–1 normalized.

        [Header("Core Stats")]
        [Tooltip("Work output quality. Dealers get better prices, cooks produce higher quality.")]
        [Range(0f, 1f)] public float skill = 0.5f;

        [Tooltip("Commitment to the player. High loyalty = low betrayal. Grows through fair treatment.")]
        [Range(0f, 1f)] public float loyalty = 0.5f;

        [Tooltip("Won't talk to police or leave evidence. Affects arrest probability.")]
        [Range(0f, 1f)] public float discretion = 0.5f;

        [Tooltip("Desire for more money/power. High greed = higher betrayal probability, demands raises.")]
        [Range(0f, 1f)] public float greed = 0.3f;

        [Tooltip("Willingness to face danger. Low courage = flee during raids. High = fight.")]
        [Range(0f, 1f)] public float courage = 0.5f;

        [Tooltip("Problem-solving ability. Cooks get quality bonus. Dealers find better customers.")]
        [Range(0f, 1f)] public float intelligence = 0.5f;

        [Tooltip("Susceptibility to police/rival offers. High = easier to flip as informant.")]
        [Range(0f, 1f)] public float corruptibility = 0.3f;

        [Tooltip("Drive to advance. High ambition workers may try to steal territory or start rival operations.")]
        [Range(0f, 1f)] public float ambition = 0.3f;

        // ─── Economics ───────────────────────────────────────────────

        [Header("Economics")]
        [Tooltip("Daily wage in dirty cash. Workers demand raises over time based on greed + ambition.")]
        public float dailyWage = 100f;

        [Tooltip("One-time cost to hire this worker.")]
        public float hiringCost = 500f;

        // ─── Risk Profile ────────────────────────────────────────────

        [Header("Risk Profile")]
        [Tooltip("Base daily betrayal probability before reputation modifiers.")]
        [Range(0f, 1f)] public float betrayalChance = 0.01f;

        [Tooltip("Base daily arrest probability before discretion/heat modifiers.")]
        [Range(0f, 1f)] public float arrestChance = 0.02f;

        [Tooltip("Known to police — increases detection radius and investigation priority.")]
        public bool hasRecord;

        // ─── Compartmentalization ────────────────────────────────────

        [Header("Compartmentalization (Spec v2.0 Section 13)")]
        [Tooltip("What this NPC knows. Fewer items = safer if they flip. Set at hire time by WorkerManager.")]
        [TextArea(2, 4)]
        public string initialKnowledge = "Knows: direct handler only";

        // ─── Computed Properties ─────────────────────────────────────

        /// <summary>
        /// Effective betrayal probability per day, accounting for greed and ambition.
        /// This is the BASE probability before player reputation modifiers are applied.
        /// Full formula at runtime: P = (greed + fear_external - loyalty + offerValue) / compartmentalization
        /// Simplified static check: base × (1 + greed) × (1 + ambition × 0.5)
        /// </summary>
        public float EffectiveBetrayalChance
            => betrayalChance * (1f + greed) * (1f + ambition * 0.5f);

        /// <summary>
        /// Effective arrest probability per day, accounting for discretion and record.
        /// Full formula at runtime also factors in property heat and player wanted level.
        /// </summary>
        public float EffectiveArrestChance
            => arrestChance * (1f - discretion * 0.7f) * (hasRecord ? 1.5f : 1f);

        /// <summary>
        /// Quality modifier for production output. Cooks use this directly.
        /// Dealers use it as price negotiation bonus.
        /// </summary>
        public float QualityModifier
            => 0.5f + (skill * 0.3f) + (intelligence * 0.2f);

        /// <summary>
        /// How quickly this worker improves through work. Intelligence accelerates growth.
        /// </summary>
        public float LearningRate
            => 0.01f + (intelligence * 0.02f);

        /// <summary>
        /// Raid response: fight or flee? Based on courage threshold.
        /// Returns true if the worker will stand and fight during a raid.
        /// </summary>
        public bool WillFightDuringRaid(float threatLevel)
            => courage > threatLevel * 0.8f;

        /// <summary>
        /// Wage demand multiplier. Greedy/ambitious workers demand more over time.
        /// Applied by WorkerManager during raise calculations.
        /// </summary>
        public float WageDemandMultiplier
            => 1f + (greed * 0.3f) + (ambition * 0.2f);
    }
}
