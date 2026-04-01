using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Clout.Core;
using Clout.Utils;

namespace Clout.World.Police
{
    /// <summary>
    /// Singleton heat response manager — spawns and manages police presence based on heat level.
    ///
    /// Spec v2.0 Section 26: 5-Dimensional Heat System (Phase 2 implements LocalPD dimension).
    ///
    /// Heat brackets determine police response:
    ///   0-50:   Normal patrols (2 officers)
    ///   50-150:  Increased patrols (4 officers)
    ///   150-300: Active investigation (6 officers + detective behavior)
    ///   300-450: Pursuit mode (8 officers, shoot to wound)
    ///   450-500: SWAT response (10+ officers, lethal force)
    ///
    /// Officers spawn at PoliceStation locations and patrol toward heat sources.
    /// Backup requests from officers spawn additional units at nearest station.
    /// </summary>
    public class HeatResponseManager : MonoBehaviour
    {
        public static HeatResponseManager Instance { get; private set; }

        [Header("Spawn Config")]
        [Tooltip("Maximum simultaneous police officers in the world.")]
        public int maxOfficers = 12;

        [Tooltip("Seconds between spawn evaluation ticks.")]
        public float spawnCheckInterval = 10f;

        [Tooltip("Minimum seconds between spawning new officers.")]
        public float spawnCooldown = 5f;

        [Header("Heat Brackets (officer targets per bracket)")]
        public int officersAtClean = 2;
        public int officersAtSuspicious = 4;
        public int officersAtWanted = 6;
        public int officersAtHunted = 8;
        public int officersAtMostWanted = 10;

        [Header("Officer Visual")]
        [Tooltip("Color for police capsule placeholder.")]
        public Color policeColor = new Color(0.1f, 0.2f, 0.8f);

        // ─── State ──────────────────────────────────────────────────

        private List<PoliceOfficerAI> _activeOfficers = new List<PoliceOfficerAI>();
        private List<PoliceStation> _stations = new List<PoliceStation>();
        private WantedSystem _wantedSystem;

        private float _spawnCheckTimer;
        private float _lastSpawnTime;

        // ─── Properties ─────────────────────────────────────────────

        public IReadOnlyList<PoliceOfficerAI> ActiveOfficers => _activeOfficers;
        public int OfficerCount => _activeOfficers.Count;

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _wantedSystem = FindAnyObjectByType<WantedSystem>();

            // Find all police stations in scene
            var stations = FindObjectsByType<PoliceStation>();
            _stations.AddRange(stations);

            // Subscribe to backup requests
            EventBus.Subscribe<PoliceBackupRequestEvent>(OnBackupRequested);

            // Initial patrol spawn
            SpawnInitialPatrol();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PoliceBackupRequestEvent>(OnBackupRequested);
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            _spawnCheckTimer += Time.deltaTime;

            if (_spawnCheckTimer >= spawnCheckInterval)
            {
                _spawnCheckTimer = 0f;
                EvaluateResponse();
            }

            // Clean up destroyed officers
            _activeOfficers.RemoveAll(o => o == null);
        }

        // ─── Response Evaluation ────────────────────────────────────

        private void EvaluateResponse()
        {
            if (_wantedSystem == null) return;

            int targetOfficers = GetTargetOfficerCount();
            int currentOfficers = _activeOfficers.Count;

            // Spawn more if under target
            if (currentOfficers < targetOfficers && currentOfficers < maxOfficers)
            {
                int toSpawn = Mathf.Min(targetOfficers - currentOfficers, 2); // Max 2 per tick
                for (int i = 0; i < toSpawn; i++)
                {
                    if (Time.time - _lastSpawnTime >= spawnCooldown)
                        SpawnOfficer();
                }
            }

            // Despawn excess when heat drops (officers return to station and despawn)
            if (currentOfficers > targetOfficers + 2)
            {
                DespawnExcessOfficers(currentOfficers - targetOfficers - 1);
            }

            // Direct officers to investigate heat source if active
            if (_wantedSystem.CurrentHeat >= _wantedSystem.suspiciousThreshold)
            {
                DirectOfficersToHeatSource();
            }
        }

        private int GetTargetOfficerCount()
        {
            WantedLevel level = _wantedSystem.CurrentLevel;
            return level switch
            {
                WantedLevel.MostWanted => officersAtMostWanted,
                WantedLevel.Hunted => officersAtHunted,
                WantedLevel.Wanted => officersAtWanted,
                WantedLevel.Suspicious => officersAtSuspicious,
                _ => officersAtClean
            };
        }

        // ─── Spawning ───────────────────────────────────────────────

        private void SpawnInitialPatrol()
        {
            for (int i = 0; i < officersAtClean; i++)
                SpawnOfficer();
        }

