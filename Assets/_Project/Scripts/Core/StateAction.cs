namespace Clout.Core
{
    /// <summary>
    /// Abstract base for all state actions — the strategy pattern backbone.
    /// Each action is a discrete, composable behavior that can be plugged
    /// into any State's update/fixedUpdate/lateUpdate lists.
    /// </summary>
    public abstract class StateAction
    {
        /// <summary>
        /// Execute this action for the given character.
        /// Returns true if this action caused a state transition.
        /// </summary>
        public abstract bool Execute(CharacterStateManager stateManager);
    }
}
