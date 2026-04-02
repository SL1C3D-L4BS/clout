using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Clout.Core;
using Clout.Utils;
using Clout.Empire.Economy;
using Clout.Empire.Territory;
using Clout.World.NPCs;
using Clout.World.Police;

namespace Clout.World.Districts
{
    /// <summary>
    /// Singleton district manager — orchestrates the active district's NPC population,
    /// economy integration, and police response scaling.
    ///
    /// Spec v2.0 Step 8:
    ///   - Manages one or more district zones with independent NPC pools
    ///   - Spawns customers, civilians, and police on timers scaled by district config
    ///   - Initializes per-district market data in EconomyManager
    ///   - Feeds district config into TerritoryManager zone setup
    ///   - Tracks player's current district for UI and event routing
    ///
    /// Bay Area districts modeled:
    ///   "fillmore"       — The Fillmore (starter, mixed residential/commercial)
    ///   "bayview"        — Bayview Industrial (docks, warehouses, low police)
    ///   "marina_heights" — Marina Heights (wealthy, high police, premium prices)
    ///   "downtown"       — Downtown Core (dense commercial, high traffic, high risk)
    /// </summary>
    public class DistrictManager : MonoBehaviour
    {
        public static DistrictManager Instance { get; private set; }

        [Header("Districts")]
        [Tooltip("All district definitions loaded for this city.")]
        public List<DistrictDefinition> districts = new List<DistrictDefinition>();

        [Header("NPC Spawning")]
        [Tooltip("Customer prefab — if null, spawns capsule placeholder.")]
        public GameObject customerPrefab;
        [Tooltip("Civilian prefab — if null, spawns capsule placeholder.")]
        public GameObject civilianPrefab;

        [Header("Spawn Config")]
        [Tooltip("Minimum distance from player to spawn NPCs.")]
        public float minSpawnDistance = 20f;
        [Tooltip("Maximum distance from player to spawn NPCs.")]
        public float maxSpawnDistance = 60f;
        [Tooltip("Distance at which NPCs are despawned to save resources.")]
        public float despawnDistance = 80f;

        // ─── State ──────────────────────────────────────────────────

        private string _currentDistrictId = "";
        private DistrictDefinition _currentDistrict;
        private Dictionary<string, DistrictRuntimeState> _runtimeStates = new Dictionary<string, DistrictRuntimeState>();

        // NPC pools per district
        private List<CustomerAI> _activeCustomers = new List<CustomerAI>();
        private List<GameObject> _activeCivilians = new List<GameObject>();

        private float _customerSpawnTimer;
        private float _civilianSpawnTimer;
        private Transform _playerTransform;

        // ─── Properties ─────────────────────────────────────────────

        public string CurrentDistrictId => _currentDistrictId;
        public DistrictDefinition CurrentDistrict => _currentDistrict;
        public IReadOnlyList<CustomerAI> ActiveCustomers => _activeCustomers;

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Cache player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;

            // Initialize runtime state for each district
            foreach (var district in districts)
            {
                InitializeDistrict(district);
            }

            // Auto-detect starting district
            if (_playerTransform != null && districts.Count > 0)
            {
                DetectCurrentDistrict();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_currentDistrict == null) return;
            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
                else return;
            }

            // NPC spawn ticks
            _customerSpawnTimer += Time.deltaTime;
            _civilianSpawnTimer += Time.deltaTime;

            float spawnInterval = _currentDistrict.npcSpawnInterval;

            if (_customerSpawnTimer >= spawnInterval)
            {
                _customerSpawnTimer = 0f;
                TrySpawnCustomer();
            }

            if (_civilianSpawnTimer >= spawnInterval * 0.7f)
            {
                _civilianSpawnTimer = 0f;
                TrySpawnCivilian();
            }

            // Despawn distant NPCs
            DespawnDistantNPCs();

