using UnityEngine;
using Unity.Cinemachine;
using FishNet.Object;
using Clout.Core;
using Clout.Combat;

namespace Clout.Camera
{
    /// <summary>
    /// Unified camera system — handles all 4 camera modes for hybrid melee+shooter gameplay.
    /// Cinemachine 3.x: CinemachineCamera + component pipeline.
    ///
    /// MODES:
    /// 1. FreeLook TPS  — Orbital follow, free mouse look (exploration + melee)
    /// 2. LockOn TPS    — Hard look at enemy target (Souls-like combat)
    /// 3. HipFire TPS   — Shoulder camera, crosshair visible (gun hip-fire)
    /// 4. ADS           — Tight aim, reduced FOV (aim-down-sights)
    ///
    /// Priority system: FreeLook(10) < HipFire(12) < ADS(15) < LockOn(20)
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        private bool isLocalPlayer = true;

        [Header("Virtual Cameras")]
        public CinemachineCamera freeLookCam;
        public CinemachineCamera lockOnCam;
        public CinemachineCamera hipFireCam;
        public CinemachineCamera adsCam;

        [Header("References")]
        public Transform playerTransform;
        public Transform cameraFollowTarget;

        [Header("Free Look Settings")]
        public float topRigHeight = 4.5f;
        public float topRigRadius = 2.5f;
        public float midRigHeight = 2.5f;
        public float midRigRadius = 5f;
        public float botRigHeight = 0.5f;
        public float botRigRadius = 2f;

        [Header("Lock-On Settings")]
        public Vector3 lockOnOffset = new Vector3(0.5f, 2.5f, -4f);

        [Header("Hip-Fire Settings")]
        public Vector3 hipFireOffset = new Vector3(1.2f, 1.8f, -2.5f);

        [Header("ADS Settings")]
        public Vector3 adsOffset = new Vector3(0.3f, 1.6f, -0.8f);
        public float defaultFOV = 60f;
        public float adsFOV = 40f;

        [Header("FOV Transition")]
        public float fovLerpSpeed = 8f;
        private float _currentFOV;
        private float _targetFOV;

        public enum CamMode { FreeLook, LockOn, HipFire, ADS }
        public CamMode CurrentMode { get; private set; } = CamMode.FreeLook;

        public void Init(Transform player, Transform followTarget)
        {
            playerTransform = player;
            cameraFollowTarget = followTarget;

            NetworkObject nob = player.GetComponent<NetworkObject>();
            if (nob != null && nob.IsSpawned && !nob.IsOwner)
            {
                isLocalPlayer = false;
                gameObject.SetActive(false);
                return;
            }

            if (freeLookCam == null) BuildFreeLookCamera();
            if (lockOnCam == null) BuildLockOnCamera();
            if (hipFireCam == null) BuildHipFireCamera();
            if (adsCam == null) BuildADSCamera();

            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
                mainCam.gameObject.AddComponent<CinemachineBrain>();

            _currentFOV = defaultFOV;
            _targetFOV = defaultFOV;

            SwitchToFreeLook();
        }

        private void LateUpdate()
        {
            if (Mathf.Abs(_currentFOV - _targetFOV) > 0.01f)
            {
                _currentFOV = Mathf.Lerp(_currentFOV, _targetFOV, fovLerpSpeed * Time.deltaTime);
                UnityEngine.Camera mainCam = UnityEngine.Camera.main;
                if (mainCam != null)
                    mainCam.fieldOfView = _currentFOV;
            }
        }

        #region Camera Builders

        private void BuildFreeLookCamera()
        {
            GameObject go = new GameObject("FreeLookCam");
            go.transform.SetParent(transform);

            freeLookCam = go.AddComponent<CinemachineCamera>();
            freeLookCam.Follow = cameraFollowTarget;
            freeLookCam.LookAt = cameraFollowTarget;

            var orbital = go.AddComponent<CinemachineOrbitalFollow>();
            orbital.Radius = midRigRadius;
            orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;

            var composer = go.AddComponent<CinemachineRotationComposer>();
            composer.Damping = new Vector2(1f, 0.5f);

            go.AddComponent<CinemachineInputAxisController>();
            freeLookCam.Priority.Value = 10;
        }

        private void BuildLockOnCamera()
        {
            GameObject go = new GameObject("LockOnCam");
            go.transform.SetParent(transform);

            lockOnCam = go.AddComponent<CinemachineCamera>();
            lockOnCam.Follow = cameraFollowTarget;
            lockOnCam.LookAt = null;

            var follow = go.AddComponent<CinemachineFollow>();
            follow.FollowOffset = lockOnOffset;
            follow.TrackerSettings.PositionDamping = new Vector3(0.5f, 0.5f, 0.5f);

            go.AddComponent<CinemachineHardLookAt>();
            lockOnCam.Priority.Value = 0;
        }

