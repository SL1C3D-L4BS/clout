using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Employees;
using Clout.Empire.Properties;
using Clout.Empire.Reputation;
using Clout.World.Police;
using Clout.Utils;

namespace Clout.Empire.Economy.Laundering
{
    /// <summary>
    /// Step 11A — Core orchestrator for all money laundering operations.
    ///
    /// Manages the 5-stage laundering pipeline:
    ///   1. PLACEMENT    — Dirty cash enters front business register (1-2 game days)
    ///   2. LAYERING     — Money moves through multiple transactions (2-5 game days)
    ///   3. INTEGRATION  — Clean money re-enters player economy (1-3 game days)
    ///   4. COOLING      — Mandatory delay before funds are spendable (method-dependent)
    ///   5. COMPLETE     — Funds deposited as clean cash
    ///
    /// Replaces the placeholder CashManager.Launder() with a full financial simulation.
    /// Players must acquire front businesses, choose laundering methods, manage throughput
    /// capacity, and avoid IRS attention.
    ///
    /// Ticks on: TransactionLedger.OnDayEnd for daily processing.
    /// Integrates: CashManager, PropertyManager, TransactionLedger, GameBalanceConfig,
    ///             EventBus, WorkerManager (Accountant role), WantedSystem.
    /// </summary>
    public class LaunderingManager : MonoBehaviour
    {
        public static LaunderingManager Instance { get; private set; }

        // ─── Config ───────────────────────────────────────────
        [Header("Laundering Methods")]
        [Tooltip("Assign all available LaunderingMethod ScriptableObjects here.")]
        public LaunderingMethod[] availableMethods;

        [Header("Pipeline Config")]
        [Tooltip("Game-day duration in real seconds (synced from GameBalanceConfig).")]
        public float gameDayDuration = 600f;

        // ─── Runtime State ────────────────────────────────────
        private readonly List<FrontBusiness> _frontBusinesses = new List<FrontBusiness>();
        private readonly List<LaunderingBatch> _activePipelines = new List<LaunderingBatch>();
        private readonly List<LaunderingRecord> _completedRecords = new List<LaunderingRecord>();

        private float _dailyCapacityUsed;
        private int _batchCounter;

        // ─── IRS System ───────────────────────────────────────
        [SerializeField] private IRSInvestigation _irsInvestigation = new IRSInvestigation();

        // ─── Velocity Tracking ────────────────────────────────
        private float[] _weeklyVolumes = new float[4];          // Rolling 4-week history
        private int _weeklyVolumeIndex;
        private float _currentWeekVolume;
        private int _currentDay;

        // ─── Events ───────────────────────────────────────────
        public event Action<LaunderingBatch> OnBatchStarted;
        public event Action<LaunderingBatch> OnBatchStageAdvanced;
        public event Action<LaunderingRecord> OnBatchCompleted;
        public event Action<SeizureResult> OnSeizureExecuted;
        public event Action OnStateChanged;

        // ─── Properties ───────────────────────────────────────
        public IReadOnlyList<FrontBusiness> FrontBusinesses => _frontBusinesses;
        public IReadOnlyList<LaunderingBatch> ActivePipelines => _activePipelines;
        public IReadOnlyList<LaunderingRecord> CompletedRecords => _completedRecords;
        public IRSInvestigation IRS => _irsInvestigation;

        public float DailyCapacityMax
        {
            get
            {
                float total = 0f;
                foreach (var front in _frontBusinesses)
                    total += front.LaunderingCapacity;
                return total;
            }
        }

        public float DailyCapacityUsed => _dailyCapacityUsed;
        public float DailyCapacityRemaining => Mathf.Max(0, DailyCapacityMax - _dailyCapacityUsed);
        public int ActiveBatchCount => _activePipelines.Count;
        public int FrontBusinessCount => _frontBusinesses.Count;

        /// <summary>Total dirty cash currently in the pipeline (not yet clean).</summary>
        public float CashInPipeline
        {
            get
            {
                float total = 0f;
                foreach (var batch in _activePipelines)
                    total += batch.dirtyAmount;
                return total;
            }
        }

        // ─── Lifecycle ────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _irsInvestigation.Initialize();
        }

