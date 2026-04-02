using UnityEngine;
using System.Collections.Generic;
using Clout.Core;
using Clout.Utils;
using Clout.World.Police;

namespace Clout.Empire.Economy.Laundering
{
    /// <summary>
    /// Step 11D — IRS investigation and audit system.
    ///
    /// Tracks cumulative IRS attention from laundering operations and triggers
    /// a 4-stage investigation pipeline when thresholds are crossed:
    ///
    ///   Stage 1: FLAG        (attention > 0.4) — Warning notification, 7-day window
    ///   Stage 2: INVESTIGATION (attention > 0.6) — Agent assigned, transactions monitored
    ///   Stage 3: AUDIT       (attention > 0.8) — Formal audit, books examined per-business
    ///   Stage 4: SEIZURE     (audit failed) — Fines, business seizure, wanted level spike
    ///
    /// Attention rises from suspicious deposits, velocity anomalies, pattern detection,
    /// and cross-business correlation. Decays naturally over time when laundering
    /// activity is reduced.
    ///
    /// Countermeasures: accountant workers, method diversification, realistic business
    /// ratios, paying fines, contesting audits.
    ///
    /// Owned by: LaunderingManager (calls ProcessDaily, CheckTriggers).
    /// Integrates: WantedSystem (charges → heat), CashManager (fines/seizure),
    ///             PropertyManager (business seizure), EventBus.
    /// </summary>
    [System.Serializable]
    public class IRSInvestigation
    {
        // ─── Attention Meter ──────────────────────────────────
        [SerializeField] private float _attention;              // 0.0 - 1.0
        [SerializeField] private float _peakAttention;          // Historical max (never fully resets)
        [SerializeField] private float _permanentFloor;         // Minimum attention from lifetime volume

        // ─── Investigation State ──────────────────────────────
        [SerializeField] private IRSInvestigationStage _stage = IRSInvestigationStage.None;
        [SerializeField] private float _stageEnteredTime;
        [SerializeField] private int _stageEnteredDay;
        [SerializeField] private float _auditProgress;          // 0-1 during Audit stage
        [SerializeField] private string _targetBusinessId;      // Business being audited
        [SerializeField] private int _auditDurationDays = 14;
        [SerializeField] private int _daysInCurrentStage;

        // ─── Trigger Tracking ─────────────────────────────────
        [SerializeField] private float _totalLaunderedLifetime;
        [SerializeField] private float[] _dailyVolumes = new float[30];  // Rolling 30-day window
        [SerializeField] private int _dailyVolumeIndex;
        [SerializeField] private List<float> _recentAmounts = new List<float>();
        [SerializeField] private int _accountantCount;

        // ─── Trigger History ──────────────────────────────────
        private readonly List<IRSTriggerRecord> _triggerHistory = new List<IRSTriggerRecord>();

        // ─── Config (from GameBalanceConfig) ──────────────────
        private float _decayRate = 0.015f;                      // Per day base decay
        private float _accountantReduction = 0.25f;             // Per accountant, diminishing

        // ─── Thresholds ───────────────────────────────────────
        public const float FLAG_THRESHOLD = 0.4f;
        public const float INVESTIGATION_THRESHOLD = 0.6f;
        public const float AUDIT_THRESHOLD = 0.8f;

        // Lifetime laundering thresholds that permanently raise floor
        private static readonly float[] LIFETIME_THRESHOLDS = { 50_000f, 250_000f, 1_000_000f, 5_000_000f };
        private static readonly float[] LIFETIME_FLOOR_BUMPS = { 0.05f, 0.10f, 0.15f, 0.25f };

        // ─── Properties ───────────────────────────────────────
        public float Attention => _attention;
        public float PeakAttention => _peakAttention;
        public IRSInvestigationStage Stage => _stage;
        public float AuditProgress => _auditProgress;
        public string TargetBusinessId => _targetBusinessId;
        public int DaysInStage => _daysInCurrentStage;
        public float TotalLaunderedLifetime => _totalLaunderedLifetime;
        public IReadOnlyList<IRSTriggerRecord> TriggerHistory => _triggerHistory;

        public bool IsUnderInvestigation => _stage != IRSInvestigationStage.None;
        public bool IsUnderAudit => _stage == IRSInvestigationStage.Audit;

        // ─── Initialization ───────────────────────────────────

        public void Initialize()
        {
            _attention = 0f;
            _peakAttention = 0f;
            _permanentFloor = 0f;
            _stage = IRSInvestigationStage.None;
            _dailyVolumes = new float[30];
            _dailyVolumeIndex = 0;
            _recentAmounts.Clear();
            _triggerHistory.Clear();
        }

