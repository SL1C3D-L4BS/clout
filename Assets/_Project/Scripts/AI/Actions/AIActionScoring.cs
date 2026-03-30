using UnityEngine;
using Clout.Core;
using Clout.Combat;

namespace Clout.AI.Actions
{
    /// <summary>
    /// Utility-theory action scoring — scores each combat action based on
    /// distance, angle, health, aggression, weapon type, ammo, and cooldown.
    /// Returns the highest-scoring AttackInputs for the AI to execute.
    /// </summary>
    public static class AIActionScoring
    {
        private const float DISTANCE_WEIGHT = 1.0f;
        private const float ANGLE_WEIGHT = 0.3f;
        private const float HEALTH_WEIGHT = 0.4f;
        private const float AMMO_WEIGHT = 0.6f;
        private const float COOLDOWN_WEIGHT = 0.8f;
        private const float AGGRESSION_BOOST = 0.5f;

        public static ScoredAction ScoreBestAction(AIStateManager ai)
        {
            if (ai.currentTarget == null) return ScoredAction.None;

            WeaponItem weapon = ai.weaponHolderManager?.rightItem;
            if (weapon == null || weapon.itemActions == null)
                return new ScoredAction(AttackInputs.rb, 0.5f, CombatRange.Melee);

            float distToTarget = Vector3.Distance(
                ai.transform.position,
                ai.currentTarget.transform.position
            );

            Vector3 dirToTarget = (ai.currentTarget.transform.position - ai.transform.position).normalized;
            float angleToTarget = Vector3.Angle(ai.transform.forward, dirToTarget);

            float healthRatio = ai.runtimeStats != null
                ? ai.runtimeStats.health.Value / Mathf.Max(1f, ai.runtimeStats.maxHealth)
                : 1f;

            ScoredAction bestAction = ScoredAction.None;

            for (int i = 0; i < weapon.itemActions.Length; i++)
            {
                if (weapon.itemActions[i] == null || weapon.itemActions[i].itemAction == null)
                    continue;

                ItemActionContainer container = weapon.itemActions[i];
                float score = ScoreAction(ai, container, distToTarget, angleToTarget, healthRatio, weapon);

                if (score > bestAction.score)
                {
                    CombatRange range = container.itemAction is RangedAttackAction
                        ? CombatRange.Ranged
                        : CombatRange.Melee;
                    bestAction = new ScoredAction(container.attackInput, score, range);
                }
            }

            return bestAction;
        }

        private static float ScoreAction(
            AIStateManager ai,
            ItemActionContainer container,
            float distance,
            float angle,
            float healthRatio,
            WeaponItem weapon)
        {
            float score = 0f;
            bool isRanged = container.itemAction is RangedAttackAction;

            // Distance scoring
            if (isRanged)
            {
                float idealMin = 5f;
                float idealMax = 20f;
                if (distance >= idealMin && distance <= idealMax)
                    score += DISTANCE_WEIGHT * 1.0f;
                else if (distance < idealMin)
                    score += DISTANCE_WEIGHT * 0.3f;
                else
                    score += DISTANCE_WEIGHT * 0.6f;
            }
            else
            {
                if (distance <= ai.attackDistance)
                    score += DISTANCE_WEIGHT * 1.0f;
                else if (distance <= ai.attackDistance * 2f)
                    score += DISTANCE_WEIGHT * 0.5f;
                else
                    score += DISTANCE_WEIGHT * 0.1f;
            }

            // Angle scoring
            float angleFactor = 1f - Mathf.Clamp01(angle / 90f);
            score += ANGLE_WEIGHT * angleFactor;

            // Health-based behavior
            if (isRanged)
            {
                score += HEALTH_WEIGHT * (1f - healthRatio);
            }
            else
            {
                score += HEALTH_WEIGHT * healthRatio * (1f + ai.aggressionLevel * AGGRESSION_BOOST);
            }

            // Ammo check (ranged only)
            if (isRanged)
            {
                RangedWeaponHook rangedHook = weapon.rangedWeaponHook;
                if (rangedHook != null)
                {
                    if (rangedHook.CurrentAmmo <= 0)
                    {
                        score *= 0.05f;
                    }
                    else
                    {
                        float ammoRatio = rangedHook.CurrentAmmo / (float)Mathf.Max(1, rangedHook.MaxAmmo);
                        score += AMMO_WEIGHT * ammoRatio;
                    }

                    if (!rangedHook.CanFire())
                        score *= 0.3f;
                }
                else
                {
                    score *= 0.1f;
                }
            }

            // Cooldown readiness
            if (ai.attackCooldownTimer <= 0)
                score += COOLDOWN_WEIGHT;
            else
                score *= 0.2f;

            // Aggression boost
            score *= (1f + ai.aggressionLevel * AGGRESSION_BOOST);

            // Randomness — prevent predictable patterns
            score *= Random.Range(0.85f, 1.15f);

            return score;
        }
    }

    public struct ScoredAction
    {
        public AttackInputs input;
        public float score;
        public CombatRange range;

        public static readonly ScoredAction None = new ScoredAction(AttackInputs.none, 0f, CombatRange.Melee);

        public ScoredAction(AttackInputs input, float score, CombatRange range)
        {
            this.input = input;
            this.score = score;
            this.range = range;
        }

        public bool IsValid => input != AttackInputs.none && score > 0f;
    }

    public enum CombatRange
    {
        Melee,
        Ranged,
        Hybrid
    }
}
