using UnityEngine;
using UnityEngine.AI;

namespace Clout.Core
{
    /// <summary>
    /// The shared character foundation — both players and NPCs extend this.
    /// Contains movement, combat, inventory, and state flags.
    ///
    /// Clout evolution: adds empire interaction, wanted level awareness,
    /// vehicle mounting, and social systems alongside combat.
    /// </summary>
    public abstract class CharacterStateManager : StateManager, IDamageable, ILockable
    {
        // ─── Components ──────────────────────────────────────────
        [Header("Components")]
        public Animator anim;
        public Rigidbody rb;
        public NavMeshAgent agent;
        public Transform model;
        public Transform lockOnTarget;

        // ─── State Machine IDs ───────────────────────────────────
        [Header("State IDs")]
        public string locomotionStateId = "locomotion";
        public string attackStateId = "attack";
        public string staggerStateId = "stagger";
        public string deathStateId = "death";
        public string interactStateId = "interact";
        public string vehicleStateId = "vehicle";

        // ─── Movement Flags ──────────────────────────────────────
        [Header("Movement")]
        [HideInInspector] public bool isInteracting;
        [HideInInspector] public bool useRootMotion;
        [HideInInspector] public bool canRotate = true;
        [HideInInspector] public bool isDead;
        [HideInInspector] public bool lockOn;
        [HideInInspector] public bool isSprinting;

        // ─── Combat Flags ────────────────────────────────────────
        [Header("Combat")]
        [HideInInspector] public bool isAiming;
        [HideInInspector] public bool isShooting;
        [HideInInspector] public bool isReloading;
        [HideInInspector] public bool canDoCombo;
        [HideInInspector] public int comboIndex;
        [HideInInspector] public Stance currentStance = Stance.Standing;
        [HideInInspector] public WeaponType currentWeaponType = WeaponType.Unarmed;

        // ─── Speed Multipliers ───────────────────────────────────
        [Header("Speed Settings")]
        public float crouchSpeedMultiplier = 0.6f;
        public float proneSpeedMultiplier = 0.3f;
        public float aimSpeedMultiplier = 0.7f;
        public float sprintSpeedMultiplier = 1.5f;

        // ─── Stats ───────────────────────────────────────────────
        [Header("Stats")]
        public Stats.RuntimeStats runtimeStats;

        // ─── Empire Awareness ────────────────────────────────────
        [Header("Empire")]
        [HideInInspector] public WantedLevel currentWantedLevel = WantedLevel.Clean;
        [HideInInspector] public float cash;

        // ─── Animation Hashes ────────────────────────────────────
        [HideInInspector] public int hashVertical;
        [HideInInspector] public int hashHorizontal;
        [HideInInspector] public int hashIsInteracting;
        [HideInInspector] public int hashLockOn;
        [HideInInspector] public int hashStance;
        [HideInInspector] public int hashIsAiming;
        [HideInInspector] public int hashIsShooting;
        [HideInInspector] public int hashWeaponType;

        public bool IsDead => isDead;

        public Transform LockOnTarget => lockOnTarget;
        public bool IsValidLockTarget => !isDead;

        public override void Init()
        {
            // Cache components
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
            if (runtimeStats == null) runtimeStats = GetComponent<Stats.RuntimeStats>();

            // Cache animation hashes
            hashVertical = Animator.StringToHash("vertical");
            hashHorizontal = Animator.StringToHash("horizontal");
            hashIsInteracting = Animator.StringToHash("isInteracting");
            hashLockOn = Animator.StringToHash("lockOn");
            hashStance = Animator.StringToHash("stance");
            hashIsAiming = Animator.StringToHash("isAiming");
            hashIsShooting = Animator.StringToHash("isShooting");
            hashWeaponType = Animator.StringToHash("weaponType");

            InitStates();
        }

        protected abstract void InitStates();

        private void Start()
        {
            Init();
        }

        // ─── Animation ──────────────────────────────────────────

        public void PlayTargetAnimation(string animName, bool isInteracting, bool mirror = false)
        {
            if (anim == null || string.IsNullOrEmpty(animName)) return;

            anim.SetBool("mirror", mirror);
            anim.SetBool("isInteracting", isInteracting);
            anim.CrossFade(animName, 0.2f);

            this.isInteracting = isInteracting;
        }

        // ─── Damage ─────────────────────────────────────────────

        public virtual void OnDamage(DamageEvent damageEvent)
        {
            if (isDead) return;

            if (runtimeStats != null)
            {
                runtimeStats.TakeDamage(damageEvent.FinalDamage);

                if (runtimeStats.health <= 0)
                {
                    ChangeState(deathStateId);
                    return;
                }
            }

            // Stagger
            PlayTargetAnimation("damage_1", true);
            ChangeState(staggerStateId);
        }
    }
}
