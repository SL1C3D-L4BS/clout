using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Economy;
using Clout.Utils;

namespace Clout.Forensics
{
    /// <summary>
    /// Step 12B — Central database of known forensic signatures.
    ///
    /// Law enforcement collects product signatures from raids, arrests, street buys,
    /// informant tips, and trash pulls. The database clusters signatures by facility
    /// origin using cosine similarity, enabling detectives to link seized product
    /// back to specific production facilities.
    ///
    /// Clustering: Signatures with cosine similarity > 0.85 are grouped into the
    /// same facility cluster. Over time, signatures degrade (evidence ages out),
    /// reducing traceability. Scrubbed product enters the database but fails to
    /// cluster with its origin.
    ///
    /// Ticks on: TransactionLedger.OnDayEnd for daily degradation.
    /// Integrates: ForensicLabAI (submits analyzed evidence), WantedSystem (links → heat),
    ///             PropertyRaidSystem (seized product → evidence), EventBus.
    /// </summary>
    public class SignatureDatabase : MonoBehaviour
    {
        public static SignatureDatabase Instance { get; private set; }

        [Header("Config")]
        [Tooltip("Cosine similarity threshold for clustering signatures to same facility.")]
        [Range(0.5f, 0.99f)]
        public float clusterThreshold = 0.85f;

        [Tooltip("Daily degradation rate applied to signature reliability.")]
        [Range(0f, 0.05f)]
        public float degradationRate = 0.005f;

        [Tooltip("Days after which signatures become unreliable (~60 days).")]
        public float maxReliableDays = 60f;

        [Tooltip("Scrubbed signature degradation multiplier (faster decay).")]
        public float scrubbedDegradationMultiplier = 3f;

        [Tooltip("Max signatures stored (FIFO eviction).")]
        public int maxSignatures = 500;

        // ─── State ────────────────────────────────────────────
        private readonly List<ForensicEntry> _entries = new List<ForensicEntry>();
        private readonly List<SignatureCluster> _clusters = new List<SignatureCluster>();
        private bool _clustersDirty = true;

        // ─── Events ───────────────────────────────────────────
        public event Action<ForensicEntry> OnSignatureRegistered;
        public event Action<SignatureCluster> OnClusterIdentified;
        public event Action<FacilityLink> OnFacilityLinked;

        // ─── Properties ───────────────────────────────────────
        public IReadOnlyList<ForensicEntry> Entries => _entries;
        public int EntryCount => _entries.Count;

        public IReadOnlyList<SignatureCluster> Clusters
        {
            get
            {
                if (_clustersDirty) RebuildClusters();
                return _clusters;
            }
        }

        // ─── Lifecycle ────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            if (TransactionLedger.Instance != null)
                TransactionLedger.Instance.OnDayEnd += ProcessDailyDegradation;
        }

        private void OnDestroy()
        {
            if (TransactionLedger.Instance != null)
                TransactionLedger.Instance.OnDayEnd -= ProcessDailyDegradation;
            if (Instance == this) Instance = null;
        }

        // ─── Registration ─────────────────────────────────────

        /// <summary>
        /// Register a signature from analyzed evidence.
        /// Called by ForensicLabAI after processing an evidence item.
        /// </summary>
        public ForensicEntry RegisterSignature(BatchSignature signature, EvidenceSource source,
            float evidenceQuality, string sourceDescription = "")
        {
            if (signature == null) return default;

            var entry = new ForensicEntry
            {
                signature = signature,
                source = source,
                evidenceQuality = Mathf.Clamp01(evidenceQuality),
                registeredTime = Time.time,
                registeredDay = TransactionLedger.Instance != null
                    ? TransactionLedger.Instance.CurrentDay : 0,
                reliability = evidenceQuality,
                sourceDescription = sourceDescription,
                isActive = true
            };

            _entries.Add(entry);
            _clustersDirty = true;

            // Cap database size (FIFO)
            if (_entries.Count > maxSignatures)
            {
                _entries.RemoveAt(0);
            }

            OnSignatureRegistered?.Invoke(entry);

            // Attempt to link to existing clusters
            AttemptClusterLink(entry);

            Debug.Log($"[Forensics] Registered signature {signature.BatchId} from {source} " +
                      $"(quality: {evidenceQuality:P0})");

            return entry;
        }