        private void Start()
        {
            // Sync config
            gameDayDuration = GameBalanceConfig.Active.gameDayDuration;

            // Subscribe to day cycle
            if (TransactionLedger.Instance != null)
                TransactionLedger.Instance.OnDayEnd += ProcessDailyTick;
        }

        private void OnDestroy()
        {
            if (TransactionLedger.Instance != null)
                TransactionLedger.Instance.OnDayEnd -= ProcessDailyTick;
        }

        // ─── Front Business Registration ──────────────────────

        /// <summary>
        /// Designate a property as a laundering front business.
        /// Automatically determines business type from property type.
        /// </summary>
        public FrontBusiness RegisterFrontBusiness(Property property)
        {
            if (property == null || !property.IsOwned) return null;

            // Check if already registered
            foreach (var existing in _frontBusinesses)
            {
                if (existing.LinkedProperty == property)
                {
                    Debug.Log($"[Laundering] {property.Definition.propertyName} is already a front.");
                    return existing;
                }
            }

            // Map PropertyType to FrontBusinessType
            FrontBusinessType? frontType = MapPropertyToFrontType(property.Definition.propertyType);
            if (frontType == null)
            {
                Debug.Log($"[Laundering] {property.Definition.propertyType} cannot be a front business.");
                return null;
            }

            // Create FrontBusiness component
            FrontBusiness front = property.gameObject.AddComponent<FrontBusiness>();
            front.Initialize(frontType.Value, property);

            _frontBusinesses.Add(front);
            OnStateChanged?.Invoke();

            EventBus.Publish(new FrontBusinessRegisteredEvent
            {
                propertyId = property.Definition.propertyName,
                businessType = frontType.Value,
                dailyCapacity = front.LaunderingCapacity
            });

            Debug.Log($"[Laundering] Registered front: {property.Definition.propertyName} " +
                      $"({frontType.Value}) — capacity: ${front.LaunderingCapacity:F0}/day");

            return front;
        }

        /// <summary>
        /// Remove a front business (property sold, seized, or player choice).
        /// Active batches through this front are failed and dirty cash returned.
        /// </summary>
        public void UnregisterFrontBusiness(FrontBusiness front)
        {
            if (front == null || !_frontBusinesses.Contains(front)) return;

            // Fail active batches going through this front
            string frontId = front.BusinessId;
            CashManager cash = CashManager.Instance;

            for (int i = _activePipelines.Count - 1; i >= 0; i--)
            {
                if (_activePipelines[i].frontBusinessId == frontId)
                {
                    var batch = _activePipelines[i];

                    // Return dirty cash (minus partial fees already taken)
                    if (cash != null)
                        cash.EarnDirty(batch.dirtyAmount * 0.8f, $"Failed laundering: {frontId} closed");

                    EventBus.Publish(new LaunderingFailedEvent
                    {
                        batchId = batch.batchId,
                        reason = "Front business closed",
                        returnedAmount = batch.dirtyAmount * 0.8f
                    });

                    _activePipelines.RemoveAt(i);
                }
            }

            _frontBusinesses.Remove(front);

            // Destroy the component (not the property itself)
            if (front != null) Destroy(front);

            OnStateChanged?.Invoke();
            Debug.Log($"[Laundering] Unregistered front: {frontId}");
        }

        /// <summary>
        /// Seize a front business — removes it AND the property from player ownership.
        /// Called by IRS seizure process.
        /// </summary>
        public void SeizeFrontBusiness(FrontBusiness front)
        {
            if (front == null) return;

            Property property = front.LinkedProperty;
            UnregisterFrontBusiness(front);

            // Remove property from player ownership
            if (property != null && PropertyManager.Instance != null)
            {
                PropertyManager.Instance.SellProperty(property);
                // Note: SellProperty gives back 60% — seizure should give 0%
                // Confiscate the sell proceeds
                CashManager cash = CashManager.Instance;
                if (cash != null)
                {
                    float sellBack = property.GetTotalValue() * 0.6f;
                    cash.Confiscate(sellBack, "IRS business seizure — proceeds confiscated");
                }
            }

            EventBus.Publish(new FrontBusinessSeizedEvent
            {
                propertyId = property != null ? property.Definition.propertyName : "",
                seizedByIRS = true
            });
        }

