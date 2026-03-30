using UnityEngine;
using UnityEngine.AI;
using Clout.Combat;
using Clout.Stats;

namespace Clout.Core
{
    /// <summary>
    /// The shared character foundation — both players and NPCs extend this.
    /// Contains movement, combat, inventory, and state flags.
    ///
    /// Ported from NullReach's CharacterStateManager with Clout-specific additions:
    /// empire interaction, wanted level awareness, vehicle mounting, crime weapons.
    /// </summary>
    public abstract class CharacterStateManager : StateManager, IDamageable, ILockable
    {
        // ─── Components ──────────────────────────────────────────
        [Header("Components")]
        public Animator anim;
        public Rigidbody rigid;
        public NavMeshAgent agent;
        public AnimatorHook animHook;
        public Transform lockOnTarget;
        public Transform model;

        // ─── State Machine IDs ───────────────────────────────────
        [Header("State IDs")]
        public string locomotionStateId = "locomotion";
        public string attackStateId = "attack";
        public string rollStateId = "roll";
        public string staggerStateId = "stagger";
        public string deathStateId = "death";
        public string interactStateId = "interact";
        public string vehicleStateId = "vehicle";

        // ─── Character Flags ────────────────────────────────────
        [Header("Character Flags")]
        [HideInInspector] public bool isGrounded;
        [HideInInspector] public bool isOnAir;
        [HideInInspector] public bool isInteracting;
        [HideInInspector] public bool useRootMotion;
        [HideInInspector] public bool canRotate = true;
        [HideInInspector] public bool isDead;
        [HideInInspector] public bool lockOn;
        [HideInInspector] public bool isTwoHanded;
        [HideInInspector] public bool canDoCombo;
        [HideInInspector] public bool isSprinting;

        // ─── Combat Flags ──────────────────────────────────────
        [Header("Combat")]
        [HideInInspector] public bool isAiming;
        [HideInInspector] public bool isShooting;
        [HideInInspector] public bool isReloading;
        [HideInInspector] public Stance currentStance = Stance.Standing;
        [HideInInspector] public WeaponType currentWeaponType = WeaponType.Unarmed;

        // ─── Movement ──────────────────────────────────────────
        [Header("Movement")]
        [HideInInspector] public float horizontal;
        [HideInInspector] public float vertical;
        [HideInInspector] public float moveAmount;
        [HideInInspector] public Vector3 moveDirection;
        public float rotationSpeed = 10f;
        public float sprintSpeed = 6f;
        public float runSpeed = 4f;
        public float walkSpeed = 2f;

        // ─── Speed Multipliers ─────────────────────────────────
        [Header("Speed Settings")]
        public float crouchSpeedMultiplier = 0.6f;
        public float proneSpeedMultiplier = 0.3f;
        public float aimSpeedMultiplier = 0.7f;
        public float sprintSpeedMultiplier = 1.5f;

        // ─── Combat References ─────────────────────────────────
        [Header("Weapons")]
        public WeaponHolderManager weaponHolderManager;
        [HideInInspector] public ItemActionContainer currentItemAction;
        [HideInInspector] public WeaponItem currentWeaponInUse;
        [HideInInspector] public Combo[] currentCombo;
        [HideInInspector] public int comboIndex;
        public AmmoCacheManager ammoCacheManager;

        // ─── Stats ─────────────────────────────────────────────
        [Header("Stats")]
        public RuntimeStats runtimeStats;

        // ─── Lock On ───────────────────────────────────────────
        [Header("Lock On")]
        public float lockOnRadius = 20f;

        // ─── Network ───────────────────────────────────────────
        [Header("Network")]
        public Network.NetworkDamageHandler networkDamageHandler;

        // ─── Empire Awareness ──────────────────────────────────
        [Header("Empire")]
        [HideInInspector] public WantedLevel currentWantedLevel = WantedLevel.Clean;
        [HideInInspector] public float cash;

        // ─── Animation Hashes ──────────────────────────────────
        [HideInInspector] public int hashVertical;
        [HideInInspector] public int hashHorizontal;
        [HideInInspector] public int hashIsInteracting;
        [HideInInspector] public int hashLockOn;
        [HideInInspector] public int hashStance;
        [HideInInspector] public int hashIsAiming;
        [HideInInspector] public int hashIsShooting;
        [HideInInspector] public int hashWeaponType;
        [HideInInspector] public int hashCanDoCombo;
        [HideInInspector] public int hashMirror;
        [HideInInspector] public int hashIsOnAir;

        public bool IsDead => isDead;

        public override void Init()
        {
            // Cache components
            if (rigid == null) rigid = GetComponent<Rigidbody>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
            if (animHook == null) animHook = GetComponentInChildren<AnimatorHook>();
            if (runtimeStats == null) runtimeStats = GetComponent<RuntimeStats>();
            if (weaponHolderManager == null) weaponHolderManager = GetComponent<WeaponHolderManager>();
            if (ammoCacheManager == null) ammoCacheManager = GetComponent<AmmoCacheManager>();
            if (networkDamageHandler == null) networkDamageHandler = GetComponent<Network.NetworkDamageHandler>();

            // NavMeshAgent passive mode
            if (agent != null)
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
            }

