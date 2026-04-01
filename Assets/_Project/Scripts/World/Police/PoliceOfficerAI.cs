using UnityEngine;
using UnityEngine.AI;
using Clout.Core;
using Clout.Utils;

namespace Clout.World.Police
{
    /// <summary>
    /// Individual police officer AI — full state machine for law enforcement behavior.
    ///
    /// Spec v2.0 Section 29: Police Patrol AI
    ///   States: Patrol → Investigate → Pursue → Arrest → Combat → CallBackup → Return
    ///
    /// Officers are spawned by HeatResponseManager based on heat brackets.
    /// Behavior escalates with wanted level:
    ///   - Clean/Suspicious: patrol only, investigate sounds/reports
    ///   - Wanted: pursue on sight, attempt arrest
    ///   - Hunted: lethal force authorized, call backup
    ///   - MostWanted: shoot on sight, continuous backup
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PoliceOfficerAI : MonoBehaviour
    {
        // ─── Configuration ──────────────────────────────────────────

        [Header("Detection")]
        [Tooltip("Base detection radius for spotting criminals.")]
        public float detectionRadius = 25f;

        [Tooltip("Field of view in degrees (half-angle from forward).")]
        public float fieldOfView = 70f;

        [Tooltip("Always-detect radius regardless of FOV.")]
        public float closeDetectionRadius = 5f;

        [Tooltip("Time between detection scans.")]
        public float scanInterval = 1f;

        [Header("Patrol")]
        [Tooltip("Patrol route waypoints. If empty, generates random patrol.")]
        public Transform[] patrolWaypoints;

        [Tooltip("Patrol speed.")]
        public float patrolSpeed = 2.5f;

        [Tooltip("Dwell time at each waypoint.")]
        public float waypointDwell = 3f;

        [Tooltip("Patrol radius for random patrol (when no waypoints set).")]
        public float patrolRadius = 30f;

        [Header("Pursuit")]
        public float chaseSpeed = 5f;

        [Tooltip("Seconds of line-of-sight loss before giving up pursuit.")]
        public float pursuitTimeout = 15f;

        [Tooltip("How far the officer will chase before giving up.")]
        public float maxPursuitDistance = 80f;

        [Header("Combat")]
        public float attackDamage = 20f;
        public float attackRange = 2.5f;
        public float attackCooldown = 1.5f;

        [Tooltip("Heat threshold for lethal force. Below this, attempt arrest.")]
        public float lethalForceThreshold = 300f;

        [Header("Arrest")]
        [Tooltip("Distance to initiate arrest.")]
        public float arrestRange = 3f;

        [Tooltip("Time to complete arrest action.")]
        public float arrestDuration = 3f;

        [Header("Backup")]
        [Tooltip("Cooldown between backup calls.")]
        public float backupCallCooldown = 30f;

        // ─── State ──────────────────────────────────────────────────

        public enum PoliceState
        {
            Patrol,
            Investigate,
            Pursue,
            Arrest,
            Combat,
            CallBackup,
            Return
        }

        [HideInInspector] public PoliceState currentState = PoliceState.Patrol;

        private NavMeshAgent _agent;
        private WantedSystem _wantedSystem;

        // Patrol
        private int _currentWaypointIndex;
        private Vector3[] _generatedWaypoints;
        private float _dwellTimer;
        private Vector3 _homePosition;     // Spawn/station position to return to

        // Detection
        private Transform _target;         // Player or criminal target
        private float _scanTimer;
        private float _lastSeenTime;       // Last time target was in LOS
        private Vector3 _lastKnownPosition;
        private Vector3 _investigatePosition;

        // Combat
        private float _attackTimer;

        // Timers
        private float _stateTimer;
        private float _arrestTimer;
        private float _lastBackupCall;

        // ─── Properties ─────────────────────────────────────────────

        public Vector3 HomePosition { get => _homePosition; set => _homePosition = value; }

        // ─── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            try { gameObject.tag = "Police"; } catch { /* Tag not defined in TagManager */ }
        }

