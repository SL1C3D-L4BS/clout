using UnityEngine;

namespace Clout.Combat
{
    /// <summary>
    /// Recoil controller — drives camera shake and aim punch on weapon fire.
    /// Curve-driven system for Cinemachine camera pipeline.
    /// </summary>
    public class RecoilController : MonoBehaviour
    {
        [Header("Recoil Curves")]
        public AnimationCurve verticalCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        public AnimationCurve horizontalCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [Header("Settings")]
        public float recoilDuration = 0.15f;
        public float recoveryDuration = 0.3f;
        public float maxVerticalRecoil = 3f;
        public float maxHorizontalRecoil = 1f;

        private float _recoilTimer;
        private float _recoveryTimer;
        private Vector3 _currentRecoilRotation;
        private Vector3 _targetRecoilRotation;
        private bool _isRecoiling;

        public Vector3 RecoilRotation => _currentRecoilRotation;

        public void AddRecoil(float verticalForce, float horizontalForce)
        {
            float vertical = verticalForce * maxVerticalRecoil;
            float horizontal = horizontalForce * maxHorizontalRecoil;

            _targetRecoilRotation += new Vector3(-vertical, horizontal, 0f);
            _recoilTimer = 0f;
            _isRecoiling = true;
        }

        public void AddRecoilFromWeapon(RangedWeaponHook weaponHook)
        {
            if (weaponHook == null) return;
            Vector3 offset = weaponHook.GetRecoilOffset();
            AddRecoil(offset.y, offset.x);
        }

        private void LateUpdate()
        {
            float delta = Time.deltaTime;

            if (_isRecoiling)
            {
                _recoilTimer += delta;
                float t = Mathf.Clamp01(_recoilTimer / recoilDuration);

                float verticalEval = verticalCurve.Evaluate(t);
                float horizontalEval = horizontalCurve.Evaluate(t);

                _currentRecoilRotation = new Vector3(
                    _targetRecoilRotation.x * verticalEval,
                    _targetRecoilRotation.y * horizontalEval,
                    0f
                );

                if (t >= 1f)
                {
                    _isRecoiling = false;
                    _recoveryTimer = 0f;
                }
            }
            else
            {
                _recoveryTimer += delta;
                float recoveryT = Mathf.Clamp01(_recoveryTimer / recoveryDuration);

                _currentRecoilRotation = Vector3.Lerp(_currentRecoilRotation, Vector3.zero, recoveryT);
                _targetRecoilRotation = Vector3.Lerp(_targetRecoilRotation, Vector3.zero, recoveryT);
            }
        }
    }
}
