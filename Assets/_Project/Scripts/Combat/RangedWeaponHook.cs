using UnityEngine;
using Clout.Core;

namespace Clout.Combat
{
    /// <summary>
    /// Ranged weapon hook — attached to gun model prefabs.
    /// Manages ammo state, fire rate gating, spread accumulation,
    /// ADS position blending, and recoil feedback.
    /// </summary>
    public class RangedWeaponHook : MonoBehaviour
    {
        [Header("References")]
        public Transform bulletOrigin;
        public Transform leftHandIK;
        public Transform rightHandIK;
        public ParticleSystem[] muzzleFlash;

        [Header("ADS Positions")]
        public Vector3 hipPosition = new Vector3(0.15f, -0.1f, 0.3f);
        public Vector3 adsPosition = new Vector3(0f, -0.05f, 0.15f);
        public float adsSpeed = 8f;
        public float adsReturnSpeed = 6f;

        [Header("ADS Camera")]
        public float adsFOV = 40f;
        public float normalFOV = 60f;

        [Header("Recoil")]
        public Vector2 recoilKickback = new Vector2(0.5f, 1f);
        public Vector2 recoilHorizontal = new Vector2(-0.2f, 0.2f);
        public float recoilRecoverySpeed = 5f;

        [Header("Spread")]
        public float baseSpread = 0.01f;
        public float spreadPerShot = 0.005f;
        public float spreadRecoveryRate = 0.03f;
        public float maxSpread = 0.08f;
        public float adsSpreadMultiplier = 0.3f;

        // Runtime state
        private int _currentAmmo;
        private float _lastShotTime;
        private float _currentSpread;
        private float _adsProgress;
        private Vector3 _currentRecoil;
        private RangedWeaponItem _weaponData;

        public int CurrentAmmo => _currentAmmo;
        public int MaxAmmo => _weaponData != null ? _weaponData.maxAmmo : 0;
        public float AdsProgress => _adsProgress;
        public float CurrentSpread => _currentSpread;
        public bool IsEmpty => _currentAmmo <= 0 && _weaponData != null && _weaponData.ammoType != AmmoType.Infinite;

        /// <summary>
        /// Initialize with weapon data. Called when weapon is equipped.
        /// </summary>
        public void Init(RangedWeaponItem weapon)
        {
            _weaponData = weapon;
            _currentAmmo = weapon.maxAmmo;
            _currentSpread = baseSpread;
            _adsProgress = 0f;
            _currentRecoil = Vector3.zero;
        }

        /// <summary>
        /// Tick the weapon state. Returns ADS progress (0-1) for camera FOV blending.
        /// </summary>
        public float Tick(float delta, bool isAiming, bool isSprinting)
        {
            float targetAds = (isAiming && !isSprinting) ? 1f : 0f;
            _adsProgress = Mathf.MoveTowards(_adsProgress, targetAds,
                (targetAds > _adsProgress ? adsSpeed : adsReturnSpeed) * delta);

            _currentSpread = Mathf.MoveTowards(_currentSpread, baseSpread, spreadRecoveryRate * delta);
            _currentRecoil = Vector3.Lerp(_currentRecoil, Vector3.zero, recoilRecoverySpeed * delta);

            transform.localPosition = Vector3.Lerp(hipPosition, adsPosition, _adsProgress);

            return _adsProgress;
        }

        /// <summary>
        /// Check if weapon can fire (ammo + fire rate).
        /// </summary>
        public bool CanFire()
        {
            if (_weaponData == null) return false;

            if (_weaponData.ammoType != AmmoType.Infinite && _currentAmmo <= 0)
                return false;

            float timeSinceLastShot = Time.realtimeSinceStartup - _lastShotTime;
            if (timeSinceLastShot < _weaponData.fireRate)
                return false;

            return true;
        }

        /// <summary>
        /// Fire the weapon. Returns the spread-adjusted direction for the bullet.
        /// </summary>
        public Vector3 Fire(Vector3 baseDirection)
        {
            if (_weaponData == null) return baseDirection;

            _lastShotTime = Time.realtimeSinceStartup;

            if (_weaponData.ammoType != AmmoType.Infinite)
                _currentAmmo--;

            float effectiveSpread = _currentSpread;
            if (_adsProgress > 0.5f)
                effectiveSpread *= adsSpreadMultiplier;

            Vector3 spreadOffset = new Vector3(
                Random.Range(-effectiveSpread, effectiveSpread),
                Random.Range(-effectiveSpread, effectiveSpread),
                Random.Range(-effectiveSpread, effectiveSpread)
            );
            Vector3 finalDirection = (baseDirection + spreadOffset).normalized;

            _currentSpread = Mathf.Min(_currentSpread + spreadPerShot, maxSpread);

            float verticalKick = Random.Range(recoilKickback.x, recoilKickback.y);
            float horizontalKick = Random.Range(recoilHorizontal.x, recoilHorizontal.y);
            _currentRecoil += new Vector3(horizontalKick, verticalKick, 0f);

            if (muzzleFlash != null)
            {
                foreach (var ps in muzzleFlash)
                {
                    if (ps != null) ps.Play();
                }
            }

            return finalDirection;
        }

        /// <summary>
        /// Reload the weapon.
        /// </summary>
        public bool Reload(int ammoAvailable)
        {
            if (_weaponData == null) return false;
            if (_weaponData.ammoType == AmmoType.Infinite) return false;
            if (_currentAmmo >= _weaponData.maxAmmo) return false;

            int needed = _weaponData.maxAmmo - _currentAmmo;
            int toLoad = Mathf.Min(needed, ammoAvailable);
            _currentAmmo += toLoad;

            return toLoad > 0;
        }

        public Vector3 GetRecoilOffset()
        {
            return _currentRecoil;
        }

        public float GetCurrentFOV()
        {
            return Mathf.Lerp(normalFOV, adsFOV, _adsProgress);
        }
    }
}