        public void SetAccountantCount(int count)
        {
            _accountantCount = count;
        }

        // ─── Attention Management ─────────────────────────────

        /// <summary>
        /// Add attention from a specific trigger. Records the trigger for audit trail.
        /// </summary>
        public void AddAttention(float amount, string triggerType, string details = "")
        {
            if (amount <= 0) return;

            // Accountants reduce attention growth
            float reduction = GetAccountantReduction();
            float adjusted = amount * (1f - reduction);

            _attention = Mathf.Min(1f, _attention + adjusted);
            _peakAttention = Mathf.Max(_peakAttention, _attention);

            _triggerHistory.Add(new IRSTriggerRecord
            {
                triggerType = triggerType,
                attentionIncrease = adjusted,
                timestamp = Time.time,
                details = details
            });

            // Cap trigger history
            if (_triggerHistory.Count > 100)
                _triggerHistory.RemoveAt(0);

            Debug.Log($"[IRS] +{adjusted:F3} attention ({triggerType}) — Total: {_attention:F2} [{_stage}]");
        }

        /// <summary>
        /// Daily decay of attention. Called by LaunderingManager at day end.
        /// </summary>
        public void DecayAttention()
        {
            float decay = _decayRate;

            // Accountants accelerate decay
            decay *= (1f + GetAccountantReduction() * 0.5f);

            // Don't decay below permanent floor
            _attention = Mathf.Max(_permanentFloor, _attention - decay);
        }

        private float GetAccountantReduction()
        {
            if (_accountantCount <= 0) return 0f;
            // Diminishing returns: 1st = 25%, 2nd = 12.5%, 3rd = 6.25%
            float total = 0f;
            for (int i = 0; i < _accountantCount; i++)
                total += _accountantReduction * Mathf.Pow(0.5f, i);
            return Mathf.Min(total, 0.6f); // Cap at 60% reduction
        }

        // ─── Transaction Analysis ─────────────────────────────

        /// <summary>
        /// Analyze a laundering transaction for IRS triggers.
        /// Called by LaunderingManager when a batch enters the pipeline.
        /// </summary>
        public void AnalyzeTransaction(float amount, LaunderingMethodType method, string businessId)
        {
            // Record for velocity tracking
            _totalLaunderedLifetime += amount;
            _dailyVolumes[_dailyVolumeIndex] += amount;
            _recentAmounts.Add(amount);
            if (_recentAmounts.Count > 20) _recentAmounts.RemoveAt(0);

            // Update permanent floor based on lifetime thresholds
            UpdatePermanentFloor();

            // ── Trigger: Large single deposit ────────────────
            if (amount > 10_000f)
            {
                AddAttention(0.15f, "large_deposit",
                    $"${amount:F0} exceeds $10K reporting threshold");
            }

            // ── Trigger: Round numbers ───────────────────────
            if (amount >= 1000f && amount % 1000f < 1f)
            {
                AddAttention(0.03f, "round_number",
                    $"${amount:F0} is a round number — suspicious pattern");
            }

            // ── Trigger: Just-under-threshold structuring ────
            if (amount >= 9000f && amount < 10_000f)
            {
                AddAttention(0.08f, "structuring_pattern",
                    $"${amount:F0} is just under $10K — classic structuring");
            }

            // ── Trigger: Velocity anomaly ────────────────────
            float avgDaily = GetAverageDailyVolume();
            float todayVolume = _dailyVolumes[_dailyVolumeIndex];
            if (avgDaily > 0 && todayVolume > avgDaily * 3f)
            {
                AddAttention(0.10f, "velocity_anomaly",
                    $"Today's volume ${todayVolume:F0} is {todayVolume / avgDaily:F1}x average");
            }

            // ── Trigger: Pattern detection (repeated amounts) ─
            int repeats = CountRepeatedAmount(amount);
            if (repeats >= 3)
            {
                AddAttention(0.08f, "pattern_detected",
                    $"${amount:F0} has appeared {repeats} times recently");
            }
        }

        /// <summary>
        /// Check for cross-business correlation.
        /// Called when multiple businesses spike on the same day.
        /// </summary>
        public void CheckCrossBusinessCorrelation(int businessesAboveThreshold)
        {
            if (businessesAboveThreshold >= 2)
            {
                AddAttention(0.12f, "cross_business_correlation",
                    $"{businessesAboveThreshold} businesses showed unusual activity simultaneously");
            }
        }

        // ─── Daily Processing ─────────────────────────────────

