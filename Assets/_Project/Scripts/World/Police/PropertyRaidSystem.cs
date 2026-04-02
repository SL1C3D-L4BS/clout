using UnityEngine;
using System.Collections.Generic;
using Clout.Core;
using Clout.Empire.Properties;
using Clout.Empire.Employees;
using Clout.Utils;

namespace Clout.World.Police
{
    /// <summary>
    /// Property raid system — police execute raids on player properties at high heat.
    ///
    /// Spec v2.0 Section 26-29:
    ///   Trigger: Heat 300+ (Hunted level) + property has stash + random daily check
    ///   P(raid) = (Heat/MaxHeat) × (1 - PropertySecurityLevel) × 0.1 per game-day
    ///
    /// Raid sequence:
    ///   1. EventBus alert: PropertyRaidAlertEvent (30-second warning)
    ///   2. Police squad spawns at property entrance
    ///   3. Guards engage, stash can be confiscated
    ///   4. Player can defend or flee
    ///   5. Outcome: stash loss, property damage, arrest risk
    /// </summary>
    public class PropertyRaidSystem : MonoBehaviour
    {
        public static PropertyRaidSystem Instance { get; private set; }

        [Header("Raid Config")]
        [Tooltip("Minimum heat to trigger raid consideration.")]
        public float minimumRaidHeat = 300f;

        [Tooltip("Base daily raid probability at max heat (before security modifier).")]
        [Range(0f, 0.5f)] public float baseRaidProbability = 0.1f;

        [Tooltip("Seconds of warning before raid begins.")]
        public float raidWarningTime = 30f;

        [Tooltip("Officers spawned per raid.")]
        public int raidSquadSize = 6;

        [Tooltip("Stash confiscation percentage (0-1).")]
        [Range(0f, 1f)] public float confiscationRate = 0.75f;

        [Tooltip("Property damage per raid (condition reduction).")]
        public float raidDamage = 0.15f;

        [Header("Raid Timing")]
        [Tooltip("Minimum seconds between raid attempts on same property.")]
        public float raidCooldownPerProperty = 300f;

        // ─── State ──────────────────────────────────────────────────

        private WantedSystem _wantedSystem;
        private Dictionary<Property, float> _lastRaidTime = new Dictionary<Property, float>();
        private Dictionary<Property, RaidState> _activeRaids = new Dictionary<Property, RaidState>();

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _wantedSystem = FindAnyObjectByType<WantedSystem>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Update active raids
            var completedRaids = new List<Property>();

            foreach (var kvp in _activeRaids)
            {
                var raid = kvp.Value;
                raid.timer += Time.deltaTime;
                _activeRaids[kvp.Key] = raid;

                switch (raid.phase)
                {
                    case RaidPhase.Warning:
                        if (raid.timer >= raidWarningTime)
                        {
                            raid.phase = RaidPhase.Executing;
                            raid.timer = 0f;
                            _activeRaids[kvp.Key] = raid;
                            ExecuteRaid(kvp.Key);
                        }
                        break;

                    case RaidPhase.Executing:
                        if (raid.timer >= 30f) // Raid lasts 30 seconds
                        {
                            raid.phase = RaidPhase.Complete;
                            _activeRaids[kvp.Key] = raid;
                            CompleteRaid(kvp.Key);
                            completedRaids.Add(kvp.Key);
                        }
                        break;
                }
            }

            foreach (var prop in completedRaids)
                _activeRaids.Remove(prop);
        }

        // ─── Raid Triggering ────────────────────────────────────────

        /// <summary>
        /// Called by TransactionLedger OnDayEnd to check if any property gets raided today.
        /// </summary>
        public void DailyRaidCheck()
        {
            if (_wantedSystem == null) return;
            if (_wantedSystem.CurrentHeat < minimumRaidHeat) return;

            var properties = FindObjectsByType<Property>();

            foreach (var prop in properties)
            {
                if (!prop.IsOwned) continue;
                if (prop.StashCount == 0) continue;
                if (_activeRaids.ContainsKey(prop)) continue;

                // Cooldown check
                if (_lastRaidTime.TryGetValue(prop, out float lastTime))
                {
                    if (Time.time - lastTime < raidCooldownPerProperty) continue;
                }

                // Raid probability: (heat/maxHeat) × (1 - security) × baseProbability
                float heatFactor = _wantedSystem.CurrentHeat / _wantedSystem.maxHeat;
                float securityFactor = 1f - prop.GetSecurityLevel();
                float probability = heatFactor * securityFactor * baseRaidProbability;

                if (Random.value < probability)
                {
                    InitiateRaid(prop);
                    break; // One raid per day max
                }
            }
        }

