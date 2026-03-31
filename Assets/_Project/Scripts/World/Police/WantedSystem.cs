using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using Clout.Core;

namespace Clout.World.Police
{
    /// <summary>
    /// Per-player wanted level system.
    ///
    /// Heat accumulates from criminal actions (dealing, violence, trespassing).
    /// Heat decays naturally over time, faster when hiding/laying low.
    /// At higher wanted levels, police AI becomes more aggressive.
    ///
    /// OFFLINE/ONLINE:
    /// - SyncVars replicate when networked, backing fields used offline
    /// - [Server] removed for offline compatibility — works both modes
    /// </summary>
    public class WantedSystem : NetworkBehaviour
    {
        [Header("Heat Config")]
        public readonly SyncVar<float> currentHeat = new SyncVar<float>(0f);
        public readonly SyncVar<WantedLevel> currentLevel = new SyncVar<WantedLevel>(WantedLevel.Clean);
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

        // Offline backing fields
        private float _offlineHeat;
        private WantedLevel _offlineLevel = WantedLevel.Clean;

        public event Action<WantedLevel> OnWantedLevelChanged;
        public event Action<float> OnHeatChanged;

        /// <summary>
        /// Whether FishNet is active.
        /// </summary>
        private new bool IsNetworked
        {
            get
            {
                try { return IsSpawned; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Current heat (works both online and offline).
        /// </summary>
        public float CurrentHeat
        {
            get
            {
                try { return IsNetworked ? currentHeat.Value : _offlineHeat; }
                catch { return _offlineHeat; }
            }
        }

        /// <summary>
        /// Current wanted level (works both online and offline).
        /// </summary>
        public WantedLevel CurrentLevel
        {
            get
            {
                try { return IsNetworked ? currentLevel.Value : _offlineLevel; }
                catch { return _offlineLevel; }
            }
        }

        /// <summary>
        /// Add heat from a criminal action. Works online and offline.
        /// </summary>
        public void AddHeat(float amount, string reason)
        {
            if (IsNetworked)
            {
                try
                {
                    currentHeat.Value = Mathf.Min(currentHeat.Value + amount, maxHeat);
                    UpdateWantedLevel();
                    OnHeatChanged?.Invoke(currentHeat.Value);
                    Debug.Log($"[Wanted] +{amount:F0} heat ({reason}) -> Total: {currentHeat.Value:F0} [{currentLevel.Value}]");
                    return;
                }
                catch { }
            }

            // Offline path
            _offlineHeat = Mathf.Min(_offlineHeat + amount, maxHeat);
            UpdateWantedLevelOffline();
            OnHeatChanged?.Invoke(_offlineHeat);
            Debug.Log($"[Wanted] +{amount:F0} heat ({reason}) -> Total: {_offlineHeat:F0} [{_offlineLevel}]");
        }

        /// <summary>
        /// Reduce heat (bribery, time passing, completing missions).
        /// </summary>
        public void ReduceHeat(float amount)
        {
            if (IsNetworked)
            {
                try
                {
                    currentHeat.Value = Mathf.Max(0, currentHeat.Value - amount);
                    UpdateWantedLevel();
                    OnHeatChanged?.Invoke(currentHeat.Value);
                    return;
                }
                catch { }
            }

            // Offline
            _offlineHeat = Mathf.Max(0, _offlineHeat - amount);
            UpdateWantedLevelOffline();
            OnHeatChanged?.Invoke(_offlineHeat);
        }

        public void SetSafeZone(bool inSafeZone) => _isInSafeZone = inSafeZone;
        public void SetHiding(bool hiding) => _isHiding = hiding;
        public void NotifySeenByPolice() => _lastSeenByPolice = Time.time;

        private void Update()
        {
            float heat = CurrentHeat;
            if (heat <= 0) return;

            // Decay heat
            float decay = naturalDecayRate;

            if (Time.time - _lastSeenByPolice > 30f)
                decay *= 2f;

            if (_isHiding) decay *= hidingDecayMultiplier;
            if (_isInSafeZone) decay *= safeZoneDecayMultiplier;

            float newHeat = Mathf.Max(0, heat - decay * Time.deltaTime);

            if (IsNetworked)
            {
                try
                {
                    currentHeat.Value = newHeat;
                    UpdateWantedLevel();
                    return;
                }
                catch { }
            }

            _offlineHeat = newHeat;
            UpdateWantedLevelOffline();
        }

        private void UpdateWantedLevel()
        {
            try
            {
                float heat = currentHeat.Value;
                WantedLevel newLevel = CalculateLevel(heat);

                if (newLevel != currentLevel.Value)
                {
                    currentLevel.Value = newLevel;
                    OnWantedLevelChanged?.Invoke(currentLevel.Value);
                }
            }
            catch
            {
                UpdateWantedLevelOffline();
            }
        }

        private void UpdateWantedLevelOffline()
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
