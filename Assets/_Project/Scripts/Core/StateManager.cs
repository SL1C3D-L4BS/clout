using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

namespace Clout.Core
{
    /// <summary>
    /// Abstract state machine base — extends NetworkBehaviour for multiplayer.
    ///
    /// NETWORK ARCHITECTURE:
    /// - State machine ticks ONLY on the authority (owner for players, server for AI)
    /// - State changes replicated via SyncVar for visual sync on non-owners
    /// - FixedUpdate/Update/LateUpdate only run state actions if ShouldTick()
    ///
    /// Ported from NullReach's StateManager, evolved for Clout's empire + combat systems.
    /// </summary>
    public abstract class StateManager : NetworkBehaviour
    {
        [Header("State Machine Debug")]
        [SerializeField] private string _currentStateId;

        public State currentState;
        protected Dictionary<string, State> allStates = new Dictionary<string, State>();

        public string CurrentStateId => _currentStateId;

        protected void RegisterState(State state)
        {
            allStates[state.id] = state;
        }

        public void ChangeState(string targetStateId)
        {
            if (string.IsNullOrEmpty(targetStateId)) return;

            if (!allStates.TryGetValue(targetStateId, out State targetState))
            {
                Debug.LogError($"[StateManager] State '{targetStateId}' not found!");
                return;
            }

            if (currentState == targetState) return;

            currentState?.onExit?.Invoke();
            currentState = targetState;
            _currentStateId = targetState.id;
            currentState.onEnter?.Invoke();
        }

        public abstract void Init();

        /// <summary>
        /// Multiplayer authority gate — only tick on owner (players) or server (AI).
        /// </summary>
        protected bool ShouldTick()
        {
            if (!IsSpawned) return true; // Offline / singleplayer
            if (IsOwner) return true;     // Player's own character
            if (IsServerInitialized && !Owner.IsValid) return true; // Server-controlled AI
            return false;
        }

        protected virtual void Update()
        {
            if (!ShouldTick() || currentState == null) return;

            float delta = Time.deltaTime;
            for (int i = 0; i < currentState.updateActions.Count; i++)
                currentState.updateActions[i].Execute(this as CharacterStateManager);
        }

        protected virtual void FixedUpdate()
        {
            if (!ShouldTick() || currentState == null) return;

            for (int i = 0; i < currentState.fixedUpdateActions.Count; i++)
                currentState.fixedUpdateActions[i].Execute(this as CharacterStateManager);
        }

        protected virtual void LateUpdate()
        {
            if (!ShouldTick() || currentState == null) return;

            for (int i = 0; i < currentState.lateUpdateActions.Count; i++)
                currentState.lateUpdateActions[i].Execute(this as CharacterStateManager);
        }
    }
}
