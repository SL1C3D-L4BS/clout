using UnityEngine;
using Clout.Core;
using Clout.Player;
using Clout.World.Police;

namespace Clout.Combat
{
    /// <summary>
    /// Melee damage collider — attached to weapon models,
    /// toggled via animation events through AnimatorHook.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DamageCollider : MonoBehaviour
    {
        [Header("Configuration")]
        public CharacterStateManager owner;
        public WeaponItem weapon;

        private Collider col;

        private void Awake()
        {
            col = GetComponent<Collider>();
            col.isTrigger = true;
            col.enabled = false;
        }

        public void Init(CharacterStateManager owner, WeaponItem weapon)
        {
            this.owner = owner;
            this.weapon = weapon;
        }

        public void EnableCollider()
        {
            col.enabled = true;
        }

        public void DisableCollider()
        {
            col.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root == owner.transform)
                return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<IDamageable>();

            if (damageable == null)
                return;

            DamageEvent damageEvent = new DamageEvent
            {
                attacker = owner,
                damageType = weapon != null ? weapon.primaryDamageType : DamageType.Blunt,
                baseDamage = weapon != null ? weapon.baseDamage : 10f,
                motionValue = weapon != null ? weapon.motionValueLight : 1f,
                hitPoint = other.ClosestPoint(transform.position),
                hitDirection = (other.transform.position - owner.transform.position).normalized
            };

            // Route through network damage handler if available
            CharacterStateManager target = other.GetComponent<CharacterStateManager>();
            if (target == null) target = other.GetComponentInParent<CharacterStateManager>();

            if (owner.networkDamageHandler != null && target != null)
            {
                owner.networkDamageHandler.RequestDamage(target, damageEvent);
            }
            else
            {
                damageable.OnDamage(damageEvent);
            }

            // Generate assault heat for player attackers
            NotifyAssaultHeat(target);
        }

        private void NotifyAssaultHeat(CharacterStateManager target)
        {
            if (target == null || owner == null) return;
            PlayerStateManager player = owner as PlayerStateManager;
            if (player == null || player.wantedSystem == null) return;

            bool isPolice = target.gameObject.CompareTag("Police");
            float heat = isPolice
                ? WantedSystem.HeatValues.AssaultPolice
                : WantedSystem.HeatValues.AssaultCivilian;
            player.wantedSystem.AddHeat(heat, isPolice ? "assaulted officer" : "assault");
        }
    }

    /// <summary>
    /// Parry collider — can reflect projectiles.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ParryCollider : MonoBehaviour
    {
        public CharacterStateManager owner;
        private Collider col;

        private void Awake()
        {
            col = GetComponent<Collider>();
            col.isTrigger = true;
            col.enabled = false;
        }

        public void EnableCollider() => col.enabled = true;
        public void DisableCollider() => col.enabled = false;

        private void OnTriggerEnter(Collider other)
        {
            IParryable parryable = other.GetComponent<IParryable>();
            if (parryable != null)
            {
                Vector3 direction = (other.transform.position - transform.position).normalized;
                parryable.OnParried(direction);

                if (parryable.IsProjectile())
                {
                    Projectile proj = other.GetComponent<Projectile>();
                    if (proj != null)
                        proj.Reflect(owner);
                }
            }
        }
    }
}
