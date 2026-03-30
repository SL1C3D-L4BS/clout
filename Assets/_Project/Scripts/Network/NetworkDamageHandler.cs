using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using Clout.Core;

namespace Clout.Network
{
    /// <summary>
    /// Server-authoritative damage validation layer.
    ///
    /// ARCHITECTURE:
    /// - DamageCollider detects hits locally (client-side)
    /// - Instead of applying directly, calls RequestDamage()
    /// - This component forwards to server via ServerRpc
    /// - Server validates (distance, cooldown) and applies
    /// - Server broadcasts hit effect to all clients
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkDamageHandler : NetworkBehaviour
    {
        [Header("Validation")]
        public float maxHitDistance = 5f;
        public float hitCooldown = 0.2f;
        private float lastHitTime;

        /// <summary>
        /// Called by DamageCollider when a hit is detected locally.
        /// </summary>
        public void RequestDamage(CharacterStateManager target, DamageEvent damageEvent)
        {
            if (target == null) return;

            // Offline mode
            if (!IsSpawned)
            {
                target.OnDamage(damageEvent);
                return;
            }

            NetworkObject targetNob = target.GetComponent<NetworkObject>();
            if (targetNob == null)
            {
                target.OnDamage(damageEvent);
                return;
            }

            ServerValidateDamage(
                targetNob,
                damageEvent.damageType,
                damageEvent.baseDamage,
                damageEvent.motionValue,
                damageEvent.hitPoint,
                damageEvent.hitDirection,
                damageEvent.isCritical,
                damageEvent.isHeadshot
            );
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerValidateDamage(
            NetworkObject targetNob,
            DamageType damageType,
            float baseDamage,
            float motionValue,
            Vector3 hitPoint,
            Vector3 hitDirection,
            bool isCritical,
            bool isHeadshot,
            NetworkConnection sender = null)
        {
            if (targetNob == null) return;

            CharacterStateManager target = targetNob.GetComponent<CharacterStateManager>();
            if (target == null || target.isDead) return;

            // Validation: distance
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist > maxHitDistance)
            {
                Debug.LogWarning($"[NetworkDamage] Rejected hit: distance {dist:F1} > max {maxHitDistance}");
                return;
            }

            // Validation: cooldown
            if (Time.time - lastHitTime < hitCooldown)
                return;
            lastHitTime = Time.time;

            CharacterStateManager attacker = GetComponent<CharacterStateManager>();
            DamageEvent serverEvent = new DamageEvent
            {
                attacker = attacker,
                damageType = damageType,
                baseDamage = baseDamage,
                motionValue = motionValue,
                hitPoint = hitPoint,
                hitDirection = hitDirection,
                isCritical = isCritical,
                isHeadshot = isHeadshot
            };

            target.OnDamage(serverEvent);
            ObserversBroadcastHit(targetNob, hitPoint, hitDirection, damageType);
        }

        [ObserversRpc]
        private void ObserversBroadcastHit(
            NetworkObject targetNob,
            Vector3 hitPoint,
            Vector3 hitDirection,
            DamageType damageType)
        {
            // TODO: Spawn hit particles, play hit SFX
        }
    }
}
