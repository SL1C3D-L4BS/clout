using UnityEngine;
using Clout.Core;
using Clout.Empire.Properties;
using Clout.Utils;

namespace Clout.Empire.Economy.Laundering
{
    /// <summary>
    /// Step 11B — Front business component attached to owned properties that serve
    /// as laundering fronts. Simulates legitimate revenue, customer flow, and
    /// per-business suspicion tracking.
    ///
    /// Each front has a laundering capacity derived from its legitimate revenue —
    /// a restaurant doing $2K/day in real sales can launder ~$3K without suspicion.
    /// Exceeding this ratio raises the per-business suspicion meter, which feeds
    /// into the global IRS attention system.
    ///
    /// Attaches to: Property GameObjects via LaunderingManager.RegisterFrontBusiness().
    /// Ticks on: LaunderingManager.ProcessDailyTick() (called from TransactionLedger.OnDayEnd).
    /// </summary>
    public class FrontBusiness : MonoBehaviour
    {
        [Header("Business Profile")]
        [SerializeField] private FrontBusinessType _businessType;
        [SerializeField] private string _businessName;

        // ─── Simulated Legitimate Business ────────────────────
        private float _baseLegitimateRevenue;
        private float _legitimateExpenses;
        private float _customerFlow;
        private float _qualityRating = 3f;              // 0-5, affects customer flow

        // ─── Laundering State ─────────────────────────────────
        private float _launderingCapacity;               // Daily max (function of legit revenue)
        private float _currentDayLaunderingVolume;       // Today's throughput
        private float _suspicionLevel;                   // 0-1, per-business risk
        private float _lifetimeLaundered;                // All-time total through this business
        private int _consecutiveHighVolumedays;          // Days above 80% capacity
        private float _capacityMultiplier;               // From business type profile
        private float _suspicionDecayRate;               // Daily decay

        // ─── Upgrade Tracking ─────────────────────────────────
        private int _renovationTier;                     // 0-3, increases legit revenue
        private bool _hasBookkeeper;                     // Accountant assigned = slower suspicion

        // ─── Linked Property ──────────────────────────────────
        private Property _linkedProperty;

        // ─── Properties ───────────────────────────────────────
        public FrontBusinessType BusinessType => _businessType;
        public string BusinessName => _businessName;
        public string BusinessId => _linkedProperty != null
            ? _linkedProperty.Definition.propertyName
            : _businessName;

        public float LegitimateRevenue => _baseLegitimateRevenue * (1f + _renovationTier * 0.25f) * (_qualityRating / 3f);
        public float LegitimateExpenses => _legitimateExpenses;
        public float LaunderingCapacity => LegitimateRevenue * _capacityMultiplier;
        public float CurrentDayVolume => _currentDayLaunderingVolume;
        public float RemainingCapacity => Mathf.Max(0, LaunderingCapacity - _currentDayLaunderingVolume);
        public float SuspicionLevel => _suspicionLevel;
        public float LifetimeLaundered => _lifetimeLaundered;
        public float CapacityUtilization => LaunderingCapacity > 0 ? _currentDayLaunderingVolume / LaunderingCapacity : 0f;
        public Property LinkedProperty => _linkedProperty;
        public int RenovationTier => _renovationTier;
        public bool HasBookkeeper => _hasBookkeeper;

        // ─── Business Type Profiles ───────────────────────────
        // Restaurant:  $2,000/day, 1.5x capacity, 0.02/day decay — high cash volume
        // AutoShop:    $3,500/day, 1.2x capacity, 0.015/day decay — plausible large txns
        // Nightclub:   $8,000/day, 2.0x capacity, 0.01/day decay — highest capacity
        // Laundromat:  $800/day,   3.0x capacity, 0.03/day decay — best ratio
        // CarWash:     $1,200/day, 2.5x capacity, 0.025/day decay — balanced

        // ─── Initialization ───────────────────────────────────

        /// <summary>
        /// Initialize this front business with a type and linked property.
        /// Called by LaunderingManager when designating a property as a front.
        /// </summary>
        public void Initialize(FrontBusinessType type, Property property)
        {
            _businessType = type;
            _linkedProperty = property;
            _businessName = property != null ? property.Definition.propertyName : type.ToString();

            ApplyBusinessProfile(type);
        }

