using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using Clout.Core;

namespace Clout.Empire.Reputation
{
    /// <summary>
    /// Per-player reputation system — "Clout" is literally the core metric.
    ///
    /// Your CLOUT score is your street reputation — it determines:
    /// - What properties you can buy
    /// - What employees you can hire
    /// - What suppliers will deal with you
    /// - Whether rivals respect or target you
    /// - Customer willingness to buy from you
    ///
    /// CLOUT is the title of the game. It's the meta-currency of power.
    ///
    /// OFFLINE/ONLINE:
    /// - SyncVars replicate when networked
    /// - Plain backing fields used when FishNet isn't running
    /// - [Server] removed for offline compatibility — authority check done manually
    /// </summary>
    public class ReputationManager : NetworkBehaviour
    {
        [Header("Clout Score")]
        public readonly SyncVar<int> cloutScore = new SyncVar<int>(0);
        public readonly SyncVar<int> cloutRank = new SyncVar<int>(0);

        [Header("Reputation Tracks")]
        public readonly SyncVar<float> streetRep = new SyncVar<float>(0f);
        public readonly SyncVar<float> civilianRep = new SyncVar<float>(50f);
        public readonly SyncVar<float> rivalRep = new SyncVar<float>(0f);
        public readonly SyncVar<float> supplierRep = new SyncVar<float>(0f);

        [Header("Clout Rank Thresholds")]
        public int[] rankThresholds = { 0, 100, 500, 2000, 10000, 50000 };
        public string[] rankNames = { "Nobody", "Corner Boy", "Hustler", "Shot Caller", "Kingpin", "Legend" };

        // Offline backing fields — used when FishNet isn't active
        private int _offlineClout;
        private int _offlineRank;

        public event Action<int, int> OnCloutChanged;
        public event Action<int, string> OnRankUp;

        /// <summary>
        /// Whether FishNet is active and this object is network-spawned.
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
        /// Get current clout score (works both online and offline).
        /// </summary>
        public int CurrentClout
        {
            get
            {
                try { return IsNetworked ? cloutScore.Value : _offlineClout; }
                catch { return _offlineClout; }
            }
        }

        /// <summary>
        /// Get current clout rank (works both online and offline).
        /// </summary>
        public int CurrentRank
        {
            get
            {
                try { return IsNetworked ? cloutRank.Value : _offlineRank; }
                catch { return _offlineRank; }
            }
        }

        public string CurrentRankName => rankNames[Mathf.Clamp(CurrentRank, 0, rankNames.Length - 1)];

        /// <summary>
        /// Add clout from an action. Works both online (server-authoritative) and offline.
        /// </summary>
        public void AddClout(int amount, string reason)
        {
            int oldClout;

            if (IsNetworked)
            {
                try
                {
                    oldClout = cloutScore.Value;
                    cloutScore.Value += amount;

                    int newRank = CalculateRank(cloutScore.Value);
                    if (newRank > cloutRank.Value)
                    {
                        cloutRank.Value = newRank;
                        OnRankUp?.Invoke(cloutRank.Value, rankNames[cloutRank.Value]);
                        Debug.Log($"[Clout] RANK UP: {rankNames[cloutRank.Value]}! (Clout: {cloutScore.Value})");
                    }

                    OnCloutChanged?.Invoke(oldClout, cloutScore.Value);
                    Debug.Log($"[Clout] +{amount} ({reason}) -> Total: {cloutScore.Value} [{rankNames[cloutRank.Value]}]");
                    return;
                }
                catch
                {
                    // Fall through to offline path
                }
            }

            // Offline path
            oldClout = _offlineClout;
            _offlineClout += amount;

            int newOfflineRank = CalculateRank(_offlineClout);
            if (newOfflineRank > _offlineRank)
            {
                _offlineRank = newOfflineRank;
                OnRankUp?.Invoke(_offlineRank, rankNames[_offlineRank]);
                Debug.Log($"[Clout] RANK UP: {rankNames[_offlineRank]}! (Clout: {_offlineClout})");
            }

            OnCloutChanged?.Invoke(oldClout, _offlineClout);
            Debug.Log($"[Clout] +{amount} ({reason}) -> Total: {_offlineClout} [{rankNames[_offlineRank]}]");
        }

        public void RemoveClout(int amount, string reason)
        {
            if (IsNetworked)
            {
                try
                {
                    int oldClout = cloutScore.Value;
                    cloutScore.Value = Mathf.Max(0, cloutScore.Value - amount);

                    int newRank = CalculateRank(cloutScore.Value);
                    if (newRank < cloutRank.Value)
                    {
                        cloutRank.Value = newRank;
                        Debug.Log($"[Clout] Rank dropped to: {rankNames[cloutRank.Value]}");
                    }

                    OnCloutChanged?.Invoke(oldClout, cloutScore.Value);
                    return;
                }
                catch { }
            }

            // Offline path
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
