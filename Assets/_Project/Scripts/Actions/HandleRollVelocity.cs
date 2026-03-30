using UnityEngine;
using Clout.Core;

namespace Clout.Actions
{
    /// <summary>
    /// Applies roll/dash velocity using an animation curve.
    /// Runs in FixedUpdate during roll state.
    /// </summary>
    public class HandleRollVelocity : StateAction
    {
        private static AnimationCurve defaultRollCurve = new AnimationCurve(
            new Keyframe(0, 1f),
            new Keyframe(0.3f, 1.2f),
            new Keyframe(0.7f, 0.6f),
            new Keyframe(1f, 0f)
        );

        private float rollTimer;
        private float rollDuration = 0.6f;
        private float rollSpeed = 8f;

        public override bool Execute(CharacterStateManager stateManager)
        {
            rollTimer += Time.fixedDeltaTime;
            float normalizedTime = rollTimer / rollDuration;

            if (normalizedTime >= 1f)
            {
                rollTimer = 0;
                if (stateManager.rigid != null)
                    stateManager.rigid.linearVelocity = new Vector3(0, stateManager.rigid.linearVelocity.y, 0);
                return false;
            }

            float curveValue = defaultRollCurve.Evaluate(normalizedTime);
            Vector3 rollDirection = stateManager.moveDirection;

            if (rollDirection == Vector3.zero)
                rollDirection = stateManager.transform.forward;

            Vector3 velocity = rollDirection.normalized * rollSpeed * curveValue;

            if (stateManager.rigid != null)
            {
                velocity.y = stateManager.rigid.linearVelocity.y;
                stateManager.rigid.linearVelocity = velocity;
            }

            return false;
        }
    }
}
