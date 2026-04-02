using UnityEngine;
using Clout.Core;

namespace Clout.AI.Actions
{
    /// <summary>
    /// AI chase — pursue target using NavMeshAgent.
    /// When in attack range and cooldown ready, initiate attack.
    /// Aggression increases chase speed and reduces attack cooldown.
    /// </summary>
    public class AIChaseTarget : StateAction
    {
        public override bool Execute(CharacterStateManager stateManager)
        {
            AIStateManager ai = stateManager as AIStateManager;
            if (ai == null || ai.currentTarget == null || ai.agent == null) return false;
            if (ai.isInteracting) return false;

            float distToTarget = Vector3.Distance(
                ai.transform.position,
                ai.currentTarget.transform.position
            );

            ai.agent.SetDestination(ai.currentTarget.transform.position);

            float aggressiveSpeed = ai.chaseSpeed * (1f + ai.aggressionLevel * 0.3f);
            ai.agent.speed = aggressiveSpeed;

            float speed = ai.agent.velocity.magnitude / Mathf.Max(0.1f, ai.agent.speed);
            ai.anim?.SetFloat("vertical", Mathf.Clamp01(speed), 0.1f, Time.deltaTime);

            Vector3 dirToTarget = ai.currentTarget.transform.position - ai.transform.position;
            dirToTarget.y = 0;
            if (dirToTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
                ai.transform.rotation = Quaternion.Slerp(
                    ai.transform.rotation, targetRot,
                    ai.aiRotationSpeed * Time.deltaTime
                );
            }

            if (distToTarget <= ai.attackDistance && ai.attackCooldownTimer <= 0)
            {
                ai.agent.velocity = Vector3.zero;
                ai.anim?.SetFloat("vertical", 0);
                ai.PlayTargetItemAction(AttackInputs.rb);
                return true;
            }

            return false;
        }
    }
}