        /// <summary>
        /// Full daily processing — decay attention, advance investigation stages,
        /// check stage transitions. Called by LaunderingManager at day end.
        /// </summary>
        public void ProcessDaily(int currentDay, List<FrontBusiness> activeFronts)
        {
            _daysInCurrentStage++;

            // Advance daily volume window
            _dailyVolumeIndex = (_dailyVolumeIndex + 1) % 30;
            _dailyVolumes[_dailyVolumeIndex] = 0f;

            // Decay attention
            DecayAttention();

            // Check cross-business correlation
            int highVolumeFronts = 0;
            foreach (var front in activeFronts)
            {
                if (front.CapacityUtilization > 0.7f)
                    highVolumeFronts++;
            }
            if (highVolumeFronts >= 2)
                CheckCrossBusinessCorrelation(highVolumeFronts);

            // Aggregate per-business exposure into global attention
            float businessExposure = 0f;
            foreach (var front in activeFronts)
                businessExposure += front.GetIRSExposure();

            if (businessExposure > 0.1f)
            {
                AddAttention(businessExposure * 0.02f, "business_exposure",
                    $"Aggregate front business exposure: {businessExposure:F2}");
            }

            // Stage transitions
            EvaluateStageTransition(currentDay, activeFronts);
        }

        // ─── Stage Machine ────────────────────────────────────

        private void EvaluateStageTransition(int currentDay, List<FrontBusiness> fronts)
        {
            switch (_stage)
            {
                case IRSInvestigationStage.None:
                    if (_attention >= FLAG_THRESHOLD)
                        TransitionToStage(IRSInvestigationStage.Flag, currentDay);
                    break;

                case IRSInvestigationStage.Flag:
                    if (_attention >= INVESTIGATION_THRESHOLD)
                        TransitionToStage(IRSInvestigationStage.Investigation, currentDay);
                    else if (_attention < FLAG_THRESHOLD * 0.7f) // Must drop significantly to clear
                        TransitionToStage(IRSInvestigationStage.None, currentDay);
                    break;

                case IRSInvestigationStage.Investigation:
                    if (_attention >= AUDIT_THRESHOLD)
                    {
                        // Select target business (highest suspicion)
                        _targetBusinessId = SelectAuditTarget(fronts);
                        TransitionToStage(IRSInvestigationStage.Audit, currentDay);
                    }
                    else if (_attention < INVESTIGATION_THRESHOLD * 0.7f)
                        TransitionToStage(IRSInvestigationStage.Flag, currentDay);
                    break;

                case IRSInvestigationStage.Audit:
                    ProcessAuditProgress(currentDay, fronts);
                    break;

                case IRSInvestigationStage.Seizure:
                    // Seizure is terminal — resolved externally via PayFine/ContestAudit
                    break;
            }
        }

        private void TransitionToStage(IRSInvestigationStage newStage, int currentDay)
        {
            IRSInvestigationStage oldStage = _stage;
            _stage = newStage;
            _stageEnteredTime = Time.time;
            _stageEnteredDay = currentDay;
            _daysInCurrentStage = 0;
            _auditProgress = 0f;

            Debug.Log($"[IRS] Stage transition: {oldStage} → {newStage} (attention: {_attention:F2})");

            // Publish events
            EventBus.Publish(new IRSStageChangedEvent
            {
                previousStage = oldStage,
                newStage = newStage,
                attention = _attention,
                targetBusinessId = _targetBusinessId ?? ""
            });

            switch (newStage)
            {
                case IRSInvestigationStage.Flag:
                    EventBus.Publish(new IRSFlagEvent
                    {
                        attention = _attention,
                        warningDays = 7
                    });
                    break;

                case IRSInvestigationStage.Investigation:
                    EventBus.Publish(new IRSInvestigationStartedEvent
                    {
                        attention = _attention
                    });
                    break;

                case IRSInvestigationStage.Audit:
                    EventBus.Publish(new IRSAuditStartedEvent
                    {
                        attention = _attention,
                        targetBusinessId = _targetBusinessId ?? "",
                        auditDurationDays = _auditDurationDays
                    });
                    break;
            }
        }

