using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Core;

namespace Clout.Empire.Reputation
{
    /// <summary>
    /// Per-player reputation system — "Clout" is literally the core metric.
    /// Phase 2 singleplayer — FishNet SyncVars will be restored in Phase 4.
    /// </summary>
    public class ReputationManager : MonoBehaviour
    {
        [Header("Clout Score")]
        // SyncVar<int> fields for Phase 4 multiplayer

        [Header("Reputation Tracks")]
        public float streetRep = 0f;
        public float civilianRep = 50f;
        public float rivalRep = 0f;
        public float supplierRep = 0f;

        [Header("Clout Rank Thresholds")]
        public int[] rankThresholds = { 0, 100, 500, 2000, 10000, 50000 };
        public string[] rankNames = { "Nobody", "Corner Boy", "Hustler", "Shot Caller", "Kingpin", "Legend" };

        // Backing fields
        private int _offlineClout;
        private int _offlineRank;

        public event Action<int, int> OnCloutChanged;
        public event Action<int, string> OnRankUp;

        /// <summary>
        /// Get current clout score.
        /// </summary>
        public int CurrentClout => _offlineClout;

        /// <summary>
        /// Get current clout rank.
        /// </summary>
        public int CurrentRank => _offlineRank;

        public string CurrentRankName => rankNames[Mathf.Clamp(CurrentRank, 0, rankNames.Length - 1)];

        /// <summary>
        /// Add clout from an action.
        /// </summary>
        public void AddClout(int amount, string reason)
        {
            int oldClout = _offlineClout;
            _offlineClout += amount;

            int newRank = CalculateRank(_offlineClout);
            if (newRank > _offlineRank)
            {
                _offlineRank = newRank;
                OnRankUp?.Invoke(_offlineRank, rankNames[_offlineRank]);
                Debug.Log($"[Clout] RANK UP: {rankNames[_offlineRank]}! (Clout: {_offlineClout})");
            }

            OnCloutChanged?.Invoke(oldClout, _offlineClout);
            Debug.Log($"[Clout] +{amount} ({reason}) -> Total: {_offlineClout} [{rankNames[_offlineRank]}]");
        }

        public void RemoveClout(int amount, string reason)
        {
            int old = _offlineClout;
            _offlineClout = Mathf.Max(0, _offlineClout - amount);

            int rank = CalculateRank(_offlineClout);
            if (rank < _offlineRank)
            {
                _offlineRank = rank;
                Debug.Log($"[Clout] Rank dropped to: {rankNames[_offlineRank]}");
            }

            OnCloutChanged?.Invoke(old, _offlineClout);
        }

        private int CalculateRank(int score)
        {
            for (int i = rankThresholds.Length - 1; i >= 0; i--)
            {
                if (score >= rankThresholds[i])
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// Check if player has enough clout for an action.
        /// </summary>
        public bool HasCloutRank(int minimumRank) => CurrentRank >= minimumRank;

        // ─── Clout Sources ────────────────────────────────────────

        public static class CloutValues
        {
            public const int FirstSale = 5;
            public const int CompleteDeal = 2;
            public const int BigDeal = 10;
            public const int DefeatRival = 25;
            public const int ClaimTerritory = 50;
            public const int BuyProperty = 30;
            public const int HireEmployee = 5;
            public const int SurviveRaid = 20;
            public const int ReachQualityTier = 15;
            public const int MultiplayerKill = 8;
            public const int WinTerritoryWar = 100;

            // Negative
            public const int GetArrested = -50;
            public const int GetRobbed = -20;
            public const int EmployeeBetray = -15;
            public const int LoseTerritory = -30;
            public const int ProductSeized = -10;
        }
    }
}
