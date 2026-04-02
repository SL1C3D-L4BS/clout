using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Economy;
using Clout.World.Police;
using Clout.Utils;

namespace Clout.Forensics
{
    /// <summary>
    /// Step 12C — AI-driven forensic analysis lab.
    ///
    /// Processes seized evidence through a queue-based analysis pipeline.
    /// Each evidence item has a quality score affecting analysis confidence,
    /// and a processing time based on source type. Once analyzed, the signature
    /// is registered in SignatureDatabase and cluster-matched to existing evidence.
    ///
    /// When a facility link is established with sufficient confidence, the system
    /// generates heat against the linked property and publishes events for the
    /// investigation system (Step 15) to consume.
    ///
    /// Processing capacity: 1 item/day base (upgradeable by difficulty).
    /// Evidence quality scales confidence: low quality = weak links, insufficient for warrants.
    ///
    /// Subscribes to: PropertyRaidedEvent, WorkerArrestedEvent (automatic evidence intake).
    /// Ticks on: TransactionLedger.OnDayEnd for daily processing.
    /// </summary>
    public class ForensicLabAI : MonoBehaviour
    {
        public static ForensicLabAI Instance { get; private set; }

        [Header("Lab Config")]
        [Tooltip("Max evidence items processed per game day.")]
        public int dailyProcessingCapacity = 1;

        [Tooltip("Minimum confidence to generate a facility link.")]
        [Range(0.3f, 0.95f)]
        public float linkConfidenceThreshold = 0.6f;

        [Tooltip("Heat generated per confirmed facility link.")]
        public float heatPerLink = 25f;

        [Tooltip("Max queued evidence items.")]
        public int maxQueueSize = 20;

        // ─── State ────────────────────────────────────────────
        private readonly Queue<EvidenceItem> _evidenceQueue = new Queue<EvidenceItem>();
        private EvidenceItem _activeAnalysis;
        private bool _isProcessing;
        private float _analysisProgress;
        private int _itemsProcessedToday;
        private int _totalItemsProcessed;
        private int _totalLinksEstablished;

        // ─── Completed Results ────────────────────────────────
        private readonly List<ForensicResult> _completedResults = new List<ForensicResult>();

        // ─── Events ───────────────────────────────────────────
        public event Action<EvidenceItem> OnEvidenceSubmitted;
        public event Action<ForensicResult> OnAnalysisComplete;
        public event Action<FacilityLink> OnFacilityLinkConfirmed;

        // ─── Properties ───────────────────────────────────────
        public int QueuedCount => _evidenceQueue.Count;
        public bool IsProcessing => _isProcessing;
        public float AnalysisProgress => _analysisProgress;
        public EvidenceItem ActiveAnalysis => _activeAnalysis;
        public int TotalProcessed => _totalItemsProcessed;
        public int TotalLinks => _totalLinksEstablished;
        public IReadOnlyList<ForensicResult> CompletedResults => _completedResults;

        // ─── Processing Times by Source ───────────────────────
        private static readonly Dictionary<EvidenceSource, int> PROCESSING_DAYS = new()
        {
            { EvidenceSource.RaidSeizure, 1 },
            { EvidenceSource.ArrestEvidence, 2 },
            { EvidenceSource.StreetBuy, 3 },
            { EvidenceSource.InformantTip, 5 },
            { EvidenceSource.TrashPull, 7 },
            { EvidenceSource.WorkerBetrayal, 1 }
        };

        // ─── Quality Ranges by Source ─────────────────────────
        private static readonly Dictionary<EvidenceSource, Vector2> QUALITY_RANGES = new()
        {
            { EvidenceSource.RaidSeizure, new Vector2(0.9f, 1.0f) },
            { EvidenceSource.ArrestEvidence, new Vector2(0.7f, 0.9f) },
            { EvidenceSource.StreetBuy, new Vector2(0.5f, 0.7f) },
            { EvidenceSource.InformantTip, new Vector2(0.3f, 0.5f) },
            { EvidenceSource.TrashPull, new Vector2(0.1f, 0.3f) },
            { EvidenceSource.WorkerBetrayal, new Vector2(0.8f, 1.0f) }
        };