        private FrontBusinessType? MapPropertyToFrontType(PropertyType propType)
        {
            return propType switch
            {
                PropertyType.Restaurant => FrontBusinessType.Restaurant,
                PropertyType.AutoShop => FrontBusinessType.AutoShop,
                PropertyType.Nightclub => FrontBusinessType.Nightclub,
                PropertyType.Laundromat => FrontBusinessType.Laundromat,
                PropertyType.CarWash => FrontBusinessType.CarWash,
                // Legacy fallbacks for existing property types
                PropertyType.Storefront => FrontBusinessType.Laundromat,
                PropertyType.Warehouse => FrontBusinessType.CarWash,
                _ => null  // Safehouse, Lab, Growhouse can't be fronts
            };
        }

        // ─── Start Laundering ─────────────────────────────────

        /// <summary>
        /// Start a new laundering batch. Deducts dirty cash immediately,
        /// puts it through the pipeline, and deposits clean cash on completion.
        /// Returns true if the batch was successfully started.
        /// </summary>
        public bool StartLaundering(float dirtyAmount, FrontBusiness business, LaunderingMethod method)
        {
            // ── Validation ────────────────────────────────────
            if (dirtyAmount <= 0 || business == null || method == null)
            {
                Debug.Log("[Laundering] Invalid parameters.");
                return false;
            }

            CashManager cash = CashManager.Instance;
            if (cash == null || cash.DirtyCash < dirtyAmount)
            {
                Debug.Log($"[Laundering] Insufficient dirty cash (have: ${cash?.DirtyCash:F0}, need: ${dirtyAmount:F0}).");
                return false;
            }

            // Check daily capacity
            if (!business.CanAcceptVolume(dirtyAmount))
            {
                // Accept partial if possible
                float remaining = business.RemainingCapacity;
                if (remaining <= 0)
                {
                    Debug.Log($"[Laundering] {business.BusinessName} at daily capacity.");
                    return false;
                }
                dirtyAmount = remaining;
            }

            // Check method-specific capacity
            float maxPerDay = GameBalanceConfig.Active.maxDailyLaunderPerBusiness;
            if (dirtyAmount > maxPerDay)
                dirtyAmount = maxPerDay;

            // Check method requirements
            if (method.requiresSpecificBusiness &&
                business.BusinessType != method.preferredBusinessType)
            {
                Debug.Log($"[Laundering] {method.methodName} requires {method.preferredBusinessType}.");
                return false;
            }

            // Check CLOUT rank
            var rep = FindAnyObjectByType<ReputationManager>();
            if (rep != null && method.requiredCloutRank > 0)
            {
                int currentRank = rep.CurrentRank;
                if (currentRank < method.requiredCloutRank)
                {
                    Debug.Log($"[Laundering] {method.methodName} requires CLOUT rank {method.requiredCloutRank} (current: {currentRank}).");
                    return false;
                }
            }

            // ── Execute ───────────────────────────────────────

            // Deduct dirty cash
            cash.SpendDirty(dirtyAmount, $"Laundering: {method.methodName} via {business.BusinessName}");

            // Register volume with front business
            business.AcceptLaunderingVolume(dirtyAmount);

            // Calculate fees and clean amount
            float fee = method.CalculateFee(dirtyAmount);
            float cleanAmount = method.CalculateCleanAmount(dirtyAmount);

            // Create pipeline batch
            float stageTime = method.DaysPerStage * gameDayDuration;
            var batch = new LaunderingBatch
            {
                batchId = $"LB_{_batchCounter++:D4}",
                dirtyAmount = dirtyAmount,
                estimatedCleanAmount = cleanAmount,
                feeAmount = fee,
                method = method.methodType,
                frontBusinessId = business.BusinessId,
                stage = LaunderingStage.Placement,
                stageStartTime = Time.time,
                stageEndTime = Time.time + stageTime,
                currentStageIndex = 0
            };

            _activePipelines.Add(batch);
            _dailyCapacityUsed += dirtyAmount;
            _currentWeekVolume += dirtyAmount;

            // IRS analysis
            _irsInvestigation.AnalyzeTransaction(dirtyAmount, method.methodType, business.BusinessId);

            // Events
            OnBatchStarted?.Invoke(batch);
            OnStateChanged?.Invoke();

            EventBus.Publish(new LaunderingStartedEvent
            {
                batchId = batch.batchId,
                dirtyAmount = dirtyAmount,
                estimatedCleanAmount = cleanAmount,
                feeAmount = fee,
                method = method.methodType,
                frontBusinessId = business.BusinessId
            });

            Debug.Log($"[Laundering] Started batch {batch.batchId}: ${dirtyAmount:F0} dirty → " +
                      $"~${cleanAmount:F0} clean via {method.methodName} @ {business.BusinessName} " +
                      $"(ETA: {method.TotalDays:F1} days)");

            return true;
        }

