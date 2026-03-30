using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using Clout.Core;

namespace Clout.World.Police
{
    /// <summary>
    /// Per-player wanted level system — server-authoritative.
    ///
    /// Heat accumulates from criminal actions (dealing, violence, trespassing).
    /// Heat decays naturally over time, faster when hiding/laying low.
    /// At higher wanted levels, police AI becomes more aggressive.
    ///
    /// Inspired by GTA + Schedule 1's wanted mechanics, but deeper:
    /// - Evidence system (witnesses, cameras, phone records)
    /// - Bribery and corruption
    /// - Disguises and identity management
    /// - Safe zones (your properties)
    /// </summary>
    public class WantedSystem : NetworkBehaviour
    {
        [Header("Heat Config")]
        public readonly SyncVar<float> currentHeat = new SyncVar<float>(0f);
        public readonly SyncVar<WantedLevel> currentLevel = new SyncVar<WantedLevel>(WantedLevel.Clean);
        public float maxHeat = 500f;

        [Header("Decay")]
        public float naturalDecayRate = 1f;         // Heat per second when not seen
        public float hidingDecayMultiplier = 3f;     // 3x faster when hiding
        public float safeZoneDecayMultiplier = 5f;   // 5x faster in your properties

        [Header("Thresholds")]
        public float suspiciousThreshold = 50f;
        public float wantedThreshold = 150f;
        public float huntedThreshold = 300f;
        public float mostWantedThreshold = 450f;

        // Runtime
        private bool _isInSafeZone;
        private bool _isHiding;
        private float _lastSeenByPolice;

        public event Action<WantedLevel> OnWantedLevelChanged;
        public event Action<float> OnHeatChanged;

        /// <summary>
        /// Add heat from a criminal action. Server-only.
        /// </summary>
        [Server]
        public void AddHeat(float amount, string reason)
        {
            currentHeat.Value = Mathf.Min(currentHeat.Value + amount, maxHeat);
            UpdateWantedLevel();
            OnHeatChanged?.Invoke(currentHeat.Value);

            Debug.Log($"[Wanted] +{amount:F0} heat ({reason}) -> Total: {currentHeat.Value:F0} [{currentLevel.Value}]");
        }

        /// <summary>
        /// Reduce heat (bribery, time passing, completing missions).
        /// </summary>
        [Server]
        public void ReduceHeat(float amount)
        {
            currentHeat.Value = Mathf.Max(0, currentHeat.Value - amount);
            UpdateWantedLevel();
            OnHeatChanged?.Invoke(currentHeat.Value);
        }

        public void SetSafeZone(bool inSafeZone) => _isInSafeZone = inSafeZone;
        public void SetHiding(bool hiding) => _isHiding = hiding;
        public void NotifySeenByPolice() => _lastSeenByPolice = Time.time;

        private void Update()
        {
            if (!IsServerInitialized) return;
            if (currentHeat.Value <= 0) return;

            // Decay heat
            float decay = naturalDecayRate;

            // Not seen for 30+ seconds = faster decay
            if (Time.time - _lastSeenByPolice > 30f)
                decay *= 2f;

            if (_isHiding) decay *= hidingDecayMultiplier;
            if (_isInSafeZone) decay *= safeZoneDecayMultiplier;

            currentHeat.Value = Mathf.Max(0, currentHeat.Value - decay * Time.deltaTime);
            UpdateWantedLevel();
        }

        private void UpdateWantedLevel()
        {
            WantedLevel newLevel;

            if (currentHeat.Value >= mostWantedThreshold) newLevel = WantedLevel.MostWanted;
            else if (currentHeat.Value >= huntedThreshold) newLevel = WantedLevel.Hunted;
            else if (currentHeat.Value >= wantedThreshold) newLevel = WantedLevel.Wanted;
            else if (currentHeat.Value >= suspiciousThreshold) newLevel = WantedLevel.Suspicious;
            else newLevel = WantedLevel.Clean;

            if (newLevel != currentLevel.Value)
            {
                currentLevel.Value = newLevel;
                OnWantedLevelChanged?.Invoke(currentLevel.Value);
            }
        }

        // ─── Heat Sources ─────────────────────────────────────────
        // Called by various game systems

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