        private void ProcessAuditProgress(int currentDay, List<FrontBusiness> fronts)
        {
            _auditProgress = Mathf.Clamp01((float)_daysInCurrentStage / _auditDurationDays);

            if (_daysInCurrentStage >= _auditDurationDays)
            {
                // Audit complete — check books
                FrontBusiness target = FindTargetBusiness(fronts);
                bool caught = target != null && target.AuditBooks();

                if (caught)
                {
                    TransitionToStage(IRSInvestigationStage.Seizure, currentDay);
                }
                else
                {
                    // Passed audit — attention drops significantly
                    _attention = Mathf.Max(_permanentFloor, _attention * 0.4f);
                    TransitionToStage(IRSInvestigationStage.None, currentDay);
                    Debug.Log("[IRS] Audit passed — attention reduced.");

                    EventBus.Publish(new IRSAuditPassedEvent
                    {
                        targetBusinessId = _targetBusinessId ?? ""
                    });
                }
            }
        }

        // ─── Seizure Resolution ───────────────────────────────

        /// <summary>
        /// Execute seizure penalties. Called by LaunderingManager when reaching Seizure stage.
        /// Returns the total financial damage inflicted.
        /// </summary>
        public SeizureResult ExecuteSeizure(List<FrontBusiness> fronts)
        {
            var result = new SeizureResult();

            // Fine: 50% of detected laundered amount (estimate from recent history)
            float detectedAmount = EstimateDetectedLaundering();
            result.fineAmount = detectedAmount * 0.5f;

            // Identify seized business
            FrontBusiness target = FindTargetBusiness(fronts);
            if (target != null)
            {
                result.seizedBusinessId = target.BusinessId;
                result.seizedBusinessType = target.BusinessType;
                result.businessSeized = true;
            }

            // Criminal charges — heat spike
            result.heatPenalty = 80f;       // Major wanted level spike
            result.wantedLevelIncrease = true;

            // Cascade risk — connected businesses get investigated
            result.cascadeBusinessIds = new List<string>();
            foreach (var front in fronts)
            {
                if (front != target && front.SuspicionLevel > 0.3f)
                    result.cascadeBusinessIds.Add(front.BusinessId);
            }

            Debug.Log($"[IRS] SEIZURE — Fine: ${result.fineAmount:F0}, " +
                      $"Business seized: {result.seizedBusinessId}, " +
                      $"Cascade: {result.cascadeBusinessIds.Count} businesses flagged");

            // Reset investigation state (but attention stays high)
            _attention = Mathf.Max(0.5f, _permanentFloor + 0.2f);
            _stage = IRSInvestigationStage.None;
            _daysInCurrentStage = 0;
            _auditProgress = 0f;

            EventBus.Publish(new IRSSeizureEvent
            {
                fineAmount = result.fineAmount,
                seizedBusinessId = result.seizedBusinessId ?? "",
                heatPenalty = result.heatPenalty,
                cascadeCount = result.cascadeBusinessIds.Count
            });

            return result;
        }

        /// <summary>
        /// Pay a fine to resolve the audit without full seizure.
        /// Only available during early Seizure stage (first 3 days).
        /// Requires clean cash.
        /// </summary>
        public float CalculateFineCost()
        {
            return EstimateDetectedLaundering() * 0.5f;
        }

        /// <summary>
        /// Contest the audit with a lawyer. Chance to reduce or dismiss.
        /// Success rate based on attention level and accountant presence.
        /// </summary>
        public bool ContestAudit()
        {
            float successChance = 0.2f; // Base 20% chance

            // Accountants improve chance
            successChance += _accountantCount * 0.1f;

            // Lower attention = better chance
            successChance += (1f - _attention) * 0.15f;

            bool success = UnityEngine.Random.value < successChance;

            if (success)
            {
                _attention = Mathf.Max(_permanentFloor, _attention * 0.5f);
                _stage = IRSInvestigationStage.Flag;
                _daysInCurrentStage = 0;
                Debug.Log("[IRS] Audit contested successfully — reduced to Flag stage.");

                EventBus.Publish(new IRSAuditContestedEvent { success = true });
            }
            else
            {
                // Failed contest — attention spikes
                _attention = Mathf.Min(1f, _attention + 0.1f);
                Debug.Log("[IRS] Audit contest FAILED — attention increased.");

                EventBus.Publish(new IRSAuditContestedEvent { success = false });
            }

            return success;
        }

        // ─── Helpers ──────────────────────────────────────────

        private float GetAverageDailyVolume()
        {
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < _dailyVolumes.Length; i++)
            {
                if (i == _dailyVolumeIndex) continue; // Exclude today
                if (_dailyVolumes[i] > 0)
                {
                    sum += _dailyVolumes[i];
                    count++;
                }
            }
            return count > 0 ? sum / count : 0f;
        }

        private int CountRepeatedAmount(float amount)
        {
            int count = 0;
            float tolerance = amount * 0.02f; // 2% tolerance
            foreach (float recent in _recentAmounts)
            {
                if (Mathf.Abs(recent - amount) <= tolerance)
                    count++;
            }
            return count;
        }

