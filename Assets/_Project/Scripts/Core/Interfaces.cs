using UnityEngine;

namespace Clout.Core
{
    // ─────────────────────────────────────────────────────────────
    //  DAMAGE SYSTEM
    // ─────────────────────────────────────────────────────────────

    public enum DamageType
    {
        Blunt,          // Fists, bats, pipes
        Slash,          // Knives, machetes
        Thrust,         // Stab attacks
        Ballistic,      // Bullets
        Explosive,      // Grenades, RPGs
        Fire,           // Molotovs
        Electric,       // Tasers, stun
        Toxic           // Poison, gas
    }

    public struct DamageEvent
    {
        public CharacterStateManager attacker;
        public DamageType damageType;
        public float baseDamage;
        public float motionValue;
        public float armorPenetration;
        public Vector3 hitPoint;
        public Vector3 hitDirection;
        public bool isCritical;
        public bool isHeadshot;

        public float FinalDamage => baseDamage * motionValue * (isCritical ? 2f : 1f) * (isHeadshot ? 2.5f : 1f);
    }

    public interface IDamageable
    {
        void OnDamage(DamageEvent damageEvent);
        bool IsDead { get; }
    }

    public interface IShootable
    {
        void OnBulletHit(Vector3 hitPoint, Vector3 hitDirection, float penetration);
        string GetImpactType();
    }

    public interface IParryable
    {
        void OnParried(Vector3 direction);
        bool IsProjectile();
    }

    // ─────────────────────────────────────────────────────────────
    //  INTERACTION SYSTEM
    // ─────────────────────────────────────────────────────────────

    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract(CharacterStateManager character);
        void OnInteract(CharacterStateManager character);
    }

    public interface ILockable
    {
        Transform GetLockOnTarget();
        bool IsAlive();
    }

    // ─────────────────────────────────────────────────────────────
    //  COMBAT ENUMS
    // ─────────────────────────────────────────────────────────────

    public enum AttackInputs
    {
        rb,     // Right bumper — light melee / hip fire
        rt,     // Right trigger — heavy melee / ADS fire
        lb,     // Left bumper — parry / block / alt fire
        lt,     // Left trigger — lock-on / ADS toggle
        none
    }

    public enum WeaponType
    {
        Unarmed,        // Fists
        MeleeOneHand,   // Knife, bat, pipe, machete
        MeleeTwoHand,   // Sledgehammer, katana
        Pistol,         // Handguns
        SMG,            // Submachine guns
        Rifle,          // Assault rifles, sniper
        Shotgun,        // Shotguns
        Heavy,          // RPG, minigun
        Thrown,          // Grenades, molotovs, bricks
        Ranged,         // Generic ranged (backward compat)
        Staff,          // Magic staff / hybrid
        Hybrid          // Melee + ranged capability
    }

    public enum Stance
    {
        Standing,
        Crouching,
        Prone,
        Sprinting,
        InVehicle,
        Swimming
    }

    public enum CameraMode
    {
        ThirdPerson,    // Default exploration
        HipFire,        // Weapon drawn, not aiming
        ADS,            // Aiming down sights
        LockOn,         // Melee lock-on
        FirstPerson,    // FPS mode (optional toggle)
        Vehicle,        // Driving camera
        Cinematic       // Cutscene camera
    }

    public enum AmmoType
    {
        Pistol,
        SMG,
        Rifle,
        Shotgun,
        Sniper,
        Explosive,
        Infinite        // Melee / thrown
    }

    // ─────────────────────────────────────────────────────────────
    //  COMBO SYSTEM
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Combo data — maps a button input to the next animation in the chain.
    /// </summary>
    [System.Serializable]
    public class Combo
    {
        public string animName;
        public AttackInputs inp;
    }

    // ─────────────────────────────────────────────────────────────
    //  EMPIRE ENUMS
    // ─────────────────────────────────────────────────────────────

    public enum ProductType
    {
        Cannabis,
        Methamphetamine,
        Cocaine,
        MDMA,
        LSD,
        Prescription,
        Custom
    }

    public enum PropertyType
    {
        Safehouse,
        Lab,
        Growhouse,
        Storefront,
        Warehouse,
        Nightclub,
        AutoShop,
        Restaurant,
        Laundromat,     // Step 11 — Best laundering ratio, low revenue
        CarWash          // Step 11 — Classic front, balanced profile
    }

    public enum EmployeeRole
    {
        Dealer,
        Cook,
        Grower,
        Guard,
        Driver,
        Accountant,
        Lookout,
        Enforcer
    }

    public enum ReputationType
    {
        Street,
        Police,
        Civilian,
        Rival,
        Supplier
    }

    public enum WantedLevel
    {
        Clean = 0,
        Suspicious = 1,
        Wanted = 2,
        Hunted = 3,
        MostWanted = 4,
        Kingpin = 5
    }

    // ─────────────────────────────────────────────────────────────
    //  RIVAL FACTIONS (Step 14)
    // ─────────────────────────────────────────────────────────────

    public enum FactionId
    {
        None,
        Cartel,
        EastsideSyndicate,
        DowntownCollective,
        NorthsideFamily,
        DocksUnion
    }

    public enum DiplomacyAction
    {
        ProposeAlliance,
        OfferTribute,
        DeclareWar,
        RequestCeasefire,
        ProposeTrade,
        BetrayAlliance,
        DemandTribute,
        RequestHelp
    }

    public enum FactionMood
    {
        Passive,
        Aggressive,
        Defensive,
        Expanding,
        Economic
    }

    public enum FactionRelationship
    {
        War,
        Hostile,
        Neutral,
        Friendly,
        Allied
    }
}