        // ─── Pipeline Processing ──────────────────────────────

        private void Update()
        {
            // Advance pipeline stages in real-time
            for (int i = _activePipelines.Count - 1; i >= 0; i--)
            {
                var batch = _activePipelines[i];

                if (Time.time >= batch.stageEndTime && batch.stage != LaunderingStage.Complete)
                {
                    batch = AdvanceBatchStage(batch);
                    _activePipelines[i] = batch;

                    if (batch.stage == LaunderingStage.Complete)
                    {
                        CompleteBatch(batch);
                        _activePipelines.RemoveAt(i);
                    }
                }
            }
        }

        private LaunderingBatch AdvanceBatchStage(LaunderingBatch batch)
        {
            // Find the method config for timing
            LaunderingMethod method = FindMethod(batch.method);
            float stageTime = method != null ? method.DaysPerStage * gameDayDuration : gameDayDuration;
            float coolingTime = method != null ? method.coolingPeriodDays * gameDayDuration : gameDayDuration;

            switch (batch.stage)
            {
                case LaunderingStage.Placement:
                    batch.stage = LaunderingStage.Layering;
                    batch.currentStageIndex = 1;
                    batch.stageStartTime = Time.time;
                    batch.stageEndTime = Time.time + stageTime;
                    break;

                case LaunderingStage.Layering:
                    batch.stage = LaunderingStage.Integration;
                    batch.currentStageIndex = 2;
                    batch.stageStartTime = Time.time;
                    batch.stageEndTime = Time.time + stageTime;
                    break;

                case LaunderingStage.Integration:
                    batch.stage = LaunderingStage.Cooling;
                    batch.currentStageIndex = 3;
                    batch.stageStartTime = Time.time;
                    batch.stageEndTime = Time.time + coolingTime;
                    break;

                case LaunderingStage.Cooling:
                    batch.stage = LaunderingStage.Complete;
                    batch.currentStageIndex = 4;
                    break;
            }

            if (batch.stage != LaunderingStage.Complete)
            {
                OnBatchStageAdvanced?.Invoke(batch);
                Debug.Log($"[Laundering] Batch {batch.batchId} → {batch.stage}");
            }

            return batch;
        }

        private void CompleteBatch(LaunderingBatch batch)
        {
            // Deposit clean cash
            CashManager cash = CashManager.Instance;
            if (cash != null)
            {
                cash.EarnClean(batch.estimatedCleanAmount,
                    $"Laundered via {batch.frontBusinessId}");
            }

            // Record completion
            var record = new LaunderingRecord
            {
                batchId = batch.batchId,
                dirtyAmount = batch.dirtyAmount,
                cleanAmount = batch.estimatedCleanAmount,
                feeAmount = batch.feeAmount,
                method = batch.method,
                frontBusinessId = batch.frontBusinessId,
                completedTime = Time.time,
                completedOnDay = _currentDay
            };

            _completedRecords.Add(record);
            if (_completedRecords.Count > 200)
                _completedRecords.RemoveAt(0);

            // Update CashManager lifetime tracking
            if (cash != null)
            {
                // Use the existing Launder tracking (adds to TotalLaundered)
                // Already tracked via EarnClean above; the LaunderingRecord is our detailed audit trail
            }

            // Events
            OnBatchCompleted?.Invoke(record);
            OnStateChanged?.Invoke();

            EventBus.Publish(new LaunderingCompletedEvent
            {
                batchId = batch.batchId,
                dirtyAmount = batch.dirtyAmount,
                cleanAmount = batch.estimatedCleanAmount,
                feeAmount = batch.feeAmount,
                method = batch.method,
                frontBusinessId = batch.frontBusinessId
            });

            Debug.Log($"[Laundering] COMPLETED batch {batch.batchId}: ${batch.dirtyAmount:F0} → " +
                      $"${batch.estimatedCleanAmount:F0} clean (fee: ${batch.feeAmount:F0})");
        }

