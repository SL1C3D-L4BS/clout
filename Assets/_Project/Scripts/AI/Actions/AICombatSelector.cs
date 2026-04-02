using UnityEngine;
using Clout.Core;
using Clout.Combat;

namespace Clout.AI.Actions
{
    /// <summary>
    /// AI combat selector — decides between melee chase and ranged engagement
    /// for hybrid-equipped enemies (e.g., gang member with gun + knife).
    ///
    /// Uses utility scoring with sticky decisions to prevent thrashing.
    /// </summary>
    public class AICombatSelector : StateAction
    {
        private float evaluationInterval = 0.5f;
        private float evaluationTimer;

        private CombatRange lastDecision = CombatRange.Melee;
        private float commitTimer;
        private float minCommitTime = 1.5f;

        public override bool Execute(CharacterStateManager stateManager)
        {
            AIStateManager ai = stateManager as AIStateManager;
            if (ai == null || ai.currentTarget == null) return false;
            if (ai.isInteracting) return false;

            commitTimer -= Time.deltaTime;
            evaluationTimer -= Time.deltaTime;

            if (evaluationTimer > 0 && commitTimer > 0)
                return ExecuteCurrentBehavior(ai);

            evaluationTimer = evaluationInterval;

            WeaponItem weapon = ai.weaponHolderManager?.rightItem;
            bool hasRanged = weapon != null && weapon.HasRangedCapability;
            bool hasMelee = weapon == null || weapon.HasMeleeCapability;

            if (hasRanged && !hasMelee)
            {
                lastDecision = CombatRange.Ranged;
                return SwitchToRangedBehavior(ai);
            }
            if (!hasRanged && hasMelee)
            {
                lastDecision = CombatRange.Melee;
                return ExecuteMeleeBehavior(ai);
            }

            // Hybrid — score and decide
            ScoredAction bestAction = AIActionScoring.ScoreBestAction(ai);

            if (!bestAction.IsValid)
                return ExecuteMeleeBehavior(ai);

            bool shouldSwitch = bestAction.range != lastDecision;

            if (shouldSwitch && commitTimer > 0)
            {
                float distToTarget = Vector3.Distance(
                    ai.transform.position,
                    ai.currentTarget.transform.position
                );

                bool emergency = (lastDecision == CombatRange.Ranged && distToTarget < 3f)
                    || (lastDecision == CombatRange.Melee && distToTarget > ai.attackDistance * 3f);

                if (!emergency)
                    return ExecuteCurrentBehavior(ai);
            }

            lastDecision = bestAction.range;
            commitTimer = minCommitTime * (1f + ai.aggressionLevel * 0.3f);

            if (bestAction.range == CombatRange.Ranged)
                return SwitchToRangedBehavior(ai);
            else
                return ExecuteMeleeBehavior(ai);
        }

        private bool ExecuteCurrentBehavior(AIStateManager ai)
        {
            if (lastDecision == CombatRange.Ranged)
                return SwitchToRangedBehavior(ai);
            else
                return ExecuteMeleeBehavior(ai);
        }

        private bool SwitchToRangedBehavior(AIStateManager ai)
        {
            if (ai.currentState == null || ai.currentState.id != ai.rangedAttackStateId)
            {
                ai.ChangeState(ai.rangedAttackStateId);
                return true;
            }
            return false;
        }

        private bool ExecuteMeleeBehavior(AIStateManager ai)
        {
            if (ai.agent == null || !ai.agent.enabled) return false;

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
