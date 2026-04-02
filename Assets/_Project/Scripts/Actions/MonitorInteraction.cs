using Clout.Core;

namespace Clout.Actions
{
    /// <summary>
    /// Monitors the "isInteracting" animator bool.
    /// When an animation finishes, returns to locomotion.
    /// Used in attack, roll, and stagger states.
    /// </summary>
    public class MonitorInteraction : StateAction
    {
        private float stuckTimer;
        private const float MaxInteractionTime = 5f;

        public override bool Execute(CharacterStateManager stateManager)
        {
            if (stateManager.anim == null) return false;

            bool animIsInteracting = stateManager.anim.GetBool("isInteracting");

            if (animIsInteracting)
            {
                var stateInfo = stateManager.anim.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.loop && stateInfo.normalizedTime >= 0.9f)
                    animIsInteracting = false;
            }

            if (animIsInteracting)
            {
                stuckTimer += UnityEngine.Time.deltaTime;
                if (stuckTimer >= MaxInteractionTime)
                    animIsInteracting = false;
            }

            if (!animIsInteracting)
            {
                stuckTimer = 0;
                stateManager.isInteracting = false;
                stateManager.anim.SetBool("isInteracting", false);
                stateManager.ChangeState(stateManager.locomotionStateId);
                return true;
            }

            return false;
        }
    }
}
