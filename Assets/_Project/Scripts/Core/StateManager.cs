using System.Collections.Generic;
using UnityEngine;

namespace Clout.Core
{
    /// <summary>
    /// Abstract state machine base — MonoBehaviour for Phase 2 singleplayer.
    /// FishNet NetworkBehaviour will be restored in Phase 4 multiplayer.
    ///
    /// Ported from NullReach's StateManager, evolved for Clout's empire + combat systems.
    /// </summary>
    public abstract class StateManager : MonoBehaviour
    {
        [Header("State Machine Debug")]
        [SerializeField] private string _currentStateId;

        public State currentState;
        protected Dictionary<string, State> allStates = new Dictionary<string, State>();
        private bool _isInitialized;

        public string CurrentStateId => _currentStateId;
        public bool IsInitialized => _isInitialized;

        protected void RegisterState(State state)
        {
            allStates[state.id] = state;
        }

        public void ChangeState(string targetStateId)
        {
            if (string.IsNullOrEmpty(targetStateId)) return;

            if (!allStates.TryGetValue(targetStateId, out State targetState))
            {
                Debug.LogError($"[StateManager] State '{targetStateId}' not found on {gameObject.name}!");
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
        /// Mark as initialized after Init() completes successfully.
        /// </summary>
        protected void MarkInitialized() => _isInitialized = true;

        /// <summary>
        /// Authority gate — Phase 2 singleplayer always ticks.
        /// FishNet authority checks will be restored in Phase 4 multiplayer.
        /// </summary>
        protected bool ShouldTick()
        {
            return true;
        }

        protected virtual void Update()
        {
            if (!ShouldTick() || currentState == null) return;

            for (int i = 0; i < currentState.updateActions.Count; i++)
            {
                try
                {
                    currentState.updateActions[i].Execute(this as CharacterStateManager);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[StateManager] Update action error on {gameObject.name}: {e.Message}");
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!ShouldTick() || currentState == null) return;

            for (int i = 0; i < currentState.fixedUpdateActions.Count; i++)
            {
                try
                {
                    currentState.fixedUpdateActions[i].Execute(this as CharacterStateManager);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[StateManager] FixedUpdate action error on {gameObject.name}: {e.Message}");
                }
            }
        }

        protected virtual void LateUpdate()
        {
            if (!ShouldTick() || currentState == null) return;

            for (int i = 0; i < currentState.lateUpdateActions.Count; i++)
            {
                try
                {
                    currentState.lateUpdateActions[i].Execute(this as CharacterStateManager);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[StateManager] LateUpdate action error on {gameObject.name}: {e.Message}");
                }
            }
        }
    }
}
