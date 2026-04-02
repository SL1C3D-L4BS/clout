using UnityEngine;
using UnityEngine.Events;
using Clout.Core;
using Clout.Utils;

namespace Clout.World.Interactables
{
    /// <summary>
    /// Destructible prop — ported from Sharp Accent's DestructiblePropObject + ShootableHook.
    /// Combines shootable physics response with health-based destruction.
    ///
    /// Used for: breakable cover, explosive barrels, destructible furniture,
    /// car windows, crates, street objects.
    ///
    /// Supports: physics hit response, health pool, VFX on hit/destroy,
    /// loot drop on destroy, pool-based VFX spawning.
    /// </summary>
    public class DestructibleProp : MonoBehaviour, IShootable, IDestructible
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 50f;
        private float _currentHealth;

        [Header("Physics")]
        [SerializeField] private float hitForce = 3f;
        private Rigidbody _rigidbody;

        [Header("VFX")]
        public string impactVFXPoolId = "impact_wood";
        public string destroyVFXPoolId = "destroy_debris";

        [Header("Events")]
        public UnityEvent onDestroyed;
        public UnityEvent<float> onDamaged; // Passes remaining health ratio

        [Header("Loot")]
        [Tooltip("Prefab to spawn on destruction (pickup, loot bag, etc.)")]
        public GameObject lootDropPrefab;

        [Header("Reset")]
        public bool canReset;
        public float resetDelay = 30f;

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private bool _destroyed;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            _currentHealth = maxHealth;
            _rigidbody = GetComponent<Rigidbody>();
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        // IShootable
        public void OnBulletHit(Vector3 hitPoint, Vector3 hitDirection, float penetration)
        {
            if (_destroyed) return;

            // Physics response
            if (_rigidbody != null)
            {
                hitDirection.Normalize();
                _rigidbody.AddForceAtPosition(hitDirection * hitForce, hitPoint, ForceMode.Impulse);
            }

            // Spawn impact VFX
            if (!string.IsNullOrEmpty(impactVFXPoolId))
            {
                var fx = ObjectPooler.Get(impactVFXPoolId, hitPoint, Quaternion.LookRotation(-hitDirection));
                if (fx != null)
                {
                    fx.SetActive(true);
                    ObjectPooler.ReturnDelayed(fx, 2f);
                }
            }

            // Apply damage — penetration value used as damage
            TakeDamage(Mathf.Max(10f, penetration), hitDirection);
        }

        public string GetImpactType() => impactVFXPoolId;

        // IDestructible
        public void TakeDamage(float amount, Vector3 hitDirection)
        {
            if (_destroyed) return;

            _currentHealth -= amount;
            onDamaged?.Invoke(_currentHealth / maxHealth);

            if (_currentHealth <= 0f)
                OnDestroyed();
        }

        public void OnDestroyed()
        {
            if (_destroyed) return;
            _destroyed = true;

            // Spawn destroy VFX
            if (!string.IsNullOrEmpty(destroyVFXPoolId))
            {
                var fx = ObjectPooler.Get(destroyVFXPoolId, transform.position, transform.rotation);
                if (fx != null)
                {
                    fx.SetActive(true);
                    ObjectPooler.ReturnDelayed(fx, 3f);
                }
            }

            // Drop loot
            if (lootDropPrefab != null)
                Instantiate(lootDropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

            onDestroyed?.Invoke();

            if (canReset)
            {
                gameObject.SetActive(false);
                Invoke(nameof(ResetProp), resetDelay);
            }
            else
            {
                Destroy(gameObject, 0.1f);
            }
        }

        private void ResetProp()
        {
            _destroyed = false;
            _currentHealth = maxHealth;
            transform.position = _startPosition;
            transform.rotation = _startRotation;
            if (_rigidbody != null) _rigidbody.linearVelocity = Vector3.zero;
            gameObject.SetActive(true);
        }
    }
}