        // ─── Daily Processing ─────────────────────────────────

        private void ProcessDailyTick()
        {
            _currentDay++;

            // Reset daily capacity
            _dailyCapacityUsed = 0f;

            // Weekly volume tracking
            if (_currentDay % 7 == 0)
            {
                _weeklyVolumeIndex = (_weeklyVolumeIndex + 1) % 4;
                _weeklyVolumes[_weeklyVolumeIndex] = _currentWeekVolume;
                _currentWeekVolume = 0f;

                // Check velocity anomaly (>15% week-over-week increase)
                float prevWeek = _weeklyVolumes[(_weeklyVolumeIndex + 3) % 4]; // Previous week
                if (prevWeek > 0 && _weeklyVolumes[_weeklyVolumeIndex] > prevWeek * 1.15f)
                {
                    float increase = (_weeklyVolumes[_weeklyVolumeIndex] / prevWeek - 1f) * 100f;
                    _irsInvestigation.AddAttention(0.05f, "weekly_velocity",
                        $"Week-over-week increase: {increase:F0}%");
                }
            }

            // Process front businesses
            foreach (var front in _frontBusinesses)
                front.ProcessDayEnd();

            // Count accountants across all front businesses
            int accountantCount = CountAccountants();
            _irsInvestigation.SetAccountantCount(accountantCount);

            // Process IRS daily
            _irsInvestigation.ProcessDaily(_currentDay, _frontBusinesses);

            // Handle IRS seizure stage
            if (_irsInvestigation.Stage == IRSInvestigationStage.Seizure)
            {
                HandleSeizure();
            }

            OnStateChanged?.Invoke();
        }

        private void HandleSeizure()
        {
            SeizureResult result = _irsInvestigation.ExecuteSeizure(_frontBusinesses);

            // Apply financial penalty
            CashManager cash = CashManager.Instance;
            if (cash != null && result.fineAmount > 0)
            {
                cash.Confiscate(result.fineAmount, "IRS seizure penalty");
            }

            // Seize the target business
            if (result.businessSeized && !string.IsNullOrEmpty(result.seizedBusinessId))
            {
                FrontBusiness target = FindFrontBusiness(result.seizedBusinessId);
                if (target != null)
                    SeizeFrontBusiness(target);
            }

            // Heat spike from criminal charges
            WantedSystem wanted = FindAnyObjectByType<WantedSystem>();
            if (wanted != null && result.heatPenalty > 0)
            {
                wanted.AddHeat(result.heatPenalty, "IRS criminal charges");
            }

            // Cascade — flag connected businesses for increased scrutiny
            foreach (string cascadeId in result.cascadeBusinessIds)
            {
                FrontBusiness cascadeFront = FindFrontBusiness(cascadeId);
                if (cascadeFront != null)
                {
                    // Boost suspicion on cascade targets
                    _irsInvestigation.AddAttention(0.05f, "cascade_investigation",
                        $"Connected business flagged: {cascadeId}");
                }
            }

            OnSeizureExecuted?.Invoke(result);
        }

        // ─── Queries ──────────────────────────────────────────

        /// <summary>Get a front business by its ID (property name).</summary>
        public FrontBusiness FindFrontBusiness(string businessId)
        {
            foreach (var front in _frontBusinesses)
            {
                if (front.BusinessId == businessId)
                    return front;
            }
            return null;
        }

        /// <summary>Get a laundering method config by type.</summary>
        public LaunderingMethod FindMethod(LaunderingMethodType type)
        {
            if (availableMethods == null) return null;
            foreach (var method in availableMethods)
            {
                if (method != null && method.methodType == type)
                    return method;
            }
            return null;
        }

        /// <summary>Get active batches for a specific front business.</summary>
        public List<LaunderingBatch> GetBatchesForBusiness(string businessId)
        {
            var result = new List<LaunderingBatch>();
            foreach (var batch in _activePipelines)
            {
                if (batch.frontBusinessId == businessId)
                    result.Add(batch);
            }
            return result;
        }

