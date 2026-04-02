using UnityEngine;
using System.Collections.Generic;
using Clout.Core;

namespace Clout.AI.Factions
{
    /// <summary>
    /// Step 14 — Utility Theory decision engine for rival factions.
    /// Pure static logic: input faction state → output scored action.
    ///
    /// Each faction evaluates 7 possible actions every game day, scores them
    /// using personality-weighted utility functions, and executes the highest.
    /// Random jitter prevents perfectly predictable behavior.
    ///
    /// ASSESS → DECIDE → EXECUTE
    /// </summary>
    public static class FactionAI
    {
        // ─── Action Types ──────────────────────────────────────────

        public enum FactionAction
        {
            ExpandTerritory,
            AttackPlayer,
            AttackRival,
            DefendTerritory,
            EconomicFocus,
            SeekAlliance,
            UndercutPrices
        }

        // ─── Decision Result ───────────────────────────────────────

        public struct DecisionResult
        {
            public FactionAction action;
            public float score;
            public FactionId targetFaction;
            public string targetZoneId;
        }

        // ─── Main Decision Entry Point ─────────────────────────────

        /// <summary>
        /// Evaluate all possible actions for a faction and return the best one.
        /// Called once per game day by FactionManager.
        /// </summary>
        public static DecisionResult Evaluate(FactionRuntimeState state, FactionManager manager)
        {
            var config = GameBalanceConfig.Active;
            var profile = state.Profile;

            float[] scores = new float[7];
            FactionId bestRivalTarget = FactionId.None;
            string bestZoneTarget = null;

            // ─── Score each action ────────────────────────────────

            scores[0] = ScoreExpandTerritory(state, manager, profile, out bestZoneTarget);
            scores[1] = ScoreAttackPlayer(state, manager, profile);
            scores[2] = ScoreAttackRival(state, manager, profile, out bestRivalTarget);
            scores[3] = ScoreDefendTerritory(state, profile);
            scores[4] = ScoreEconomicFocus(state, profile);
            scores[5] = ScoreSeekAlliance(state, manager, profile);
            scores[6] = ScoreUndercutPrices(state, profile);

            // ─── Apply jitter ─────────────────────────────────────

            float jitter = config.factionDecisionJitter;
            for (int i = 0; i < scores.Length; i++)
            {
                scores[i] *= 1f + Random.Range(-jitter, jitter);
                scores[i] = Mathf.Max(0f, scores[i]);
            }

            // ─── Global modifiers ─────────────────────────────────

            // High heat → boost defend, nerf attacks
            if (state.HeatLevel > profile.heatCautionThreshold)
            {
                float heatPressure = Mathf.Clamp01(state.HeatLevel / 400f);
                scores[3] *= 1f + heatPressure;       // Defend
                scores[1] *= 1f - heatPressure * 0.6f; // Attack player
                scores[2] *= 1f - heatPressure * 0.4f; // Attack rival
            }

            // Low cash → boost economics
            if (state.Cash < profile.desperateCashThreshold)
            {
                float desperation = 1f - (state.Cash / profile.desperateCashThreshold);
                scores[4] *= 1f + desperation * 1.5f;
            }

            // Player disposition modifiers
            if (state.PlayerDisposition < -0.5f)
            {
                scores[1] *= 1.4f; // More likely to attack player
            }
            else if (state.PlayerDisposition > 0.5f)
            {
                scores[1] *= 0.2f; // Very unlikely to attack ally
                scores[5] *= 1.3f; // Reinforce alliance
            }

            // ─── Pick highest ─────────────────────────────────────

            int bestIndex = 0;
            float bestScore = scores[0];
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] > bestScore)
                {
                    bestScore = scores[i];
                    bestIndex = i;
                }
            }

            return new DecisionResult
            {
                action = (FactionAction)bestIndex,
                score = bestScore,
                targetFaction = bestRivalTarget,
                targetZoneId = bestZoneTarget
            };
        }

        // ─── Scoring Functions ─────────────────────────────────────

        private static float ScoreExpandTerritory(FactionRuntimeState state, FactionManager manager,
            FactionProfile profile, out string bestZone)
        {
            bestZone = null;
            int emptyAdjacent = 0;

            // Count unclaimed or weakly held adjacent zones
            var allZones = manager.GetAllZoneIds();
            float bestZoneScore = 0f;

            foreach (var zoneId in allZones)
            {
                // Skip zones we already own
                if (state.ControlledZones.Contains(zoneId)) continue;

                var ownerFaction = manager.GetFactionControllingZone(zoneId);
                float zoneAttractiveness;

                if (ownerFaction == FactionId.None)
                {
                    // Unclaimed — very attractive
                    zoneAttractiveness = 1.0f;
                    emptyAdjacent++;
                }
                else if (ownerFaction != state.Profile.factionId)
                {
                    // Rival-held — less attractive, scaled by our aggression
                    var rivalState = manager.GetFaction(ownerFaction);
                    if (rivalState == null) continue;
                    float rivalWeakness = 1f - rivalState.CombatStrength;
                    zoneAttractiveness = 0.4f + rivalWeakness * 0.4f;
                }
                else continue;

                if (zoneAttractiveness > bestZoneScore)
                {
                    bestZoneScore = zoneAttractiveness;
                    bestZone = zoneId;
                }
            }

            float score = (emptyAdjacent * 0.3f + bestZoneScore * 0.5f) * profile.expansion;
            score += (1f - state.CombatStrength) * profile.aggression * 0.2f;

            return score;
        }

        private static float ScoreAttackPlayer(FactionRuntimeState state, FactionManager manager,
            FactionProfile profile)
        {
            float playerWealth = manager.GetPlayerWealthEstimate();
            float playerStrength = manager.GetPlayerStrengthEstimate();
            float playerFear = manager.GetPlayerFear();

            float wealthAppeal = Mathf.Clamp01(playerWealth / 50000f) * profile.greed;
            float weakness = (1f - playerStrength) * profile.aggression;
            float fearDeterrent = playerFear * profile.caution;

            float score = wealthAppeal * 0.4f + weakness * 0.4f - fearDeterrent * 0.5f;

            // Don't attack allies
            if (state.IsAlliedWithPlayer) score *= 0.05f;

            // War bonus — already at war, keep pressure
            if (state.IsAtWarWithPlayer) score *= 1.5f;

            return Mathf.Max(0f, score);
        }

        private static float ScoreAttackRival(FactionRuntimeState state, FactionManager manager,
            FactionProfile profile, out FactionId bestTarget)
        {
            bestTarget = FactionId.None;
            float bestScore = 0f;

            foreach (var rival in manager.GetAllFactions())
            {
                if (rival.Profile.factionId == state.Profile.factionId) continue;

                float rivalWeakness = 1f - rival.CombatStrength;
                float rivalWealth = Mathf.Clamp01(rival.Cash / 20000f);
                float rivalStrength = rival.CombatStrength;

                // Check disposition toward this rival
                float disposition = 0f;
                state.FactionDispositions.TryGetValue(rival.Profile.factionId, out disposition);

                float score = (rivalWeakness * profile.aggression * 0.5f)
                            + (rivalWealth * profile.greed * 0.3f)
                            - (rivalStrength * profile.caution * 0.4f)
                            - (Mathf.Max(0f, disposition) * 0.3f); // Don't attack friends

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = rival.Profile.factionId;
                }
            }

            return Mathf.Max(0f, bestScore);
        }

        private static float ScoreDefendTerritory(FactionRuntimeState state, FactionProfile profile)
        {
            int zoneCount = state.ControlledZones.Count;
            float recentLosses = state.RecentTerritoryLosses;
            float threat = state.PerceivedThreatLevel;

            float score = (threat * profile.caution * 0.5f)
                        + (recentLosses * 2.0f)
                        + (zoneCount > 0 ? 0.2f : 0f); // Some base motivation if we have zones

            return score;
        }

        private static float ScoreEconomicFocus(FactionRuntimeState state, FactionProfile profile)
        {
            float cashNeed = 1f - Mathf.Clamp01(state.Cash / 20000f);
            float zoneRevenue = state.ControlledZones.Count * 0.1f;

            float score = (cashNeed * profile.greed * 0.6f)
                        + (zoneRevenue * 0.2f);

            // Base economic desire — factions always want money
            score += 0.15f * profile.greed;

            return score;
        }

        private static float ScoreSeekAlliance(FactionRuntimeState state, FactionManager manager,
            FactionProfile profile)
        {
            // More attractive when weak or facing threats
            float weakness = 1f - state.CombatStrength;
            float threat = state.PerceivedThreatLevel;
            float playerStrength = manager.GetPlayerStrengthEstimate();

            float score = (threat * profile.diplomacy * 0.4f)
                        + (weakness * profile.diplomacy * 0.3f)
                        + (playerStrength * profile.caution * 0.2f);

            // Already allied → low motivation
            if (state.IsAlliedWithPlayer) score *= 0.1f;

            // At war → ceasefire desire scaled by losses
            if (state.IsAtWarWithPlayer && state.CombatStrength < 0.3f)
                score *= 2.0f;

            return score;
        }

        private static float ScoreUndercutPrices(FactionRuntimeState state, FactionProfile profile)
        {
            float marketPresence = Mathf.Clamp01(state.ControlledZones.Count / 5f);
            float supplyExcess = Mathf.Clamp01(state.DailySupply / 50f);

            float score = (marketPresence * profile.greed * 0.5f)
                        + (supplyExcess * 0.3f);

            return score;
        }

        // ─── Mood Determination ────────────────────────────────────

        /// <summary>
        /// Determine faction's current mood based on recent decisions and state.
        /// Used for UI display and behavior hints.
        /// </summary>
        public static FactionMood DetermineMood(FactionRuntimeState state)
        {
            if (state.IsAtWarWithPlayer || state.PerceivedThreatLevel > 0.7f)
                return FactionMood.Aggressive;

            if (state.RecentTerritoryLosses > 0 || state.CombatStrength < 0.3f)
                return FactionMood.Defensive;

            if (state.Cash < state.Profile.desperateCashThreshold)
                return FactionMood.Economic;

            if (state.ControlledZones.Count < 2)
                return FactionMood.Expanding;

            return FactionMood.Passive;
        }
    }
}
