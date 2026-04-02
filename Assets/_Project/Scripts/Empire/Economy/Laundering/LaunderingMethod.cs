using UnityEngine;
using Clout.Core;

namespace Clout.Empire.Economy.Laundering
{
    // ─────────────────────────────────────────────────────────────
    //  ENUMS
    // ─────────────────────────────────────────────────────────────

    public enum LaunderingMethodType
    {
        Structuring,        // Sub-threshold deposits — safe, slow
        Smurfing,           // Multiple runners — fast, risky
        RealEstate,         // Property flip cycle — very slow, safest
        CashIntensive,      // Mix with front business revenue — cheapest
        Crypto              // Digital mixing — late-game, high capacity
    }

    public enum FrontBusinessType
    {
        Restaurant,         // Moderate capacity, high cash volume
        AutoShop,           // Large transactions plausible
        Nightclub,          // Highest capacity, slowest suspicion decay
        Laundromat,         // Low revenue but best laundering ratio
        CarWash             // Classic front, balanced profile
    }

    public enum LaunderingStage
    {
        Placement,          // Dirty cash enters front business register
        Layering,           // Funds split across methods to obscure origin
        Integration,        // Clean cash re-enters player economy
        Cooling,            // Mandatory delay before spendable
        Complete            // Funds available
    }

    public enum IRSInvestigationStage
    {
        None,               // No active investigation
        Flag,               // Suspicious activity flagged — warning
        Investigation,      // IRS agent assigned — monitoring
        Audit,              // Formal audit — books examined
        Seizure             // Audit failed — penalties applied
    }

    // ─────────────────────────────────────────────────────────────
    //  DATA STRUCTURES
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A single batch of dirty cash moving through the laundering pipeline.
    /// Tracks amount, method, current stage, and timing.
    /// </summary>
    [System.Serializable]
    public struct LaunderingBatch
    {
        public string batchId;
        public float dirtyAmount;
        public float estimatedCleanAmount;
        public float feeAmount;
        public LaunderingMethodType method;
        public string frontBusinessId;
        public LaunderingStage stage;
        public float stageStartTime;
        public float stageEndTime;
        public int currentStageIndex;       // 0-4 maps to pipeline stages

        public float Progress
        {
            get
            {
                if (stage == LaunderingStage.Complete) return 1f;
                if (stageEndTime <= stageStartTime) return 0f;
                return Mathf.Clamp01((Time.time - stageStartTime) / (stageEndTime - stageStartTime));
            }
        }

        public float TotalProgress => (currentStageIndex + Progress) / 4f;
    }

    /// <summary>
    /// Record of a completed laundering operation for audit trail and forensics.
    /// </summary>
    [System.Serializable]
    public struct LaunderingRecord
    {
        public string batchId;
        public float dirtyAmount;
        public float cleanAmount;
        public float feeAmount;
        public LaunderingMethodType method;
        public string frontBusinessId;
        public float completedTime;
        public int completedOnDay;
    }

    /// <summary>
    /// IRS audit trigger — records what caused an attention spike.
    /// </summary>
    [System.Serializable]
    public struct IRSTriggerRecord
    {
        public string triggerType;          // "large_deposit", "velocity_anomaly", "pattern", etc.
        public float attentionIncrease;
        public float timestamp;
        public string details;
    }

    // ─────────────────────────────────────────────────────────────
    //  LAUNDERING METHOD SCRIPTABLE OBJECT
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Step 11C — Defines a laundering method's risk/reward/capacity profile.
    /// Create one asset per method: SO_Method_Structuring, SO_Method_Smurfing, etc.
    ///
    /// Methods gate by CLOUT rank and required business type. Each has distinct
    /// tradeoffs between speed, risk, capacity, and cost — forcing the player
    /// to diversify across methods to optimize throughput while managing IRS exposure.
    /// </summary>
    [CreateAssetMenu(fileName = "LaunderingMethod", menuName = "Clout/Empire/Laundering Method")]
    public class LaunderingMethod : ScriptableObject
    {
        [Header("Identity")]
        public string methodName;
        [TextArea(2, 4)]
        public string description;
        public LaunderingMethodType methodType;

        [Header("Performance")]
        [Tooltip("Base IRS attention per transaction (0-1). Lower = safer.")]
        [Range(0f, 1f)]
        public float riskProfile = 0.15f;

        [Tooltip("Days to complete full pipeline (Placement through Cooling).")]
        [Min(0.5f)]
        public float processingDays = 3f;

        [Tooltip("Cooling period in days after Integration before funds are spendable.")]
        [Min(0f)]
        public float coolingPeriodDays = 1f;

        [Tooltip("Max daily throughput per business using this method.")]
        public float dailyCapacity = 8000f;

        [Tooltip("Fee as percentage of laundered amount (0-1). Lower = cheaper.")]
        [Range(0f, 0.5f)]
        public float feePercentage = 0.05f;

        [Header("Requirements")]
        [Tooltip("Minimum CLOUT rank to unlock this method (0 = always available).")]
        public int requiredCloutRank = 0;

        [Tooltip("Required front business type. Set to Restaurant for 'Any' — checked at runtime.")]
        public FrontBusinessType preferredBusinessType;

        [Tooltip("If true, only works with the preferred business type. If false, any business works.")]
        public bool requiresSpecificBusiness = false;

        [Header("Detection Modifiers")]
        [Tooltip("Multiplier to suspicion growth rate when using this method.")]
        public float suspicionMultiplier = 1f;

        [Tooltip("If true, round-number detection applies (amounts ending in 000).")]
        public bool vulnerableToRoundNumberDetection = true;

        [Tooltip("If true, pattern detection applies (same amount repeated 3+ times).")]
        public bool vulnerableToPatternDetection = true;

        // ─── Computed Helpers ──────────────────────────────────

        /// <summary>Total pipeline duration including cooling.</summary>
        public float TotalDays => processingDays + coolingPeriodDays;

        /// <summary>Duration per pipeline stage (Placement/Layering/Integration split evenly).</summary>
        public float DaysPerStage => processingDays / 3f;

        /// <summary>Calculate the clean cash received after fees for a given dirty amount.</summary>
        public float CalculateCleanAmount(float dirtyAmount)
        {
            float fee = dirtyAmount * feePercentage;
            return dirtyAmount - fee;
        }

        /// <summary>Calculate the fee for a given dirty amount.</summary>
        public float CalculateFee(float dirtyAmount)
        {
            return dirtyAmount * feePercentage;
        }

        /// <summary>
        /// Get the IRS attention increase for a specific transaction amount.
        /// Larger transactions relative to capacity generate more attention.
        /// </summary>
        public float CalculateAttentionForAmount(float amount)
        {
            float capacityRatio = dailyCapacity > 0 ? amount / dailyCapacity : 1f;
            return riskProfile * Mathf.Lerp(0.5f, 1.5f, capacityRatio);
        }
    }
}