        // ─── Lifecycle ────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to day cycle
            if (TransactionLedger.Instance != null)
                TransactionLedger.Instance.OnDayEnd += ProcessDailyAnalysis;

            // Subscribe to events that automatically generate evidence
            EventBus.Subscribe<PropertyRaidedEvent>(OnPropertyRaided);
            EventBus.Subscribe<WorkerArrestedEvent>(OnWorkerArrested);
        }

        private void OnDestroy()
        {
            if (TransactionLedger.Instance != null)
                TransactionLedger.Instance.OnDayEnd -= ProcessDailyAnalysis;

            EventBus.Unsubscribe<PropertyRaidedEvent>(OnPropertyRaided);
            EventBus.Unsubscribe<WorkerArrestedEvent>(OnWorkerArrested);

            if (Instance == this) Instance = null;
        }

        // ─── Evidence Submission ──────────────────────────────

        /// <summary>
        /// Submit evidence for forensic analysis. Enters the processing queue.
        /// Returns true if accepted, false if queue is full.
        /// </summary>
        public bool SubmitEvidence(BatchSignature signature, EvidenceSource source,
            string description = "")
        {
            if (signature == null) return false;
            if (_evidenceQueue.Count >= maxQueueSize)
            {
                Debug.Log("[ForensicLab] Evidence queue full — item rejected.");
                return false;
            }

            // Determine quality from source type
            var qualityRange = QUALITY_RANGES.ContainsKey(source)
                ? QUALITY_RANGES[source]
                : new Vector2(0.5f, 0.7f);
            float quality = UnityEngine.Random.Range(qualityRange.x, qualityRange.y);

            int processingDays = PROCESSING_DAYS.ContainsKey(source)
                ? PROCESSING_DAYS[source] : 3;

            var item = new EvidenceItem
            {
                signature = signature,
                source = source,
                quality = quality,
                processingDaysRequired = processingDays,
                processingDaysElapsed = 0,
                submittedTime = Time.time,
                description = description
            };

            _evidenceQueue.Enqueue(item);
            OnEvidenceSubmitted?.Invoke(item);

            EventBus.Publish(new ForensicEvidenceSubmittedEvent
            {
                batchId = signature.BatchId,
                source = source,
                quality = quality
            });

            Debug.Log($"[ForensicLab] Evidence submitted: {signature.BatchId} from {source} " +
                      $"(quality: {quality:P0}, queue: {_evidenceQueue.Count})");

            return true;
        }

        /// <summary>
        /// Submit evidence without an existing signature — generates one from parameters.
        /// Used by raid system where stash doesn't carry explicit signatures yet.
        /// </summary>
        public bool SubmitRaidEvidence(string propertyId, string productId,
            int quantitySeized, EvidenceSource source = EvidenceSource.RaidSeizure)
        {
            // Generate a signature based on the property (facility seed)
            var sig = BatchSignature.GenerateTest(propertyId, productId);
            return SubmitEvidence(sig, source,
                $"Seized {quantitySeized}x {productId} from {propertyId}");
        }

        // ─── Automatic Evidence Intake ────────────────────────

        private void OnPropertyRaided(PropertyRaidedEvent evt)
        {
            if (evt.productConfiscated > 0)
            {
                SubmitRaidEvidence(evt.propertyId, "seized_product",
                    evt.productConfiscated, EvidenceSource.RaidSeizure);
            }
        }

        private void OnWorkerArrested(WorkerArrestedEvent evt)
        {
            if (evt.knewCriticalInfo)
            {
                // Arrested worker with knowledge = evidence from betrayal
                var sig = BatchSignature.GenerateTest(evt.assignedPropertyId,
                    "worker_knowledge");
                SubmitEvidence(sig, EvidenceSource.WorkerBetrayal,
                    $"Worker {evt.workerName} arrested at {evt.assignedPropertyId}");
            }
        }

