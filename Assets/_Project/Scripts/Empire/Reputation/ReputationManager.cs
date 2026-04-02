using UnityEngine;
using System;
using Clout.Core;
using Clout.Utils;

namespace Clout.Empire.Reputation
{
    /// <summary>
    /// Player reputation system — 4D reputation vector + composite CLOUT score.
    ///
    /// Spec v2.0 Section 36: Reputation is a 4-dimensional vector that NPCs evaluate
    /// independently. The composite CLOUT score is derived from all four dimensions
    /// and serves as the public-facing rank. Each dimension affects NPC behavior differently:
    ///
    ///   Fear     — Intimidation success, worker obedience, but increases snitch risk
    ///   Respect  — Deal quality, recruitment pool, worker loyalty
    ///   Reliability — Supplier trust, contract fulfillment, repeat customers
    ///   Ruthlessness — Territory defense, rival deterrence, but attracts investigation
    ///
    /// The betrayal formula (Spec Section 13) references Fear directly:
    ///   P(betray) = (Greed + Fear - Loyalty + ExternalOffer) / Compartmentalization
    ///   where Fear is the PLAYER's fear rating — high fear suppresses betrayal
    ///
    /// Phase 2 singleplayer — FishNet SyncVars will be restored in Phase 4.
    /// </summary>
    public class ReputationManager : MonoBehaviour
    {
        // ─── 4D Reputation Vector ────────────────────────────────────

        [Header("4D Reputation Vector (0.0 – 1.0)")]
        [Range(0f, 1f)] public float fear;
        [Range(0f, 1f)] public float respect;
        [Range(0f, 1f)] public float reliability;
        [Range(0f, 1f)] public float ruthlessness;

        // ─── Composite CLOUT Score ───────────────────────────────────

        [Header("CLOUT Rank")]
        public int[] rankThresholds = { 0, 100, 500, 2000, 10000, 50000 };
        public string[] rankNames = { "Nobody", "Corner Boy", "Hustler", "Shot Caller", "Kingpin", "Legend" };

        // ─── Legacy Reputation Tracks (NPC-faction specific) ─────────

        [Header("Faction Reputation")]
        public float streetRep = 0f;
        public float civilianRep = 50f;
        public float rivalRep = 0f;
        public float supplierRep = 0f;

        // ─── Reputation Growth Rates ─────────────────────────────────
        // How fast each dimension grows/decays per event. Logarithmic growth prevents runaway.

        private const float GROWTH_BASE = 0.02f;     // Base increment per event
        private const float DECAY_RATE = 0.001f;      // Natural decay per game-day toward 0.5 equilibrium
        private const float EQUILIBRIUM = 0.3f;        // Reputation drifts toward this over time

        // ─── Backing Fields ──────────────────────────────────────────

        private int _cloutScore;
        private int _cloutRank;

        // ─── Events ─────────────────────────────────────────────────

        public event Action<int, int> OnCloutChanged;           // oldScore, newScore
        public event Action<int, string> OnRankUp;              // newRank, rankName
        public event Action<ReputationVector> OnReputationChanged;  // full 4D vector

        // ─── Public Properties ───────────────────────────────────────

        public int CurrentClout => _cloutScore;
        public int CurrentRank => _cloutRank;
        public string CurrentRankName => rankNames[Mathf.Clamp(_cloutRank, 0, rankNames.Length - 1)];

        /// <summary>
        /// Returns the full 4D reputation vector as a struct for external systems.
        /// </summary>
        public ReputationVector GetReputationVector()
        {
            return new ReputationVector
            {
                fear = fear,
                respect = respect,
                reliability = reliability,
                ruthlessness = ruthlessness
            };
        }

        /// <summary>
        /// Composite score used for NPC utility calculations.
        /// Weighted sum: Respect and Reliability matter most for empire building.
        /// </summary>
        public float GetCompositeInfluence()
        {
            return (fear * 0.2f) + (respect * 0.35f) + (reliability * 0.25f) + (ruthlessness * 0.2f);
        }

        // ─── Reputation Modification ─────────────────────────────────

        /// <summary>
        /// Modify a specific reputation dimension. Uses logarithmic growth
        /// so early gains are fast and mastery takes dedication.
        /// Amount is in "events" — 1.0 = standard event, 2.0 = big event.
        /// </summary>
        public void ModifyReputation(ReputationDimension dimension, float amount, string reason)
        {
            float current = GetDimension(dimension);

            // Logarithmic growth: diminishing returns as you approach 1.0
            float effectiveGrowth;
            if (amount > 0)
                effectiveGrowth = GROWTH_BASE * amount * (1f - current);  // Harder to grow near 1.0
            else
                effectiveGrowth = GROWTH_BASE * amount * current;          // Harder to lose near 0.0

            float newValue = Mathf.Clamp01(current + effectiveGrowth);
            SetDimension(dimension, newValue);

            // Publish event
            EventBus.Publish(new ReputationChangedEvent
            {
                dimension = dimension,
                oldValue = current,
                newValue = newValue,
                reason = reason,
                vector = GetReputationVector()
            });

            OnReputationChanged?.Invoke(GetReputationVector());
        }