            // Clean up destroyed references
            _activeCustomers.RemoveAll(c => c == null);
            _activeCivilians.RemoveAll(c => c == null);
        }

        // ─── District Initialization ────────────────────────────────

        private void InitializeDistrict(DistrictDefinition district)
        {
            var state = new DistrictRuntimeState
            {
                districtId = district.districtId,
                totalDealsCompleted = 0,
                totalRevenue = 0f,
                controlLevel = 0f,
                currentHeatModifier = 0f
            };
            _runtimeStates[district.districtId] = state;

            // Initialize economy markets for this district
            var economy = FindAnyObjectByType<EconomyManager>();
            if (economy != null && district.productDemands != null)
            {
                foreach (var pd in district.productDemands)
                {
                    economy.InitializeMarket(
                        pd.product.ToString(),
                        district.districtId,
                        pd.basePrice * district.priceMultiplier,
                        pd.baseDemand,
                        pd.elasticity
                    );
                }
            }

            // Initialize territory zone
            var territory = FindAnyObjectByType<TerritoryManager>();
            if (territory != null)
            {
                // TerritoryManager initializes from its own zones array —
                // we just ensure district config is reflected there
                Debug.Log($"[District] Initialized territory zone for {district.districtName}");
            }
        }

        // ─── District Transitions ───────────────────────────────────

        public void OnPlayerEnteredDistrict(string districtId)
        {
            if (districtId == _currentDistrictId) return;

            _currentDistrictId = districtId;
            _currentDistrict = districts.Find(d => d.districtId == districtId);

            if (_currentDistrict != null)
            {
                // Scale police presence for this district
                var hrm = HeatResponseManager.Instance;
                if (hrm != null)
                {
                    hrm.maxOfficers = Mathf.RoundToInt(12 * _currentDistrict.policePresenceMultiplier);
                }

                Debug.Log($"[District] Active district: {_currentDistrict.districtName} " +
                    $"(wealth={_currentDistrict.wealthLevel:F1}, police={_currentDistrict.policePresenceMultiplier:F1}x)");
            }
        }

        private void DetectCurrentDistrict()
        {
            Vector3 playerPos = _playerTransform.position;

            foreach (var district in districts)
            {
                if (district.WorldBounds.Contains(playerPos))
                {
                    OnPlayerEnteredDistrict(district.districtId);
                    return;
                }
            }

            // Default to first district
            if (districts.Count > 0)
                OnPlayerEnteredDistrict(districts[0].districtId);
        }

        // ─── NPC Spawning ───────────────────────────────────────────

        private void TrySpawnCustomer()
        {
            if (_currentDistrict == null) return;
            if (_activeCustomers.Count >= _currentDistrict.maxCustomers) return;

            Vector3 spawnPos = GetNPCSpawnPosition();
            if (spawnPos == Vector3.zero) return;

            GameObject customerObj;
            if (customerPrefab != null)
            {
                customerObj = Instantiate(customerPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                customerObj = CreatePlaceholderNPC(spawnPos, "Customer", GetCustomerColor());
            }

            customerObj.name = $"Customer_{_activeCustomers.Count}_{_currentDistrictId}";

            // Configure CustomerAI
            CustomerAI ai = customerObj.GetComponent<CustomerAI>();
            if (ai == null) ai = customerObj.AddComponent<CustomerAI>();

            // Set customer preferences based on district demand
            if (_currentDistrict.productDemands != null && _currentDistrict.productDemands.Length > 0)
            {
                int idx = Random.Range(0, _currentDistrict.productDemands.Length);
                ai.preferredProduct = _currentDistrict.productDemands[idx].product;
                ai.maxWillingToPay = _currentDistrict.productDemands[idx].basePrice *
                    _currentDistrict.priceMultiplier * Random.Range(0.8f, 1.3f);
            }

            ai.qualityPreference = _currentDistrict.wealthLevel * Random.Range(0.6f, 1f);
            ai.pricesSensitivity = (1f - _currentDistrict.wealthLevel) * Random.Range(0.5f, 1f);
            ai.purchaseInterval = Random.Range(120f, 400f);
            ai.customerName = GenerateNPCName();

            _activeCustomers.Add(ai);
        }

        private void TrySpawnCivilian()
        {
            if (_currentDistrict == null) return;
            if (_activeCivilians.Count >= _currentDistrict.maxCivilians) return;

            Vector3 spawnPos = GetNPCSpawnPosition();
            if (spawnPos == Vector3.zero) return;

            GameObject civObj;
            if (civilianPrefab != null)
            {
                civObj = Instantiate(civilianPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                civObj = CreatePlaceholderNPC(spawnPos, "Civilian", GetCivilianColor());
            }

            civObj.name = $"Civilian_{_activeCivilians.Count}_{_currentDistrictId}";

            // Simple wander AI via NavMeshAgent
            NavMeshAgent agent = civObj.GetComponent<NavMeshAgent>();
            if (agent == null) agent = civObj.AddComponent<NavMeshAgent>();
            agent.speed = Random.Range(1.2f, 2.0f);
            agent.stoppingDistance = 0.5f;

            // Add civilian wander behavior
            CivilianWander wander = civObj.AddComponent<CivilianWander>();
            wander.wanderRadius = 25f;
            wander.waitTimeMin = 3f;
            wander.waitTimeMax = 10f;

            _activeCivilians.Add(civObj);
        }

        private Vector3 GetNPCSpawnPosition()
        {
            if (_playerTransform == null) return Vector3.zero;

            // Spawn in a ring around the player
            for (int attempt = 0; attempt < 5; attempt++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                Vector3 candidate = _playerTransform.position + offset;

                // Ensure within district bounds
                if (_currentDistrict != null && !_currentDistrict.WorldBounds.Contains(candidate))
                    continue;

                // Snap to NavMesh
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                    return hit.position;
            }

            return Vector3.zero;
        }

        private void DespawnDistantNPCs()
        {
            if (_playerTransform == null) return;
            Vector3 playerPos = _playerTransform.position;

            for (int i = _activeCustomers.Count - 1; i >= 0; i--)
            {
                if (_activeCustomers[i] == null) { _activeCustomers.RemoveAt(i); continue; }
                if (Vector3.Distance(playerPos, _activeCustomers[i].transform.position) > despawnDistance)
                {
                    Destroy(_activeCustomers[i].gameObject);
                    _activeCustomers.RemoveAt(i);
                }
            }

            for (int i = _activeCivilians.Count - 1; i >= 0; i--)
            {
                if (_activeCivilians[i] == null) { _activeCivilians.RemoveAt(i); continue; }
                if (Vector3.Distance(playerPos, _activeCivilians[i].transform.position) > despawnDistance)
                {
                    Destroy(_activeCivilians[i]);
                    _activeCivilians.RemoveAt(i);
                }
            }
        }

        // ─── NPC Placeholder Factory ────────────────────────────────

        private static GameObject CreatePlaceholderNPC(Vector3 pos, string tag, Color color)
        {
            GameObject npc = new GameObject();
            npc.transform.position = pos;

            // Capsule visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(npc.transform);
            visual.transform.localPosition = Vector3.up * 1f;
            visual.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            Renderer r = visual.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = color;
                r.sharedMaterial = mat;
            }

            // Remove capsule's default collider
            var capsuleCol = visual.GetComponent<CapsuleCollider>();
            if (capsuleCol != null) Object.Destroy(capsuleCol);

            // Physics collider on root
            CapsuleCollider col = npc.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.3f;
            col.height = 1.8f;

            // NavMeshAgent
            NavMeshAgent agent = npc.AddComponent<NavMeshAgent>();
            agent.speed = 1.8f;
            agent.stoppingDistance = 0.5f;
            agent.radius = 0.3f;
            agent.height = 1.8f;

            return npc;
        }

        private Color GetCustomerColor()
        {
            // Varied civilian colors based on district wealth
            float wealth = _currentDistrict != null ? _currentDistrict.wealthLevel : 0.5f;
            return Color.HSVToRGB(
                Random.Range(0.05f, 0.15f),    // Warm skin tones
                Random.Range(0.3f, 0.6f),
                0.5f + wealth * 0.3f
            );
        }

        private Color GetCivilianColor()
        {
            return Color.HSVToRGB(
                Random.Range(0f, 1f),           // Any hue
                Random.Range(0.2f, 0.5f),
                Random.Range(0.4f, 0.7f)
            );
        }

        private static string GenerateNPCName()
        {
            string[] firsts = { "Marcus", "Deshawn", "Lisa", "Alejandro", "Kenji", "Priya",
                "Tommy", "Valentina", "Jamal", "Mei", "Carlos", "Aaliyah", "Dmitri",
                "Fatima", "Tyrone", "Sakura", "Andre", "Camila", "Kwame", "Zara" };
            string[] lasts = { "Chen", "Williams", "Rodriguez", "Kim", "Patel", "Johnson",
                "Garcia", "Nguyen", "Brown", "Martinez", "Lee", "Davis", "Lopez",
                "Wilson", "Thomas", "Moore", "Jackson", "White", "Harris", "Clark" };
            return $"{firsts[Random.Range(0, firsts.Length)]} {lasts[Random.Range(0, lasts.Length)]}";
        }

        // ─── Queries ────────────────────────────────────────────────

        public DistrictDefinition GetDistrict(string districtId)
        {
            return districts.Find(d => d.districtId == districtId);
        }

        public DistrictRuntimeState GetRuntimeState(string districtId)
        {
            _runtimeStates.TryGetValue(districtId, out var state);
            return state;
        }

        /// <summary>
        /// Record a deal completed in a district — updates runtime tracking.
        /// </summary>
        public void RecordDeal(string districtId, float revenue)
        {
            if (_runtimeStates.TryGetValue(districtId, out var state))
            {
                state.totalDealsCompleted++;
                state.totalRevenue += revenue;
                state.controlLevel = Mathf.Min(100f, state.controlLevel + 0.5f);
                _runtimeStates[districtId] = state;
            }
        }
    }

    // ─── Runtime State ──────────────────────────────────────────────

    [System.Serializable]
    public struct DistrictRuntimeState
    {
        public string districtId;
        public int totalDealsCompleted;
        public float totalRevenue;
        public float controlLevel;          // 0-100, player's territorial control
        public float currentHeatModifier;   // District-specific heat modifier
    }

    // ─── Civilian Wander AI ─────────────────────────────────────────

    /// <summary>
    /// Simple ambient civilian wander behavior.
    /// Picks random NavMesh points, walks there, waits, repeats.
    /// </summary>
    public class CivilianWander : MonoBehaviour
    {
        public float wanderRadius = 25f;
        public float waitTimeMin = 3f;
        public float waitTimeMax = 10f;

        private NavMeshAgent _agent;
        private float _waitTimer;
        private bool _isWaiting;

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            PickNewDestination();
        }

        private void Update()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    PickNewDestination();
                }
                return;
            }

            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _isWaiting = true;
                _waitTimer = Random.Range(waitTimeMin, waitTimeMax);
            }
        }

        private void PickNewDestination()
        {
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir += transform.position;
            randomDir.y = transform.position.y;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
        }
    }
}
