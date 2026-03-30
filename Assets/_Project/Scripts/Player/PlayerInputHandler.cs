using UnityEngine;
using UnityEngine.InputSystem;

namespace Clout.Player
{
    /// <summary>
    /// Raw input capture from Unity's Input System.
    /// Stores input state for consumption by StateActions.
    ///
    /// Supports keyboard/mouse + gamepad with context-sensitive bindings.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        // Movement
        [HideInInspector] public Vector2 moveInput;
        [HideInInspector] public Vector2 lookInput;
        [HideInInspector] public float moveAmount;

        // Combat — melee
        [HideInInspector] public bool rbPressed;     // Light attack / hip fire
        [HideInInspector] public bool rtPressed;     // Heavy attack / ADS fire
        [HideInInspector] public bool lbPressed;     // Parry / block
        [HideInInspector] public bool ltPressed;     // Lock-on / ADS toggle

        // Combat — ranged
        [HideInInspector] public bool fireHeld;      // Continuous fire
        [HideInInspector] public bool aimHeld;       // ADS held
        [HideInInspector] public bool reloadPressed;
        [HideInInspector] public bool stancePressed; // Crouch toggle

        // Movement actions
        [HideInInspector] public bool rollPressed;
        [HideInInspector] public bool sprintHeld;
        [HideInInspector] public bool jumpPressed;

        // Interaction
        [HideInInspector] public bool interactPressed;
        [HideInInspector] public bool inventoryPressed;
        [HideInInspector] public bool phonePressed;   // Open phone UI (empire management)

        // Vehicle
        [HideInInspector] public bool enterVehiclePressed;

        // Input Actions reference
        private PlayerInput _playerInput;

        private void OnEnable()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogWarning("[Input] No PlayerInput component found. Add one with the Clout input actions.");
                return;
            }

            // Bind actions safely
            TryBindAction("Move", ctx => moveInput = ctx.ReadValue<Vector2>(), ctx => moveInput = Vector2.zero);
            TryBindAction("Look", ctx => lookInput = ctx.ReadValue<Vector2>(), ctx => lookInput = Vector2.zero);

            TryBindAction("LightAttack", ctx => rbPressed = true, null);
            TryBindAction("HeavyAttack", ctx => rtPressed = true, null);
            TryBindAction("Block", ctx => lbPressed = true, null);
            TryBindAction("LockOn", ctx => ltPressed = true, null);

            TryBindAction("Fire", ctx => fireHeld = true, ctx => fireHeld = false);
            TryBindAction("Aim", ctx => aimHeld = true, ctx => aimHeld = false);
            TryBindAction("Reload", ctx => reloadPressed = true, null);
            TryBindAction("StanceToggle", ctx => stancePressed = true, null);

            TryBindAction("Roll", ctx => rollPressed = true, null);
            TryBindAction("Sprint", ctx => sprintHeld = true, ctx => sprintHeld = false);
            TryBindAction("Jump", ctx => jumpPressed = true, null);

            TryBindAction("Interact", ctx => interactPressed = true, null);
            TryBindAction("Inventory", ctx => inventoryPressed = true, null);
            TryBindAction("Phone", ctx => phonePressed = true, null);
            TryBindAction("EnterVehicle", ctx => enterVehiclePressed = true, null);
        }

        /// <summary>
        /// Consume one-shot inputs after processing. Called by InputHandler action.
        /// </summary>
        public void ConsumeInputs()
        {
            rbPressed = false;
            rtPressed = false;
            lbPressed = false;
            ltPressed = false;
            reloadPressed = false;
            stancePressed = false;
            rollPressed = false;
            jumpPressed = false;
            interactPressed = false;
            inventoryPressed = false;
            phonePressed = false;
            enterVehiclePressed = false;
        }

        /// <summary>
        /// Calculate move amount for animator blending.
        /// </summary>
        public void CalculateMoveAmount()
        {
            moveAmount = Mathf.Clamp01(Mathf.Abs(moveInput.x) + Mathf.Abs(moveInput.y));
        }

        /// <summary>
        /// Safe input action binding — handles missing actions gracefully.
        /// </summary>
        private void TryBindAction(string actionName,
            System.Action<InputAction.CallbackContext> performed,
            System.Action<InputAction.CallbackContext> canceled)
        {
            if (_playerInput == null || _playerInput.actions == null) return;
            var action = _playerInput.actions.FindAction(actionName);
            if (action == null) return;

            if (performed != null)
                action.performed += performed;
            if (canceled != null)
                action.canceled += canceled;
        }
    }
}
