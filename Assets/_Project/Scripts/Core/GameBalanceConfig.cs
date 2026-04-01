using UnityEngine;

namespace Clout.Core
{
    /// <summary>
    /// Step 10 — Centralized balance tuning ScriptableObject.
    /// Every magic number across 130+ scripts consolidated into one editable asset.
    ///
    /// Designers can duplicate this SO for different difficulty presets:
    ///   GameBalance_Easy, GameBalance_Normal, GameBalance_Hardcore
    ///
    /// At runtime, GameFlowManager reads the active config. All managers
    /// that previously hard-coded values now query GameBalanceConfig.Active.
    /// </summary>
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Clout/Game Balance Config")]
    public class GameBalanceConfig : ScriptableObject
    {
        // ─── Singleton Active Config ────────────────────────────────

        private static GameBalanceConfig _active;
        public static GameBalanceConfig Active
        {
            get
            {
                if (_active == null)
                {
                    _active = Resources.Load<GameBalanceConfig>("GameBalanceConfig");
                    if (_active == null)
                    {
                        Debug.LogWarning("[GameBalanceConfig] No config found in Resources/. Using defaults.");
                        _active = CreateInstance<GameBalanceConfig>();
                    }
                }
                return _active;
            }
            set => _active = value;
        }

        // ─── Economy — Pricing ──────────────────────────────────────

        [Header("Economy — Pricing")]
        [Tooltip("Global price multiplier applied to all street prices")]
        public float globalPriceMultiplier = 1.0f;

        [Tooltip("Maximum demand/supply ratio before price caps")]
        public float maxDemandSupplyRatio = 3.0f;

        [Tooltip("Minimum demand/supply ratio floor")]
        public float minDemandSupplyRatio = 0.2f;

        [Tooltip("Default price elasticity if not specified per product")]
        public float defaultElasticity = 0.3f;

        [Tooltip("Maximum heat-based risk premium on prices (0-1)")]
        public float maxRiskPremium = 0.5f;

        [Tooltip("Quality exponent for price calculation (higher = more reward for quality)")]
        public float qualityExponent = 1.5f;

        [Tooltip("Market price update interval in seconds")]
        public float priceUpdateInterval = 300f;

        // ─── Economy — Cash ─────────────────────────────────────────

        [Header("Economy — Cash")]
        [Tooltip("Starting dirty cash for new game")]
        public float startingDirtyCash = 500f;

        [Tooltip("Starting clean cash for new game")]
        public float startingCleanCash = 0f;

        [Tooltip("Maximum cash cap")]
        public float maxCash = 10_000_000f;

        [Tooltip("Property sell-back percentage (0-1)")]
        public float propertySellbackRate = 0.6f;

        // ─── Economy — Laundering ───────────────────────────────────

        [Header("Economy — Laundering")]
        [Tooltip("Base laundering fee rate (0-1)")]
        public float launderingFeeRate = 0.1f;

        [Tooltip("Maximum daily launder amount per front business")]
        public float maxDailyLaunderPerBusiness = 5000f;

        // ─── Workers — Wages & Capacity ─────────────────────────────

        [Header("Workers — Wages & Capacity")]
        [Tooltip("Max workers allowed per CLOUT rank (index 0-5)")]
        public int[] maxWorkersByRank = { 1, 2, 4, 7, 11, 15 };

        [Tooltip("Worker shift duration in seconds")]
        public float workerShiftDuration = 300f;

        [Tooltip("Worker rest duration after shift")]
        public float workerRestDuration = 60f;

        [Tooltip("Daily wage multiplier (applied to base wage)")]
        public float wageMultiplier = 1.0f;

        [Tooltip("Loyalty gain per completed shift")]
        public float loyaltyGainPerShift = 0.005f;

        [Tooltip("Loyalty loss per missed wage payment")]
        public float loyaltyLossPerMissedWage = 0.15f;

        [Tooltip("Skill improvement rate per shift (logarithmic base)")]
        public float skillGrowthRate = 0.01f;

        // ─── Workers — Betrayal ─────────────────────────────────────

        [Header("Workers — Betrayal")]
        [Tooltip("Base betrayal check interval in game days")]
        public int betrayalCheckIntervalDays = 3;

        [Tooltip("Minimum loyalty to suppress betrayal checks entirely")]
        public float betrayalSafeLoyalty = 0.85f;

        [Tooltip("External offer value range (min)")]
        public float externalOfferMin = 100f;

        [Tooltip("External offer value range (max)")]
        public float externalOfferMax = 5000f;

        // ─── Heat & Wanted ──────────────────────────────────────────

        [Header("Heat & Wanted")]
        [Tooltip("Maximum heat value")]
        public float maxHeat = 500f;

        [Tooltip("Heat per public deal")]
        public float heatPerDeal = 20f;

        [Tooltip("Heat per witnessed crime")]
        public float heatPerWitnessedCrime = 15f;

        [Tooltip("Heat per assault on police")]
        public float heatPerAssaultPolice = 100f;

        [Tooltip("Heat per worker arrested")]
        public float heatPerWorkerArrest = 30f;

        [Tooltip("Passive heat decay rate per second")]
        public float heatDecayPerSecond = 0.15f;