        private void Start()
        {
            _wantedSystem = FindAnyObjectByType<WantedSystem>();
            _homePosition = transform.position;

            if (patrolWaypoints == null || patrolWaypoints.Length == 0)
                GeneratePatrolWaypoints();

            _agent.speed = patrolSpeed;
        }

        private void Update()
        {
            _stateTimer += Time.deltaTime;
            _scanTimer += Time.deltaTime;

            // Continuous scanning in most states
            if (currentState != PoliceState.Arrest && _scanTimer >= scanInterval)
            {
                _scanTimer = 0f;
                ScanForCriminals();
            }

            switch (currentState)
            {
                case PoliceState.Patrol:     UpdatePatrol(); break;
                case PoliceState.Investigate: UpdateInvestigate(); break;
                case PoliceState.Pursue:      UpdatePursue(); break;
                case PoliceState.Arrest:      UpdateArrest(); break;
                case PoliceState.Combat:      UpdateCombat(); break;
                case PoliceState.CallBackup:  UpdateCallBackup(); break;
                case PoliceState.Return:      UpdateReturn(); break;
            }
        }

        // ─── Patrol ─────────────────────────────────────────────────

        private void UpdatePatrol()
        {
            _agent.speed = patrolSpeed;

            Vector3[] waypoints = patrolWaypoints != null && patrolWaypoints.Length > 0
                ? GetTransformPositions(patrolWaypoints)
                : _generatedWaypoints;

            if (waypoints == null || waypoints.Length == 0) return;

            float distToWaypoint = Vector3.Distance(transform.position, waypoints[_currentWaypointIndex]);

            if (distToWaypoint < 2f)
            {
                _dwellTimer += Time.deltaTime;
                if (_dwellTimer >= waypointDwell)
                {
                    _dwellTimer = 0f;
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
                    _agent.SetDestination(waypoints[_currentWaypointIndex]);
                }
            }
            else if (!_agent.hasPath || _agent.remainingDistance < 1f)
            {
                _agent.SetDestination(waypoints[_currentWaypointIndex]);
            }
        }

        // ─── Investigate ────────────────────────────────────────────

        private void UpdateInvestigate()
        {
            _agent.speed = patrolSpeed * 1.3f;

            if (!_agent.hasPath || _agent.remainingDistance < 1f)
                _agent.SetDestination(_investigatePosition);

            float distToInvestigate = Vector3.Distance(transform.position, _investigatePosition);

            if (distToInvestigate < 3f)
            {
                // Arrived at investigation point — look around
                _dwellTimer += Time.deltaTime;
                if (_dwellTimer >= 5f)
                {
                    // Nothing found, return to patrol
                    TransitionTo(PoliceState.Return);
                }
            }

            // If we spot the player during investigation, escalate
            if (_target != null && HasLineOfSight(_target))
            {
                float heat = GetCurrentHeat();
                if (heat >= _wantedSystem.wantedThreshold)
                {
                    TransitionTo(PoliceState.Pursue);
                }
            }

            // Timeout
            if (_stateTimer > 30f)
                TransitionTo(PoliceState.Return);
        }

        // ─── Pursue ─────────────────────────────────────────────────

        private void UpdatePursue()
        {
            _agent.speed = chaseSpeed;

            if (_target == null)
            {
                // Lost target — go to last known position
                _agent.SetDestination(_lastKnownPosition);
                if (Vector3.Distance(transform.position, _lastKnownPosition) < 3f)
                    TransitionTo(PoliceState.Investigate);
                return;
            }

            // Update tracking
            float distToTarget = Vector3.Distance(transform.position, _target.position);
            bool hasLOS = HasLineOfSight(_target);

            if (hasLOS)
            {
                _lastSeenTime = Time.time;
                _lastKnownPosition = _target.position;
                _agent.SetDestination(_target.position);

                // Notify WantedSystem that police see the player
                if (_wantedSystem != null)
                    _wantedSystem.NotifySeenByPolice();
            }

            // Close enough to engage
            if (distToTarget <= attackRange * 1.2f)
            {
                float heat = GetCurrentHeat();
                if (heat >= lethalForceThreshold)
                    TransitionTo(PoliceState.Combat);
                else
                    TransitionTo(PoliceState.Arrest);
                return;
            }

            // Lost line of sight timeout
            if (Time.time - _lastSeenTime > pursuitTimeout)
            {
                _investigatePosition = _lastKnownPosition;
                TransitionTo(PoliceState.Investigate);
                return;
            }

            // Too far from home — give up
            if (Vector3.Distance(transform.position, _homePosition) > maxPursuitDistance)
            {
                TransitionTo(PoliceState.Return);
                return;
            }

            // Call backup during extended pursuit
            if (_stateTimer > 10f && Time.time - _lastBackupCall > backupCallCooldown)
            {
                TransitionTo(PoliceState.CallBackup);
            }
        }

