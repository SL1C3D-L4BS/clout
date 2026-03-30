using Clout.Core;

namespace Clout.Actions
{
    /// <summary>
    /// Ticks stamina recovery — runs every Update in locomotion state.
    /// </summary>
    public class HandleStats : StateAction
    {
        public override bool Execute(CharacterStateManager stateManager)
        {
            float delta = UnityEngine.Time.deltaTime;
            stateManager.runtimeStats.HandleStamina(delta, stateManager.isSprinting);
            return false;
        }
    }
}
