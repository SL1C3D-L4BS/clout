using Clout.Core;
using Clout.Player;

namespace Clout.Actions
{
    /// <summary>
    /// Detects combo input during attack state.
    /// When canDoCombo is true (set by animation event via AnimatorHook),
    /// checks for attack input and chains into the next combo animation.
    /// </summary>
    public class InputsForCombo : StateAction
    {
        public override bool Execute(CharacterStateManager stateManager)
        {
            if (!stateManager.canDoCombo) return false;

            PlayerStateManager player = stateManager as PlayerStateManager;
            if (player == null || player.inputHandler == null) return false;

            PlayerInputHandler input = player.inputHandler;

            if (input.rbPressed)
            {
                stateManager.DoCombo(AttackInputs.rb);
                input.rbPressed = false;
                return true;
            }

            if (input.rtPressed)
            {
                stateManager.DoCombo(AttackInputs.rt);
                input.rtPressed = false;
                return true;
            }

            if (input.lbPressed)
            {
                stateManager.DoCombo(AttackInputs.lb);
                input.lbPressed = false;
                return true;
            }

            if (input.ltPressed)
            {
                stateManager.DoCombo(AttackInputs.lt);
                input.ltPressed = false;
                return true;
            }

            return false;
        }
    }
}
