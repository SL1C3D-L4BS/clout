using UnityEngine;
using Clout.Core;

namespace Clout.AI.Factions
{
    /// <summary>
    /// Step 14 — ScriptableObject defining a rival faction's identity and personality.
    /// Designers can create unlimited faction variants by duplicating + tweaking.
    /// Personality weights drive the Utility Theory AI decision engine.
    /// </summary>
    [CreateAssetMenu(fileName = "FactionProfile", menuName = "Clout/Faction Profile")]
    public class FactionProfile : ScriptableObject
    {
        // ─── Identity ──────────────────────────────────────────────

        [Header("Identity")]
        public FactionId factionId = FactionId.None;
        public string factionName = "Unknown Faction";
        public string leaderName = "Unknown";
        [TextArea(2, 4)]
        public string description = "";
        public Color factionColor = Color.red;

        // ─── Personality Weights (0-1) ─────────────────────────────
        // These drive the Utility Theory AI scoring in FactionAI.cs

        [Header("Personality Weights")]
        [Tooltip("Tendency toward violence and territory seizure")]
        [Range(0f, 1f)]
        public float aggression = 0.5f;

        [Tooltip("Tendency toward defensive play and avoiding heat")]
        [Range(0f, 1f)]
        public float caution = 0.5f;

        [Tooltip("Tendency toward alliances and trade deals")]
        [Range(0f, 1f)]
        public float diplomacy = 0.5f;

        [Tooltip("Territory hunger — how fast they spread")]
        [Range(0f, 1f)]
        public float expansion = 0.5f;

        [Tooltip("Economic focus — price undercutting intensity")]
        [Range(0f, 1f)]
        public float greed = 0.5f;

        // ─── Starting Configuration ────────────────────────────────

        [Header("Starting Config")]
        [Tooltip("Products this faction primarily deals")]
        public ProductType[] preferredProducts;

        [Tooltip("Zone IDs this faction controls at game start")]
        public string[] startingZoneIds;

        [Tooltip("Daily production capacity in units")]
        public float baseSupplyPerDay = 10f;

        [Tooltip("Base fighting power (0-1)")]
        [Range(0f, 1f)]
        public float baseCombatStrength = 0.5f;

        [Tooltip("Starting bankroll")]
        public float startingCash = 10000f;

        // ─── Thresholds ────────────────────────────────────────────

        [Header("Behavior Thresholds")]
        [Tooltip("Disposition at which this faction will backstab allies")]
        [Range(-1f, 0f)]
        public float betrayalThreshold = -0.3f;

        [Tooltip("Cash threshold below which faction prioritizes economics")]
        public float desperateCashThreshold = 2000f;

        [Tooltip("Heat threshold above which faction becomes cautious")]
        public float heatCautionThreshold = 200f;

        // ─── Helpers ───────────────────────────────────────────────

        /// <summary>Returns dominant personality trait name for UI display.</summary>
        public string GetArchetype()
        {
            float max = aggression;
            string archetype = "Aggressive";

            if (caution > max) { max = caution; archetype = "Cautious"; }
            if (diplomacy > max) { max = diplomacy; archetype = "Diplomatic"; }
            if (expansion > max) { max = expansion; archetype = "Expansionist"; }
            if (greed > max) { max = greed; archetype = "Greedy"; }

            return archetype;
        }
    }
}