        private void UpdatePermanentFloor()
        {
            for (int i = 0; i < LIFETIME_THRESHOLDS.Length; i++)
            {
                if (_totalLaunderedLifetime >= LIFETIME_THRESHOLDS[i])
                    _permanentFloor = Mathf.Max(_permanentFloor, LIFETIME_FLOOR_BUMPS[i]);
            }
        }

        private float EstimateDetectedLaundering()
        {
            // Sum recent 30 days of volume as estimate
            float total = 0f;
            foreach (float vol in _dailyVolumes)
                total += vol;
            return Mathf.Max(total, 10_000f); // Minimum $10K
        }

        private string SelectAuditTarget(List<FrontBusiness> fronts)
        {
            if (fronts == null || fronts.Count == 0) return "";

            FrontBusiness highest = fronts[0];
            float highestSuspicion = highest.SuspicionLevel;

            for (int i = 1; i < fronts.Count; i++)
            {
                if (fronts[i].SuspicionLevel > highestSuspicion)
                {
                    highest = fronts[i];
                    highestSuspicion = fronts[i].SuspicionLevel;
                }
            }

            return highest.BusinessId;
        }

        private FrontBusiness FindTargetBusiness(List<FrontBusiness> fronts)
        {
            if (string.IsNullOrEmpty(_targetBusinessId)) return null;
            foreach (var front in fronts)
            {
                if (front.BusinessId == _targetBusinessId)
                    return front;
            }
            return null;
        }

        // ─── Serialization ────────────────────────────────────

        public IRSSaveData GetSaveData()
        {
            return new IRSSaveData
            {
                attention = _attention,
                peakAttention = _peakAttention,
                permanentFloor = _permanentFloor,
                stage = _stage,
                daysInCurrentStage = _daysInCurrentStage,
                auditProgress = _auditProgress,
                targetBusinessId = _targetBusinessId ?? "",
                totalLaunderedLifetime = _totalLaunderedLifetime,
                dailyVolumes = (float[])_dailyVolumes.Clone(),
                dailyVolumeIndex = _dailyVolumeIndex,
                accountantCount = _accountantCount
            };
        }

        public void LoadSaveData(IRSSaveData data)
        {
            _attention = data.attention;
            _peakAttention = data.peakAttention;
            _permanentFloor = data.permanentFloor;
            _stage = data.stage;
            _daysInCurrentStage = data.daysInCurrentStage;
            _auditProgress = data.auditProgress;
            _targetBusinessId = data.targetBusinessId;
            _totalLaunderedLifetime = data.totalLaunderedLifetime;
            _dailyVolumes = data.dailyVolumes ?? new float[30];
            _dailyVolumeIndex = data.dailyVolumeIndex;
            _accountantCount = data.accountantCount;
        }
    }

    // ─── Seizure Result ───────────────────────────────────────

    public struct SeizureResult
    {
        public float fineAmount;
        public string seizedBusinessId;
        public FrontBusinessType seizedBusinessType;
        public bool businessSeized;
        public float heatPenalty;
        public bool wantedLevelIncrease;
        public List<string> cascadeBusinessIds;
    }

    // ─── Save Data ────────────────────────────────────────────

    [System.Serializable]
    public struct IRSSaveData
    {
        public float attention;
        public float peakAttention;
        public float permanentFloor;
        public IRSInvestigationStage stage;
        public int daysInCurrentStage;
        public float auditProgress;
        public string targetBusinessId;
        public float totalLaunderedLifetime;
        public float[] dailyVolumes;
        public int dailyVolumeIndex;
        public int accountantCount;
    }

    // ─── IRS Events ───────────────────────────────────────────

    public struct IRSStageChangedEvent
    {
        public IRSInvestigationStage previousStage;
        public IRSInvestigationStage newStage;
        public float attention;
        public string targetBusinessId;
    }

    public struct IRSFlagEvent
    {
        public float attention;
        public int warningDays;
    }

    public struct IRSInvestigationStartedEvent
    {
        public float attention;
    }

    public struct IRSAuditStartedEvent
    {
        public float attention;
        public string targetBusinessId;
        public int auditDurationDays;
    }

    public struct IRSAuditPassedEvent
    {
        public string targetBusinessId;
    }

    public struct IRSSeizureEvent
    {
        public float fineAmount;
        public string seizedBusinessId;
        public float heatPenalty;
        public int cascadeCount;
    }

    public struct IRSAuditContestedEvent
    {
        public bool success;
    }
}
