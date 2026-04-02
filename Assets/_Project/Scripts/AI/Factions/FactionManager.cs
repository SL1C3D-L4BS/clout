using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Clout.Core;
using Clout.Utils;
using Clout.Empire.Territory;

namespace Clout.AI.Factions
{
    /// <summary>
    /// Step 14 — Central orchestrator for all rival factions.
    /// Singleton that manages faction lifecycle, daily AI ticks, and
    /// bridges between factions and all existing game systems.
    ///
    /// Hooks into TransactionLedger.OnDayEnd for the daily decision loop.
    /// </summary>
    public class FactionManager : MonoBehaviour
    {
        public static FactionManager Instance { get; private set; }

        [Header("Faction Profiles")]
        [Tooltip("Assign all FactionProfile ScriptableObjects here")]
        public FactionProfile[] factionProfiles;

        [Header("References")]
        [Tooltip("Reference to TerritoryManager for zone operations")]
        public TerritoryManager territoryManager;

        // ─── Runtime State ─────────────────────────────────────────

        private Dictionary<FactionId, FactionRuntimeState> _factions
            = new Dictionary<FactionId, FactionRuntimeState>();

        private int _currentGameDay = 0;

        // ─── Player Estimates (updated externally or by event) ─────

        private float _playerWealth = 500f;
        private float _playerStrength = 0.3f;
        private float _playerFear = 0f;

        // ─── Lifecycle ─────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            InitializeFactions();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (Instance == this) Instance = null;
        }

        // ─── Initialization ────────────────────────────────────────

        private void InitializeFactions()
        {
            if (factionProfiles == null || factionProfiles.Length == 0)
            {
                // Auto-load from Resources if not assigned in inspector
                factionProfiles = Resources.LoadAll<FactionProfile>("Factions");
            }

            foreach (var profile in factionProfiles)
            {
                if (profile == null || profile.factionId == FactionId.None) continue;

                var state = new FactionRuntimeState
                {
                    Profile = profile,
                    Cash = profile.startingCash,
                    CombatStrength = profile.baseCombatStrength,
                    HeatLevel = 0f,
                    PlayerDisposition = 0f,
                    ControlledZones = new List<string>(),
                    FactionDispositions = new Dictionary<FactionId, float>(),
                    DailyRevenue = 0f,
                    DailyExpenses = 0f,
                    IsAtWarWithPlayer = false,
                    IsAlliedWithPlayer = false,
                    DaysSinceLastAction = 0,
                    DaysSinceBetrayalEligible = 0,
                    CurrentMood = FactionMood.Passive,
                    DailySupply = profile.baseSupplyPerDay,
                    RecentTerritoryLosses = 0,
                    PerceivedThreatLevel = 0f,
                    HasPendingAction = false,
                    PendingActionType = DiplomacyAction.ProposeAlliance,
                    PendingDemand = 0f
                };

                // Initialize inter-faction dispositions to neutral
                foreach (var other in factionProfiles)
                {
                    if (other != null && other.factionId != profile.factionId && other.factionId != FactionId.None)
                        state.FactionDispositions[other.factionId] = 0f;
                }

                // Claim starting zones
                if (profile.startingZoneIds != null && territoryManager != null)
                {
                    foreach (var zoneId in profile.startingZoneIds)
                    {
                        int factionOwnerId = (int)profile.factionId + 100; // Offset to avoid player ID collision
                        territoryManager.AddInfluence(zoneId, factionOwnerId, 100f);
                        state.ControlledZones.Add(zoneId);
                    }
                }

                _factions[profile.factionId] = state;
                Debug.Log($"[FactionManager] Initialized {profile.factionName} ({profile.GetArchetype()}) — " +
                          $"Zones: {state.ControlledZones.Count}, Cash: ${state.Cash:F0}, Combat: {state.CombatStrength:F2}");
            }

            Debug.Log($"[FactionManager] {_factions.Count} rival factions active");
        }

        // ─── Event Subscriptions ───────────────────────────────────