        // ─── Arrest ─────────────────────────────────────────────────

        private void UpdateArrest()
        {
            if (_target == null)
            {
                TransitionTo(PoliceState.Return);
                return;
            }

            float dist = Vector3.Distance(transform.position, _target.position);

            if (dist > arrestRange * 2f)
            {
                // Target fled during arrest — pursue
                TransitionTo(PoliceState.Pursue);
                return;
            }

            // Move close
            if (dist > arrestRange)
            {
                _agent.SetDestination(_target.position);
                return;
            }

            // In range — execute arrest
            _agent.ResetPath();
            transform.LookAt(new Vector3(_target.position.x, transform.position.y, _target.position.z));
            _arrestTimer += Time.deltaTime;

            if (_arrestTimer >= arrestDuration)
            {
                ExecuteArrest();
                TransitionTo(PoliceState.Return);
            }
        }

        // ─── Combat ─────────────────────────────────────────────────

        private void UpdateCombat()
        {
            if (_target == null)
            {
                TransitionTo(PoliceState.Return);
                return;
            }

            float dist = Vector3.Distance(transform.position, _target.position);

            if (dist > attackRange * 1.5f)
            {
                // Chase toward target
                _agent.speed = chaseSpeed;
                _agent.SetDestination(_target.position);
            }
            else
            {
                // In range — attack
                _agent.ResetPath();
                transform.LookAt(new Vector3(_target.position.x, transform.position.y, _target.position.z));

                _attackTimer += Time.deltaTime;
                if (_attackTimer >= attackCooldown)
                {
                    _attackTimer = 0f;
                    PerformAttack();
                }
            }

            // Update last known position
            if (HasLineOfSight(_target))
            {
                _lastSeenTime = Time.time;
                _lastKnownPosition = _target.position;
                _wantedSystem?.NotifySeenByPolice();
            }

            // Call backup periodically
            if (Time.time - _lastBackupCall > backupCallCooldown)
            {
                RequestBackup();
            }

            // Lost target
            if (Time.time - _lastSeenTime > pursuitTimeout * 0.5f)
            {
                _investigatePosition = _lastKnownPosition;
                TransitionTo(PoliceState.Investigate);
            }
        }

        // ─── Call Backup ────────────────────────────────────────────

        private void UpdateCallBackup()
        {
            RequestBackup();
            // Immediately transition back to pursuit
            TransitionTo(PoliceState.Pursue);
        }

        // ─── Return ─────────────────────────────────────────────────

        private void UpdateReturn()
        {
            _agent.speed = patrolSpeed;

            if (!_agent.hasPath || _agent.remainingDistance < 1f)
                _agent.SetDestination(_homePosition);

            if (Vector3.Distance(transform.position, _homePosition) < 5f)
            {
                _currentWaypointIndex = 0;
                TransitionTo(PoliceState.Patrol);
            }

            // Timeout — teleport back
            if (_stateTimer > 30f)
            {
                transform.position = _homePosition;
                TransitionTo(PoliceState.Patrol);
            }
        }

        // ─── Detection ──────────────────────────────────────────────

        private void ScanForCriminals()
        {
            if (_wantedSystem == null) return;

            float heat = GetCurrentHeat();

            // Only look for trouble if there IS trouble
            if (heat < _wantedSystem.suspiciousThreshold && currentState == PoliceState.Patrol)
                return;

            // Find player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.transform.position);

            // Detection radius scales with heat
            float effectiveRadius = detectionRadius * (1f + heat / _wantedSystem.maxHeat);

            if (dist > effectiveRadius) return;

            // Close detection bypasses FOV
            bool inCloseRange = dist <= closeDetectionRadius;

            if (!inCloseRange)
            {
                // FOV check
                Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToPlayer);
                if (angle > fieldOfView) return;
            }

