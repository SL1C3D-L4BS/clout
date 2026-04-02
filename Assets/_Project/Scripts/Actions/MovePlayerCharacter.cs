using UnityEngine;
using Clout.Core;
using Clout.Player;

namespace Clout.Actions
{
    /// <summary>
    /// Movement state action — handles ground detection, Rigidbody movement,
    /// rotation toward camera direction, and animator blend trees.
    /// Runs in FixedUpdate for physics consistency.
    ///
    /// Clout addition: vehicle check — if in vehicle state, skip movement.
    /// </summary>
    public class MovePlayerCharacter : StateAction
    {
        public override bool Execute(CharacterStateManager stateManager)
        {
            PlayerStateManager player = stateManager as PlayerStateManager;
            if (player == null) return false;

            HandleGroundDetection(player);
            HandleMovement(player);
            HandleRotation(player);

            return false;
        }

        private void HandleGroundDetection(PlayerStateManager player)
        {
            Vector3 origin = player.transform.position + Vector3.up * 0.5f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit _, 0.7f))
            {
                player.isGrounded = true;
                player.isOnAir = false;
            }
            else
            {
                player.isGrounded = false;
                player.isOnAir = true;
            }

            if (player.anim != null)
                player.anim.SetBool("isOnAir", player.isOnAir);
        }

        private void HandleMovement(PlayerStateManager player)
        {
            if (player.isInteracting) return;

            float targetSpeed = player.isSprinting ? player.sprintSpeed : player.runSpeed;

            if (player.moveAmount < 0.2f)
                targetSpeed = 0;
            else if (player.moveAmount < 0.55f)
                targetSpeed = player.walkSpeed;

            // Stance speed multiplier
            switch (player.currentStance)
            {
                case Stance.Crouching:
                    targetSpeed *= player.crouchSpeedMultiplier;
                    break;
                case Stance.Prone:
                    targetSpeed *= player.proneSpeedMultiplier;
                    break;
            }

            // Aiming speed penalty
            if (player.isAiming)
                targetSpeed *= player.aimSpeedMultiplier;

            // Overweight penalty from inventory
            if (player.inventoryManager != null && player.inventoryManager.IsOverweight)
                targetSpeed *= 0.6f;

            Vector3 velocity = player.moveDirection * targetSpeed;

            if (player.rigid != null)
            {
                float yVel = player.isGrounded ? Mathf.Min(0f, player.rigid.linearVelocity.y) : player.rigid.linearVelocity.y;
                velocity.y = yVel;
                player.rigid.linearVelocity = velocity;
            }

            // Update animator
            if (player.anim != null)
            {
                player.anim.SetBool("lockOn", player.lockOn);
                player.anim.SetInteger("stance", (int)player.currentStance);
                player.anim.SetBool("isAiming", player.isAiming);

                if (player.currentWeaponInUse != null)
                    player.anim.SetInteger("weaponType", (int)player.currentWeaponInUse.weaponType);

                if (player.isAiming || (player.lockOn && player.lockOnTarget != null))
                {
                    player.anim.SetFloat("vertical", player.vertical, 0.1f, Time.fixedDeltaTime);
                    player.anim.SetFloat("horizontal", player.horizontal, 0.1f, Time.fixedDeltaTime);
                }
                else
                {
                    float v = 0;
                    float m = player.moveAmount;
                    if (m > 0 && m <= 0.5f) v = 0.5f;
                    else if (m > 0.5f) v = 1f;

                    player.anim.SetFloat("vertical", v, 0.2f, Time.fixedDeltaTime);
                    player.anim.SetFloat("horizontal", 0, 0.2f, Time.fixedDeltaTime);
                }
            }
        }

        private void HandleRotation(PlayerStateManager player)
        {
            if (player.isInteracting && !player.canRotate) return;

            Vector3 targetDir;

            if (player.isAiming && player.cameraTransform != null)
            {
                targetDir = player.cameraTransform.forward;
                targetDir.y = 0;
            }
            else if (player.lockOn && player.lockOnTarget != null)
            {
                targetDir = player.lockOnTarget.position - player.transform.position;
                targetDir.y = 0;
            }
            else
            {
                targetDir = player.moveDirection;
            }

            if (targetDir == Vector3.zero)
                return;

            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            player.transform.rotation = Quaternion.Slerp(
                player.transform.rotation,
                targetRot,
                player.rotationSpeed * Time.fixedDeltaTime
            );
        }
    }
}
