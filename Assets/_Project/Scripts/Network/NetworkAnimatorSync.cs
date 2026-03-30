using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Clout.Core;

namespace Clout.Network
{
    /// <summary>
    /// Supplements FishNet's NetworkAnimator with game-specific sync.
    /// Handles CrossFade calls and state sync for non-owner HUD display.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkAnimatorSync : NetworkBehaviour
    {
        [Header("References")]
        public CharacterStateManager stateManager;

        private readonly SyncVar<float> _syncHealth = new SyncVar<float>();
        private readonly SyncVar<float> _syncStamina = new SyncVar<float>();
        private readonly SyncVar<bool> _syncIsDead = new SyncVar<bool>();
        private readonly SyncVar<bool> _syncLockOn = new SyncVar<bool>();
        private readonly SyncVar<string> _syncCurrentState = new SyncVar<string>();

        private void Awake()
        {
            if (stateManager == null)
                stateManager = GetComponentInParent<CharacterStateManager>();

            _syncHealth.OnChange += OnHealthChanged;
            _syncIsDead.OnChange += OnDeadChanged;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (stateManager != null && stateManager.runtimeStats != null)
            {
                _syncHealth.Value = stateManager.runtimeStats.health;
                _syncStamina.Value = stateManager.runtimeStats.stamina;
            }
        }

        private void Update()
        {
            if (!IsServerInitialized) return;
            if (stateManager == null) return;

            if (stateManager.runtimeStats != null)
            {
                _syncHealth.Value = stateManager.runtimeStats.health;
                _syncStamina.Value = stateManager.runtimeStats.stamina;
            }

            _syncIsDead.Value = stateManager.isDead;
            _syncLockOn.Value = stateManager.lockOn;

            if (stateManager.currentState != null)
                _syncCurrentState.Value = stateManager.currentState.id;
        }

        public void ReplicateCrossFade(string animName, bool isInteracting, bool isMirrored)
        {
            if (!IsSpawned) return;

            if (IsServerInitialized)
            {
                ObserversCrossFade(animName, isInteracting, isMirrored);
            }
            else if (IsOwner)
            {
                ServerRequestCrossFade(animName, isInteracting, isMirrored);
            }
        }

        [ServerRpc]
        private void ServerRequestCrossFade(string animName, bool isInteracting, bool isMirrored)
        {
            ObserversCrossFade(animName, isInteracting, isMirrored);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversCrossFade(string animName, bool isInteracting, bool isMirrored)
        {
            if (stateManager == null || stateManager.anim == null) return;

            stateManager.anim.SetBool("isInteracting", isInteracting);
            stateManager.anim.SetBool("mirror", isMirrored);
            stateManager.anim.CrossFade(animName, 0.2f);
        }

        private void OnHealthChanged(float prev, float next, bool asServer)
        {
            if (asServer) return;
            if (stateManager != null && stateManager.runtimeStats != null)
                stateManager.runtimeStats.health = (int)next;
        }

        private void OnDeadChanged(bool prev, bool next, bool asServer)
        {
            if (asServer) return;
            if (stateManager != null)
                stateManager.isDead = next;
        }

        public float GetSyncedHealth() => _syncHealth.Value;
        public float GetSyncedStamina() => _syncStamina.Value;
        public bool GetSyncedIsDead() => _syncIsDead.Value;
    }
}