        private void SpawnOfficer()
        {
            Vector3 spawnPos = GetSpawnPosition();

            GameObject officerObj = new GameObject($"Police_Officer_{_activeOfficers.Count}");
            officerObj.tag = "Police";
            officerObj.transform.position = spawnPos;

            // NavMeshAgent
            NavMeshAgent agent = officerObj.AddComponent<NavMeshAgent>();
            agent.speed = 2.5f;
            agent.stoppingDistance = 1.5f;
            agent.radius = 0.3f;
            agent.height = 1.8f;

            // Capsule visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(officerObj.transform);
            visual.transform.localPosition = Vector3.up * 1f;
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = policeColor;

            // Remove capsule's default collider
            var capsuleCol = visual.GetComponent<CapsuleCollider>();
            if (capsuleCol != null) Object.Destroy(capsuleCol);

            // Physics collider on root
            CapsuleCollider col = officerObj.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.35f;
            col.height = 2f;

            // Police AI
            PoliceOfficerAI ai = officerObj.AddComponent<PoliceOfficerAI>();
            ai.HomePosition = spawnPos;

            // Scale lethality with heat
            if (_wantedSystem != null && _wantedSystem.CurrentHeat >= _wantedSystem.huntedThreshold)
            {
                ai.lethalForceThreshold = _wantedSystem.wantedThreshold; // Lower threshold = more aggressive
                ai.attackDamage = 30f;
            }

            _activeOfficers.Add(ai);
            _lastSpawnTime = Time.time;

            EventBus.Publish(new PoliceSpawnedEvent
            {
                officerObject = officerObj,
                spawnPosition = spawnPos,
                totalOfficers = _activeOfficers.Count
            });
        }

        private void DespawnExcessOfficers(int count)
        {
            for (int i = 0; i < count && _activeOfficers.Count > officersAtClean; i++)
            {
                // Despawn officers in Return or Patrol state (not actively engaged)
                for (int j = _activeOfficers.Count - 1; j >= 0; j--)
                {
                    var officer = _activeOfficers[j];
                    if (officer == null) continue;

                    if (officer.currentState == PoliceOfficerAI.PoliceState.Patrol ||
                        officer.currentState == PoliceOfficerAI.PoliceState.Return)
                    {
                        _activeOfficers.RemoveAt(j);
                        Destroy(officer.gameObject);
                        break;
                    }
                }
            }
        }

        private Vector3 GetSpawnPosition()
        {
            // Spawn at nearest station to player
            if (_stations.Count > 0)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Vector3 refPos = player != null ? player.transform.position : Vector3.zero;

                PoliceStation nearest = null;
                float nearestDist = float.MaxValue;

                foreach (var station in _stations)
                {
                    if (station == null) continue;
                    float dist = Vector3.Distance(refPos, station.SpawnPoint);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = station;
                    }
                }

                if (nearest != null)
                    return nearest.SpawnPoint;
            }

            // Fallback — spawn at edge of scene
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 edgePos = new Vector3(Mathf.Cos(angle) * 60f, 0f, Mathf.Sin(angle) * 60f);

            if (NavMesh.SamplePosition(edgePos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
                return hit.position;

            return edgePos;
        }

        // ─── Direction ──────────────────────────────────────────────

        private void DirectOfficersToHeatSource()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            Vector3 heatSource = player.transform.position;

            foreach (var officer in _activeOfficers)
            {
                if (officer == null) continue;

                if (officer.currentState == PoliceOfficerAI.PoliceState.Patrol)
                {
                    officer.InvestigateLocation(heatSource + Random.insideUnitSphere * 10f);
                }
            }
        }

        // ─── Event Handlers ─────────────────────────────────────────

        private void OnBackupRequested(PoliceBackupRequestEvent evt)
        {
            if (_activeOfficers.Count < maxOfficers)
            {
                int reinforcements = 2;
                if (evt.currentHeat >= _wantedSystem?.huntedThreshold)
                    reinforcements = 4;

                for (int i = 0; i < reinforcements && _activeOfficers.Count < maxOfficers; i++)
                    SpawnOfficer();

                Debug.Log($"[HeatResponse] Backup dispatched: +{reinforcements} officers. Total: {_activeOfficers.Count}");
            }
        }

        // ─── Queries ────────────────────────────────────────────────

        /// <summary>
        /// Get all officers currently in pursuit or combat state.
        /// </summary>
        public List<PoliceOfficerAI> GetActivelyEngagedOfficers()
        {
            var result = new List<PoliceOfficerAI>();
            foreach (var officer in _activeOfficers)
            {
                if (officer == null) continue;
                if (officer.currentState == PoliceOfficerAI.PoliceState.Pursue ||
                    officer.currentState == PoliceOfficerAI.PoliceState.Combat ||
                    officer.currentState == PoliceOfficerAI.PoliceState.Arrest)
                {
                    result.Add(officer);
                }
            }
            return result;
        }
    }
}
