using UnityEngine;
using Clout.Core;
using Clout.Combat;

namespace Clout.AI.Actions
{
    /// <summary>
    /// AI ranged attack — maintains distance and fires at target.
    /// Combines strafe movement with shooting through the weapon pipeline.
    ///
    /// AI will: maintain optimal distance, strafe to avoid hits,
    /// fire when facing target, reload when empty, fall back to melee if no ammo.
    /// </summary>
    public class AIRangedAttack : StateAction
    {
        private float strafeTimer;
        private float strafeDirection = 1f;
        private float strafeChangeInterval = 1.5f;

        private float optimalRangeMin = 8f;
        private float optimalRangeMax = 15f;
        private float tooCloseThreshold = 4f;

        private float aimSettleTime = 0.3f;
        private float aimTimer;
        private bool isAimSettled;

        public override bool Execute(CharacterStateManager stateManager)
        {
            AIStateManager ai = stateManager as AIStateManager;
            if (ai == null || ai.currentTarget == null) return false;
            if (ai.isInteracting) return false;

            WeaponItem weapon = ai.weaponHolderManager?.rightItem;
            if (weapon == null || !weapon.HasRangedCapability)
            {
                ai.ChangeState(ai.chaseStateId);
                return true;
            }

            RangedWeaponHook rangedHook = weapon.rangedWeaponHook;
            float distToTarget = Vector3.Distance(
                ai.transform.position,
                ai.currentTarget.transform.position
            );

            // Reload check
            if (rangedHook != null && rangedHook.CurrentAmmo <= 0)
            {
                if (TryReload(ai, weapon, rangedHook))
                    return false;

                ai.ChangeState(ai.chaseStateId);
                return true;
            }

            HandlePositioning(ai, distToTarget);
            RotateTowardTarget(ai);
            HandleFiring(ai, weapon, rangedHook, distToTarget);

            return false;
        }

        private void HandlePositioning(AIStateManager ai, float distance)
        {
            if (ai.agent == null || !ai.agent.enabled) return;

            Vector3 dirToTarget = (ai.currentTarget.transform.position - ai.transform.position).normalized;
            Vector3 rightOfTarget = Vector3.Cross(Vector3.up, dirToTarget).normalized;

            Vector3 desiredPosition;

            if (distance < tooCloseThreshold)
            {
                desiredPosition = ai.transform.position - dirToTarget * 5f;
                ai.agent.speed = ai.chaseSpeed * 1.2f;
            }
            else if (distance < optimalRangeMin)
            {
                desiredPosition = ai.transform.position - dirToTarget * 3f + rightOfTarget * strafeDirection * 2f;
                ai.agent.speed = ai.chaseSpeed;
            }
            else if (distance > optimalRangeMax)
            {
                desiredPosition = ai.currentTarget.transform.position - dirToTarget * optimalRangeMax * 0.8f;
                ai.agent.speed = ai.chaseSpeed;
            }
            else
            {
                desiredPosition = ai.transform.position + rightOfTarget * strafeDirection * 3f;
                ai.agent.speed = ai.chaseSpeed * 0.6f;
            }

            ai.agent.SetDestination(desiredPosition);

            strafeTimer += Time.deltaTime;
            if (strafeTimer >= strafeChangeInterval)
            {
                strafeTimer = 0f;
                strafeDirection = Random.value > 0.5f ? 1f : -1f;
                strafeChangeInterval = Random.Range(1f, 3f);
            }

            float speed = ai.agent.velocity.magnitude / Mathf.Max(0.1f, ai.agent.speed);
            ai.anim?.SetFloat("vertical", Mathf.Clamp01(speed), 0.1f, Time.deltaTime);

            float strafeAmount = Vector3.Dot(ai.agent.velocity.normalized, ai.transform.right);
            ai.anim?.SetFloat("horizontal", strafeAmount, 0.1f, Time.deltaTime);
        }

        private void RotateTowardTarget(AIStateManager ai)
        {
            Vector3 dirToTarget = ai.currentTarget.transform.position - ai.transform.position;
            dirToTarget.y = 0;
            if (dirToTarget.sqrMagnitude < 0.01f) return;

            Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
            ai.transform.rotation = Quaternion.Slerp(
                ai.transform.rotation, targetRot,
                ai.aiRotationSpeed * 2f * Time.deltaTime
            );
        }

        private void HandleFiring(AIStateManager ai, WeaponItem weapon, RangedWeaponHook rangedHook, float distance)
        {
            Vector3 dirToTarget = (ai.currentTarget.transform.position - ai.transform.position).normalized;
            float angle = Vector3.Angle(ai.transform.forward, dirToTarget);

            if (angle > 15f)
            {
                aimTimer = 0f;
                isAimSettled = false;
                return;
            }

            if (!isAimSettled)
            {
                aimTimer += Time.deltaTime;
                float adjustedSettleTime = aimSettleTime * (1f - ai.aggressionLevel * 0.5f);
                if (aimTimer >= adjustedSettleTime)
                    isAimSettled = true;
                else
                    return;
            }

            if (rangedHook != null && !rangedHook.CanFire()) return;
            if (ai.attackCooldownTimer > 0) return;

            ai.isAiming = true;
            ai.PlayTargetItemAction(AttackInputs.rt);
            ai.isAiming = false;
        }

        private bool TryReload(AIStateManager ai, WeaponItem weapon, RangedWeaponHook rangedHook)
        {
            RangedWeaponItem rangedWeapon = weapon as RangedWeaponItem;
            if (rangedWeapon == null) return false;

            if (ai.ammoCacheManager != null && !ai.ammoCacheManager.HasAmmo(rangedWeapon.ammoType))
                return false;

            int needed = rangedWeapon.maxAmmo - rangedHook.CurrentAmmo;
            int available = ai.ammoCacheManager != null
                ? ai.ammoCacheManager.ConsumeAmmo(rangedWeapon.ammoType, needed)
                : needed;

            rangedHook.Reload(available);

            ai.isReloading = true;
            string reloadAnim = rangedWeapon.reloadAnimation;
            if (string.IsNullOrEmpty(reloadAnim)) reloadAnim = "reload_standing";
            ai.PlayTargetAnimation(reloadAnim, true);

            return true;
        }
    }
}
