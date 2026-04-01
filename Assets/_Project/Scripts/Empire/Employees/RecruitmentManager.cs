using UnityEngine;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Properties;
using Clout.Empire.Reputation;

namespace Clout.Empire.Employees
{
    /// <summary>
    /// Generates and manages the available hire pool. Recruit quality is gated by CLOUT rank.
    ///
    /// Spec v2.0 Section 13:
    ///   - Higher reputation unlocks better recruits (higher skill, lower betrayal chance)
    ///   - Pool refreshes daily with randomized candidates
    ///   - Fear reputation attracts loyal-but-low-quality thugs
    ///   - Respect reputation attracts skilled professionals
    ///   - Recruitment quality = f(respect, fear, rank)
    ///
    /// The hire pool is a rotating set of EmployeeDefinition SOs generated procedurally
    /// from base templates with stat variance applied.
    /// </summary>
    public class RecruitmentManager : MonoBehaviour
    {
        public static RecruitmentManager Instance { get; private set; }

        [Header("Pool Configuration")]
        [Tooltip("Base templates for each role. System generates variants from these.")]
        public EmployeeDefinition[] dealerTemplates;
        public EmployeeDefinition[] cookTemplates;
        public EmployeeDefinition[] guardTemplates;

        [Tooltip("Max candidates available at once.")]
        public int maxPoolSize = 6;

        [Tooltip("Stat variance applied to templates when generating recruits.")]
        [Range(0f, 0.3f)] public float statVariance = 0.15f;

        [Header("Quality Gating")]
        [Tooltip("Minimum CLOUT rank to unlock each quality tier. Index = tier (0-4).")]
        public int[] qualityTierRankReqs = { 0, 1, 2, 3, 5 };

        [Tooltip("Stat floor for each quality tier.")]
        public float[] qualityTierStatFloors = { 0.2f, 0.35f, 0.5f, 0.65f, 0.8f };

        // ─── State ──────────────────────────────────────────────────

        private List<RecruitCandidate> _hirePool = new List<RecruitCandidate>();
        private int _lastRefreshDay = -1;

        // ─── Events ─────────────────────────────────────────────────

        public event System.Action OnPoolRefreshed;

        // ─── Properties ─────────────────────────────────────────────

        public IReadOnlyList<RecruitCandidate> HirePool => _hirePool;

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Initial pool generation
            RefreshPool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─── Pool Management ────────────────────────────────────────

        /// <summary>
        /// Refresh the hire pool. Called daily or on demand.
        /// Higher CLOUT rank = access to better quality tiers.
        /// </summary>
        public void RefreshPool()
        {
            _hirePool.Clear();

            int maxTier = GetMaxUnlockedTier();
            float recruitQuality = GetRecruitmentQualityBonus();

            for (int i = 0; i < maxPoolSize; i++)
            {
                // Distribute roles roughly evenly with some randomness
                EmployeeRole role;
                float r = Random.value;
                if (r < 0.4f) role = EmployeeRole.Dealer;
                else if (r < 0.7f) role = EmployeeRole.Cook;
                else role = EmployeeRole.Guard;

                // Pick quality tier — higher tiers more likely at higher ranks
                int tier = Random.Range(0, maxTier + 1);
                float statFloor = tier < qualityTierStatFloors.Length
                    ? qualityTierStatFloors[tier]
                    : 0.2f;

                RecruitCandidate candidate = GenerateCandidate(role, statFloor, recruitQuality);
                _hirePool.Add(candidate);
            }

            OnPoolRefreshed?.Invoke();
        }

        /// <summary>
        /// Attempt to hire a candidate from the pool. Returns the WorkerInstance if successful.
        /// </summary>
        public WorkerInstance HireCandidate(int poolIndex, Property property)
        {
            if (poolIndex < 0 || poolIndex >= _hirePool.Count) return null;

            RecruitCandidate candidate = _hirePool[poolIndex];
            WorkerManager wm = WorkerManager.Instance;
            if (wm == null) return null;

            WorkerInstance worker = wm.HireWorker(candidate.definition, property, candidate.role);

            if (worker != null)
            {
                _hirePool.RemoveAt(poolIndex);
            }

            return worker;
        }

        /// <summary>
        /// Remove a candidate from the pool (declined/expired).
        /// </summary>
        public void DismissCandidate(int poolIndex)
        {
            if (poolIndex >= 0 && poolIndex < _hirePool.Count)
                _hirePool.RemoveAt(poolIndex);
        }

        // ─── Candidate Generation ───────────────────────────────────

