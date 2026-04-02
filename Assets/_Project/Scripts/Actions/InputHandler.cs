using UnityEngine;
using Clout.Core;
using Clout.Combat;
using Clout.Player;

namespace Clout.Actions
{
    /// <summary>
    /// Input state action — polls PlayerInputHandler, calculates camera-relative movement,
    /// triggers combat actions. 100ms input buffer for responsive attack chaining.
    /// </summary>
    public class InputHandler : StateAction
    {
        private float inputBufferTime = 0.1f;
        private float attackBufferTimer;
        private AttackInputs bufferedAttack = AttackInputs.none;

        public override bool Execute(CharacterStateManager stateManager)
        {
            PlayerStateManager player = stateManager as PlayerStateManager;
            if (player == null) return false;

            PlayerInputHandler input = player.inputHandler;
            if (input == null) return false;

            HandleMovementInput(player, input);
            HandleAimInput(player, input);
            HandleShootingInput(player, input);
            HandleAttackInput(player, input);
            HandleRollInput(player, input);
            HandleLockOnInput(player, input);
            HandleInteractInput(player, input);
            HandleTwoHandToggle(player, input);
            HandleReloadInput(player, input);
            HandleStanceInput(player, input);

            input.ConsumeInputs();

            return false;
        }

        private void HandleMovementInput(PlayerStateManager player, PlayerInputHandler input)
        {
            Vector2 moveInput = input.moveInput;
            player.horizontal = moveInput.x;
            player.vertical = moveInput.y;

            if (player.cameraTransform != null)
            {
                Vector3 camForward = player.cameraTransform.forward;
                Vector3 camRight = player.cameraTransform.right;
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                player.moveDirection = camForward * player.vertical + camRight * player.horizontal;
            }
            else
            {
                player.moveDirection = new Vector3(player.horizontal, 0, player.vertical);
            }

            player.moveAmount = Mathf.Clamp01(Mathf.Abs(player.horizontal) + Mathf.Abs(player.vertical));

            bool canSprint = !player.isAiming
                && player.currentStance == Stance.Standing
                && player.moveAmount > 0.5f;
            player.isSprinting = input.sprintHeld && canSprint;

            if (player.isSprinting)
                player.currentStance = Stance.Sprinting;
            else if (player.currentStance == Stance.Sprinting)
                player.currentStance = Stance.Standing;
        }

        private void HandleAttackInput(PlayerStateManager player, PlayerInputHandler input)
        {
            if (attackBufferTimer > 0)
            {
                attackBufferTimer -= Time.deltaTime;
                if (!player.isInteracting && bufferedAttack != AttackInputs.none)
                {
                    player.PlayTargetItemAction(bufferedAttack);
                    bufferedAttack = AttackInputs.none;
                    attackBufferTimer = 0;
                    return;
                }
            }

            if (player.isInteracting) return;

            if (input.rbPressed) { BufferOrFire(player, AttackInputs.rb); return; }
            if (input.rtPressed) { BufferOrFire(player, AttackInputs.rt); return; }
            if (input.lbPressed) { BufferOrFire(player, AttackInputs.lb); return; }
            if (input.ltPressed) { BufferOrFire(player, AttackInputs.lt); return; }
        }

        private void BufferOrFire(PlayerStateManager player, AttackInputs attack)
        {
            player.PlayTargetItemAction(attack);
            bufferedAttack = attack;
            attackBufferTimer = inputBufferTime;
        }

        private void HandleRollInput(PlayerStateManager player, PlayerInputHandler input)
        {
            if (player.isInteracting) return;
            if (!input.rollPressed) return;

            if (!player.runtimeStats.ConsumeStamina(player.runtimeStats.rollCost))
                return;

            if (!player.ConsumeDodge())
                return;

            string rollAnim;
            if (player.moveAmount > 0.2f)
            {
                Vector2 move = input.moveInput;
                if (Mathf.Abs(move.y) > Mathf.Abs(move.x))
                    rollAnim = move.y > 0 ? "roll_forward" : "roll_backwards";
                else
                    rollAnim = move.x > 0 ? "roll_right" : "roll_left";
            }
            else
            {
                rollAnim = "step_back";
            }

            player.PlayTargetAnimation(rollAnim, true);
            player.ChangeState(player.rollStateId);
        }

        private void HandleLockOnInput(PlayerStateManager player, PlayerInputHandler input)
        {
            if (!input.ltPressed) return;

            // Only lock-on with melee weapons — ranged uses LT for ADS
            if (player.currentWeaponInUse != null && player.currentWeaponInUse.HasRangedCapability)
                return;

            if (player.lockOn)
            {
                player.OnClearLookOverride();
            }
            else
            {
                ILockable target = player.FindLockableTarget();
                if (target != null)
                {
                    player.OnAssignLookOverride(target.GetLockOnTarget());
                }
            }
        }

