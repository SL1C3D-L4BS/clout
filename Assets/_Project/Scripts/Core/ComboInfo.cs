using UnityEngine;

namespace Clout.Core
{
    /// <summary>
    /// StateMachineBehaviour that loads combo data when entering an attack animation state.
    /// Attach this to attack animation states in the Animator Controller.
    /// </summary>
    public class ComboInfo : StateMachineBehaviour
    {
        public Combo[] combos;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            CharacterStateManager controller = animator.GetComponentInParent<CharacterStateManager>();
            if (controller != null)
            {
                controller.currentCombo = combos;
                controller.comboIndex = 0;
                controller.canDoCombo = false;
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            CharacterStateManager controller = animator.GetComponentInParent<CharacterStateManager>();
            if (controller != null)
            {
                controller.canDoCombo = false;
            }
        }
    }

    /// <summary>
    /// Sets a bool when entering/exiting an animation state.
    /// Attach to interacting animation states to auto-clear the flag.
    /// </summary>
    public class OnStateEnterBool : StateMachineBehaviour
    {
        public string boolName = "isInteracting";
        public bool status;
        public bool resetOnExit = true;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetBool(boolName, status);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (resetOnExit)
                animator.SetBool(boolName, !status);
        }
    }
}