            // Line of sight check
            if (!HasLineOfSight(player.transform)) return;

            // Target acquired
            _target = player.transform;
            _lastSeenTime = Time.time;
            _lastKnownPosition = player.transform.position;

            // Escalate based on wanted level
            if (currentState == PoliceState.Patrol || currentState == PoliceState.Return)
            {
                if (heat >= _wantedSystem.wantedThreshold)
                {
                    TransitionTo(PoliceState.Pursue);
                }
                else if (heat >= _wantedSystem.suspiciousThreshold)
                {
                    _investigatePosition = player.transform.position;
                    TransitionTo(PoliceState.Investigate);
                }
            }
        }

        private bool HasLineOfSight(Transform target)
        {
            if (target == null) return false;

            Vector3 eyePos = transform.position + Vector3.up * 1.6f;
            Vector3 targetPos = target.position + Vector3.up * 1f;
            Vector3 dir = targetPos - eyePos;

            if (Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, dir.magnitude))
            {
                return hit.transform == target || hit.transform.IsChildOf(target);
            }

            return true; // No obstruction
        }

        // ─── Actions ────────────────────────────────────────────────

        private void PerformAttack()
        {
            if (_target == null) return;

            var damageable = _target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.OnDamage(new DamageEvent
                {
                    baseDamage = attackDamage,
                    damageType = DamageType.Blunt,
                    hitPoint = _target.position,
                    hitDirection = (_target.position - transform.position).normalized
                });
            }
        }

        private void ExecuteArrest()
        {
            if (_target == null) return;

            // Publish arrest event
            EventBus.Publish(new PlayerArrestedEvent
            {
                arrestingOfficer = gameObject,
                location = transform.position,
                heatAtArrest = GetCurrentHeat(),
                wasLethalForce = false
            });

            // Confiscate — handled by listeners
            // Reset heat partially
            if (_wantedSystem != null)
                _wantedSystem.ReduceHeat(_wantedSystem.CurrentHeat * 0.7f);

            _target = null;
            Debug.Log("[Police] Arrest executed!");
        }

        private void RequestBackup()
        {
            _lastBackupCall = Time.time;

            EventBus.Publish(new PoliceBackupRequestEvent
            {
                requestingOfficer = gameObject,
                location = _lastKnownPosition,
                currentHeat = GetCurrentHeat()
            });
        }

        /// <summary>
        /// Called by HeatResponseManager to direct this officer to investigate a location.
        /// </summary>
        public void InvestigateLocation(Vector3 position)
        {
            _investigatePosition = position;
            if (currentState == PoliceState.Patrol || currentState == PoliceState.Return)
                TransitionTo(PoliceState.Investigate);
        }

        // ─── Patrol Generation ──────────────────────────────────────

        private void GeneratePatrolWaypoints()
        {
            int count = 4;
            _generatedWaypoints = new Vector3[count];
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * patrolRadius,
                    0f,
                    Mathf.Sin(angle) * patrolRadius
                );

                Vector3 candidate = _homePosition + offset;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius * 0.5f, NavMesh.AllAreas))
                    _generatedWaypoints[i] = hit.position;
                else
                    _generatedWaypoints[i] = _homePosition;
            }
        }

        private Vector3[] GetTransformPositions(Transform[] transforms)
        {
            Vector3[] positions = new Vector3[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
                positions[i] = transforms[i] != null ? transforms[i].position : _homePosition;
            return positions;
        }

        // ─── Helpers ────────────────────────────────────────────────

        private float GetCurrentHeat()
        {
            return _wantedSystem != null ? _wantedSystem.CurrentHeat : 0f;
        }

        private void TransitionTo(PoliceState newState)
        {
            currentState = newState;
            _stateTimer = 0f;
            _dwellTimer = 0f;
            _arrestTimer = 0f;
        }
    }
}
