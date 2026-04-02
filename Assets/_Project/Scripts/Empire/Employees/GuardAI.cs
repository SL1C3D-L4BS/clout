using UnityEngine;
using UnityEngine.AI;
using Clout.Core;
using Clout.Empire.Properties;
using Clout.Utils;

namespace Clout.Empire.Employees
{
    /// <summary>
    /// Property security guard AI — patrols assigned property perimeter,
    /// detects and engages hostile NPCs, contributes to property security level.
    ///
    /// Behavior loop (Spec v2.0 Section 13):
    ///   Idle → Patrol → DetectThreat → Engage/Alert → ReturnToPatrol
    ///
    /// Guard effectiveness = courage × skill. Affects property security level
    /// which reduces raid success probability and deters rival faction incursions.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardAI : MonoBehaviour
    {
        // ─── Configuration ──────────────────────────────────────────

        [Header("Patrol")]
        [Tooltip("Radius around property to patrol.")]
        public float patrolRadius = 15f;

        [Tooltip("Time spent at each patrol waypoint before moving.")]
        public float waypointDwell = 4f;

        [Tooltip("Number of patrol waypoints to generate.")]
        public int waypointCount = 4;

        [Header("Detection")]
        [Tooltip("Radius to detect hostile NPCs.")]
        public float detectionRadius = 12f;

        [Tooltip("Time between threat scans.")]
        public float scanInterval = 2f;

        [Header("Combat")]
        [Tooltip("Damage per attack.")]
        public float attackDamage = 15f;

        [Tooltip("Attack interval in seconds.")]
        public float attackCooldown = 1.5f;

        [Tooltip("Range at which guard can attack.")]
        public float attackRange = 2.5f;

        // ─── State ──────────────────────────────────────────────────

        private enum GuardState
        {
            Idle,
            Patrolling,
            Investigating,
            Engaging,
            Returning,
            Fleeing,
            Resting
        }

        private GuardState _state = GuardState.Idle;
        private WorkerInstance _worker;
        private NavMeshAgent _agent;

        // Patrol
        private Vector3[] _waypoints;
        private int _currentWaypointIndex;
        private Vector3 _homePosition;

        // Combat
        private GameObject _threatTarget;
        private float _attackTimer;

        // Timers
        private float _dwellTimer;
        private float _scanTimer;
        private float _restTimer;
        private float _stateTimer;

        // ─── Initialization ─────────────────────────────────────────

        public void Initialize(WorkerInstance worker)
        {
            _worker = worker;
            _agent = GetComponent<NavMeshAgent>();
            _homePosition = worker.assignedProperty != null
                ? worker.assignedProperty.transform.position
                : transform.position;

            GeneratePatrolWaypoints();
            _state = GuardState.Idle;
        }

        // ─── Update Loop ────────────────────────────────────────────

        private void Update()
        {
            if (_worker == null || _worker.state == WorkerState.Arrested || _worker.state == WorkerState.Dead)
                return;

            _stateTimer += Time.deltaTime;
            _scanTimer += Time.deltaTime;

            // Threat scanning runs in all active states
            if (_state != GuardState.Fleeing && _state != GuardState.Resting &&
                _scanTimer >= scanInterval)
            {
                _scanTimer = 0f;
                CheckForThreats();
            }

            switch (_state)
            {
                case GuardState.Idle:
                    HandleIdle();
                    break;
                case GuardState.Patrolling:
                    HandlePatrolling();
                    break;
                case GuardState.Investigating:
                    HandleInvestigating();
                    break;
                case GuardState.Engaging:
                    HandleEngaging();
                    break;
                case GuardState.Returning:
                    HandleReturning();
                    break;
                case GuardState.Fleeing:
                    HandleFleeing();
                    break;
                case GuardState.Resting:
                    HandleResting();
                    break;
            }
        }

        // ─── State Handlers ─────────────────────────────────────────

        private void HandleIdle()
        {
            _worker.state = WorkerState.Working;
            _currentWaypointIndex = 0;

            if (_waypoints != null && _waypoints.Length > 0)
            {
                _agent.SetDestination(_waypoints[0]);
                TransitionTo(GuardState.Patrolling);
            }
        }

        private void HandlePatrolling()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                TransitionTo(GuardState.Idle);
                return;
            }

            float distToWaypoint = Vector3.Distance(transform.position, _waypoints[_currentWaypointIndex]);

