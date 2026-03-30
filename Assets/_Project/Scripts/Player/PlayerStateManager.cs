using UnityEngine;
using Clout.Core;
using Clout.Empire.Reputation;
using Clout.World.Police;

namespace Clout.Player
{
    /// <summary>
    /// The player controller — the god class that everything flows through.
    /// Extends CharacterStateManager with camera, input, empire interaction,
    /// and vehicle mounting.
    ///
    /// Evolved from NullReach's PlayerStateManager, extended for Clout's
    /// crime empire systems.
    /// </summary>
    public class PlayerStateManager : CharacterStateManager
    {
        [Header("Camera")]
        public Transform cameraTransform;
        // public CameraManager cameraManager; // TODO: Port from NullReach

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

        [Header("Starting Equipment")]
        // public WeaponItem startingWeapon;  // TODO: Port weapon system

        public override void Init()
        {
            base.Init();

            // Cache input handler
            if (inputHandler == null)
                inputHandler = GetComponent<PlayerInputHandler>();

            // Cache camera transform
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            // Cache empire components
            if (reputationManager == null)
                reputationManager = GetComponent<ReputationManager>();
            if (wantedSystem == null)
                wantedSystem = GetComponent<WantedSystem>();

            // Lock cursor for third person
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        protected override void InitStates()
        {
            // === LOCOMOTION STATE ===
            locomotionState = new State { id = locomotionStateId };
            // locomotionState.fixedUpdateActions.Add(new Actions.MovePlayerCharacter());
            // locomotionState.updateActions.Add(new Actions.InputHandler());
            RegisterState(locomotionState);

            // === ATTACK STATE ===
            attackState = new State { id = attackStateId };
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
            };
            RegisterState(attackState);

            // === ROLL STATE ===
            rollState = new State { id = "roll" };
            rollState.onEnter = () =>
            {
                useRootMotion = false;
                isInteracting = true;
            };
            rollState.onExit = () => isInteracting = false;
            RegisterState(rollState);

            // === STAGGER STATE ===
            staggerState = new State { id = staggerStateId };
            staggerState.onEnter = () =>
            {
                useRootMotion = true;
                canRotate = false;
                isInteracting = true;
            };
            staggerState.onExit = () => useRootMotion = false;
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

            // Start in locomotion
            ChangeState(locomotionStateId);
        }

        protected override void Update()
        {
            if (isDead) return;
            base.Update();

            // Camera mode update
            // if (cameraManager != null)
            //     cameraManager.UpdateCameraMode(this);

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

        public bool ConsumeDodge()
        {
            if (currentDodgeCharges <= 0) return false;
            currentDodgeCharges--;
            _dodgeCooldownTimer = dodgeCooldown;
            return true;
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

        #endregion
    }
}
