using UnityEngine;
using Clout.Core;

namespace Clout.Combat
{
    /// <summary>
    /// Bridge between Unity's Animator and the game's systems.
    /// Receives animation events, drives root motion, controls damage colliders,
    /// and manages IK for head tracking + weapon hand placement.
    ///
    /// Weapon-conditional root motion: melee attacks use root motion,
    /// aiming/shooting suppresses it for input-driven movement.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimatorHook : MonoBehaviour
    {
        private CharacterStateManager controller;
        private Animator anim;

        [Header("Look At IK")]
        public Transform lookAtTarget;
        public float lookAtWeight = 0.8f;

        [Header("Hand IK")]
        public Transform leftHandIKTarget;
        public Transform rightHandIKTarget;
        [Range(0, 1)] public float handIKWeight = 0f;
        private float _targetHandIKWeight;
        private float _handIKLerpSpeed = 5f;

        [Header("Foot IK")]
        public bool enableFootIK;
        public LayerMask groundLayer = ~0;
        public float footRaycastDistance = 1.2f;
        public float footIKOffset = 0.05f;

        public void Init(CharacterStateManager controller)
        {
            this.controller = controller;
            anim = GetComponent<Animator>();
        }

        /// <summary>
        /// Root motion — weapon-conditional.
        /// Melee: full root motion. Aiming: suppressed. Locomotion: configurable.
        /// </summary>
        private void OnAnimatorMove()
        {
            if (controller == null) return;

            if (controller.useRootMotion)
            {
                if (controller.isAiming)
                    return;

                if (controller.agent != null && controller.agent.enabled)
                {
                    controller.agent.velocity = anim.deltaPosition / Time.deltaTime;
                }
                else if (controller.rigid != null)
                {
                    controller.rigid.linearVelocity = anim.deltaPosition / Time.deltaTime;
                }
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (anim == null || controller == null) return;

            // === HEAD LOOK-AT IK ===
            if (lookAtTarget != null)
            {
                anim.SetLookAtWeight(lookAtWeight, 0.5f, 1f, 0f, 0.5f);
                anim.SetLookAtPosition(lookAtTarget.position);
            }
            else
            {
                anim.SetLookAtWeight(0);
            }

            // === HAND IK — Weapon grip placement ===
            handIKWeight = Mathf.Lerp(handIKWeight, _targetHandIKWeight, _handIKLerpSpeed * Time.deltaTime);

            if (handIKWeight > 0.01f)
            {
                if (leftHandIKTarget != null)
                {
                    anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, handIKWeight);
                    anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, handIKWeight);
                    anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIKTarget.position);
                    anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIKTarget.rotation);
                }

                if (rightHandIKTarget != null)
                {
                    anim.SetIKPositionWeight(AvatarIKGoal.RightHand, handIKWeight);
                    anim.SetIKRotationWeight(AvatarIKGoal.RightHand, handIKWeight);
                    anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandIKTarget.position);
                    anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandIKTarget.rotation);
                }
            }
            else
            {
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
                anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            }

            // === FOOT IK — Terrain adaptation ===
            if (enableFootIK && controller.isGrounded)
            {
                ApplyFootIK(AvatarIKGoal.LeftFoot);
                ApplyFootIK(AvatarIKGoal.RightFoot);
            }
        }

        private void ApplyFootIK(AvatarIKGoal foot)
        {
            Vector3 footPos = anim.GetIKPosition(foot);
            Vector3 rayStart = footPos + Vector3.up * 0.5f;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, footRaycastDistance, groundLayer))
            {
                Vector3 targetPos = hit.point + Vector3.up * footIKOffset;
                anim.SetIKPositionWeight(foot, 1f);
                anim.SetIKPosition(foot, targetPos);

                Quaternion footRotation = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(controller.transform.forward, hit.normal),
                    hit.normal
                );
                anim.SetIKRotationWeight(foot, 0.5f);
                anim.SetIKRotation(foot, footRotation);
            }
        }

        /// <summary>
        /// Set weapon IK targets — called when weapon is equipped.
        /// </summary>
        public void SetWeaponIKTargets(Transform leftHand, Transform rightHand, float weight)
        {
            leftHandIKTarget = leftHand;
            rightHandIKTarget = rightHand;
            _targetHandIKWeight = weight;
        }

        public void ClearWeaponIK()
        {
            _targetHandIKWeight = 0f;
            leftHandIKTarget = null;
            rightHandIKTarget = null;
        }

        #region Animation Event Callbacks

        public void OpenDamageCollider()
        {
            if (controller.currentWeaponInUse != null && controller.currentWeaponInUse.weaponHook != null)
                controller.currentWeaponInUse.weaponHook.OpenDamageCollider();
        }

        public void CloseDamageCollider()
        {
            if (controller.currentWeaponInUse != null && controller.currentWeaponInUse.weaponHook != null)
                controller.currentWeaponInUse.weaponHook.CloseDamageCollider();
        }

        public void OpenDamageColliders() => OpenDamageCollider();
        public void CloseDamageColliders() => CloseDamageCollider();

        public void EnableCombo()
        {
            controller.canDoCombo = true;
            if (anim != null)
                anim.SetBool("canDoCombo", true);
        }

        public void DisableCombo()
        {
            controller.canDoCombo = false;
            if (anim != null)
                anim.SetBool("canDoCombo", false);
        }

        public void EnableRotation()
        {
            controller.canRotate = true;
        }

        public void DisableRotation()
        {
            controller.canRotate = false;
        }

        public void OnFireFrame()
        {
            controller.isShooting = true;
        }

        public void OnReloadComplete()
        {
            controller.isReloading = false;
            controller.isInteracting = false;
        }

        public void OpenParryCollider() { }
        public void CloseParryCollider() { }

        #endregion
    }
}