        /// <summary>
        /// Initialize without a linked property (standalone test mode).
        /// </summary>
        public void Initialize(FrontBusinessType type, string name)
        {
            _businessType = type;
            _businessName = name;
            _linkedProperty = null;

            ApplyBusinessProfile(type);
        }

        private void ApplyBusinessProfile(FrontBusinessType type)
        {
            switch (type)
            {
                case FrontBusinessType.Restaurant:
                    _baseLegitimateRevenue = 2000f;
                    _legitimateExpenses = 1200f;
                    _customerFlow = 150f;
                    _capacityMultiplier = 1.5f;
                    _suspicionDecayRate = 0.02f;
                    break;

                case FrontBusinessType.AutoShop:
                    _baseLegitimateRevenue = 3500f;
                    _legitimateExpenses = 2000f;
                    _customerFlow = 25f;
                    _capacityMultiplier = 1.2f;
                    _suspicionDecayRate = 0.015f;
                    break;

                case FrontBusinessType.Nightclub:
                    _baseLegitimateRevenue = 8000f;
                    _legitimateExpenses = 5500f;
                    _customerFlow = 400f;
                    _capacityMultiplier = 2.0f;
                    _suspicionDecayRate = 0.01f;
                    break;

                case FrontBusinessType.Laundromat:
                    _baseLegitimateRevenue = 800f;
                    _legitimateExpenses = 300f;
                    _customerFlow = 60f;
                    _capacityMultiplier = 3.0f;
                    _suspicionDecayRate = 0.03f;
                    break;

                case FrontBusinessType.CarWash:
                    _baseLegitimateRevenue = 1200f;
                    _legitimateExpenses = 500f;
                    _customerFlow = 80f;
                    _capacityMultiplier = 2.5f;
                    _suspicionDecayRate = 0.025f;
                    break;
            }
        }

        // ─── Daily Processing ─────────────────────────────────

        /// <summary>
        /// Called by LaunderingManager at end of each game day.
        /// Simulates business activity, updates suspicion, resets daily volume.
        /// </summary>
        public void ProcessDayEnd()
        {
            // Track consecutive high-volume days (above 80% capacity)
            if (CapacityUtilization > 0.8f)
                _consecutiveHighVolumedays++;
            else
                _consecutiveHighVolumedays = Mathf.Max(0, _consecutiveHighVolumedays - 1);

            // Update suspicion
            UpdateSuspicion();

            // Simulate customer flow variance (+/- 20%)
            _customerFlow *= UnityEngine.Random.Range(0.8f, 1.2f);

            // Reset daily volume
            _currentDayLaunderingVolume = 0f;
        }

        private void UpdateSuspicion()
        {
            float safeRatio = 0.6f; // Below 60% of capacity is safe
            float volumeRatio = CapacityUtilization;

            if (volumeRatio > safeRatio)
            {
                // Suspicion grows when laundering exceeds safe threshold
                float overageRatio = (volumeRatio - safeRatio) / (1f - safeRatio);
                float growthRate = overageRatio * 0.08f; // Up to 8% per day at full capacity

                // Consecutive high-volume days compound the growth
                if (_consecutiveHighVolumedays > 3)
                    growthRate *= 1f + (_consecutiveHighVolumedays - 3) * 0.15f;

                // Bookkeeper slows suspicion growth by 40%
                if (_hasBookkeeper) growthRate *= 0.6f;

                _suspicionLevel = Mathf.Min(1f, _suspicionLevel + growthRate);
            }
            else
            {
                // Suspicion decays when operating below safe threshold
                float decayRate = _suspicionDecayRate;

                // Bookkeeper accelerates decay by 50%
                if (_hasBookkeeper) decayRate *= 1.5f;

                _suspicionLevel = Mathf.Max(0f, _suspicionLevel - decayRate);
            }
        }

        // ─── Laundering Operations ────────────────────────────

        /// <summary>
        /// Attempt to launder an amount through this business today.
        /// Returns the amount actually accepted (may be capped by remaining capacity).
        /// </summary>
        public float AcceptLaunderingVolume(float amount)
        {
            float remaining = RemainingCapacity;
            if (remaining <= 0) return 0f;

            float accepted = Mathf.Min(amount, remaining);
            _currentDayLaunderingVolume += accepted;
            _lifetimeLaundered += accepted;

            return accepted;
        }

