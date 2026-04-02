using UnityEngine;
using System;
using Clout.Core;
using Clout.Empire.Crafting;
using Clout.Empire.Economy;
using Clout.Utils;

namespace Clout.Forensics
{
    /// <summary>
    /// Step 12D — Equipment upgrade that scrubs forensic signatures from product batches.
    ///
    /// Attached to CraftingStation as an optional upgrade. Each scrub level injects
    /// progressively more noise into the batch signature, making it harder for
    /// ForensicLabAI to trace product back to its origin facility.
    ///
    /// Tradeoff: Scrubbing reduces output yield. Players must balance forensic
    /// safety against profit margins.
    ///
    /// Level 1: -5% yield, similarity drops from ~0.95 to ~0.80
    /// Level 2: -12% yield, similarity drops to ~0.65 (below cluster threshold)
    /// Level 3: -20% yield, signatures effectively randomized
    ///
    /// Upgrade cost scales with level. Requires clean cash (equipment purchase).
    /// Can be toggled on/off without losing the upgrade level.
    /// </summary>
    public class SignatureScrubber : MonoBehaviour
    {
        [Header("Scrubber Config")]
        [SerializeField] private int _scrubLevel;               // 0 = none, 1-3 = active
        [SerializeField] private bool _isEnabled = true;        // Toggle without losing level
        [SerializeField] private CraftingStation _linkedStation;

        // ─── Scrub Profiles ───────────────────────────────────
        // Level, Yield Penalty, Noise Injection, Upgrade Cost
        private static readonly ScrubProfile[] PROFILES = new ScrubProfile[]
        {
            new() { level = 0, yieldPenalty = 0f,    noiseInjection = 0f,   upgradeCost = 0f },
            new() { level = 1, yieldPenalty = 0.05f, noiseInjection = 0.15f, upgradeCost = 2500f },
            new() { level = 2, yieldPenalty = 0.12f, noiseInjection = 0.30f, upgradeCost = 8000f },
            new() { level = 3, yieldPenalty = 0.20f, noiseInjection = 0.50f, upgradeCost = 25000f },
        };

        // ─── Properties ───────────────────────────────────────
        public int ScrubLevel => _scrubLevel;
        public bool IsEnabled => _isEnabled && _scrubLevel > 0;
        public float YieldPenalty => IsEnabled ? GetProfile().yieldPenalty : 0f;
        public float NoiseInjection => IsEnabled ? GetProfile().noiseInjection : 0f;
        public CraftingStation LinkedStation => _linkedStation;

        /// <summary>Effective output multiplier (1.0 = no penalty).</summary>
        public float OutputMultiplier => 1f - YieldPenalty;

        /// <summary>Human-readable scrub level name.</summary>
        public string LevelName => _scrubLevel switch
        {
            0 => "None",
            1 => "Basic (Similarity ~0.80)",
            2 => "Advanced (Similarity ~0.65)",
            3 => "Military (Randomized)",
            _ => "Unknown"
        };

        // ─── Initialization ───────────────────────────────────

        /// <summary>
        /// Initialize scrubber and link to a CraftingStation.
        /// Called when purchasing/installing the scrubber upgrade.
        /// </summary>
        public void Initialize(CraftingStation station, int level = 1)
        {
            _linkedStation = station;
            _scrubLevel = Mathf.Clamp(level, 0, 3);
            _isEnabled = true;
        }

        /// <summary>
        /// Auto-detect and link to CraftingStation on same GameObject.
        /// </summary>
        private void Start()
        {
            if (_linkedStation == null)
                _linkedStation = GetComponent<CraftingStation>();
        }

        // ─── Scrubbing Operations ─────────────────────────────

        /// <summary>
        /// Apply scrubbing to a batch signature. Called during CraftingStation.CompleteBatch().
        /// Modifies the signature in-place.
        /// </summary>
        public void ScrubSignature(BatchSignature signature)
        {
            if (!IsEnabled || signature == null) return;

            ScrubProfile profile = GetProfile();
            signature.ApplyScrub(_scrubLevel, profile.noiseInjection);

            Debug.Log($"[Scrubber] Applied level {_scrubLevel} scrub to {signature.BatchId} " +
                      $"(noise: {profile.noiseInjection:F2})");
        }

        /// <summary>
        /// Calculate the yield-adjusted output quantity for a batch.
        /// Returns reduced quantity based on scrub level penalty.
        /// </summary>
        public int AdjustYield(int baseQuantity)
        {
            if (!IsEnabled) return baseQuantity;
            return Mathf.Max(1, Mathf.RoundToInt(baseQuantity * OutputMultiplier));
        }