        private void BuildHipFireCamera()
        {
            GameObject go = new GameObject("HipFireCam");
            go.transform.SetParent(transform);

            hipFireCam = go.AddComponent<CinemachineCamera>();
            hipFireCam.Follow = cameraFollowTarget;
            hipFireCam.LookAt = cameraFollowTarget;

            var follow = go.AddComponent<CinemachineFollow>();
            follow.FollowOffset = hipFireOffset;
            follow.TrackerSettings.PositionDamping = new Vector3(0.3f, 0.3f, 0.3f);

            var composer = go.AddComponent<CinemachineRotationComposer>();
            composer.Damping = new Vector2(0.5f, 0.3f);

            go.AddComponent<CinemachineInputAxisController>();
            hipFireCam.Priority.Value = 0;
        }

        private void BuildADSCamera()
        {
            GameObject go = new GameObject("ADSCam");
            go.transform.SetParent(transform);

            adsCam = go.AddComponent<CinemachineCamera>();
            adsCam.Follow = cameraFollowTarget;
            adsCam.LookAt = cameraFollowTarget;

            var follow = go.AddComponent<CinemachineFollow>();
            follow.FollowOffset = adsOffset;
            follow.TrackerSettings.PositionDamping = new Vector3(0.1f, 0.1f, 0.1f);

            var composer = go.AddComponent<CinemachineRotationComposer>();
            composer.Damping = new Vector2(0.2f, 0.2f);

            go.AddComponent<CinemachineInputAxisController>();
            adsCam.Priority.Value = 0;
        }

        #endregion

        #region Camera Mode Switching

        public void SwitchToFreeLook()
        {
            CurrentMode = CamMode.FreeLook;
            _targetFOV = defaultFOV;
            SetPriorities(freeLook: 10, lockOn: 0, hipFire: 0, ads: 0);
        }

        public void SwitchToLockOn(Transform target)
        {
            if (lockOnCam == null) return;
            CurrentMode = CamMode.LockOn;
            _targetFOV = defaultFOV;
            lockOnCam.LookAt = target;
            SetPriorities(freeLook: 0, lockOn: 20, hipFire: 0, ads: 0);
        }

        public void SwitchToHipFire()
        {
            CurrentMode = CamMode.HipFire;
            _targetFOV = defaultFOV - 5f;
            SetPriorities(freeLook: 0, lockOn: 0, hipFire: 12, ads: 0);
        }

        public void SwitchToADS(float weaponFOV = 0f)
        {
            CurrentMode = CamMode.ADS;
            _targetFOV = weaponFOV > 0 ? weaponFOV : adsFOV;
            SetPriorities(freeLook: 0, lockOn: 0, hipFire: 0, ads: 15);
        }

        /// <summary>
        /// Update camera mode based on character state. Call every frame.
        /// Priority: LockOn > ADS > HipFire > FreeLook
        /// </summary>
        public void UpdateCameraMode(CharacterStateManager character)
        {
            if (character == null) return;

            if (character.lockOn && character.lockOnTarget != null)
            {
                if (CurrentMode != CamMode.LockOn)
                    SwitchToLockOn(character.lockOnTarget);
                return;
            }

            if (character.isAiming)
            {
                float weaponFov = adsFOV;
                if (character.currentWeaponInUse is RangedWeaponItem ranged)
                    weaponFov = ranged.adsFOV;
                else if (character.currentWeaponInUse?.rangedWeaponHook != null)
                    weaponFov = character.currentWeaponInUse.rangedWeaponHook.GetCurrentFOV();

                if (CurrentMode != CamMode.ADS)
                    SwitchToADS(weaponFov);
                return;
            }

            WeaponItem weapon = character.currentWeaponInUse;
            if (weapon == null && character.weaponHolderManager != null)
                weapon = character.weaponHolderManager.rightItem;

            if (weapon != null && weapon.HasRangedCapability)
            {
                if (CurrentMode != CamMode.HipFire)
                    SwitchToHipFire();
                return;
            }

            if (CurrentMode != CamMode.FreeLook)
                SwitchToFreeLook();
        }

        private void SetPriorities(int freeLook, int lockOn, int hipFire, int ads)
        {
            if (freeLookCam != null) freeLookCam.Priority.Value = freeLook;
            if (lockOnCam != null) lockOnCam.Priority.Value = lockOn;
            if (hipFireCam != null) hipFireCam.Priority.Value = hipFire;
            if (adsCam != null) adsCam.Priority.Value = ads;
        }

        #endregion
    }
}
