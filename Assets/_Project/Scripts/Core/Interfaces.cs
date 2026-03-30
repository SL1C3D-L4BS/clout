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
        Transform LockOnTarget { get; }
        bool IsValidLockTarget { get; }
    }

    // ─────────────────────────────────────────────────────────────
    //  ENUMS
    // ─────────────────────────────────────────────────────────────

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
        Thrown          // Grenades, molotovs, bricks
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
        Safehouse,          // Base of operations
        Lab,                // Production facility
        Growhouse,          // Cannabis cultivation
        Storefront,         // Legit business front
        Warehouse,          // Bulk storage
        Nightclub,          // Money laundering + social
        AutoShop,           // Vehicle modifications
        Restaurant          // Money laundering front
    }

    public enum EmployeeRole
    {
        Dealer,             // Sells on the street
        Cook,               // Produces product
        Grower,             // Cultivates plants
        Guard,              // Protects properties
        Driver,             // Delivery runs
        Accountant,         // Manages finances
        Lookout,            // Warns of police
        Enforcer            // Handles rival threats
    }

    public enum ReputationType
    {
        Street,             // Criminal underworld rep
        Police,             // Heat / wanted level
        Civilian,           // Public perception
        Rival,              // Other empire relations
        Supplier            // Wholesale connections
    }

    public enum WantedLevel
    {
        Clean = 0,          // No heat
        Suspicious = 1,     // Being watched
        Wanted = 2,         // Active pursuit
        Hunted = 3,         // Aggressive response
        MostWanted = 4,     // Full SWAT / special units
        Kingpin = 5         // Perpetual high alert
    }

    public enum AmmoType
    {
        Pistol,
        SMG,
        Rifle,
        Shotgun,
        Sniper,
        Explosive,
        Infinite            // Melee / thrown
    }
}
