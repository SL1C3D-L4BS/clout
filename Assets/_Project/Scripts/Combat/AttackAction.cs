using UnityEngine;
using Clout.Core;

namespace Clout.Combat
{
    /// <summary>
    /// Melee attack action — plays animation at current combo index.
    /// Combo advancement happens in DoCombo() when EnableCombo animation event fires.
    ///
    /// Crime weapons: bat swing, knife slash, machete chop, pipe smash, brass knuckle punch.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Item Actions/Attack Action")]
    public class AttackAction : ItemAction
    {
        public override void ExecuteAction(ItemActionContainer container, CharacterStateManager stateManager)
        {
            // Stamina check
            float cost = (container.attackInput == AttackInputs.rt || container.attackInput == AttackInputs.lt)
                ? stateManager.runtimeStats.heavyAttackCost
                : stateManager.runtimeStats.lightAttackCost;

            if (!stateManager.runtimeStats.ConsumeStamina(cost))
                return;

            // Assign current weapon and action for combo tracking
            stateManager.AssignCurrentWeaponAndAction(container.itemActual as WeaponItem, container);

            // Play the animation at the current combo index
            stateManager.PlayTargetAnimation(container.animName, true, container.isMirrored);
            stateManager.ChangeState(stateManager.attackStateId);
        }
    }
}
