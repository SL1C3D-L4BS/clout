using UnityEngine;
using Clout.Core;

namespace Clout.World.Interactables
{
    /// <summary>
    /// Door interaction — ported from Sharp Accent's DoorHook.
    /// Used for: property entry/exit, building access, locked doors.
    ///
    /// Supports: animation playback, rotation override (player faces door),
    /// optional lock requiring key item, auto-close after use.
    /// </summary>
    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Door Config")]
        public string interactionAnimation = "open_door";
        public InteractionType interactionType = InteractionType.Door;
        public bool closeAfterInteract = true;
        public float autoCloseDelay = 3f;

        [Header("Lock")]
        public bool isLocked;
        public string requiredKeyId; // Item name that unlocks this door

        [Header("References")]
        public Transform rotationOverride;
        public Animator doorAnimator;
        public string doorOpenClip = "Open";
        public string doorCloseClip = "Close";

        [Header("Scene Transition")]
        [Tooltip("If set, entering this door loads an interior scene additively")]
        public string targetSceneName;
        public Vector3 spawnPosition;

        private bool _isOpen;

        private void Awake()
        {
            gameObject.layer = 15; // Interactable layer
            if (rotationOverride == null)
                rotationOverride = transform;
        }

        public string InteractionPrompt
        {
            get
            {
                if (isLocked) return "Locked";
                if (!string.IsNullOrEmpty(targetSceneName)) return "Enter";
                return _isOpen ? "Close" : "Open";
            }
        }

        public bool CanInteract(CharacterStateManager interactor)
        {
            if (!isLocked) return true;
            return false;
        }

        public void OnInteract(CharacterStateManager interactor)
        {
            if (isLocked && !string.IsNullOrEmpty(requiredKeyId))
            {
                // TODO: Check inventory for key, consume if found
                return;
            }

            // Rotate player to face door
            if (rotationOverride != null)
                interactor.transform.rotation = rotationOverride.rotation;

            // Play player animation
            if (!string.IsNullOrEmpty(interactionAnimation))
                interactor.PlayTargetAnimation(interactionAnimation, true);

            // Toggle door
            _isOpen = !_isOpen;
            if (doorAnimator != null)
                doorAnimator.Play(_isOpen ? doorOpenClip : doorCloseClip);

            // Scene transition
            if (_isOpen && !string.IsNullOrEmpty(targetSceneName))
            {
                // TODO: Wire to SceneTransitionManager
                Debug.Log($"[Door] Would load scene: {targetSceneName}");
            }

            // Auto-close
            if (_isOpen && closeAfterInteract)
                Invoke(nameof(AutoClose), autoCloseDelay);
        }

        private void AutoClose()
        {
            if (!_isOpen) return;
            _isOpen = false;
            if (doorAnimator != null)
                doorAnimator.Play(doorCloseClip);
        }
    }
}