        [Tooltip("Heat decay rate in safe zone per second")]
        public float heatDecaySafeZone = 0.5f;

        [Tooltip("Heat decay rate while hiding per second")]
        public float heatDecayHiding = 0.8f;

        [Tooltip("Wanted level thresholds: Suspicious, Wanted, Hunted, MostWanted")]
        public float[] wantedThresholds = { 50f, 150f, 300f, 450f };

        // ─── Police ─────────────────────────────────────────────────

        [Header("Police")]
        [Tooltip("Maximum simultaneous police officers")]
        public int maxPoliceOfficers = 12;

        [Tooltip("Police spawn cooldown in seconds")]
        public float policeSpawnCooldown = 15f;

        [Tooltip("Police response time in seconds (base, modified by district)")]
        public float policeResponseTime = 25f;

        [Tooltip("Property raid heat threshold (0-100 normalized)")]
        public float raidHeatThreshold = 60f;

        [Tooltip("Base raid chance per day per property")]
        public float baseRaidChancePerDay = 0.05f;

        [Tooltip("Evidence degradation time in seconds")]
        public float evidenceDegradationTime = 120f;

        // ─── Reputation ─────────────────────────────────────────────

        [Header("Reputation")]
        [Tooltip("CLOUT rank thresholds")]
        public int[] rankThresholds = { 0, 100, 500, 2000, 10000, 50000 };

        [Tooltip("Daily reputation decay rate toward equilibrium")]
        public float reputationDecayRate = 0.02f;

        [Tooltip("CLOUT earned per completed deal")]
        public int cloutPerDeal = 2;

        [Tooltip("CLOUT earned per first sale")]
        public int cloutFirstSale = 5;

        [Tooltip("CLOUT earned per property purchased")]
        public int cloutPerProperty = 10;

        [Tooltip("CLOUT earned per rival defeated")]
        public int cloutPerRivalDefeated = 25;

        // ─── Properties ─────────────────────────────────────────────

        [Header("Properties")]
        [Tooltip("Maximum properties a player can own")]
        public int maxOwnedProperties = 10;

        [Tooltip("Property condition degradation per missed upkeep day")]
        public float conditionDegradationRate = 0.05f;

        [Tooltip("Base property repair cost per condition point")]
        public float repairCostPerPoint = 50f;

        // ─── Production ─────────────────────────────────────────────

        [Header("Production")]
        [Tooltip("Base cooking time multiplier")]
        public float cookingTimeMultiplier = 1.0f;

        [Tooltip("Base explosion chance for low-skill cooks")]
        public float baseExplosionChance = 0.03f;

        [Tooltip("Skill threshold below which explosion is possible")]
        public float explosionSkillThreshold = 0.3f;

        // ─── District ───────────────────────────────────────────────

        [Header("District")]
        [Tooltip("Customer NPC spawn interval in seconds")]
        public float customerSpawnInterval = 12f;

        [Tooltip("Civilian NPC spawn interval in seconds")]
        public float civilianSpawnInterval = 8f;

        [Tooltip("NPC despawn distance from player")]
        public float npcDespawnDistance = 80f;

        // ─── Game Day ───────────────────────────────────────────────

        [Header("Game Day")]
        [Tooltip("Real-time seconds per game day (600 = 10 minutes)")]
        public float gameDayDuration = 600f;

        [Tooltip("Auto-save interval in game days")]
        public int autoSaveIntervalDays = 1;

        // ─── Difficulty Presets ─────────────────────────────────────

        /// <summary>Apply Easy difficulty preset.</summary>
        public void ApplyEasyPreset()
        {
            globalPriceMultiplier = 1.2f;
            heatDecayPerSecond = 0.25f;
            heatPerDeal = 12f;
            maxPoliceOfficers = 8;
            policeResponseTime = 35f;
            betrayalSafeLoyalty = 0.7f;
            loyaltyGainPerShift = 0.008f;
            startingDirtyCash = 1000f;
            wageMultiplier = 0.8f;
            baseRaidChancePerDay = 0.02f;
        }

        /// <summary>Apply Normal difficulty preset (default values).</summary>
        public void ApplyNormalPreset()
        {
            // All values are already defaults from field initializers
            globalPriceMultiplier = 1.0f;
            heatDecayPerSecond = 0.15f;
            heatPerDeal = 20f;
            maxPoliceOfficers = 12;
            policeResponseTime = 25f;
            betrayalSafeLoyalty = 0.85f;
            loyaltyGainPerShift = 0.005f;
            startingDirtyCash = 500f;
            wageMultiplier = 1.0f;
            baseRaidChancePerDay = 0.05f;
        }

        /// <summary>Apply Hardcore difficulty preset.</summary>
        public void ApplyHardcorePreset()
        {
            globalPriceMultiplier = 0.8f;
            heatDecayPerSecond = 0.08f;
            heatPerDeal = 30f;
            maxPoliceOfficers = 16;
            policeResponseTime = 15f;
            betrayalSafeLoyalty = 0.95f;
            loyaltyGainPerShift = 0.003f;
            startingDirtyCash = 250f;
            wageMultiplier = 1.3f;
            baseRaidChancePerDay = 0.1f;
        }
    }
}
