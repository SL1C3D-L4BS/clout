using UnityEngine;
using System.Collections.Generic;
using Clout.Core;
using Clout.Utils;

namespace Clout.World.Police
{
    /// <summary>
    /// Witness system — civilians who observe crimes generate heat.
    ///
    /// Spec v2.0 Section 30: Witness & Evidence Mechanics
    ///   - Civilian NPCs within detection radius of a crime become witnesses
    ///   - Heat generated = CrimeSeverity × WitnessCount × ReliabilityFactor
    ///   - Evidence degrades over time (30s for dealing, 5min for murder)
    ///   - Player can intimidate witnesses (Fear reputation check)
    ///   - Eliminating a witness adds massive heat but removes testimony
    ///
    /// Crime severity scale:
    ///   Dealing: 20    Assault: 40    Robbery: 60    Murder: 100
    ///   Gunfire: 50    Lab Explosion: 70
    ///
    /// Subscribes to combat and empire EventBus events to detect crimes.
    /// </summary>
    public class WitnessSystem : MonoBehaviour
    {
        public static WitnessSystem Instance { get; private set; }

        [Header("Detection")]
        [Tooltip("Radius within which civilians can witness crimes.")]
        public float witnessRadius = 20f;

        [Tooltip("Layer mask for civilian NPCs.")]
        public LayerMask civilianLayer;

        [Header("Evidence Degradation")]
        [Tooltip("Base evidence decay time for minor crimes (seconds).")]
        public float minorCrimeEvidenceTime = 30f;

        [Tooltip("Base evidence decay time for major crimes (seconds).")]
        public float majorCrimeEvidenceTime = 300f;

        [Header("Heat Multipliers")]
        [Tooltip("Heat multiplier per additional witness beyond the first.")]
        public float perWitnessMultiplier = 0.3f;

        [Tooltip("Base reliability of civilian witnesses (0-1).")]
        public float baseReliability = 0.7f;

        [Header("Intimidation")]
        [Tooltip("Fear reputation threshold to intimidate witnesses.")]
        public float intimidationFearThreshold = 0.5f;

        [Tooltip("Radius for intimidation effect.")]
        public float intimidationRadius = 10f;

        // ─── State ──────────────────────────────────────────────────

        private List<CrimeReport> _activeReports = new List<CrimeReport>();
        private WantedSystem _wantedSystem;

        // ─── Properties ─────────────────────────────────────────────

        public IReadOnlyList<CrimeReport> ActiveReports => _activeReports;

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _wantedSystem = FindAnyObjectByType<WantedSystem>();

            // Subscribe to crime-generating events
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<WeaponFiredEvent>(OnWeaponFired);
            EventBus.Subscribe<DealCompletedEvent>(OnDealCompleted);
            EventBus.Subscribe<DamageTakenEvent>(OnDamageTaken);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<WeaponFiredEvent>(OnWeaponFired);
            EventBus.Unsubscribe<DealCompletedEvent>(OnDealCompleted);
            EventBus.Unsubscribe<DamageTakenEvent>(OnDamageTaken);

            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Degrade evidence over time
            for (int i = _activeReports.Count - 1; i >= 0; i--)
            {
                var report = _activeReports[i];
                report.timeRemaining -= Time.deltaTime;

                if (report.timeRemaining <= 0f)
                {
                    _activeReports.RemoveAt(i);
                }
                else
                {
                    _activeReports[i] = report;
                }
            }
        }

        // ─── Crime Reporting ────────────────────────────────────────

        /// <summary>
        /// Report a crime at a location. Scans for witnesses and generates heat.
        /// </summary>
        public void ReportCrime(Vector3 crimeLocation, CrimeType crimeType, GameObject perpetrator)
        {
            float severity = GetCrimeSeverity(crimeType);
            int witnessCount = CountWitnesses(crimeLocation);

            if (witnessCount == 0 && crimeType != CrimeType.Gunfire)
                return; // No witnesses, no heat (except gunfire which is always audible)

            // Calculate heat: severity × (1 + extraWitnesses × multiplier) × reliability
            float witnessMultiplier = 1f + Mathf.Max(0, witnessCount - 1) * perWitnessMultiplier;
            float heat = severity * witnessMultiplier * baseReliability;

            // Gunfire generates minimum heat even without visual witnesses
            if (crimeType == CrimeType.Gunfire && witnessCount == 0)
                heat = severity * 0.3f;

            // Apply heat
            if (_wantedSystem != null)
                _wantedSystem.AddHeat(heat, $"{crimeType} witnessed by {witnessCount}");

            // Create crime report for evidence tracking
            float evidenceTime = severity >= 60f ? majorCrimeEvidenceTime : minorCrimeEvidenceTime;

            var report = new CrimeReport
            {
                crimeType = crimeType,
                location = crimeLocation,
                severity = severity,
                witnessCount = witnessCount,
                heatGenerated = heat,
                timeRemaining = evidenceTime,
                timestamp = Time.time
            };

            _activeReports.Add(report);

            EventBus.Publish(new CrimeWitnessedEvent
            {
                crimeType = crimeType.ToString(),
                location = crimeLocation,
                witnessCount = witnessCount,
                heatGenerated = heat,
                severity = severity
            });

            // Alert nearby police to investigate
            var responseManager = HeatResponseManager.Instance;
            if (responseManager != null)
            {
                foreach (var officer in responseManager.ActiveOfficers)
                {
                    if (officer == null) continue;
                    float dist = Vector3.Distance(officer.transform.position, crimeLocation);
                    if (dist < witnessRadius * 2f)
                        officer.InvestigateLocation(crimeLocation);
                }
            }
        }

