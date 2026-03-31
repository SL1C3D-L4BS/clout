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
        /// Keyboard/mouse fallback polling using the new Input System device APIs.
        /// Runs every frame alongside (or instead of) PlayerInput action bindings.
        ///
        /// Default bindings:
        ///   Move       — WASD
        ///   Look       — Mouse Delta
        ///   Light Atk  — Left Mouse / Mouse 0
        ///   Heavy Atk  — Right Mouse / Mouse 1
        ///   Block       — Q
        ///   Lock-On    — Tab
        ///   Fire (hold)— Left Mouse (hold)
        ///   Aim (hold) — Right Mouse (hold)
        ///   Reload     — R
        ///   Crouch     — C
        ///   Roll/Dodge — Space
        ///   Sprint     — Left Shift (hold)
        ///   Interact   — E
        ///   Inventory  — I
        ///   Phone      — P
        ///   Vehicle    — F
        /// </summary>
        private void Update()
        {
            PollKeyboardMouse();
            CalculateMoveAmount();
        }

        private void PollKeyboardMouse()
        {
            var kb    = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;

            if (kb == null && mouse == null) return;

            // ── Vector2 axes — ALWAYS poll directly from hardware ──
            // PlayerInput actions may not have proper bindings configured,
            // so raw keyboard/mouse polling is the primary input source.

            if (kb != null)
            {
                moveInput = new Vector2(
                    (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f),
                    (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f)
                );
            }

            if (mouse != null)
            {
                lookInput = mouse.delta.ReadValue() * 0.05f;
            }

            // ── Gamepad axes — supplement if gamepad connected ──
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad != null)
            {
                Vector2 leftStick = gamepad.leftStick.ReadValue();
                Vector2 rightStick = gamepad.rightStick.ReadValue();

                // Use gamepad if sticks have meaningful input (overrides keyboard if present)
                if (leftStick.sqrMagnitude > 0.04f)
                    moveInput = leftStick;
                if (rightStick.sqrMagnitude > 0.04f)
                    lookInput = rightStick * 2f;
            }

            // ── Boolean inputs — keyboard ──
            if (kb != null)
            {
                if (kb.leftShiftKey.isPressed)      sprintHeld    = true;
                if (kb.spaceKey.wasPressedThisFrame) rollPressed   = true;
                if (kb.eKey.wasPressedThisFrame)     interactPressed = true;
                if (kb.rKey.wasPressedThisFrame)     reloadPressed = true;
                if (kb.cKey.wasPressedThisFrame)     stancePressed = true;
                if (kb.iKey.wasPressedThisFrame)     inventoryPressed = true;
                if (kb.pKey.wasPressedThisFrame)     phonePressed  = true;
                if (kb.fKey.wasPressedThisFrame)     enterVehiclePressed = true;
                if (kb.qKey.wasPressedThisFrame)     lbPressed     = true;
                if (kb.tabKey.wasPressedThisFrame)   ltPressed     = true;

                // ESC to toggle cursor (debug/testing)
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    if (Cursor.lockState == CursorLockMode.Locked)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }

            // ── Boolean inputs — mouse ──
            if (mouse != null)
            {
                if (mouse.leftButton.isPressed)          fireHeld  = true;
                if (mouse.rightButton.isPressed)         aimHeld   = true;
                if (mouse.leftButton.wasPressedThisFrame)  rbPressed = true;
                if (mouse.rightButton.wasPressedThisFrame) rtPressed = true;
            }

            // ── Boolean inputs — gamepad ──
            if (gamepad != null)
            {
                if (gamepad.rightShoulder.wasPressedThisFrame) rbPressed = true;
                if (gamepad.rightTrigger.wasPressedThisFrame)  rtPressed = true;
                if (gamepad.leftShoulder.wasPressedThisFrame)  lbPressed = true;
                if (gamepad.leftTrigger.wasPressedThisFrame)   ltPressed = true;
                if (gamepad.buttonSouth.wasPressedThisFrame)   rollPressed = true;   // A / Cross
                if (gamepad.buttonWest.wasPressedThisFrame)    reloadPressed = true;  // X / Square
                if (gamepad.buttonNorth.wasPressedThisFrame)   interactPressed = true;// Y / Triangle
                if (gamepad.buttonEast.wasPressedThisFrame)    stancePressed = true;  // B / Circle
                if (gamepad.dpad.up.wasPressedThisFrame)       inventoryPressed = true;
                if (gamepad.dpad.down.wasPressedThisFrame)     phonePressed = true;
                if (gamepad.dpad.right.wasPressedThisFrame)    enterVehiclePressed = true;
                if (gamepad.leftStickButton.isPressed)         sprintHeld = true;     // L3
            }
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