        private void SubscribeEvents()
        {
            EventBus.Subscribe<DealCompletedEvent>(OnDealCompleted);
            EventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);
            EventBus.Subscribe<MoneyChangedEvent>(OnMoneyChanged);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<DealCompletedEvent>(OnDealCompleted);
            EventBus.Unsubscribe<ReputationChangedEvent>(OnReputationChanged);
            EventBus.Unsubscribe<MoneyChangedEvent>(OnMoneyChanged);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        // ─── Event Handlers ────────────────────────────────────────

        private void OnDealCompleted(DealCompletedEvent evt)
        {
            // Player dealing in faction territory affects disposition
            foreach (var kvp in _factions)
            {
                if (kvp.Value.ControlledZones.Contains(evt.districtId))
                {
                    // Dealing on their turf — they don't like that
                    ModifyDisposition(kvp.Key, -0.02f, "Player dealing in our territory");
                    break;
                }
            }
        }

        private void OnReputationChanged(ReputationChangedEvent evt)
        {
            // Update player fear estimate from reputation
            if (evt.dimension == Clout.Empire.Reputation.ReputationDimension.Fear)
            {
                _playerFear = evt.newValue;
            }
        }

        private void OnMoneyChanged(MoneyChangedEvent evt)
        {
            _playerWealth = evt.totalCash;
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Player strength grows with kills
            _playerStrength = Mathf.Min(1f, _playerStrength + 0.01f);
        }

        // ═══════════════════════════════════════════════════════════
        //  DAILY TICK — Called by TransactionLedger.OnDayEnd
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Process all factions for one game day.
        /// Call this from TransactionLedger.OnDayEnd or game day tick.
        /// </summary>
        public void ProcessDayEnd()
        {
            _currentGameDay++;
            var config = GameBalanceConfig.Active;

            foreach (var kvp in _factions)
            {
                var state = kvp.Value;

                // ─── Revenue ──────────────────────────────────
                state.DailyRevenue = state.ControlledZones.Count * config.factionDailyIncomePerZone;
                state.Cash += state.DailyRevenue;

                // ─── Combat Strength Regen ────────────────────
                state.CombatStrength = Mathf.Min(config.factionMaxCombatStrength,
                    state.CombatStrength + config.factionCombatStrengthRegen);

                // ─── Heat Decay ───────────────────────────────
                state.HeatLevel = Mathf.Max(0f, state.HeatLevel - 5f);

                // ─── AI Decision ──────────────────────────────
                if (state.DaysSinceLastAction >= config.factionMinDaysBetweenActions)
                {
                    var decision = FactionAI.Evaluate(state, this);
                    ExecuteDecision(state, decision);
                    state.DaysSinceLastAction = 0;

                    EventBus.Publish(new FactionDayProcessedEvent
                    {
                        factionId = state.Profile.factionId,
                        actionTaken = decision.action.ToString(),
                        currentMood = state.CurrentMood
                    });
                }
                else
                {
                    state.DaysSinceLastAction++;
                }

                // ─── Update Mood ──────────────────────────────
                state.CurrentMood = FactionAI.DetermineMood(state);

                // ─── Decay recent losses ──────────────────────
                state.RecentTerritoryLosses = Mathf.Max(0, state.RecentTerritoryLosses - 1);
            }

            // ─── Process Diplomacy ────────────────────────────────
            if (FactionDiplomacy.Instance != null)
            {
                FactionDiplomacy.Instance.ProcessDailyDiplomacy();
            }
        }

        // ─── Decision Execution ────────────────────────────────────

        private void ExecuteDecision(FactionRuntimeState state, FactionAI.DecisionResult decision)
        {
            var config = GameBalanceConfig.Active;
            var profile = state.Profile;

            switch (decision.action)
            {
                case FactionAI.FactionAction.ExpandTerritory:
                    ExecuteExpand(state, decision.targetZoneId, config);
                    break;

                case FactionAI.FactionAction.AttackPlayer:
                    ExecuteAttackPlayer(state, config);
                    break;

                case FactionAI.FactionAction.AttackRival:
                    ExecuteAttackRival(state, decision.targetFaction);
                    break;

                case FactionAI.FactionAction.DefendTerritory:
                    ExecuteDefend(state, config);
                    break;

                case FactionAI.FactionAction.EconomicFocus:
                    ExecuteEconomicFocus(state);
                    break;

                case FactionAI.FactionAction.SeekAlliance:
                    ExecuteSeekAlliance(state);
                    break;

                case FactionAI.FactionAction.UndercutPrices:
                    ExecuteUndercutPrices(state, config);
                    break;
            }

            Debug.Log($"[FactionManager] {profile.factionName} → {decision.action} (score: {decision.score:F2})");
        }