        // ─── Querying ─────────────────────────────────────────

        /// <summary>
        /// Find all signatures related to a query signature above a given threshold.
        /// Returns entries sorted by similarity (highest first).
        /// </summary>
        public List<SimilarityResult> FindRelated(BatchSignature query, float threshold = -1f)
        {
            if (query == null) return new List<SimilarityResult>();
            if (threshold < 0) threshold = clusterThreshold;

            var results = new List<SimilarityResult>();

            foreach (var entry in _entries)
            {
                if (!entry.isActive || entry.signature == null) continue;

                float raw = BatchSignature.CosineSimilarity(query, entry.signature);
                float effective = GetEffectiveSimilarity(raw, entry);

                if (effective >= threshold)
                {
                    results.Add(new SimilarityResult
                    {
                        entry = entry,
                        rawSimilarity = raw,
                        effectiveSimilarity = effective,
                        facilitySimilarity = BatchSignature.FacilitySimilarity(query, entry.signature),
                        recipeSimilarity = BatchSignature.RecipeSimilarity(query, entry.signature)
                    });
                }
            }

            results.Sort((a, b) => b.effectiveSimilarity.CompareTo(a.effectiveSimilarity));
            return results;
        }

        /// <summary>
        /// Find the most likely facility of origin for a signature.
        /// Returns the facility link with highest confidence, or null.
        /// </summary>
        public FacilityLink FindFacilityOrigin(BatchSignature query)
        {
            var related = FindRelated(query, 0.7f);
            if (related.Count == 0) return default;

            // Group by facility seed to find strongest cluster
            var facilityCounts = new Dictionary<int, float>();
            foreach (var result in related)
            {
                int seed = result.entry.signature.FacilitySeed;
                if (!facilityCounts.ContainsKey(seed))
                    facilityCounts[seed] = 0f;
                facilityCounts[seed] += result.effectiveSimilarity;
            }

            // Find strongest facility match
            int bestSeed = 0;
            float bestScore = 0f;
            foreach (var kvp in facilityCounts)
            {
                if (kvp.Value > bestScore)
                {
                    bestSeed = kvp.Key;
                    bestScore = kvp.Value;
                }
            }

            // Find the best individual entry for that facility
            SimilarityResult bestMatch = default;
            foreach (var result in related)
            {
                if (result.entry.signature.FacilitySeed == bestSeed)
                {
                    bestMatch = result;
                    break; // Already sorted by similarity
                }
            }

            var link = new FacilityLink
            {
                facilitySeed = bestSeed,
                confidence = Mathf.Clamp01(bestScore / related.Count),
                matchCount = 0,
                strongestMatch = bestMatch.effectiveSimilarity,
                productId = query.ProductId
            };

            // Count matches for this facility
            foreach (var result in related)
            {
                if (result.entry.signature.FacilitySeed == bestSeed)
                    link.matchCount++;
            }

            return link;
        }

        /// <summary>
        /// Compute effective similarity accounting for evidence degradation.
        /// </summary>
        public float GetEffectiveSimilarity(float rawSimilarity, ForensicEntry entry)
        {
            float gameDayDuration = GameBalanceConfig.Active.gameDayDuration;
            float ageDays = entry.signature.AgeDays(Time.time, gameDayDuration);

            float decay = degradationRate * ageDays;
            if (entry.signature.IsScrubbed)
                decay *= scrubbedDegradationMultiplier;

            float reliability = Mathf.Max(0f, entry.reliability - decay);
            return rawSimilarity * reliability;
        }

        // ─── Clustering ───────────────────────────────────────

