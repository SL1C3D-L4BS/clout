using UnityEngine;
using Clout.Core;
using Clout.Utils;

namespace Clout.AI.Factions
{
    /// <summary>
    /// Step 14 — Diplomacy system handling all player↔faction and faction↔faction
    /// diplomatic interactions: alliances, wars, tribute, trade, betrayal.
    ///
    /// Disposition scale: -1.0 (hostile war) to +1.0 (blood alliance)
    /// Relationship tiers auto-derived from disposition value.
    /// </summary>
    public class FactionDiplomacy : MonoBehaviour
    {
        public static FactionDiplomacy Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ─── Relationship Tier from Disposition ────────────────────

        public static FactionRelationship GetRelationship(float disposition)
        {
            if (disposition <= -0.6f) return FactionRelationship.War;
            if (disposition <= -0.2f) return FactionRelationship.Hostile;
            if (disposition < 0.3f)  return FactionRelationship.Neutral;
            if (disposition < 0.7f)  return FactionRelationship.Friendly;
            return FactionRelationship.Allied;
        }

        // ═══════════════════════════════════════════════════════════
        //  PLAYER-INITIATED ACTIONS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Player proposes alliance. Requires disposition ≥ allianceReq.
        /// </summary>
        public bool PlayerProposeAlliance(FactionId factionId)
        {
            var state = FactionManager.Instance.GetFaction(factionId);
            if (state == null) return false;

            var config = GameBalanceConfig.Active;
            if (state.PlayerDisposition < config.factionAllianceDispositionReq)
            {
                Debug.Log($"[Diplomacy] {state.Profile.factionName} rejected alliance — disposition too low ({state.PlayerDisposition:F2})");
                return false;
            }

            if (state.IsAlliedWithPlayer)
            {
                Debug.Log($"[Diplomacy] Already allied with {state.Profile.factionName}");
                return false;
            }

            // Accept!
            state.IsAlliedWithPlayer = true;
            state.IsAtWarWithPlayer = false;
            FactionManager.Instance.ModifyDisposition(factionId, 0.2f, "Alliance formed");

            EventBus.Publish(new FactionAllianceFormedEvent
            {
                factionId = factionId,
                withPlayer = true
            });

            Debug.Log($"[Diplomacy] Alliance formed with {state.Profile.factionName}!");
            return true;
        }

        /// <summary>
        /// Player offers tribute (cash) to improve disposition.
        /// +0.1 disposition per tributeRate cash.
        /// </summary>
        public bool PlayerOfferTribute(FactionId factionId, float cashAmount)
        {
            var state = FactionManager.Instance.GetFaction(factionId);
            if (state == null) return false;

            var config = GameBalanceConfig.Active;
            float dispositionGain = (cashAmount / config.factionTributeRate) * 0.1f;
            dispositionGain = Mathf.Min(dispositionGain, 0.3f); // Cap per transaction

            FactionManager.Instance.ModifyDisposition(factionId, dispositionGain,
                $"Tribute of ${cashAmount:F0}");

            EventBus.Publish(new FactionTributeEvent
            {
                from = FactionId.None, // Player
                to = factionId,
                fromPlayer = true,
                amount = cashAmount
            });

            state.Cash += cashAmount;

            Debug.Log($"[Diplomacy] Paid ${cashAmount:F0} tribute to {state.Profile.factionName} — disposition +{dispositionGain:F2}");
            return true;
        }

        /// <summary>
        /// Player declares war on a faction.
        /// </summary>
        public void PlayerDeclareWar(FactionId factionId)
        {
            var state = FactionManager.Instance.GetFaction(factionId);
            if (state == null) return;

            bool wasAlly = state.IsAlliedWithPlayer;
            state.IsAtWarWithPlayer = true;
            state.IsAlliedWithPlayer = false;
            FactionManager.Instance.ModifyDisposition(factionId, -1f, "War declared by player");
            state.PlayerDisposition = -1f; // Force to max hostile

            EventBus.Publish(new FactionWarDeclaredEvent
            {
                aggressor = FactionId.None, // Player
                targetFaction = factionId,
                targetsPlayer = false
            });

            // Betrayal penalty — all factions lose trust if we broke an alliance
            if (wasAlly)
            {
                var config = GameBalanceConfig.Active;
                foreach (var faction in FactionManager.Instance.GetAllFactions())
                {
                    if (faction.Profile.factionId == factionId) continue;
                    FactionManager.Instance.ModifyDisposition(faction.Profile.factionId,
                        -config.factionBetrayalDispositionPenalty, "Player betrayed an ally");
                }

                EventBus.Publish(new FactionBetrayalEvent
                {
                    betrayer = FactionId.None,
                    victim = factionId,
                    victimIsPlayer = false
                });
            }

            Debug.Log($"[Diplomacy] War declared on {state.Profile.factionName}!");
        }

