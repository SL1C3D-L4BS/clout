using UnityEngine;
using Clout.Core;

namespace Clout.Actions
{
    /// <summary>
    /// Handles character rotation during attacks.
    /// Only rotates when canRotate is true (set by animation events).
    /// During lock-on, always faces the target.
    /// </summary>
    public class HandleRotation : StateAction
    {
        public override bool Execute(CharacterStateManager stateManager)
        {
            if (!stateManager.canRotate) return false;

            Vector3 targetDir;

            if (stateManager.lockOn && stateManager.lockOnTarget != null)
            {
                targetDir = stateManager.lockOnTarget.position - stateManager.transform.position;
            }
            else
            {
                targetDir = stateManager.moveDirection;
            }

            targetDir.y = 0;
            if (targetDir == Vector3.zero) return false;

            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            stateManager.transform.rotation = Quaternion.Slerp(
                stateManager.transform.rotation,
                targetRot,
                stateManager.rotationSpeed * Time.deltaTime
            );

            return false;
        }
    }
}