        // ─── Daily Processing ─────────────────────────────────

        private void ProcessDailyAnalysis()
        {
            _itemsProcessedToday = 0;

            // Advance current analysis
            if (_isProcessing)
            {
                _activeAnalysis.processingDaysElapsed++;
                _analysisProgress = _activeAnalysis.processingDaysRequired > 0
                    ? (float)_activeAnalysis.processingDaysElapsed / _activeAnalysis.processingDaysRequired
                    : 1f;

                if (_activeAnalysis.processingDaysElapsed >= _activeAnalysis.processingDaysRequired)
                {
                    CompleteAnalysis();
                }
            }

            // Start new analyses up to daily capacity
            while (_itemsProcessedToday < dailyProcessingCapacity &&
                   !_isProcessing &&
                   _evidenceQueue.Count > 0)
            {
                StartNextAnalysis();
            }
        }

        private void StartNextAnalysis()
        {
            if (_evidenceQueue.Count == 0) return;

            _activeAnalysis = _evidenceQueue.Dequeue();
            _isProcessing = true;
            _analysisProgress = 0f;

            Debug.Log($"[ForensicLab] Started analysis: {_activeAnalysis.signature.BatchId} " +
                      $"(ETA: {_activeAnalysis.processingDaysRequired} days)");
        }

        private void CompleteAnalysis()
        {
            _isProcessing = false;
            _itemsProcessedToday++;
            _totalItemsProcessed++;

            BatchSignature sig = _activeAnalysis.signature;
            SignatureDatabase db = SignatureDatabase.Instance;

            var result = new ForensicResult
            {
                batchId = sig.BatchId,
                productId = sig.ProductId,
                source = _activeAnalysis.source,
                quality = _activeAnalysis.quality,
                analysisConfidence = _activeAnalysis.quality,
                facilityIdentified = false,
                facilitySeed = 0,
                facilityConfidence = 0f
            };

            if (db != null)
            {
                // Register in database
                db.RegisterSignature(sig, _activeAnalysis.source,
                    _activeAnalysis.quality, _activeAnalysis.description);

                // Attempt facility identification
                FacilityLink link = db.FindFacilityOrigin(sig);
                if (link.confidence >= linkConfidenceThreshold)
                {
                    result.facilityIdentified = true;
                    result.facilitySeed = link.facilitySeed;
                    result.facilityConfidence = link.confidence;

                    _totalLinksEstablished++;

                    // Generate heat against linked facility
                    WantedSystem wanted = FindAnyObjectByType<WantedSystem>();
                    if (wanted != null)
                    {
                        float heatAmount = heatPerLink * link.confidence;
                        wanted.AddHeat(heatAmount,
                            $"Forensic link: product traced to facility (confidence: {link.confidence:P0})");
                    }

                    OnFacilityLinkConfirmed?.Invoke(link);

                    Debug.Log($"[ForensicLab] FACILITY IDENTIFIED: seed {link.facilitySeed} " +
                              $"(confidence: {link.confidence:P0}, matches: {link.matchCount})");
                }
            }

            _completedResults.Add(result);
            if (_completedResults.Count > 50)
                _completedResults.RemoveAt(0);

            OnAnalysisComplete?.Invoke(result);

            EventBus.Publish(new ForensicAnalysisCompleteEvent
            {
                batchId = sig.BatchId,
                facilityIdentified = result.facilityIdentified,
                facilitySeed = result.facilitySeed,
                confidence = result.facilityConfidence
            });

            _analysisProgress = 1f;
        }

        // ─── Queries ──────────────────────────────────────────

        /// <summary>Get all pending evidence items in queue.</summary>
        public List<EvidenceItem> GetQueuedEvidence()
        {
            return new List<EvidenceItem>(_evidenceQueue);
        }