        private RecruitCandidate GenerateCandidate(EmployeeRole role, float statFloor, float qualityBonus)
        {
            // Pick a template if available, otherwise generate from scratch
            EmployeeDefinition template = GetTemplateForRole(role);

            // Generate procedural stats
            string candidateName = GenerateName();

            float skill = ClampStat(statFloor + Random.Range(-statVariance, statVariance) + qualityBonus * 0.1f);
            float loyalty = ClampStat(0.4f + Random.Range(-statVariance, statVariance));
            float discretion = ClampStat(statFloor * 0.8f + Random.Range(-statVariance, statVariance));
            float greed = ClampStat(0.3f + Random.Range(-0.1f, 0.3f));
            float courage = ClampStat(0.4f + Random.Range(-statVariance, statVariance));
            float intelligence = ClampStat(statFloor * 0.7f + Random.Range(-statVariance, statVariance) + qualityBonus * 0.05f);
            float corruptibility = ClampStat(0.3f + Random.Range(-0.1f, 0.2f) - qualityBonus * 0.1f);
            float ambition = ClampStat(0.3f + Random.Range(-0.1f, 0.3f));

            // Apply template overrides if available
            if (template != null)
            {
                skill = ClampStat(template.skill + Random.Range(-statVariance, statVariance) + qualityBonus * 0.1f);
                loyalty = ClampStat(template.loyalty + Random.Range(-statVariance, statVariance));
                discretion = ClampStat(template.discretion + Random.Range(-statVariance, statVariance));
                greed = ClampStat(template.greed + Random.Range(-statVariance, statVariance));
                courage = ClampStat(template.courage + Random.Range(-statVariance, statVariance));
                intelligence = ClampStat(template.intelligence + Random.Range(-statVariance, statVariance));
                corruptibility = ClampStat(template.corruptibility + Random.Range(-statVariance, statVariance));
                ambition = ClampStat(template.ambition + Random.Range(-statVariance, statVariance));
                candidateName = template.employeeName;
            }

            // Hiring cost scales with quality
            float avgStat = (skill + discretion + intelligence + courage) / 4f;
            float hiringCost = 200f + avgStat * 800f;
            float dailyWage = 50f + avgStat * 150f;

            // Create a runtime SO for this candidate
            EmployeeDefinition def = ScriptableObject.CreateInstance<EmployeeDefinition>();
            def.employeeName = candidateName;
            def.role = role;
            def.skill = skill;
            def.loyalty = loyalty;
            def.discretion = discretion;
            def.greed = greed;
            def.courage = courage;
            def.intelligence = intelligence;
            def.corruptibility = corruptibility;
            def.ambition = ambition;
            def.hiringCost = hiringCost;
            def.dailyWage = dailyWage;
            def.betrayalChance = 0.01f + corruptibility * 0.02f;
            def.arrestChance = 0.02f * (1f - discretion * 0.5f);
            def.hasRecord = Random.value < 0.3f;
            def.backstory = GenerateBackstory(role, avgStat);

            return new RecruitCandidate
            {
                definition = def,
                role = role,
                qualityTier = Mathf.FloorToInt(avgStat * 4f),
                averageStat = avgStat
            };
        }

        private EmployeeDefinition GetTemplateForRole(EmployeeRole role)
        {
            EmployeeDefinition[] templates = role switch
            {
                EmployeeRole.Dealer => dealerTemplates,
                EmployeeRole.Cook => cookTemplates,
                EmployeeRole.Guard => guardTemplates,
                _ => null
            };

            if (templates != null && templates.Length > 0)
                return templates[Random.Range(0, templates.Length)];

            return null;
        }

        // ─── Tier / Quality ─────────────────────────────────────────

        private int GetMaxUnlockedTier()
        {
            var rep = FindAnyObjectByType<ReputationManager>();
            int rank = rep != null ? rep.CurrentRank : 0;

            int maxTier = 0;
            for (int i = 0; i < qualityTierRankReqs.Length; i++)
            {
                if (rank >= qualityTierRankReqs[i])
                    maxTier = i;
            }

            return maxTier;
        }

        private float GetRecruitmentQualityBonus()
        {
            var rep = FindAnyObjectByType<ReputationManager>();
            return rep != null ? rep.GetRecruitmentQuality() : 0f;
        }

        // ─── Name Generation ────────────────────────────────────────

        private static readonly string[] FirstNames =
        {
            "Marcus", "Dwayne", "Rico", "Jamal", "Tony", "Hector",
            "Slim", "Big Mike", "Lil D", "T-Bone", "Shadow", "Ghost",
            "Maria", "Rosa", "Diamond", "Jade", "Raven", "Angel",
            "Ox", "Bones", "Smoke", "Ice", "Razor", "Viper"
        };

        private static readonly string[] LastNames =
        {
            "Williams", "Jackson", "Rodriguez", "Martinez", "Thompson",
            "Garcia", "Brown", "Davis", "Wilson", "Moore",
            "Lopez", "Harris", "Clark", "Lewis", "Robinson"
        };

        private string GenerateName()
        {
            string first = FirstNames[Random.Range(0, FirstNames.Length)];
            string last = LastNames[Random.Range(0, LastNames.Length)];
            return $"{first} {last}";
        }

        private string GenerateBackstory(EmployeeRole role, float quality)
        {
            if (quality > 0.7f)
            {
                return role switch
                {
                    EmployeeRole.Dealer => "Experienced street operator with established connections.",
                    EmployeeRole.Cook => "Former chemistry student with advanced lab skills.",
                    EmployeeRole.Guard => "Ex-military with close protection training.",
                    _ => "Seasoned professional looking for steady work."
                };
            }
            else if (quality > 0.4f)
            {
                return role switch
                {
                    EmployeeRole.Dealer => "Knows the neighborhood. Reliable middleman.",
                    EmployeeRole.Cook => "Self-taught. Gets the job done.",
                    EmployeeRole.Guard => "Tough local who can handle trouble.",
                    _ => "Decent worker. Nothing special."
                };
            }
            else
            {
                return role switch
                {
                    EmployeeRole.Dealer => "New to the game. Eager but green.",
                    EmployeeRole.Cook => "Says they can cook. We'll see.",
                    EmployeeRole.Guard => "Big enough to scare people. That's about it.",
                    _ => "Desperate for work. Cheap but risky."
                };
            }
        }

        private float ClampStat(float v) => Mathf.Clamp01(v);
    }

    // ─── Recruit Candidate Data ─────────────────────────────────────

    [System.Serializable]
    public class RecruitCandidate
    {
        public EmployeeDefinition definition;
        public EmployeeRole role;
        public int qualityTier;    // 0-4
        public float averageStat;  // Quick quality reference
    }
}
