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
    /// </summary>
    public class ReputationManager : NetworkBehaviour
    {
        [Header("Clout Score")]
        [SyncVar] public int cloutScore = 0;
        [SyncVar] public int cloutRank = 0;          // Tier: 0=Nobody, 1=Corner Boy, 2=Hustler, 3=Boss, 4=Kingpin, 5=Legend

        [Header("Reputation Tracks")]
        [SyncVar] public float streetRep = 0f;        // Criminal underworld
        [SyncVar] public float civilianRep = 50f;     // Public perception (starts neutral)
        [SyncVar] public float rivalRep = 0f;         // Other empire fear/respect
        [SyncVar] public float supplierRep = 0f;      // Wholesale connections

        [Header("Clout Rank Thresholds")]
        public int[] rankThresholds = { 0, 100, 500, 2000, 10000, 50000 };
        public string[] rankNames = { "Nobody", "Corner Boy", "Hustler", "Shot Caller", "Kingpin", "Legend" };

        public event Action<int, int> OnCloutChanged;     // oldClout, newClout
        public event Action<int, string> OnRankUp;        // newRank, rankName

        /// <summary>
        /// Add clout from an action. Server-authoritative.
        /// </summary>
        [Server]
        public void AddClout(int amount, string reason)
        {
            int oldClout = cloutScore;
            cloutScore += amount;

            // Check for rank up
            int newRank = CalculateRank();
            if (newRank > cloutRank)
            {
                cloutRank = newRank;
                OnRankUp?.Invoke(cloutRank, rankNames[cloutRank]);
                Debug.Log($"[Clout] RANK UP: {rankNames[cloutRank]}! (Clout: {cloutScore})");
            }

            OnCloutChanged?.Invoke(oldClout, cloutScore);
            Debug.Log($"[Clout] +{amount} ({reason}) -> Total: {cloutScore} [{rankNames[cloutRank]}]");
        }

        [Server]
        public void RemoveClout(int amount, string reason)
        {
            int oldClout = cloutScore;
            cloutScore = Mathf.Max(0, cloutScore - amount);

            int newRank = CalculateRank();
            if (newRank < cloutRank)
            {
                cloutRank = newRank;
                Debug.Log($"[Clout] Rank dropped to: {rankNames[cloutRank]}");
            }

            OnCloutChanged?.Invoke(oldClout, cloutScore);
        }

        private int CalculateRank()
        {
            for (int i = rankThresholds.Length - 1; i >= 0; i--)
            {
                if (cloutScore >= rankThresholds[i])
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// Check if player has enough clout for an action.
        /// </summary>
        public bool HasCloutRank(int minimumRank) => cloutRank >= minimumRank;

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