        /// <summary>
        /// Check if this business can accept a given amount today.
        /// </summary>
        public bool CanAcceptVolume(float amount)
        {
            return RemainingCapacity >= amount;
        }

        // ─── Upgrades & Modifiers ─────────────────────────────

        /// <summary>
        /// Upgrade the business renovation tier (0-3).
        /// Each tier increases legitimate revenue by 25%, making more room for laundering.
        /// </summary>
        public bool UpgradeRenovation()
        {
            if (_renovationTier >= 3) return false;
            _renovationTier++;
            Debug.Log($"[Front] {_businessName} renovated to tier {_renovationTier} — " +
                      $"revenue: ${LegitimateRevenue:F0}/day, capacity: ${LaunderingCapacity:F0}/day");
            return true;
        }

        /// <summary>
        /// Set whether an accountant (bookkeeper) is assigned to this business.
        /// Reduces suspicion growth rate and accelerates decay.
        /// </summary>
        public void SetBookkeeper(bool hasBookkeeper)
        {
            _hasBookkeeper = hasBookkeeper;
        }

        // ─── IRS Exposure ─────────────────────────────────────

        /// <summary>
        /// Calculate this business's contribution to IRS attention.
        /// Based on suspicion level, volume anomalies, and revenue-expense ratio.
        /// </summary>
        public float GetIRSExposure()
        {
            float exposure = 0f;

            // Base suspicion contribution
            exposure += _suspicionLevel * 0.4f;

            // Revenue-to-expense anomaly (industry norm: 1.5-2.5x)
            float revenueExpenseRatio = _legitimateExpenses > 0
                ? (LegitimateRevenue + _currentDayLaunderingVolume) / _legitimateExpenses
                : 10f;

            if (revenueExpenseRatio > 3f)
                exposure += (revenueExpenseRatio - 3f) * 0.05f;

            // Consecutive high-volume penalty
            if (_consecutiveHighVolumedays > 5)
                exposure += (_consecutiveHighVolumedays - 5) * 0.02f;

            return Mathf.Clamp01(exposure);
        }

        /// <summary>
        /// Audit this business — compare laundering volume against legitimate revenue.
        /// Returns true if the audit finds evidence of laundering.
        /// </summary>
        public bool AuditBooks()
        {
            // If current volume exceeds legitimate revenue, caught
            if (_currentDayLaunderingVolume > LegitimateRevenue)
                return true;

            // If lifetime laundered is suspiciously high relative to business age
            // (simplified: check against 30-day revenue equivalent)
            if (_lifetimeLaundered > LegitimateRevenue * 30f && _suspicionLevel > 0.5f)
                return true;

            // Random check weighted by suspicion
            return UnityEngine.Random.value < _suspicionLevel * 0.3f;
        }

        // ─── Serialization Helpers ────────────────────────────

        /// <summary>State snapshot for save system.</summary>
        public FrontBusinessSaveData GetSaveData()
        {
            return new FrontBusinessSaveData
            {
                businessType = _businessType,
                businessName = _businessName,
                propertyId = _linkedProperty != null ? _linkedProperty.Definition.propertyName : "",
                suspicionLevel = _suspicionLevel,
                lifetimeLaundered = _lifetimeLaundered,
                renovationTier = _renovationTier,
                hasBookkeeper = _hasBookkeeper,
                consecutiveHighVolumeDays = _consecutiveHighVolumedays,
                qualityRating = _qualityRating
            };
        }

        /// <summary>Restore state from save data.</summary>
        public void LoadSaveData(FrontBusinessSaveData data)
        {
            _suspicionLevel = data.suspicionLevel;
            _lifetimeLaundered = data.lifetimeLaundered;
            _renovationTier = data.renovationTier;
            _hasBookkeeper = data.hasBookkeeper;
            _consecutiveHighVolumedays = data.consecutiveHighVolumeDays;
            _qualityRating = data.qualityRating;
        }
    }

    // ─── Save Data ────────────────────────────────────────────

    [System.Serializable]
    public struct FrontBusinessSaveData
    {
        public FrontBusinessType businessType;
        public string businessName;
        public string propertyId;
        public float suspicionLevel;
        public float lifetimeLaundered;
        public int renovationTier;
        public bool hasBookkeeper;
        public int consecutiveHighVolumeDays;
        public float qualityRating;
    }
}
