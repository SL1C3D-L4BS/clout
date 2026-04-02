using UnityEngine;
using System;
using Clout.Core;

namespace Clout.World.Police
{
    /// <summary>
    /// Per-player wanted level system.
    /// Phase 2 singleplayer — FishNet SyncVars will be restored in Phase 4.
    /// </summary>
    public class WantedSystem : MonoBehaviour
    {
        [Header("Heat Config")]
        // SyncVar fields for Phase 4 multiplayer
        public float maxHeat = 500f;

        [Header("Decay")]
        public float naturalDecayRate = 1f;
        public float hidingDecayMultiplier = 3f;
        public float safeZoneDecayMultiplier = 5f;

        [Header("Thresholds")]
        public float suspiciousThreshold = 50f;
        public float wantedThreshold = 150f;
        public float huntedThreshold = 300f;
        public float mostWantedThreshold = 450f;

        // Runtime
        private bool _isInSafeZone;
        private bool _isHiding;
        private float _lastSeenByPolice;

        // Backing fields
        private float _offlineHeat;
        private WantedLevel _offlineLevel = WantedLevel.Clean;

        public event Action<WantedLevel> OnWantedLevelChanged;
        public event Action<float> OnHeatChanged;

        /// <summary>
        /// Current heat.
        /// </summary>
        public float CurrentHeat => _offlineHeat;

        /// <summary>
        /// Current wanted level.
        /// </summary>
        public WantedLevel CurrentLevel => _offlineLevel;

        /// <summary>
        /// Add heat from a criminal action.
        /// </summary>
        public void AddHeat(float amount, string reason)
        {
            _offlineHeat = Mathf.Min(_offlineHeat + amount, maxHeat);
            UpdateWantedLevel();
            OnHeatChanged?.Invoke(_offlineHeat);
            Debug.Log($"[Wanted] +{amount:F0} heat ({reason}) -> Total: {_offlineHeat:F0} [{_offlineLevel}]");
        }

        /// <summary>
        /// Reduce heat (bribery, time passing, completing missions).
        /// </summary>
        public void ReduceHeat(float amount)
        {
            _offlineHeat = Mathf.Max(0, _offlineHeat - amount);
            UpdateWantedLevel();
            OnHeatChanged?.Invoke(_offlineHeat);
        }

        public void SetSafeZone(bool inSafeZone) => _isInSafeZone = inSafeZone;
        public void SetHiding(bool hiding) => _isHiding = hiding;
        public void NotifySeenByPolice() => _lastSeenByPolice = Time.time;

        private void Update()
        {
            if (_offlineHeat <= 0) return;

            // Decay heat
            float decay = naturalDecayRate;

            if (Time.time - _lastSeenByPolice > 30f)
                decay *= 2f;

            if (_isHiding) decay *= hidingDecayMultiplier;
            if (_isInSafeZone) decay *= safeZoneDecayMultiplier;

            _offlineHeat = Mathf.Max(0, _offlineHeat - decay * Time.deltaTime);
            UpdateWantedLevel();
        }

        private void UpdateWantedLevel()
        {
            WantedLevel newLevel = CalculateLevel(_offlineHeat);
            if (newLevel != _offlineLevel)
            {
                _offlineLevel = newLevel;
                OnWantedLevelChanged?.Invoke(_offlineLevel);
            }
        }

        private WantedLevel CalculateLevel(float heat)
        {
            if (heat >= mostWantedThreshold) return WantedLevel.MostWanted;
            if (heat >= huntedThreshold) return WantedLevel.Hunted;
            if (heat >= wantedThreshold) return WantedLevel.Wanted;
            if (heat >= suspiciousThreshold) return WantedLevel.Suspicious;
            return WantedLevel.Clean;
        }

        // ─── Heat Sources ─────────────────────────────────────────

        public static class HeatValues
        {
            public const float DealingInPublic = 20f;
            public const float DealingNearPolice = 50f;
            public const float AssaultCivilian = 40f;
            public const float AssaultPolice = 100f;
            public const float MurderCivilian = 80f;
            public const float MurderPolice = 150f;
            public const float GunfireInPublic = 30f;
            public const float SpeedingInVehicle = 10f;
            public const float Trespassing = 15f;
            public const float DrugPossession = 25f;
            public const float WeaponPossession = 20f;
            public const float RobberyStore = 60f;
            public const float LabExplosion = 70f;
            public const float NeighborComplaint = 10f;
        }
    }
}