        /// <summary>
        /// Player requests ceasefire. Costs a fraction of faction's cash as reparations.
        /// </summary>
        public bool PlayerRequestCeasefire(FactionId factionId, out float reparationCost)
        {
            var state = FactionManager.Instance.GetFaction(factionId);
            reparationCost = 0f;
            if (state == null) return false;

            if (!state.IsAtWarWithPlayer)
            {
                Debug.Log($"[Diplomacy] Not at war with {state.Profile.factionName}");
                return false;
            }

            var config = GameBalanceConfig.Active;
            reparationCost = state.Cash * config.factionCeasefireRate;

            // AI acceptance check — based on their combat strength vs ours
            float acceptChance = 0.3f + (1f - state.CombatStrength) * 0.4f;
            acceptChance += state.Profile.diplomacy * 0.2f;
            acceptChance -= state.Profile.aggression * 0.15f;

            if (Random.value > acceptChance)
            {
                Debug.Log($"[Diplomacy] {state.Profile.factionName} rejected ceasefire!");
                return false;
            }

            state.IsAtWarWithPlayer = false;
            FactionManager.Instance.ModifyDisposition(factionId, 0.3f, "Ceasefire accepted");

            Debug.Log($"[Diplomacy] Ceasefire with {state.Profile.factionName} — reparations: ${reparationCost:F0}");
            return true;
        }

        /// <summary>
        /// Player proposes trade deal with a faction.
        /// Boosts disposition and provides economic benefit to both.
        /// </summary>
        public bool PlayerProposeTrade(FactionId factionId, ProductType product, float quantity)
        {
            var state = FactionManager.Instance.GetFaction(factionId);
            if (state == null) return false;

            if (state.IsAtWarWithPlayer)
            {
                Debug.Log($"[Diplomacy] Cannot trade during war with {state.Profile.factionName}");
                return false;
            }

            // Acceptance based on diplomacy personality and product desire
            float acceptChance = 0.4f + state.Profile.diplomacy * 0.3f;
            if (state.PlayerDisposition > 0f) acceptChance += 0.2f;

            if (Random.value > acceptChance)
            {
                Debug.Log($"[Diplomacy] {state.Profile.factionName} declined trade offer");
                return false;
            }

            FactionManager.Instance.ModifyDisposition(factionId, 0.05f, "Trade deal");

            EventBus.Publish(new FactionTradeProposedEvent
            {
                factionId = factionId,
                product = product,
                amount = quantity,
                toPlayer = false
            });

            Debug.Log($"[Diplomacy] Trade accepted: {quantity} units of {product} with {state.Profile.factionName}");
            return true;
        }

        /// <summary>
        /// Player betrays an existing alliance. Massive reputation and disposition hit.
        /// </summary>
        public void PlayerBetrayAlliance(FactionId factionId)
        {
            var state = FactionManager.Instance.GetFaction(factionId);
            if (state == null || !state.IsAlliedWithPlayer) return;

            PlayerDeclareWar(factionId); // Handles all the betrayal logic
        }

        // ═══════════════════════════════════════════════════════════
        //  AI-INITIATED ACTIONS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// AI faction demands tribute from the player. Called by FactionManager
        /// when a strong aggressive faction decides to pressure the player.
        /// </summary>
        public void AIDemandTribute(FactionId factionId, float demandAmount)
        {
            var state = FactionManager.Instance.GetFaction(factionId);
            if (state == null) return;

            // Store as pending demand — UI will present it
            state.PendingDemand = demandAmount;
            state.HasPendingAction = true;
            state.PendingActionType = DiplomacyAction.DemandTribute;

            Debug.Log($"[Diplomacy] {state.Profile.factionName} demands ${demandAmount:F0} tribute from player!");
        }

        /// <summary>
        /// AI faction declares war on the player.
        /// </summary>
        public void AIDeclareWarOnPlayer(FactionId factionId)
        {
            var state = FactionManager.Instance.GetFaction(factionId);
            if (state == null) return;

            bool wasAlly = state.IsAlliedWithPlayer;
            state.IsAtWarWithPlayer = true;
            state.IsAlliedWithPlayer = false;
            state.PlayerDisposition = -1f;

            EventBus.Publish(new FactionWarDeclaredEvent
            {
                aggressor = factionId,
                targetFaction = FactionId.None,
                targetsPlayer = true
            });

            if (wasAlly)
            {
                EventBus.Publish(new FactionBetrayalEvent
                {
                    betrayer = factionId,
                    victim = FactionId.None,
                    victimIsPlayer = true
                });
            }

            Debug.Log($"[Diplomacy] {state.Profile.factionName} declares WAR on the player!");
        }

