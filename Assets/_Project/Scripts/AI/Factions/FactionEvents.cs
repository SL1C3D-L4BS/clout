using Clout.Core;

namespace Clout.Utils
{
    // ─── Faction Events (Step 14) ─────────────────────────────────

    public struct FactionExpandedEvent
    {
        public FactionId factionId;
        public string zoneId;
        public float newInfluence;
    }

    public struct FactionAttackEvent
    {
        public FactionId attacker;
        public FactionId targetFaction;
        public bool targetsPlayer;
        public float attackStrength;
    }

    public struct FactionWarDeclaredEvent
    {
        public FactionId aggressor;
        public FactionId targetFaction;
        public bool targetsPlayer;
    }

    public struct FactionAllianceFormedEvent
    {
        public FactionId factionId;
        public bool withPlayer;
    }

    public struct FactionAllianceBrokenEvent
    {
        public FactionId factionId;
        public string reason;
    }

    public struct FactionBetrayalEvent
    {
        public FactionId betrayer;
        public FactionId victim;
        public bool victimIsPlayer;
    }

    public struct FactionDefeatedEvent
    {
        public FactionId defeated;
        public FactionId victor;
        public bool victorIsPlayer;
    }

    public struct FactionTributeEvent
    {
        public FactionId from;
        public FactionId to;
        public bool fromPlayer;
        public float amount;
    }

    public struct FactionDispositionChangedEvent
    {
        public FactionId factionId;
        public float oldValue;
        public float newValue;
        public string reason;
    }

    public struct FactionDayProcessedEvent
    {
        public FactionId factionId;
        public string actionTaken;
        public FactionMood currentMood;
    }

    public struct FactionTradeProposedEvent
    {
        public FactionId factionId;
        public ProductType product;
        public float amount;
        public bool toPlayer;
    }
}