        // ─── Witness Detection ──────────────────────────────────────

        private int CountWitnesses(Vector3 crimeLocation)
        {
            Collider[] hits = Physics.OverlapSphere(crimeLocation, witnessRadius);
            int count = 0;

            foreach (var hit in hits)
            {
                if (hit.gameObject.CompareTag("Player")) continue;
                if (hit.gameObject.CompareTag("Police"))
                {
                    count += 3; // Police witness = triple weight
                    continue;
                }

                // Any NPC with a NavMeshAgent counts as a potential witness
                if (hit.GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
                {
                    // Line of sight check
                    Vector3 eyePos = hit.transform.position + Vector3.up * 1.5f;
                    Vector3 dir = crimeLocation - eyePos;

                    if (!Physics.Raycast(eyePos, dir.normalized, dir.magnitude - 1f))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        // ─── Intimidation ───────────────────────────────────────────

        /// <summary>
        /// Player attempts to intimidate witnesses at a location.
        /// Requires Fear reputation > threshold. Reduces witness heat contribution.
        /// Returns number of witnesses intimidated.
        /// </summary>
        public int IntimidateWitnesses(Vector3 location, float playerFearReputation)
        {
            if (playerFearReputation < intimidationFearThreshold)
                return 0;

            float successChance = Mathf.Clamp01((playerFearReputation - intimidationFearThreshold) * 2f);
            int intimidated = 0;

            // Remove recent reports near location
            for (int i = _activeReports.Count - 1; i >= 0; i--)
            {
                var report = _activeReports[i];
                float dist = Vector3.Distance(report.location, location);

                if (dist < intimidationRadius && Random.value < successChance)
                {
                    // Reduce evidence time significantly
                    report.timeRemaining *= 0.2f;
                    _activeReports[i] = report;
                    intimidated++;
                }
            }

            if (intimidated > 0)
            {
                // Intimidation itself generates a small amount of heat
                _wantedSystem?.AddHeat(5f, "witness intimidation");
            }

            return intimidated;
        }

        // ─── Crime Severity ─────────────────────────────────────────

        private float GetCrimeSeverity(CrimeType type)
        {
            return type switch
            {
                CrimeType.Dealing => 20f,
                CrimeType.Gunfire => 50f,
                CrimeType.Assault => 40f,
                CrimeType.Robbery => 60f,
                CrimeType.Murder => 100f,
                CrimeType.MurderPolice => 150f,
                CrimeType.LabExplosion => 70f,
                CrimeType.Trespassing => 15f,
                CrimeType.DrugPossession => 25f,
                _ => 10f
            };
        }

        // ─── Event Handlers ─────────────────────────────────────────

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (evt.killer == null) return;
            CrimeType type = evt.wasPolice ? CrimeType.MurderPolice : CrimeType.Murder;
            ReportCrime(evt.enemy.transform.position, type, evt.killer);
        }

        private void OnWeaponFired(WeaponFiredEvent evt)
        {
            ReportCrime(evt.position, CrimeType.Gunfire, evt.shooter);
        }

        private void OnDealCompleted(DealCompletedEvent evt)
        {
            // Only witnessed deals generate heat — use player position
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                ReportCrime(player.transform.position, CrimeType.Dealing, player);
        }

        private void OnDamageTaken(DamageTakenEvent evt)
        {
            if (evt.attacker == null) return;
            if (!evt.attacker.CompareTag("Player")) return;

            CrimeType type = evt.wasLethal ? CrimeType.Murder : CrimeType.Assault;
            ReportCrime(evt.target.transform.position, type, evt.attacker);
        }
    }

    // ─── Data Types ─────────────────────────────────────────────────

    public enum CrimeType
    {
        Dealing,
        Gunfire,
        Assault,
        Robbery,
        Murder,
        MurderPolice,
        LabExplosion,
        Trespassing,
        DrugPossession
    }

    [System.Serializable]
    public struct CrimeReport
    {
        public CrimeType crimeType;
        public Vector3 location;
        public float severity;
        public int witnessCount;
        public float heatGenerated;
        public float timeRemaining;
        public float timestamp;
    }
}
