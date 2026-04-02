using UnityEngine;
using Clout.Core;

namespace Clout.AI.Actions
{
    /// <summary>
    /// AI detection — finds and tracks targets via OverlapSphere + FOV check.
    /// Aggression increases detection radius.
    ///
    /// Clout: detects players + rival gang members. Police AI variant will
    /// detect based on wanted level.
    /// </summary>
    public class AIDetection : StateAction
    {
        public override bool Execute(CharacterStateManager stateManager)
        {
            AIStateManager ai = stateManager as AIStateManager;
            if (ai == null) return false;

            // Validate existing target
            if (ai.currentTarget != null)
            {
                if (ai.currentTarget.isDead)
                {
                    ai.currentTarget = null;
                    ai.ChangeState(ai.patrolStateId);
                    return true;
                }

                float dist = Vector3.Distance(ai.transform.position, ai.currentTarget.transform.position);
                float maxDist = ai.detectionRadius * 1.5f;

                if (dist > maxDist)
                {
                    ai.currentTarget = null;
                    ai.ChangeState(ai.patrolStateId);
                    return true;
                }

                return false;
            }

            // Scan for new targets — aggression widens detection
            float radius = ai.detectionRadius * (1f + ai.aggressionLevel * 0.5f);
            Collider[] hits = Physics.OverlapSphere(ai.transform.position, radius, ai.detectionLayer);

            float closest = float.MaxValue;
            CharacterStateManager closestTarget = null;

            for (int i = 0; i < hits.Length; i++)
            {
                CharacterStateManager potential = hits[i].GetComponentInParent<CharacterStateManager>();
                if (potential == null || potential == ai || potential.isDead)
                    continue;

                // Don't target other AI (unless rival — future: check faction)
                if (potential is AIStateManager)
                    continue;

                float dist = Vector3.Distance(ai.transform.position, potential.transform.position);

                // FOV check — 120° cone, but always detect very close targets
                Vector3 dirToTarget = (potential.transform.position - ai.transform.position).normalized;
                float angle = Vector3.Angle(ai.transform.forward, dirToTarget);
                if (angle > 60f && dist > 5f)
                    continue;

                // Line of sight
                int losBlockMask = ai.environmentLayer;
                if (losBlockMask == 0)
                    losBlockMask = ~(ai.detectionLayer | (1 << ai.gameObject.layer));

                Vector3 eyePos = ai.transform.position + Vector3.up * 1.5f;
                Vector3 targetPos = potential.transform.position + Vector3.up;
                if (Physics.Linecast(eyePos, targetPos, losBlockMask))
                    continue;

                if (dist < closest)
                {
                    closest = dist;
                    closestTarget = potential;
                }
            }

            if (closestTarget != null)
            {
                ai.currentTarget = closestTarget;
                ai.ChangeState(ai.chaseStateId);
                return true;
            }

            return false;
        }
    }
}
