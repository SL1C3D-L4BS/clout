using UnityEngine;
using Clout.Core;
using Clout.Combat;
using Clout.Actions;
using Clout.Inventory;
using Clout.Empire.Reputation;
using Clout.World.Police;

namespace Clout.Player
{
    /// <summary>
    /// The player controller — the god class that everything flows through.
    /// Extends CharacterStateManager with camera, input, empire interaction,
    /// weapon management, and vehicle mounting.
    ///
    /// Ported from NullReach's PlayerStateManager, extended for Clout's
    /// crime empire systems. Full state wiring with all combat actions.
    /// </summary>
    public class PlayerStateManager : CharacterStateManager
    {
        [Header("Camera")]
        public Transform cameraTransform;
        public Camera.CameraManager cameraManager;

        [Header("Input")]
        public PlayerInputHandler inputHandler;

        [Header("Player States")]
        public State locomotionState;
        public State attackState;
        public State rollState;
        public State staggerState;
        public State deathState;
        public State interactionState;

        [Header("Dash / Dodge")]
        public int maxDodgeCharges = 2;
        public int currentDodgeCharges = 2;
        public float dodgeCooldown = 3f;
        private float _dodgeCooldownTimer;

        [Header("Empire Components")]
        public ReputationManager reputationManager;
        public WantedSystem wantedSystem;
        public InventoryManager inventoryManager;

        [Header("Starting Equipment")]
        public WeaponItem startingRightWeapon;
        public WeaponItem startingLeftWeapon;

        // Interaction prompt — set by InputHandler, read by HUD
        [HideInInspector] public string currentInteractionPrompt = "";