        /// <summary>
        /// Bulk modify multiple dimensions from a single action.
        /// Example: completing a violent deal increases Fear + Ruthlessness, decreases Reliability.
        /// </summary>
        public void ModifyReputationBulk(ReputationModifier modifier, string reason)
        {
            if (modifier.fear != 0f) ModifyReputation(ReputationDimension.Fear, modifier.fear, reason);
            if (modifier.respect != 0f) ModifyReputation(ReputationDimension.Respect, modifier.respect, reason);
            if (modifier.reliability != 0f) ModifyReputation(ReputationDimension.Reliability, modifier.reliability, reason);
            if (modifier.ruthlessness != 0f) ModifyReputation(ReputationDimension.Ruthlessness, modifier.ruthlessness, reason);
        }

        // ─── CLOUT Score (Derived from 4D Vector + Actions) ──────────

        /// <summary>
        /// Add CLOUT from a discrete action. CLOUT is the public-facing score;
        /// the 4D vector is the hidden underlying system.
        /// </summary>
        public void AddClout(int amount, string reason)
        {
            int oldClout = _cloutScore;
            _cloutScore += amount;

            int newRank = CalculateRank(_cloutScore);
            if (newRank > _cloutRank)
            {
                _cloutRank = newRank;
                OnRankUp?.Invoke(_cloutRank, rankNames[_cloutRank]);
                Debug.Log($"[Clout] RANK UP: {rankNames[_cloutRank]}! (Clout: {_cloutScore})");
            }

            OnCloutChanged?.Invoke(oldClout, _cloutScore);
        }

        public void RemoveClout(int amount, string reason)
        {
            int old = _cloutScore;
            _cloutScore = Mathf.Max(0, _cloutScore - amount);

            int rank = CalculateRank(_cloutScore);
            if (rank < _cloutRank)
            {
                _cloutRank = rank;
            }

            OnCloutChanged?.Invoke(old, _cloutScore);
        }

        public bool HasCloutRank(int minimumRank) => _cloutRank >= minimumRank;

        // ─── Reputation Queries (for NPC decision-making) ────────────

        /// <summary>
        /// NPC evaluates whether to obey the player based on Fear + Respect.
        /// Returns 0–1 compliance probability.
        /// </summary>
        public float GetComplianceProbability()
        {
            return Mathf.Clamp01((fear * 0.6f) + (respect * 0.4f));
        }

        /// <summary>
        /// NPC evaluates whether to snitch based on Fear (suppresses) and Reliability (trust).
        /// Returns 0–1 snitch probability modifier (multiply with base snitch chance).
        /// Lower is better for the player.
        /// </summary>
        public float GetSnitchModifier()
        {
            // High fear = low snitch chance. High reliability = they trust you won't find out
            return Mathf.Clamp01(1f - (fear * 0.5f) - (reliability * 0.3f) + (ruthlessness * 0.2f));
        }

        /// <summary>
        /// Recruitment quality modifier — higher = better recruits available.
        /// Spec v2.0: CLOUT-gated tiers but also influenced by Respect dimension.
        /// </summary>
        public float GetRecruitmentQuality()
        {
            float rankBonus = _cloutRank / (float)Mathf.Max(1, rankNames.Length - 1);
            return Mathf.Clamp01((rankBonus * 0.6f) + (respect * 0.3f) + (reliability * 0.1f));
        }

        /// <summary>
        /// Worker betrayal suppression — how much the player's reputation reduces betrayal probability.
        /// Used in: P(betray) = (Greed + ExternalOffer - Loyalty) × (1 - BetrayalSuppression) / Compartmentalization
        /// </summary>
        public float GetBetrayalSuppression()
        {
            // High fear strongly suppresses betrayal. High respect adds some.
            return Mathf.Clamp01((fear * 0.6f) + (respect * 0.3f) + (ruthlessness * 0.1f));
        }

        /// <summary>
        /// Deal price modifier — customers pay more when you have high respect/fear.
        /// Returns multiplier (1.0 = no change, 1.3 = +30%).
        /// </summary>
        public float GetDealPriceModifier()
        {
            return 1f + (respect * 0.2f) + (fear * 0.1f);
        }

        /// <summary>
        /// Territory defense modifier — rivals think twice about attacking.
        /// </summary>
        public float GetTerritoryDefenseModifier()
        {
            return 1f + (fear * 0.3f) + (ruthlessness * 0.2f);
        }

        // ─── Daily Decay ─────────────────────────────────────────────

        /// <summary>
        /// Called by TransactionLedger.OnDayEnd or manually.
        /// Reputation drifts toward equilibrium — you must keep earning it.
        /// </summary>
        public void ProcessDailyDecay()
        {
            fear = Mathf.Lerp(fear, EQUILIBRIUM, DECAY_RATE);
            respect = Mathf.Lerp(respect, EQUILIBRIUM, DECAY_RATE);
            reliability = Mathf.Lerp(reliability, EQUILIBRIUM, DECAY_RATE);
            ruthlessness = Mathf.Lerp(ruthlessness, EQUILIBRIUM, DECAY_RATE);
        }