        /// <summary>Get the aggregate detection risk across all operations.</summary>
        public float GetAggregateRisk()
        {
            float risk = _irsInvestigation.Attention;
            foreach (var front in _frontBusinesses)
                risk = Mathf.Max(risk, front.SuspicionLevel);
            return Mathf.Clamp01(risk);
        }

        /// <summary>Check if a property is registered as a front business.</summary>
        public bool IsPropertyAFront(Property property)
        {
            foreach (var front in _frontBusinesses)
            {
                if (front.LinkedProperty == property)
                    return true;
            }
            return false;
        }

        /// <summary>Get the front business component for a property.</summary>
        public FrontBusiness GetFrontForProperty(Property property)
        {
            foreach (var front in _frontBusinesses)
            {
                if (front.LinkedProperty == property)
                    return front;
            }
            return null;
        }

        /// <summary>Count accountant workers assigned to front business properties.</summary>
        private int CountAccountants()
        {
            // WorkerManager tracks workers per property
            var workerMgr = FindAnyObjectByType<WorkerManager>();
            if (workerMgr == null) return 0;

            int count = 0;
            foreach (var front in _frontBusinesses)
            {
                if (front.LinkedProperty == null) continue;
                count += workerMgr.GetWorkerCountAtProperty(
                    front.LinkedProperty.Definition.propertyName,
                    EmployeeRole.Accountant);
            }
            return count;
        }

        // ─── Serialization ────────────────────────────────────

        public LaunderingManagerSaveData GetSaveData()
        {
            var frontSaves = new List<FrontBusinessSaveData>();
            foreach (var front in _frontBusinesses)
                frontSaves.Add(front.GetSaveData());

            return new LaunderingManagerSaveData
            {
                activePipelines = new List<LaunderingBatch>(_activePipelines),
                completedRecords = new List<LaunderingRecord>(_completedRecords),
                frontBusinesses = frontSaves,
                irsSaveData = _irsInvestigation.GetSaveData(),
                currentDay = _currentDay,
                batchCounter = _batchCounter,
                weeklyVolumes = (float[])_weeklyVolumes.Clone(),
                weeklyVolumeIndex = _weeklyVolumeIndex,
                currentWeekVolume = _currentWeekVolume
            };
        }

        public void LoadSaveData(LaunderingManagerSaveData data)
        {
            _activePipelines.Clear();
            _activePipelines.AddRange(data.activePipelines);
            _completedRecords.Clear();
            _completedRecords.AddRange(data.completedRecords);
            _currentDay = data.currentDay;
            _batchCounter = data.batchCounter;
            _weeklyVolumes = data.weeklyVolumes ?? new float[4];
            _weeklyVolumeIndex = data.weeklyVolumeIndex;
            _currentWeekVolume = data.currentWeekVolume;
            _irsInvestigation.LoadSaveData(data.irsSaveData);

            // Front businesses are re-registered from PropertyManager on load
        }
    }

    // ─── Save Data ────────────────────────────────────────────

    [System.Serializable]
    public struct LaunderingManagerSaveData
    {
        public List<LaunderingBatch> activePipelines;
        public List<LaunderingRecord> completedRecords;
        public List<FrontBusinessSaveData> frontBusinesses;
        public IRSSaveData irsSaveData;
        public int currentDay;
        public int batchCounter;
        public float[] weeklyVolumes;
        public int weeklyVolumeIndex;
        public float currentWeekVolume;
    }

    // ─── Laundering Events ────────────────────────────────────

    public struct LaunderingStartedEvent
    {
        public string batchId;
        public float dirtyAmount;
        public float estimatedCleanAmount;
        public float feeAmount;
        public LaunderingMethodType method;
        public string frontBusinessId;
    }

    public struct LaunderingCompletedEvent
    {
        public string batchId;
        public float dirtyAmount;
        public float cleanAmount;
        public float feeAmount;
        public LaunderingMethodType method;
        public string frontBusinessId;
    }

    public struct LaunderingFailedEvent
    {
        public string batchId;
        public string reason;
        public float returnedAmount;
    }

    public struct FrontBusinessRegisteredEvent
    {
        public string propertyId;
        public FrontBusinessType businessType;
        public float dailyCapacity;
    }

    public struct FrontBusinessSeizedEvent
    {
        public string propertyId;
        public bool seizedByIRS;
    }
}