            // Cache animation hashes
            hashVertical = Animator.StringToHash("vertical");
            hashHorizontal = Animator.StringToHash("horizontal");
            hashIsInteracting = Animator.StringToHash("isInteracting");
            hashLockOn = Animator.StringToHash("lockOn");
            hashStance = Animator.StringToHash("stance");
            hashIsAiming = Animator.StringToHash("isAiming");
            hashIsShooting = Animator.StringToHash("isShooting");
            hashWeaponType = Animator.StringToHash("weaponType");
            hashCanDoCombo = Animator.StringToHash("canDoCombo");
            hashMirror = Animator.StringToHash("mirror");
            hashIsOnAir = Animator.StringToHash("isOnAir");

            // Initialize animator hook
            if (animHook != null)
                animHook.Init(this);

            InitStates();
        }

        protected abstract void InitStates();

        protected virtual void Start()
        {
            Init();
        }

        // ─── Animation ────────────────────────────────────────

        /// <summary>
        /// Play a target animation with crossfade.
        /// </summary>
        public void PlayTargetAnimation(string animName, bool isInteracting, bool isMirrored = false)
        {
            if (anim == null || string.IsNullOrEmpty(animName)) return;

            this.isInteracting = isInteracting;
            anim.SetBool(hashIsInteracting, isInteracting);
            anim.SetBool(hashMirror, isMirrored);
            anim.CrossFade(animName, 0.2f);
        }

        /// <summary>
        /// Play the item action mapped to the given attack input.
        /// Override in PlayerStateManager and AIStateManager.
        /// </summary>
        public virtual void PlayTargetItemAction(AttackInputs attackInput) { }

        /// <summary>
        /// Assign current weapon and action for combo tracking.
        /// </summary>
        public void AssignCurrentWeaponAndAction(WeaponItem weapon, ItemActionContainer action)
        {
            currentWeaponInUse = weapon;
            currentItemAction = action;
            if (action != null) action.animIndex = 0;
        }

        /// <summary>
        /// Delegate damage collider control to weapon hook.
        /// </summary>
        public void HandleDamageCollider(bool open)
        {
            if (currentWeaponInUse != null && currentWeaponInUse.weaponHook != null)
            {
                if (open)
                    currentWeaponInUse.weaponHook.OpenDamageCollider();
                else
                    currentWeaponInUse.weaponHook.CloseDamageCollider();
            }
        }

        /// <summary>
        /// Execute the next combo in the chain.
        /// Path 1: ItemActionContainer animIndex advancement (combo chain)
        /// Path 2: ComboInfo StateMachineBehaviour branching
        /// </summary>
        public virtual void DoCombo(AttackInputs attackInput)
        {
            // Path 1: ItemActionContainer combo chain
            if (currentItemAction != null && currentItemAction.hasNextCombo)
            {
                currentItemAction.animIndex++;
                PlayTargetAnimation(currentItemAction.animName, true);
                canDoCombo = false;
                return;
            }

            // Path 2: ComboInfo StateMachineBehaviour branching
            if (currentCombo == null || currentCombo.Length == 0)
                return;

            for (int i = 0; i < currentCombo.Length; i++)
            {
                if (currentCombo[i].inp == attackInput)
                {
                    PlayTargetAnimation(currentCombo[i].animName, true);
                    comboIndex = i;
                    canDoCombo = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Load weapon item actions. Called when switching weapons.
        /// </summary>
        public virtual void UpdateItemActionsWithCurrent()
        {
            if (weaponHolderManager == null) return;

            if (weaponHolderManager.rightItem != null)
            {
                string targetIdle = isTwoHanded
                    ? weaponHolderManager.rightItem.twoHanded_anim
                    : weaponHolderManager.rightItem.oneHanded_anim;

                if (!string.IsNullOrEmpty(targetIdle))
                    anim.CrossFade(targetIdle, 0.2f);
            }
        }

        // ─── Lock On ──────────────────────────────────────────

        /// <summary>
        /// Find the closest lockable target within range.
        /// </summary>
        public ILockable FindLockableTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, lockOnRadius);
            ILockable closest = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                ILockable lockable = hits[i].GetComponent<ILockable>();
                if (lockable == null || !lockable.IsAlive())
                    continue;

                if (hits[i].transform.root == transform)
                    continue;

                float dist = Vector3.Distance(transform.position, hits[i].transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = lockable;
                }
            }

            return closest;
        }

        public Transform GetLockOnTarget()
        {
            return lockOnTarget;
        }

        public bool IsAlive()
        {
            return !isDead;
        }

        // ─── Damage ───────────────────────────────────────────

        public virtual void OnDamage(DamageEvent damageEvent)
        {
            if (isDead) return;

            if (runtimeStats != null)
            {
                runtimeStats.TakeDamage(damageEvent.FinalDamage);

                if (runtimeStats.health <= 0)
                {
                    isDead = true;
                    OnDeath();
                    return;
                }
            }

            // Stagger
            PlayTargetAnimation("damage_1", true);
            if (allStates.ContainsKey(staggerStateId))
                ChangeState(staggerStateId);
        }

        protected virtual void OnDeath()
        {
            PlayTargetAnimation("death", true);
            if (allStates.ContainsKey(deathStateId))
                ChangeState(deathStateId);
        }
    }
}