        // ─── Internals ──────────────────────────────────────────────

        private float GetDimension(ReputationDimension dim)
        {
            return dim switch
            {
                ReputationDimension.Fear => fear,
                ReputationDimension.Respect => respect,
                ReputationDimension.Reliability => reliability,
                ReputationDimension.Ruthlessness => ruthlessness,
                _ => 0f
            };
        }

        private void SetDimension(ReputationDimension dim, float value)
        {
            switch (dim)
            {
                case ReputationDimension.Fear: fear = value; break;
                case ReputationDimension.Respect: respect = value; break;
                case ReputationDimension.Reliability: reliability = value; break;
                case ReputationDimension.Ruthlessness: ruthlessness = value; break;
            }
        }

        private int CalculateRank(int score)
        {
            for (int i = rankThresholds.Length - 1; i >= 0; i--)
            {
                if (score >= rankThresholds[i])
                    return i;
            }
            return 0;
        }

        // ─── Clout Sources ──────────────────────────────────────────
        // Predefined CLOUT values for common actions + their 4D reputation effects.

        public static class CloutValues
        {
            public const int FirstSale = 5;
            public const int CompleteDeal = 2;
            public const int BigDeal = 10;
            public const int DefeatRival = 25;
            public const int ClaimTerritory = 50;
            public const int BuyProperty = 30;
            public const int HireEmployee = 5;
            public const int SurviveRaid = 20;
            public const int ReachQualityTier = 15;
            public const int MultiplayerKill = 8;
            public const int WinTerritoryWar = 100;

            // Negative
            public const int GetArrested = -50;
            public const int GetRobbed = -20;
            public const int EmployeeBetray = -15;
            public const int LoseTerritory = -30;
            public const int ProductSeized = -10;
        }

        /// <summary>
        /// Predefined reputation modifiers for common actions.
        /// Usage: rep.ModifyReputationBulk(ReputationModifiers.CompleteDeal, "sold 5g to customer");
        /// </summary>
        public static class ReputationModifiers
        {
            public static readonly ReputationModifier CompleteDeal = new ReputationModifier
                { respect = 0.5f, reliability = 1f };

            public static readonly ReputationModifier BigDeal = new ReputationModifier
                { respect = 1.5f, reliability = 1f };

            public static readonly ReputationModifier ViolentKill = new ReputationModifier
                { fear = 2f, ruthlessness = 1.5f, respect = -0.5f };

            public static readonly ReputationModifier DefeatRival = new ReputationModifier
                { fear = 1f, respect = 1.5f, ruthlessness = 0.5f };

            public static readonly ReputationModifier BuyProperty = new ReputationModifier
                { respect = 1f, reliability = 0.5f };

            public static readonly ReputationModifier HireWorker = new ReputationModifier
                { respect = 0.3f };

            public static readonly ReputationModifier WorkerBetrayal = new ReputationModifier
                { fear = -1f, respect = -0.5f };

            public static readonly ReputationModifier SurviveRaid = new ReputationModifier
                { fear = 0.5f, respect = 1f, ruthlessness = 0.5f };

            public static readonly ReputationModifier GetArrested = new ReputationModifier
                { fear = -1f, respect = -1.5f, reliability = -1f };

            public static readonly ReputationModifier IntimidateNPC = new ReputationModifier
                { fear = 1.5f, respect = -0.3f, ruthlessness = 1f };

            public static readonly ReputationModifier HonorDeal = new ReputationModifier
                { reliability = 1.5f, respect = 0.5f };

            public static readonly ReputationModifier BreakDeal = new ReputationModifier
                { reliability = -2f, respect = -1f, fear = 0.5f };
        }
    }

    // ─── Supporting Types ────────────────────────────────────────────

    /// <summary>
    /// 4D reputation vector — passed as value type for events and queries.
    /// </summary>
    [System.Serializable]
    public struct ReputationVector
    {
        public float fear;
        public float respect;
        public float reliability;
        public float ruthlessness;

        /// <summary>
        /// Composite influence score (0–1). Used for general reputation checks.
        /// </summary>
        public float Composite => (fear * 0.2f) + (respect * 0.35f) + (reliability * 0.25f) + (ruthlessness * 0.2f);

        public override string ToString()
            => $"[F:{fear:F2} R:{respect:F2} L:{reliability:F2} X:{ruthlessness:F2}]";
    }

    /// <summary>
    /// Modifier applied to reputation — positive values increase, negative decrease.
    /// Values are in "event units" (1.0 = standard event magnitude).
    /// </summary>
    [System.Serializable]
    public struct ReputationModifier
    {
        public float fear;
        public float respect;
        public float reliability;
        public float ruthlessness;
    }

    /// <summary>
    /// Which dimension of the 4D reputation vector.
    /// </summary>
    public enum ReputationDimension
    {
        Fear,
        Respect,
        Reliability,
        Ruthlessness
    }
}
