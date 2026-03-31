using UnityEngine;
using Clout.Core;

namespace Clout.Network
{
    /// <summary>
    /// Animator state sync — Phase 2 singleplayer (local only).
    /// FishNet SyncVars and RPCs will be restored in Phase 4 multiplayer.
    /// </summary>
    public class NetworkAnimatorSync : MonoBehaviour
    {
        [Header("References")]
        public CharacterStateManager stateManager;

        // Backing fields (were SyncVars in Phase 1)
        private float _syncHealth;
        private float _syncStamina;
        private bool _syncIsDead;
        private bool _syncLockOn;
        private string _syncCurrentState;

        private void Awake()
        {
            if (stateManager == null)
                stateManager = GetComponentInParent<CharacterStateManager>();
        }

        private void Start()
        {
            if (stateManager != null && stateManager.runtimeStats != null)
            {
                _syncHealth = stateManager.runtimeStats.Health;
                _syncStamina = stateManager.runtimeStats.Stamina;
            }
        }

        private void Update()
        {
            if (stateManager == null) return;

            if (stateManager.runtimeStats != null)
            {
                _syncHealth = stateManager.runtimeStats.Health;
                _syncStamina = stateManager.runtimeStats.Stamina;
            }

            _syncIsDead = stateManager.isDead;
            _syncLockOn = stateManager.lockOn;

            if (stateManager.currentState != null)
                _syncCurrentState = stateManager.currentState.id;
        }

        /// <summary>
        /// In Phase 2, CrossFade is applied locally. Phase 4 will replicate via RPCs.
        /// </summary>
        public void ReplicateCrossFade(string animName, bool isInteracting, bool isMirrored)
        {
            if (stateManager == null || stateManager.anim == null) return;

            stateManager.anim.SetBool("isInteracting", isInteracting);
            stateManager.anim.SetBool("mirror", isMirrored);
            stateManager.anim.CrossFade(animName, 0.2f);
        }

        public float GetSyncedHealth() => _syncHealth;
        public float GetSyncedStamina() => _syncStamina;
        public bool GetSyncedIsDead() => _syncIsDead;
    }
}