        private void RebuildClusters()
        {
            _clusters.Clear();
            var assigned = new HashSet<int>();

            for (int i = 0; i < _entries.Count; i++)
            {
                if (assigned.Contains(i) || !_entries[i].isActive) continue;

                var cluster = new SignatureCluster
                {
                    facilitySeed = _entries[i].signature.FacilitySeed,
                    members = new List<int> { i },
                    productIds = new HashSet<string> { _entries[i].signature.ProductId },
                    averageQuality = _entries[i].signature.Quality,
                    confidence = _entries[i].reliability
                };

                assigned.Add(i);

                // Find all entries that cluster with this one
                for (int j = i + 1; j < _entries.Count; j++)
                {
                    if (assigned.Contains(j) || !_entries[j].isActive) continue;

                    float sim = BatchSignature.CosineSimilarity(
                        _entries[i].signature, _entries[j].signature);
                    float effective = GetEffectiveSimilarity(sim, _entries[j]);

                    if (effective >= clusterThreshold)
                    {
                        cluster.members.Add(j);
                        cluster.productIds.Add(_entries[j].signature.ProductId);
                        cluster.averageQuality += _entries[j].signature.Quality;
                        cluster.confidence = Mathf.Max(cluster.confidence, _entries[j].reliability);
                        assigned.Add(j);
                    }
                }

                if (cluster.members.Count > 0)
                    cluster.averageQuality /= cluster.members.Count;

                _clusters.Add(cluster);
            }

            _clustersDirty = false;
        }

        private void AttemptClusterLink(ForensicEntry newEntry)
        {
            // Quick check: does this entry cluster with any existing?
            foreach (var existing in _entries)
            {
                if (existing.signature == newEntry.signature) continue;
                if (!existing.isActive) continue;

                float sim = BatchSignature.CosineSimilarity(newEntry.signature, existing.signature);
                float effective = GetEffectiveSimilarity(sim, existing);

                if (effective >= clusterThreshold)
                {
                    var link = new FacilityLink
                    {
                        facilitySeed = existing.signature.FacilitySeed,
                        confidence = effective,
                        matchCount = 1,
                        strongestMatch = effective,
                        productId = newEntry.signature.ProductId
                    };

                    OnFacilityLinked?.Invoke(link);
                    _clustersDirty = true;

                    EventBus.Publish(new ForensicLinkEstablishedEvent
                    {
                        newBatchId = newEntry.signature.BatchId,
                        linkedBatchId = existing.signature.BatchId,
                        similarity = effective,
                        facilitySeed = existing.signature.FacilitySeed
                    });

                    Debug.Log($"[Forensics] LINK: {newEntry.signature.BatchId} → " +
                              $"cluster (facility seed: {existing.signature.FacilitySeed}, " +
                              $"similarity: {effective:F3})");
                    return;
                }
            }

            // No cluster match — new origin
            OnClusterIdentified?.Invoke(new SignatureCluster
            {
                facilitySeed = newEntry.signature.FacilitySeed,
                members = new List<int> { _entries.Count - 1 },
                productIds = new HashSet<string> { newEntry.signature.ProductId },
                averageQuality = newEntry.signature.Quality,
                confidence = newEntry.reliability
            });
        }

        // ─── Daily Degradation ────────────────────────────────

        private void ProcessDailyDegradation()
        {
            float gameDayDuration = GameBalanceConfig.Active.gameDayDuration;
            int removedCount = 0;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                if (!entry.isActive) continue;

                float ageDays = entry.signature.AgeDays(Time.time, gameDayDuration);

                // Deactivate signatures past max reliable age
                if (ageDays > maxReliableDays)
                {
                    entry.isActive = false;
                    _entries[i] = entry;
                    removedCount++;
                    _clustersDirty = true;
                }
            }