        private void HandleInteractInput(PlayerStateManager player, PlayerInputHandler input)
        {
            if (player.isInteracting) return;

            // Scan for nearby interactables — always update prompt
            IInteractable closest = null;
            float closestDist = float.MaxValue;

            Collider[] hits = Physics.OverlapSphere(player.transform.position, 2.5f);
            foreach (var hit in hits)
            {
                if (hit.transform.root == player.transform) continue;

                IInteractable interactable = hit.GetComponent<IInteractable>();
                if (interactable == null) continue;
                if (!interactable.CanInteract(player)) continue;

                float dist = Vector3.Distance(player.transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closest = interactable;
                    closestDist = dist;
                }
            }

            // Store current prompt for HUD display
            player.currentInteractionPrompt = closest != null ? closest.InteractionPrompt : "";

            // Execute interaction on input
            if (input.interactPressed && closest != null)
            {
                closest.OnInteract(player);
            }
        }

        private void HandleTwoHandToggle(PlayerStateManager player, PlayerInputHandler input)
        {
            // Clout: reserved for future two-hand weapon grip toggle
        }

        #region Shooter Input Handlers

        private void HandleAimInput(PlayerStateManager player, PlayerInputHandler input)
        {
            WeaponItem weapon = player.weaponHolderManager?.rightItem;
            bool weaponCanAim = weapon != null && weapon.HasRangedCapability;

            if (weaponCanAim)
            {
                player.isAiming = input.aimHeld;

                if (player.isAiming && player.isSprinting)
                {
                    player.isSprinting = false;
                    player.currentStance = Stance.Standing;
                }
            }
            else
            {
                player.isAiming = false;
            }

            if (player.anim != null)
                player.anim.SetBool("isAiming", player.isAiming);
        }

        private void HandleShootingInput(PlayerStateManager player, PlayerInputHandler input)
        {
            if (player.isInteracting && !player.isShooting) return;

            WeaponItem weapon = player.weaponHolderManager?.rightItem;
            if (weapon == null || !weapon.HasRangedCapability) return;

            bool wantsFire = input.fireHeld || (input.rtPressed && weapon.HasRangedCapability);
            if (!wantsFire) return;

            RangedWeaponHook rangedHook = weapon.rangedWeaponHook;
            if (rangedHook == null || !rangedHook.CanFire()) return;

            player.isShooting = true;

            if (weapon.itemActions != null)
            {
                for (int i = 0; i < weapon.itemActions.Length; i++)
                {
                    if (weapon.itemActions[i] == null) continue;
                    AttackInputs targetInput = player.isAiming ? AttackInputs.rt : AttackInputs.rb;
                    if (weapon.itemActions[i].attackInput == targetInput)
                    {
                        weapon.itemActions[i].itemActual = weapon;
                        weapon.itemActions[i].ExecuteItemAction(player);

                        input.rtPressed = false;
                        input.rbPressed = false;
                        return;
                    }
                }
            }
        }

        private void HandleReloadInput(PlayerStateManager player, PlayerInputHandler input)
        {
            if (!input.reloadPressed) return;
            if (player.isInteracting) return;

            WeaponItem weapon = player.weaponHolderManager?.rightItem;
            if (weapon == null || !weapon.HasRangedCapability) return;

            RangedWeaponItem rangedWeapon = weapon as RangedWeaponItem;
            RangedWeaponHook rangedHook = weapon.rangedWeaponHook;
            if (rangedWeapon == null || rangedHook == null) return;
            if (rangedHook.CurrentAmmo >= rangedWeapon.maxAmmo) return;

            if (player.ammoCacheManager != null && !player.ammoCacheManager.HasAmmo(rangedWeapon.ammoType))
                return;

            int needed = rangedWeapon.maxAmmo - rangedHook.CurrentAmmo;
            int available = player.ammoCacheManager != null
                ? player.ammoCacheManager.ConsumeAmmo(rangedWeapon.ammoType, needed)
                : needed;

            rangedHook.Reload(available);

            player.isReloading = true;
            string reloadAnim = rangedWeapon.reloadAnimation;
            if (string.IsNullOrEmpty(reloadAnim)) reloadAnim = "reload_standing";
            player.PlayTargetAnimation(reloadAnim, true);
        }

        private void HandleStanceInput(PlayerStateManager player, PlayerInputHandler input)
        {
            if (!input.stancePressed) return;
            if (player.isInteracting) return;
            if (player.isSprinting) return;

            switch (player.currentStance)
            {
                case Stance.Standing:
                    player.currentStance = Stance.Crouching;
                    break;
                case Stance.Crouching:
                    player.currentStance = Stance.Standing;
                    break;
                case Stance.Prone:
                    player.currentStance = Stance.Crouching;
                    break;
            }

            if (player.anim != null)
                player.anim.SetInteger("stance", (int)player.currentStance);
        }

        #endregion
    }
}
