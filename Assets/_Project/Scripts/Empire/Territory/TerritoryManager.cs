using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace Clout.Empire.Territory
{
    /// <summary>
    /// Manages territory control zones across the city.
    /// Territories have owners (players/AI cartels), influence levels,
    /// and economic properties (customer density, police presence, rival threat).
    ///
    /// Multiplayer: server-authoritative territory ownership with SyncVars.
    /// This is the PvPvE competitive layer — fight for corners, blocks, districts.
    /// </summary>
    public class TerritoryManager : NetworkBehaviour
    {
        [Header("Config")]
        public TerritoryZone[] zones;

        // Runtime — server manages all territory state
        private Dictionary<string, TerritoryState> _territoryStates = new Dictionary<string, TerritoryState>();

        public override void OnStartServer()
        {
            base.OnStartServer();
            InitializeZones();
        }

        private void InitializeZones()
        {
            foreach (var zone in zones)
            {
                _territoryStates[zone.zoneId] = new TerritoryState
                {
                    zoneId = zone.zoneId,
                    ownerId = -1,              // Unclaimed
                    influence = 0f,
                    heatLevel = zone.baseHeatLevel,
                    customerDensity = zone.baseCustomerDensity
                };
            }
        }

        /// <summary>
        /// Claim or contest a territory zone. Influence builds over time
        /// through dealing, property ownership, and defeating rivals.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AddInfluenceServerRpc(string zoneId, int playerId, float amount)
        {
            if (!_territoryStates.TryGetValue(zoneId, out var state)) return;

            if (state.ownerId == playerId || state.ownerId == -1)
            {
                state.influence += amount;
                state.ownerId = playerId;

                if (state.influence >= 100f)
                {
                    // Fully controlled — unlock zone bonuses
                    OnZoneControlled(zoneId, playerId);
                }
            }
            else
            {
                // Contested — reduce current owner's influence
                state.influence -= amount * 0.5f;
                if (state.influence <= 0)
                {
                    // Territory flipped!
                    state.ownerId = playerId;
                    state.influence = amount;
                    OnZoneContested(zoneId, playerId);
                }
            }

            _territoryStates[zoneId] = state;
        }

        private void OnZoneControlled(string zoneId, int playerId)
        {
            Debug.Log($"[Territory] Zone {zoneId} fully controlled by player {playerId}");
        }

        private void OnZoneContested(string zoneId, int playerId)
        {
            Debug.Log($"[Territory] Zone {zoneId} contested — new owner: player {playerId}");
        }
    }

    [System.Serializable]
    public class TerritoryZone
    {
        public string zoneId;
        public string zoneName;
        public Bounds worldBounds;
        public float baseCustomerDensity = 0.5f;
        public float baseHeatLevel = 0.1f;
        public float baseRivalThreat = 0.2f;
    }

    [System.Serializable]
    public struct TerritoryState
    {
        public string zoneId;
        public int ownerId;
        public float influence;
        public float heatLevel;
        public float customerDensity;
    }
}