        // ─── Upgrades ─────────────────────────────────────────

        /// <summary>
        /// Upgrade the scrubber to the next level. Requires clean cash.
        /// Returns true if upgrade successful.
        /// </summary>
        public bool Upgrade()
        {
            if (_scrubLevel >= 3)
            {
                Debug.Log("[Scrubber] Already at max level.");
                return false;
            }

            int nextLevel = _scrubLevel + 1;
            float cost = PROFILES[nextLevel].upgradeCost;

            CashManager cash = CashManager.Instance;
            if (cash == null || !cash.CanAffordClean(cost))
            {
                Debug.Log($"[Scrubber] Can't afford upgrade to level {nextLevel} (${cost:F0} clean required).");
                return false;
            }

            cash.SpendClean(cost, $"Scrubber upgrade: Level {nextLevel}");
            _scrubLevel = nextLevel;

            EventBus.Publish(new ScrubberUpgradedEvent
            {
                stationId = _linkedStation != null ? _linkedStation.stationId : "",
                newLevel = _scrubLevel,
                cost = cost
            });

            Debug.Log($"[Scrubber] Upgraded to level {_scrubLevel}: {LevelName} " +
                      $"(yield penalty: {YieldPenalty:P0}, cost: ${cost:F0})");

            return true;
        }

        /// <summary>
        /// Get the cost of the next upgrade level, or 0 if maxed.
        /// </summary>
        public float GetNextUpgradeCost()
        {
            if (_scrubLevel >= 3) return 0f;
            return PROFILES[_scrubLevel + 1].upgradeCost;
        }

        /// <summary>
        /// Check if the next level upgrade is affordable.
        /// </summary>
        public bool CanAffordUpgrade()
        {
            float cost = GetNextUpgradeCost();
            if (cost <= 0) return false;
            CashManager cash = CashManager.Instance;
            return cash != null && cash.CanAffordClean(cost);
        }

        // ─── Toggle ───────────────────────────────────────────

        /// <summary>
        /// Toggle scrubber on/off without losing upgrade level.
        /// Useful when player wants full yield temporarily.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            Debug.Log($"[Scrubber] {(enabled ? "Enabled" : "Disabled")} (level: {_scrubLevel})");
        }

        public void Toggle() => SetEnabled(!_isEnabled);

        // ─── Helpers ──────────────────────────────────────────

        private ScrubProfile GetProfile()
        {
            int idx = Mathf.Clamp(_scrubLevel, 0, PROFILES.Length - 1);
            return PROFILES[idx];
        }

        /// <summary>
        /// Install a scrubber on a CraftingStation. Creates the component if needed.
        /// </summary>
        public static SignatureScrubber InstallOnStation(CraftingStation station, int level = 1)
        {
            if (station == null) return null;

            // Check for existing scrubber
            SignatureScrubber existing = station.GetComponent<SignatureScrubber>();
            if (existing != null)
            {
                // Upgrade existing
                while (existing.ScrubLevel < level && existing.ScrubLevel < 3)
                    existing.Upgrade();
                return existing;
            }

            // Create new scrubber
            SignatureScrubber scrubber = station.gameObject.AddComponent<SignatureScrubber>();
            scrubber.Initialize(station, level);
            return scrubber;
        }

        // ─── Serialization ────────────────────────────────────

        public ScrubberSaveData GetSaveData()
        {
            return new ScrubberSaveData
            {
                scrubLevel = _scrubLevel,
                isEnabled = _isEnabled,
                stationId = _linkedStation != null ? _linkedStation.stationId : ""
            };
        }

        public void LoadSaveData(ScrubberSaveData data)
        {
            _scrubLevel = data.scrubLevel;
            _isEnabled = data.isEnabled;
        }
    }

    // ─── Internal Types ───────────────────────────────────────

    [Serializable]
    internal struct ScrubProfile
    {
        public int level;
        public float yieldPenalty;
        public float noiseInjection;
        public float upgradeCost;
    }

    // ─── Save Data ────────────────────────────────────────────

    [Serializable]
    public struct ScrubberSaveData
    {
        public int scrubLevel;
        public bool isEnabled;
        public string stationId;
    }

    // ─── Events ───────────────────────────────────────────────

    public struct ScrubberUpgradedEvent
    {
        public string stationId;
        public int newLevel;
        public float cost;
    }

    public struct BatchSignatureCreatedEvent
    {
        public string batchId;
        public string productId;
        public string stationId;
        public int facilitySeed;
        public int scrubLevel;
        public float quality;
    }
}