        private void ExecuteExpand(FactionRuntimeState state, string targetZone, GameBalanceConfig config)
        {
            if (string.IsNullOrEmpty(targetZone) || territoryManager == null) return;

            int factionOwnerId = (int)state.Profile.factionId + 100;
            territoryManager.AddInfluence(targetZone, factionOwnerId, config.factionExpansionRate);

            // Check if we just claimed it
            var currentOwner = GetFactionControllingZone(targetZone);
            if (currentOwner == state.Profile.factionId && !state.ControlledZones.Contains(targetZone))
            {
                state.ControlledZones.Add(targetZone);
            }

            EventBus.Publish(new FactionExpandedEvent
            {
                factionId = state.Profile.factionId,
                zoneId = targetZone,
                newInfluence = config.factionExpansionRate
            });
        }

        private void ExecuteAttackPlayer(FactionRuntimeState state, GameBalanceConfig config)
        {
            if (!state.IsAtWarWithPlayer)
            {
                // Escalate to war
                if (FactionDiplomacy.Instance != null)
                    FactionDiplomacy.Instance.AIDeclareWarOnPlayer(state.Profile.factionId);
            }

            // Deal damage — reduce player territory influence
            if (state.ControlledZones.Count > 0 && territoryManager != null)
            {
                // Attack from closest faction zone
                int factionOwnerId = (int)state.Profile.factionId + 100;
                float attackPower = config.factionAttackDamage * state.CombatStrength * 50f;

                // Reduce player influence in contested zones
                EventBus.Publish(new FactionAttackEvent
                {
                    attacker = state.Profile.factionId,
                    targetFaction = FactionId.None,
                    targetsPlayer = true,
                    attackStrength = attackPower
                });
            }

            // War generates heat
            state.HeatLevel += config.factionWarHeatGeneration;
        }

        private void ExecuteAttackRival(FactionRuntimeState state, FactionId target)
        {
            if (target == FactionId.None) return;

            if (FactionDiplomacy.Instance != null)
                FactionDiplomacy.Instance.AIAttackFaction(state.Profile.factionId, target);
        }

        private void ExecuteDefend(FactionRuntimeState state, GameBalanceConfig config)
        {
            // Fortify — boost influence in all owned zones
            if (territoryManager == null) return;

            int factionOwnerId = (int)state.Profile.factionId + 100;
            foreach (var zoneId in state.ControlledZones)
            {
                territoryManager.AddInfluence(zoneId, factionOwnerId, config.factionExpansionRate * 0.5f);
            }
        }

        private void ExecuteEconomicFocus(FactionRuntimeState state)
        {
            // Generate bonus cash from production
            float bonus = state.DailySupply * 50f * state.Profile.greed;
            state.Cash += bonus;
            state.DailyRevenue += bonus;
        }

        private void ExecuteSeekAlliance(FactionRuntimeState state)
        {
            if (state.IsAlliedWithPlayer || state.IsAtWarWithPlayer) return;

            if (FactionDiplomacy.Instance != null)
                FactionDiplomacy.Instance.AIProposeAllianceToPlayer(state.Profile.factionId);
        }

        private void ExecuteUndercutPrices(FactionRuntimeState state, GameBalanceConfig config)
        {
            // Price undercutting — affects market competition
            // MarketSimulator integration: rivalBaseSupplyPerDay already exists in config
            state.DailySupply = state.Profile.baseSupplyPerDay * (1f + config.factionUndercutIntensity);

            // Costs money to undercut
            float undercutCost = state.Cash * 0.05f;
            state.Cash -= undercutCost;
            state.DailyExpenses += undercutCost;
        }

        // ═══════════════════════════════════════════════════════════
        //  PUBLIC API — Queried by FactionAI and other systems
        // ═══════════════════════════════════════════════════════════