            if (removedCount > 0)
            {
                Debug.Log($"[Forensics] Daily degradation: {removedCount} signatures expired.");
            }
        }

        // ─── Serialization ────────────────────────────────────

        public SignatureDatabaseSaveData GetSaveData()
        {
            var entrySaves = new List<ForensicEntrySaveData>();
            foreach (var entry in _entries)
            {
                if (!entry.isActive) continue;
                entrySaves.Add(new ForensicEntrySaveData
                {
                    signature = entry.signature.GetSaveData(),
                    source = entry.source,
                    evidenceQuality = entry.evidenceQuality,
                    registeredTime = entry.registeredTime,
                    registeredDay = entry.registeredDay,
                    reliability = entry.reliability,
                    sourceDescription = entry.sourceDescription
                });
            }

            return new SignatureDatabaseSaveData
            {
                entries = entrySaves
            };
        }

        public void LoadSaveData(SignatureDatabaseSaveData data)
        {
            _entries.Clear();
            if (data.entries != null)
            {
                foreach (var save in data.entries)
                {
                    _entries.Add(new ForensicEntry
                    {
                        signature = BatchSignature.FromSaveData(save.signature),
                        source = save.source,
                        evidenceQuality = save.evidenceQuality,
                        registeredTime = save.registeredTime,
                        registeredDay = save.registeredDay,
                        reliability = save.reliability,
                        sourceDescription = save.sourceDescription,
                        isActive = true
                    });
                }
            }
            _clustersDirty = true;
        }
    }

    // ─── Data Structures ──────────────────────────────────────

    public enum EvidenceSource
    {
        RaidSeizure,        // Quality: 0.9-1.0, Processing: 1 day
        ArrestEvidence,     // Quality: 0.7-0.9, Processing: 2 days
        StreetBuy,          // Quality: 0.5-0.7, Processing: 3 days (undercover)
        InformantTip,       // Quality: 0.3-0.5, Processing: 5 days
        TrashPull,          // Quality: 0.1-0.3, Processing: 7 days
        WorkerBetrayal      // Quality: 0.8-1.0, Processing: 1 day (direct knowledge)
    }

    [Serializable]
    public struct ForensicEntry
    {
        public BatchSignature signature;
        public EvidenceSource source;
        public float evidenceQuality;
        public float registeredTime;
        public int registeredDay;
        public float reliability;
        public string sourceDescription;
        public bool isActive;
    }

    public struct SignatureCluster
    {
        public int facilitySeed;
        public List<int> members;           // Indices into SignatureDatabase._entries
        public HashSet<string> productIds;  // Unique products in this cluster
        public float averageQuality;
        public float confidence;

        public int MemberCount => members != null ? members.Count : 0;
    }

    public struct FacilityLink
    {
        public int facilitySeed;
        public float confidence;
        public int matchCount;
        public float strongestMatch;
        public string productId;
    }

    public struct SimilarityResult
    {
        public ForensicEntry entry;
        public float rawSimilarity;
        public float effectiveSimilarity;
        public float facilitySimilarity;
        public float recipeSimilarity;
    }

    // ─── Save Data ────────────────────────────────────────────

    [Serializable]
    public struct ForensicEntrySaveData
    {
        public BatchSignatureSaveData signature;
        public EvidenceSource source;
        public float evidenceQuality;
        public float registeredTime;
        public int registeredDay;
        public float reliability;
        public string sourceDescription;
    }

    [Serializable]
    public struct SignatureDatabaseSaveData
    {
        public List<ForensicEntrySaveData> entries;
    }

    // ─── Forensic Events ──────────────────────────────────────

    public struct ForensicLinkEstablishedEvent
    {
        public string newBatchId;
        public string linkedBatchId;
        public float similarity;
        public int facilitySeed;
    }

    public struct ForensicEvidenceSubmittedEvent
    {
        public string batchId;
        public EvidenceSource source;
        public float quality;
    }

    public struct ForensicAnalysisCompleteEvent
    {
        public string batchId;
        public bool facilityIdentified;
        public int facilitySeed;
        public float confidence;
    }
}
