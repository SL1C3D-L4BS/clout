using UnityEngine;
using UnityEngine.AI;
using Clout.Core;

namespace Clout.AI.Actions
{
    /// <summary>
    /// AI patrol — wander between random points within a radius.
    /// Aggression affects behavior: low = calm, high = erratic movement.
    /// </summary>
    public class AIPatrol : StateAction
    {
        private Vector3 patrolTarget;
        private float waitTimer;
        private bool hasPatrolTarget;
        private float patrolRadius = 10f;

        public override bool Execute(CharacterStateManager stateManager)
        {
            AIStateManager ai = stateManager as AIStateManager;
            if (ai == null || ai.agent == null) return false;
            if (ai.isInteracting) return false;

            if (waitTimer > 0)
            {
                waitTimer -= Time.deltaTime;
                ai.anim?.SetFloat("vertical", 0, 0.2f, Time.deltaTime);
                return false;
            }

            if (!hasPatrolTarget || ai.agent.remainingDistance < 0.5f)
            {
                Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
                randomDir += ai.transform.position;
                randomDir.y = ai.transform.position.y;

                if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
                {
                    patrolTarget = hit.position;
                    ai.agent.SetDestination(patrolTarget);
                    hasPatrolTarget = true;
                }

                float aggressionFactor = 1f - ai.aggressionLevel * 0.7f;
                waitTimer = Random.Range(2f, 5f) * aggressionFactor;
            }

            float speed = ai.agent.velocity.magnitude / Mathf.Max(0.1f, ai.agent.speed);
            ai.anim?.SetFloat("vertical", speed, 0.2f, Time.deltaTime);

            if (ai.agent.velocity.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(ai.agent.velocity.normalized);
                ai.transform.rotation = Quaternion.Slerp(
                    ai.transform.rotation, targetRot,
                    ai.aiRotationSpeed * Time.deltaTime
                );
            }

            return false;
        }
    }
}