        public FactionRuntimeState GetFaction(FactionId id)
        {
            _factions.TryGetValue(id, out var state);
            return state;
        }

        public List<FactionRuntimeState> GetAllFactions()
        {
            return _factions.Values.ToList();
        }

        public List<string> GetAllZoneIds()
        {
            var zones = new List<string>();
            if (territoryManager != null && territoryManager.zones != null)
            {
                foreach (var zone in territoryManager.zones)
                    zones.Add(zone.zoneId);
            }
            return zones;
        }

        public FactionId GetFactionControllingZone(string zoneId)
        {
            foreach (var kvp in _factions)
            {
                if (kvp.Value.ControlledZones.Contains(zoneId))
                    return kvp.Key;
            }
            return FactionId.None;
        }

        public void ModifyDisposition(FactionId factionId, float amount, string reason)
        {
            if (!_factions.TryGetValue(factionId, out var state)) return;

            float old = state.PlayerDisposition;
            state.PlayerDisposition = Mathf.Clamp(state.PlayerDisposition + amount, -1f, 1f);

            EventBus.Publish(new FactionDispositionChangedEvent
            {
                factionId = factionId,
                oldValue = old,
                newValue = state.PlayerDisposition,
                reason = reason
            });
        }

        public void DamageFaction(FactionId factionId, float amount)
        {
            if (!_factions.TryGetValue(factionId, out var state)) return;
            state.CombatStrength = Mathf.Max(0.05f, state.CombatStrength - amount);

            // Check for defeat
            if (state.CombatStrength <= 0.05f && state.ControlledZones.Count == 0)
            {
                EventBus.Publish(new FactionDefeatedEvent
                {
                    defeated = factionId,
                    victor = FactionId.None,
                    victorIsPlayer = true
                });
            }
        }

        // ─── Player Estimates ──────────────────────────────────────

        public float GetPlayerWealthEstimate() => _playerWealth;
        public float GetPlayerStrengthEstimate() => _playerStrength;
        public float GetPlayerFear() => _playerFear;

        public void SetPlayerStrengthEstimate(float value) => _playerStrength = Mathf.Clamp01(value);

        // ─── Utility Queries ───────────────────────────────────────

        public FactionRuntimeState GetStrongestFaction()
        {
            FactionRuntimeState best = null;
            float bestStrength = -1f;
            foreach (var kvp in _factions)
            {
                float s = kvp.Value.CombatStrength + kvp.Value.ControlledZones.Count * 0.1f;
                if (s > bestStrength) { bestStrength = s; best = kvp.Value; }
            }
            return best;
        }

        public FactionRuntimeState GetWeakestFaction()
        {
            FactionRuntimeState worst = null;
            float worstStrength = float.MaxValue;
            foreach (var kvp in _factions)
            {
                float s = kvp.Value.CombatStrength + kvp.Value.ControlledZones.Count * 0.1f;
                if (s < worstStrength) { worstStrength = s; worst = kvp.Value; }
            }
            return worst;
        }

        public int GetCurrentGameDay() => _currentGameDay;

        public int GetTotalFactionZones()
        {
            int total = 0;
            foreach (var kvp in _factions)
                total += kvp.Value.ControlledZones.Count;
            return total;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  FACTION RUNTIME STATE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Mutable runtime state for a single faction. Created at initialization,
    /// updated every game day by FactionManager + FactionAI.
    /// </summary>
    public class FactionRuntimeState
    {
        public FactionProfile Profile;
        public float Cash;
        public float CombatStrength;
        public float HeatLevel;
        public float PlayerDisposition;
        public List<string> ControlledZones;
        public Dictionary<FactionId, float> FactionDispositions;
        public float DailyRevenue;
        public float DailyExpenses;
        public float DailySupply;
        public bool IsAtWarWithPlayer;
        public bool IsAlliedWithPlayer;
        public int DaysSinceLastAction;
        public int DaysSinceBetrayalEligible;
        public FactionMood CurrentMood;
        public int RecentTerritoryLosses;
        public float PerceivedThreatLevel;

        // Pending diplomacy actions (for UI notification)
        public bool HasPendingAction;
        public DiplomacyAction PendingActionType;
        public float PendingDemand;
    }
}
