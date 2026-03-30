using UnityEngine;
using Clout.Core;

namespace Clout.Combat
{
    /// <summary>
    /// Physics-based projectile — implements IParryable so melee can reflect bullets.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class Projectile : MonoBehaviour, IParryable
    {
        [Header("Projectile Settings")]
        public float speed = 30f;
        public float lifetime = 5f;
        public float damageMultiplier = 1f;
        public GameObject impactFX;

        private CharacterStateManager owner;
        private RangedWeaponItem weapon;
        private Rigidbody rigid;
        private bool isReflected;
        private float reflectDamageMultiplier = 3f;

        public void Init(CharacterStateManager owner, RangedWeaponItem weapon)
        {
            this.owner = owner;
            this.weapon = weapon;
            rigid = GetComponent<Rigidbody>();
            rigid.useGravity = false;
            rigid.linearVelocity = transform.forward * speed;

            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// Reflect this projectile back — changes owner so it damages enemies instead.
        /// </summary>
        public void Reflect(CharacterStateManager newOwner)
        {
            owner = newOwner;
            isReflected = true;

            Vector3 newDirection = -rigid.linearVelocity.normalized;

            // Try to find a target to home toward
            Collider[] hits = Physics.OverlapSphere(transform.position, 30f);
            float closest = float.MaxValue;
            Transform target = null;

            foreach (var hit in hits)
            {
                if (hit.transform.root == newOwner.transform) continue;
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closest)
                {
                    closest = dist;
                    target = hit.transform;
                }
            }

            if (target != null)
                newDirection = (target.position - transform.position).normalized;

            transform.forward = newDirection;
            rigid.linearVelocity = newDirection * speed * 1.5f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root == owner.transform)
                return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                float multiplier = isReflected ? reflectDamageMultiplier : damageMultiplier;

                DamageEvent dmg = new DamageEvent
                {
                    attacker = owner,
                    damageType = weapon != null ? weapon.primaryDamageType : DamageType.Ballistic,
                    baseDamage = (weapon != null ? weapon.baseDamage : 10f) * multiplier,
                    motionValue = 1f,
                    hitPoint = other.ClosestPoint(transform.position),
                    hitDirection = rigid.linearVelocity.normalized
                };

                damageable.OnDamage(dmg);
            }

            if (impactFX != null)
                Instantiate(impactFX, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }

        // IParryable
        public void OnParried(Vector3 direction) { }
        public bool IsProjectile() => true;
    }
}