            if (distToWaypoint < 2f)
            {
                // Dwell at waypoint — look around
                _dwellTimer += Time.deltaTime;

                if (_dwellTimer >= waypointDwell)
                {
                    _dwellTimer = 0f;
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                    _agent.SetDestination(_waypoints[_currentWaypointIndex]);

                    // Complete a patrol circuit = 1 shift
                    if (_currentWaypointIndex == 0)
                    {
                        _worker.shiftsCompleted++;
                        _worker.ImproveSkill();
                    }
                }
            }
        }

        private void HandleInvestigating()
        {
            if (_threatTarget == null)
            {
                TransitionTo(GuardState.Patrolling);
                _agent.SetDestination(_waypoints[_currentWaypointIndex]);
                return;
            }

            float dist = Vector3.Distance(transform.position, _threatTarget.transform.position);

            if (dist <= attackRange)
            {
                // Decide: fight or flee based on courage
                float threatLevel = 0.5f; // TODO: assess from threat's combat stats
                if (_worker.definition.WillFightDuringRaid(threatLevel))
                {
                    TransitionTo(GuardState.Engaging);
                }
                else
                {
                    _worker.state = WorkerState.Fleeing;
                    TransitionTo(GuardState.Fleeing);
                }
                return;
            }

            // Move toward threat
            _agent.SetDestination(_threatTarget.transform.position);

            // Lost target or timeout
            if (_stateTimer > 15f)
            {
                _threatTarget = null;
                TransitionTo(GuardState.Returning);
            }
        }

        private void HandleEngaging()
        {
            if (_threatTarget == null)
            {
                TransitionTo(GuardState.Returning);
                return;
            }

            float dist = Vector3.Distance(transform.position, _threatTarget.transform.position);

            if (dist > attackRange * 1.5f)
            {
                // Chase
                _agent.SetDestination(_threatTarget.transform.position);
            }
            else
            {
                // In range — attack
                _agent.ResetPath();
                transform.LookAt(_threatTarget.transform.position);

                _attackTimer += Time.deltaTime;
                if (_attackTimer >= attackCooldown)
                {
                    _attackTimer = 0f;
                    PerformAttack();
                }
            }

            // Disengage timeout — don't chase forever
            if (_stateTimer > 30f)
            {
                _threatTarget = null;
                TransitionTo(GuardState.Returning);
            }
        }

        private void HandleReturning()
        {
            if (!_agent.hasPath || _agent.remainingDistance < 1f)
            {
                _agent.SetDestination(_homePosition);
            }

            if (Vector3.Distance(transform.position, _homePosition) < 3f)
            {
                TransitionTo(GuardState.Patrolling);
                if (_waypoints != null && _waypoints.Length > 0)
                    _agent.SetDestination(_waypoints[_currentWaypointIndex]);
            }

            if (_stateTimer > 20f)
            {
                transform.position = _homePosition;
                TransitionTo(GuardState.Patrolling);
            }
        }

        private void HandleFleeing()
        {
            // Run away from threat toward home
            if (_threatTarget != null)
            {
                Vector3 fleeDir = (transform.position - _threatTarget.transform.position).normalized;
                Vector3 fleeDest = transform.position + fleeDir * 20f;

                if (NavMesh.SamplePosition(fleeDest, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    _agent.SetDestination(hit.position);
                }
            }

            // After fleeing for a bit, calm down and return
            if (_stateTimer > 10f)
            {
                _worker.state = WorkerState.Working;
                _threatTarget = null;
                TransitionTo(GuardState.Returning);
            }
        }

        private void HandleResting()
        {
            _restTimer += Time.deltaTime;
            float restDuration = WorkerManager.Instance != null
                ? WorkerManager.Instance.restDuration
                : 60f;

            if (_restTimer >= restDuration)
            {
                _worker.state = WorkerState.Idle;
                TransitionTo(GuardState.Idle);
            }
        }

        // ─── Threat Detection ───────────────────────────────────────

        private void CheckForThreats()
        {
            if (_state == GuardState.Engaging || _state == GuardState.Investigating) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                // Detect hostile NPCs — tagged "Enemy" or "Police"
                if (hit.CompareTag("Enemy") || hit.CompareTag("Police"))
                {
                    _threatTarget = hit.gameObject;
                    _agent.SetDestination(_threatTarget.transform.position);
                    TransitionTo(GuardState.Investigating);
                    return;
                }
            }
        }

        private void PerformAttack()
        {
            if (_threatTarget == null) return;

            // Apply damage — effectiveness = courage × skill
            float effectiveness = _worker.courage * _worker.skill;
            float damage = attackDamage * (0.5f + effectiveness);

            // Apply via IDamageable interface (Core/Interfaces.cs)
            var damageable = _threatTarget.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.OnDamage(new DamageEvent
                {
                    baseDamage = damage,
                    damageType = DamageType.Blunt,
                    hitPoint = _threatTarget.transform.position,
                    hitDirection = (_threatTarget.transform.position - transform.position).normalized
                });
            }

            EventBus.Publish(new WeaponFiredEvent
            {
                shooter = gameObject,
                weaponId = "guard_melee",
                position = transform.position
            });
        }

        // ─── Patrol Waypoint Generation ─────────────────────────────

        private void GeneratePatrolWaypoints()
        {
            _waypoints = new Vector3[waypointCount];
            float angleStep = 360f / waypointCount;

            for (int i = 0; i < waypointCount; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * patrolRadius,
                    0f,
                    Mathf.Sin(angle) * patrolRadius
                );

                Vector3 candidate = _homePosition + offset;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius * 0.5f, NavMesh.AllAreas))
                {
                    _waypoints[i] = hit.position;
                }
                else
                {
                    _waypoints[i] = _homePosition;
                }
            }
        }

        /// <summary>
        /// Security contribution of this guard to the property.
        /// Used by Property.GetSecurityLevel() to aggregate guard effectiveness.
        /// Range: 0.0 (useless) to 1.0 (elite).
        /// </summary>
        public float GetSecurityContribution()
        {
            if (_worker == null) return 0f;
            return (_worker.courage * 0.4f + _worker.skill * 0.4f + _worker.discretion * 0.2f);
        }

        private void TransitionTo(GuardState newState)
        {
            _state = newState;
            _stateTimer = 0f;
        }
    }
}