        /// <summary>
        /// AI proposes alliance to the player. Stored as pending for UI display.
        /// </summary>
        public void AIProposeAllianceToPlayer(FactionId factionId)
        {
            var state = FactionManager.Instance.GetFaction(factionId);
            if (state == null || state.IsAlliedWithPlayer) return;

            state.HasPendingAction = true;
            state.PendingActionType = DiplomacyAction.ProposeAlliance;

            Debug.Log($"[Diplomacy] {state.Profile.factionName} proposes alliance with the player!");
        }

        /// <summary>
        /// AI faction attacks another AI faction. Both lose combat strength.
        /// </summary>
        public void AIAttackFaction(FactionId attacker, FactionId defender)
        {
            var attackerState = FactionManager.Instance.GetFaction(attacker);
            var defenderState = FactionManager.Instance.GetFaction(defender);
            if (attackerState == null || defenderState == null) return;

            var config = GameBalanceConfig.Active;
            float damage = config.factionAttackDamage;

            // Attacker deals damage scaled by combat strength
            float attackDamage = damage * attackerState.CombatStrength;
            float counterDamage = damage * defenderState.CombatStrength * 0.6f; // Defender fights back

            defenderState.CombatStrength = Mathf.Max(0.05f, defenderState.CombatStrength - attackDamage);
            attackerState.CombatStrength = Mathf.Max(0.05f, attackerState.CombatStrength - counterDamage);

            // Disposition impact
            float currentDisp = 0f;
            defenderState.FactionDispositions.TryGetValue(attacker, out currentDisp);
            defenderState.FactionDispositions[attacker] = Mathf.Max(-1f, currentDisp - 0.3f);

            EventBus.Publish(new FactionAttackEvent
            {
                attacker = attacker,
                targetFaction = defender,
                targetsPlayer = false,
                attackStrength = attackDamage
            });

            // Check for defeat
            if (defenderState.CombatStrength <= 0.05f && defenderState.ControlledZones.Count == 0)
            {
                EventBus.Publish(new FactionDefeatedEvent
                {
                    defeated = defender,
                    victor = attacker,
                    victorIsPlayer = false
                });
            }

            // Generate area heat from the conflict
            // WantedSystem integration — wars attract police attention
            Debug.Log($"[Diplomacy] {attackerState.Profile.factionName} attacked {defenderState.Profile.factionName}! " +
                      $"Damage dealt: {attackDamage:F2}, Counter: {counterDamage:F2}");
        }

        // ═══════════════════════════════════════════════════════════
        //  DAILY PROCESSING
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Process daily diplomacy decay and auto-betrayal checks.
        /// Called by FactionManager during OnDayEnd.
        /// </summary>
        public void ProcessDailyDiplomacy()
        {
            var config = GameBalanceConfig.Active;
            var factions = FactionManager.Instance.GetAllFactions();

            foreach (var state in factions)
            {
                // Decay disposition toward neutral
                if (state.PlayerDisposition > 0.01f)
                {
                    state.PlayerDisposition = Mathf.Max(0f,
                        state.PlayerDisposition - config.factionDispositionDecayRate);
                }
                else if (state.PlayerDisposition < -0.01f)
                {
                    state.PlayerDisposition = Mathf.Min(0f,
                        state.PlayerDisposition + config.factionDispositionDecayRate);
                }

                // Betrayal check — allied factions may backstab
                if (state.IsAlliedWithPlayer && state.PlayerDisposition < state.Profile.betrayalThreshold)
                {
                    state.DaysSinceBetrayalEligible++;
                    if (state.DaysSinceBetrayalEligible >= config.factionBetrayalCooldown)
                    {
                        float betrayalChance = state.Profile.aggression * 0.3f +
                                             (1f - state.Profile.diplomacy) * 0.2f;
                        if (Random.value < betrayalChance)
                        {
                            AIDeclareWarOnPlayer(state.Profile.factionId);
                            state.DaysSinceBetrayalEligible = 0;
                        }
                    }
                }
                else
                {
                    state.DaysSinceBetrayalEligible = 0;
                }
            }
        }
    }
}
