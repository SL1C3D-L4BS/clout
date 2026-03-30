using UnityEngine;
using Clout.Core;

namespace Clout.Combat
{
    /// <summary>
    /// Abstract base for all items.
    /// </summary>
    public abstract class Item : ScriptableObject
    {
        public string itemName;
        public Sprite icon;
        public GameObject modelPrefab;
    }

    /// <summary>
    /// Weapon definition — supports hybrid melee+ranged.
    /// Each weapon defines its own actions via the strategy pattern.
    /// WeaponType determines camera mode, input routing, and animation layer.
    ///
    /// Crime weapons: bat, pipe, machete, knife, brass knuckles, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Items/Melee Weapon")]
    public class WeaponItem : Item
    {
        [Header("Weapon Classification")]
        public WeaponType weaponType = WeaponType.MeleeOneHand;

        [Header("Animation Overrides")]
        public string oneHanded_anim = "Empty";
        public string twoHanded_anim = "Two Handed";

        [Header("Item Actions — Defines What This Weapon Can Do")]
        public ItemActionContainer[] itemActions = new ItemActionContainer[4];

        [Header("Damage")]
        public DamageType primaryDamageType = DamageType.Blunt;
        public float baseDamage = 20f;
        public float motionValueLight = 1.0f;
        public float motionValueHeavy = 1.6f;

        [Header("Weight")]
        public float weight = 5f;

        [Header("Poise Damage")]
        public float poiseDamage = 15f;

        [Header("Stance Override")]
        [Tooltip("If set, equipping this weapon forces a specific stance")]
        public bool overrideStance;
        public Stance forcedStance = Stance.Standing;

        // Runtime references — set when equipped
        [System.NonSerialized] public WeaponHook weaponHook;
        [System.NonSerialized] public RangedWeaponHook rangedWeaponHook;

        /// <summary>
        /// Whether this weapon has any ranged capability.
        /// </summary>
        public bool HasRangedCapability =>
            weaponType == WeaponType.Ranged ||
            weaponType == WeaponType.Pistol ||
            weaponType == WeaponType.SMG ||
            weaponType == WeaponType.Rifle ||
            weaponType == WeaponType.Shotgun ||
            weaponType == WeaponType.Heavy ||
            weaponType == WeaponType.Hybrid;

        /// <summary>
        /// Whether this weapon has melee capability.
        /// </summary>
        public bool HasMeleeCapability =>
            weaponType == WeaponType.MeleeOneHand ||
            weaponType == WeaponType.MeleeTwoHand ||
            weaponType == WeaponType.Hybrid ||
            weaponType == WeaponType.Unarmed;
    }

    /// <summary>
    /// Ranged weapon — guns, from pistols to RPGs.
    /// Same ItemAction architecture as melee, but with ammo and projectiles.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Items/Ranged Weapon")]
    public class RangedWeaponItem : WeaponItem
    {
        [Header("Ranged Properties")]
        public AmmoType ammoType;
        public AmmoDefinition ammoDefinition;
        public int maxAmmo = 30;
        public float fireRate = 0.1f;
        public float range = 100f;
        public float spread = 0.02f;
        public bool isHitscan = true;
        public GameObject projectilePrefab;

        [Header("ADS — Aim Down Sights")]
        public float adsFOV = 40f;
        public float adsSpeedMultiplier = 0.6f;
        public float adsSpreadMultiplier = 0.3f;
        public string adsIdleAnimation = "aim_idle";

        [Header("Reload")]
        public string reloadAnimation = "reload_standing";
        public float reloadTime = 2f;

        [Header("Alt Fire")]
        public string altFireAnimation;
        public float altFireCooldown = 3f;

        private void OnEnable()
        {
            if (weaponType == WeaponType.MeleeOneHand)
                weaponType = WeaponType.Pistol;
        }
    }

    /// <summary>
    /// Container linking an attack input to an action + animation.
    /// Combo system: animNames[] holds the full combo chain.
    /// </summary>
    [System.Serializable]
    public class ItemActionContainer
    {
        [Tooltip("Combo chain — each entry is one animation in sequence")]
        public string[] animNames;
        public ItemAction itemAction;
        public AttackInputs attackInput;
        public bool isMirrored;
        public bool isTwoHanded;

        [System.NonSerialized] public Item itemActual;
        [System.NonSerialized] public int animIndex;

        /// <summary>
        /// Current animation name from the combo chain.
        /// </summary>
        public string animName
        {
            get
            {
                if (animNames == null || animNames.Length == 0) return "";
                return animNames[Mathf.Clamp(animIndex, 0, animNames.Length - 1)];
            }
        }

        /// <summary>
        /// Whether there are more animations after the current index.
        /// </summary>
        public bool hasNextCombo => animNames != null && animIndex < animNames.Length - 1;

        public void ExecuteItemAction(CharacterStateManager stateManager)
        {
            if (itemAction != null)
                itemAction.ExecuteAction(this, stateManager);
        }
    }

    /// <summary>
    /// Abstract action executed by an item.
    /// Concrete implementations: AttackAction, RangedAttackAction, etc.
    /// </summary>
    public abstract class ItemAction : ScriptableObject
    {
        public abstract void ExecuteAction(ItemActionContainer container, CharacterStateManager stateManager);
    }

    /// <summary>
    /// Weapon hook — attached to weapon model prefab.
    /// Manages damage colliders on the weapon.
    /// </summary>
    public class WeaponHook : MonoBehaviour
    {
        public DamageCollider damageCollider;

        public void OpenDamageCollider()
        {
            if (damageCollider != null)
                damageCollider.EnableCollider();
        }

        public void CloseDamageCollider()
        {
            if (damageCollider != null)
                damageCollider.DisableCollider();
        }
    }

    /// <summary>
    /// Manages weapon model instances on character hands.
    /// </summary>
    public class WeaponHolderManager : MonoBehaviour
    {
        public WeaponHolderHook rightHook;
        public WeaponHolderHook leftHook;

        [System.NonSerialized] public WeaponItem rightItem;
        [System.NonSerialized] public WeaponItem leftItem;

        public void Init()
        {
            WeaponHolderHook[] hooks = GetComponentsInChildren<WeaponHolderHook>();
            foreach (var hook in hooks)
            {
                if (hook.isLeftHook)
                    leftHook = hook;
                else
                    rightHook = hook;
            }
        }

        public void LoadWeaponOnHook(WeaponItem weapon, bool isLeft)
        {
            WeaponHolderHook targetHook = isLeft ? leftHook : rightHook;
            if (targetHook == null) return;

            targetHook.ClearWeapon();

            if (weapon == null || weapon.modelPrefab == null) return;

            GameObject model = Instantiate(weapon.modelPrefab, targetHook.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            weapon.weaponHook = model.GetComponent<WeaponHook>();
            weapon.rangedWeaponHook = model.GetComponent<RangedWeaponHook>();

            if (weapon.rangedWeaponHook != null && weapon is RangedWeaponItem rangedItem)
                weapon.rangedWeaponHook.Init(rangedItem);

            if (weapon.weaponHook != null && weapon.weaponHook.damageCollider != null)
            {
                CharacterStateManager owner = GetComponentInParent<CharacterStateManager>();
                weapon.weaponHook.damageCollider.Init(owner, weapon);
            }

            if (isLeft)
                leftItem = weapon;
            else
                rightItem = weapon;
        }
    }

    /// <summary>
    /// Transform hook on character skeleton for weapon placement.
    /// </summary>
    public class WeaponHolderHook : MonoBehaviour
    {
        public bool isLeftHook;

        public void ClearWeapon()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