        public override void Init()
        {
            base.Init();

            // Cache input handler
            if (inputHandler == null)
                inputHandler = GetComponent<PlayerInputHandler>();

            // Cache camera transform
            if (cameraTransform == null && UnityEngine.Camera.main != null)
                cameraTransform = UnityEngine.Camera.main.transform;

            // Cache empire components
            if (reputationManager == null)
                reputationManager = GetComponent<ReputationManager>();
            if (wantedSystem == null)
                wantedSystem = GetComponent<WantedSystem>();
            if (inventoryManager == null)
                inventoryManager = GetComponent<InventoryManager>();

            // Lock cursor for third person
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        protected override void InitStates()
        {
            // === LOCOMOTION STATE ===
            locomotionState = new State { id = locomotionStateId };
            locomotionState.fixedUpdateActions.Add(new MovePlayerCharacter());
            locomotionState.updateActions.Add(new InputHandler());
            locomotionState.updateActions.Add(new HandleStats());
            RegisterState(locomotionState);

            // === ATTACK STATE ===
            attackState = new State { id = attackStateId };
            attackState.updateActions.Add(new MonitorInteraction());
            attackState.updateActions.Add(new InputsForCombo());
            attackState.updateActions.Add(new HandleRotation());
            attackState.updateActions.Add(new HandleStats());

            attackState.onEnter = () =>
            {
                useRootMotion = true;
                canRotate = false;
                canDoCombo = false;
            };

            attackState.onExit = () =>
            {
                useRootMotion = false;
                canDoCombo = false;
                comboIndex = 0;

                if (currentItemAction != null)
                    currentItemAction.animIndex = 0;

                HandleDamageCollider(false);
            };

            RegisterState(attackState);

            // === ROLL STATE ===
            rollState = new State { id = rollStateId };
            rollState.fixedUpdateActions.Add(new HandleRollVelocity());
            rollState.updateActions.Add(new MonitorInteraction());

            rollState.onEnter = () =>
            {
                useRootMotion = false;
                isInteracting = true;
            };

            rollState.onExit = () =>
            {
                isInteracting = false;
            };

            RegisterState(rollState);

            // === STAGGER STATE ===
            staggerState = new State { id = staggerStateId };
            staggerState.updateActions.Add(new MonitorInteraction());

            staggerState.onEnter = () =>
            {
                useRootMotion = true;
                canRotate = false;
                isInteracting = true;
            };

            staggerState.onExit = () =>
            {
                useRootMotion = false;
            };

            RegisterState(staggerState);

            // === DEATH STATE ===
            deathState = new State { id = deathStateId };
            deathState.onEnter = () =>
            {
                isDead = true;
                isInteracting = true;
                if (anim != null) anim.SetBool("isDead", true);
                if (agent != null) agent.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            };
            RegisterState(deathState);

            // === INTERACTION STATE ===
            interactionState = new State { id = interactStateId };
            interactionState.onEnter = () => isInteracting = true;
            interactionState.onExit = () => isInteracting = false;
            RegisterState(interactionState);

            // === SET STARTING STATE ===
            ChangeState(locomotionStateId);

            // === LOAD STARTING WEAPONS ===
            if (weaponHolderManager != null)
            {
                weaponHolderManager.Init();

                if (startingRightWeapon != null)
                    weaponHolderManager.LoadWeaponOnHook(startingRightWeapon, false);

                if (startingLeftWeapon != null)
                    weaponHolderManager.LoadWeaponOnHook(startingLeftWeapon, true);

                UpdateItemActionsWithCurrent();
            }
        }

        protected override void Update()
        {
            if (isDead) return;
            base.Update();

            // Camera mode auto-select
            if (cameraManager != null)
                cameraManager.UpdateCameraMode(this);

            // Dodge cooldown recovery
            if (currentDodgeCharges < maxDodgeCharges)
            {
                _dodgeCooldownTimer -= Time.deltaTime;
                if (_dodgeCooldownTimer <= 0)
                {
                    currentDodgeCharges++;
                    _dodgeCooldownTimer = dodgeCooldown;
                }
            }
        }

        #region Combat

        public override void PlayTargetItemAction(AttackInputs attackInput)
        {
            // Right hand weapon
            WeaponItem weapon = weaponHolderManager?.rightItem;
            if (weapon != null && weapon.itemActions != null)
            {
                for (int i = 0; i < weapon.itemActions.Length; i++)
                {
                    if (weapon.itemActions[i] == null) continue;
                    if (weapon.itemActions[i].attackInput == attackInput)
                    {
                        weapon.itemActions[i].itemActual = weapon;
                        weapon.itemActions[i].ExecuteItemAction(this);
                        return;
                    }
                }
            }

            // Left hand weapon
            WeaponItem leftWeapon = weaponHolderManager?.leftItem;
            if (leftWeapon != null && leftWeapon.itemActions != null)
            {
                for (int i = 0; i < leftWeapon.itemActions.Length; i++)
                {
                    if (leftWeapon.itemActions[i] == null) continue;
                    if (leftWeapon.itemActions[i].attackInput == attackInput)
                    {
                        leftWeapon.itemActions[i].itemActual = leftWeapon;
                        leftWeapon.itemActions[i].ExecuteItemAction(this);
                        return;
                    }
                }
            }
        }

        public bool ConsumeDodge()
        {
            if (currentDodgeCharges <= 0) return false;
            currentDodgeCharges--;
            _dodgeCooldownTimer = dodgeCooldown;
            return true;
        }

        #endregion

        #region Lock On

        public void OnAssignLookOverride(Transform target)
        {
            lockOnTarget = target;
            lockOn = true;

            if (cameraManager != null)
                cameraManager.SwitchToLockOn(target);

            if (animHook != null)
                animHook.lookAtTarget = target;
        }

        public void OnClearLookOverride()
        {
            lockOnTarget = null;
            lockOn = false;

            if (cameraManager != null)
                cameraManager.SwitchToFreeLook();

            if (animHook != null)
                animHook.lookAtTarget = null;
        }

        #endregion

        #region Empire Interaction

        /// <summary>
        /// Called when player makes a deal. Updates economy, reputation, and heat.
        /// </summary>
        public void OnDealCompleted(float dealValue, bool isPublic)
        {
            cash += dealValue;

            if (reputationManager != null)
                reputationManager.AddClout(ReputationManager.CloutValues.CompleteDeal, "deal");

            if (wantedSystem != null && isPublic)
                wantedSystem.AddHeat(WantedSystem.HeatValues.DealingInPublic, "dealing in public");
        }

        /// <summary>
        /// Called when player kills an enemy. Awards CLOUT and increases heat.
        /// </summary>
        public void OnEnemyKilled(bool isPolice)
        {
            if (reputationManager != null)
                reputationManager.AddClout(ReputationManager.CloutValues.DefeatRival, "combat kill");

            if (wantedSystem != null)
            {
                float heat = isPolice
                    ? WantedSystem.HeatValues.MurderPolice
                    : WantedSystem.HeatValues.MurderCivilian;
                wantedSystem.AddHeat(heat, isPolice ? "killed officer" : "killed civilian");
            }
        }

        #endregion
    }
}