        /// <summary>Get total heat generated from all forensic links.</summary>
        public float GetTotalForensicHeat()
        {
            return _totalLinksEstablished * heatPerLink;
        }

        // ─── Serialization ────────────────────────────────────

        public ForensicLabSaveData GetSaveData()
        {
            var queueList = new List<EvidenceItemSaveData>();
            foreach (var item in _evidenceQueue)
            {
                queueList.Add(new EvidenceItemSaveData
                {
                    signature = item.signature.GetSaveData(),
                    source = item.source,
                    quality = item.quality,
                    processingDaysRequired = item.processingDaysRequired,
                    processingDaysElapsed = item.processingDaysElapsed,
                    description = item.description
                });
            }

            return new ForensicLabSaveData
            {
                evidenceQueue = queueList,
                totalItemsProcessed = _totalItemsProcessed,
                totalLinksEstablished = _totalLinksEstablished,
                isProcessing = _isProcessing,
                activeAnalysis = _isProcessing
                    ? new EvidenceItemSaveData
                    {
                        signature = _activeAnalysis.signature.GetSaveData(),
                        source = _activeAnalysis.source,
                        quality = _activeAnalysis.quality,
                        processingDaysRequired = _activeAnalysis.processingDaysRequired,
                        processingDaysElapsed = _activeAnalysis.processingDaysElapsed,
                        description = _activeAnalysis.description
                    }
                    : default
            };
        }

        public void LoadSaveData(ForensicLabSaveData data)
        {
            _evidenceQueue.Clear();
            if (data.evidenceQueue != null)
            {
                foreach (var save in data.evidenceQueue)
                {
                    _evidenceQueue.Enqueue(new EvidenceItem
                    {
                        signature = BatchSignature.FromSaveData(save.signature),
                        source = save.source,
                        quality = save.quality,
                        processingDaysRequired = save.processingDaysRequired,
                        processingDaysElapsed = save.processingDaysElapsed,
                        description = save.description
                    });
                }
            }

            _totalItemsProcessed = data.totalItemsProcessed;
            _totalLinksEstablished = data.totalLinksEstablished;

            if (data.isProcessing)
            {
                var active = data.activeAnalysis;
                _activeAnalysis = new EvidenceItem
                {
                    signature = BatchSignature.FromSaveData(active.signature),
                    source = active.source,
                    quality = active.quality,
                    processingDaysRequired = active.processingDaysRequired,
                    processingDaysElapsed = active.processingDaysElapsed,
                    description = active.description
                };
                _isProcessing = true;
                _analysisProgress = active.processingDaysRequired > 0
                    ? (float)active.processingDaysElapsed / active.processingDaysRequired : 0f;
            }
        }
    }

    // ─── Data Structures ──────────────────────────────────────

    [Serializable]
    public struct EvidenceItem
    {
        public BatchSignature signature;
        public EvidenceSource source;
        public float quality;
        public int processingDaysRequired;
        public int processingDaysElapsed;
        public float submittedTime;
        public string description;

        public float Progress => processingDaysRequired > 0
            ? Mathf.Clamp01((float)processingDaysElapsed / processingDaysRequired) : 0f;
    }

    public struct ForensicResult
    {
        public string batchId;
        public string productId;
        public EvidenceSource source;
        public float quality;
        public float analysisConfidence;
        public bool facilityIdentified;
        public int facilitySeed;
        public float facilityConfidence;
    }

    // ─── Save Data ────────────────────────────────────────────

    [Serializable]
    public struct EvidenceItemSaveData
    {
        public BatchSignatureSaveData signature;
        public EvidenceSource source;
        public float quality;
        public int processingDaysRequired;
        public int processingDaysElapsed;
        public string description;
    }

    [Serializable]
    public struct ForensicLabSaveData
    {
        public List<EvidenceItemSaveData> evidenceQueue;
        public int totalItemsProcessed;
        public int totalLinksEstablished;
        public bool isProcessing;
        public EvidenceItemSaveData activeAnalysis;
    }
}