        /// <summary>
        /// Manually trigger a raid on a specific property (for testing or story events).
        /// </summary>
        public void ForceRaid(Property property)
        {
            if (!_activeRaids.ContainsKey(property))
                InitiateRaid(property);
        }

        // ─── Raid Execution ─────────────────────────────────────────

        private void InitiateRaid(Property property)
        {
            _activeRaids[property] = new RaidState
            {
                phase = RaidPhase.Warning,
                timer = 0f
            };

            // Alert the player
            EventBus.Publish(new PropertyRaidAlertEvent
            {
                propertyId = property.Definition != null ? property.Definition.propertyName : "unknown",
                warningTime = raidWarningTime,
                raidStrength = raidSquadSize
            });

            Debug.LogWarning($"[Raid] RAID INCOMING on {(property.Definition != null ? property.Definition.propertyName : "property")} in {raidWarningTime}s!");
        }

        private void ExecuteRaid(Property property)
        {
            _lastRaidTime[property] = Time.time;

            // Spawn raid squad at property
            Vector3 spawnPos = property.transform.position + property.transform.forward * 8f;

            var responseManager = HeatResponseManager.Instance;
            if (responseManager != null)
            {
                // Spawn additional officers for the raid
                for (int i = 0; i < raidSquadSize; i++)
                {
                    Vector3 offset = Random.insideUnitSphere * 3f;
                    offset.y = 0f;
                    Vector3 officerPos = spawnPos + offset;

                    if (UnityEngine.AI.NavMesh.SamplePosition(officerPos, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                        officerPos = hit.position;

                    // Officers will be spawned by HeatResponseManager via backup
                    EventBus.Publish(new PoliceBackupRequestEvent
                    {
                        requestingOfficer = gameObject,
                        location = officerPos,
                        currentHeat = _wantedSystem != null ? _wantedSystem.CurrentHeat : 300f
                    });
                }
            }

            EventBus.Publish(new PropertyRaidedEvent
            {
                propertyId = property.Definition != null ? property.Definition.propertyName : "unknown",
                productConfiscated = 0, // Filled in CompleteRaid
                cashConfiscated = 0f
            });

            Debug.LogWarning($"[Raid] RAID EXECUTING on {(property.Definition != null ? property.Definition.propertyName : "property")}!");
        }

        private void CompleteRaid(Property property)
        {
            // Confiscate stash
            int totalConfiscated = 0;
            if (Random.value < confiscationRate)
            {
                totalConfiscated = property.ConfiscateStash();
            }
            else
            {
                // Partial confiscation
                totalConfiscated = Mathf.RoundToInt(property.ConfiscateStash() * confiscationRate);
            }

            // Property damage
            // Use DegradeFromNonPayment as a proxy for structural damage
            for (int i = 0; i < Mathf.CeilToInt(raidDamage / 0.05f); i++)
                property.DegradeFromNonPayment();

            // Add heat from the raid itself
            if (_wantedSystem != null)
                _wantedSystem.AddHeat(20f, "property raided");

            EventBus.Publish(new PropertyRaidedEvent
            {
                propertyId = property.Definition != null ? property.Definition.propertyName : "unknown",
                productConfiscated = totalConfiscated,
                cashConfiscated = 0f
            });

            Debug.LogWarning($"[Raid] Raid complete. Confiscated {totalConfiscated} units. Property damaged.");
        }

        // ─── Queries ────────────────────────────────────────────────

        public bool IsPropertyUnderRaid(Property property)
        {
            return _activeRaids.ContainsKey(property);
        }
    }

    // ─── Data Types ─────────────────────────────────────────────────

    public enum RaidPhase
    {
        Warning,
        Executing,
        Complete
    }

    [System.Serializable]
    public struct RaidState
    {
        public RaidPhase phase;
        public float timer;
    }
}
