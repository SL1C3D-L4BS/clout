using UnityEngine;
using Clout.Core;

namespace Clout.Network
{
    /// <summary>
    /// Damage validation layer — Phase 2 singleplayer (direct apply).
    /// FishNet ServerRpc/ObserversRpc will be restored in Phase 4 multiplayer.
    ///
    /// ARCHITECTURE (Phase 4):
    /// - DamageCollider detects hits locally (client-side)
    /// - Instead of applying directly, calls RequestDamage()
    /// - This component forwards to server via ServerRpc
    /// - Server validates (distance, cooldown) and applies
    /// - Server broadcasts hit effect to all clients
    /// </summary>
    public class NetworkDamageHandler : MonoBehaviour
    {
        [Header("Validation")]
        public float maxHitDistance = 5f;
        public float hitCooldown = 0.2f;
        private float lastHitTime;

        /// <summary>
        /// Called by DamageCollider when a hit is detected locally.
        /// In Phase 2, applies damage directly. Phase 4 will route through ServerRpc.
        /// </summary>
        public void RequestDamage(CharacterStateManager target, DamageEvent damageEvent)
        {
            if (target == null) return;
            target.OnDamage(damageEvent);
        }

        /// <summary>
        /// Server-side damage validation — kept as regular method for Phase 4.
        /// </summary>
        private void ServerValidateDamage(
            GameObject targetObj,
            DamageType damageType,
            float baseDamage,
            float motionValue,
            Vector3 hitPoint,
            Vector3 hitDirection,
            bool isCritical,
            bool isHeadshot)
        {
            if (targetObj == null) return;

            CharacterStateManager target = targetObj.GetComponent<CharacterStateManager>();
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
            BroadcastHit(targetObj, hitPoint, hitDirection, damageType);
        }

        /// <summary>
        /// Hit broadcast — kept as regular method for Phase 4 ObserversRpc.
        /// </summary>
        private void BroadcastHit(
            GameObject targetObj,
            Vector3 hitPoint,
            Vector3 hitDirection,
            DamageType damageType)
        {
            // TODO: Spawn hit particles, play hit SFX
        }
    }
}
