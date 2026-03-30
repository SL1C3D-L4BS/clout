using UnityEngine;
using Clout.Core;
using Clout.Player;
using Clout.World.Police;

namespace Clout.Combat
{
    /// <summary>
    /// Ranged attack action — gun system integrated with RangedWeaponHook.
    /// Routes fire through RangedWeaponHook for ammo consumption, spread
    /// accumulation, fire-rate gating, and recoil feedback.
    /// Falls back to legacy direct-fire for weapons without a RangedWeaponHook.
    /// </summary>
    [CreateAssetMenu(menuName = "Clout/Item Actions/Ranged Attack Action")]
    public class RangedAttackAction : ItemAction
    {
        public override void ExecuteAction(ItemActionContainer container, CharacterStateManager stateManager)
        {
            RangedWeaponItem rangedWeapon = container.itemActual as RangedWeaponItem;
            if (rangedWeapon == null) return;

            // Stamina cost — reduced for ranged
            float cost = stateManager.runtimeStats.lightAttackCost * 0.3f;
            if (!stateManager.runtimeStats.ConsumeStamina(cost))
                return;

            stateManager.currentWeaponInUse = rangedWeapon;
            stateManager.currentItemAction = container;

            // === RangedWeaponHook path (preferred) ===
            RangedWeaponHook hook = rangedWeapon.rangedWeaponHook;
            if (hook != null)
            {
                if (hook.CurrentAmmo <= 0) return;
                if (!hook.CanFire()) return;

                Vector3 aimDirection = GetAimDirection(stateManager, hook);
                Vector3 spreadDirection = hook.Fire(aimDirection);

                string fireAnim = container.animName;
                if (string.IsNullOrEmpty(fireAnim)) fireAnim = "fire_standing";
                stateManager.PlayTargetAnimation(fireAnim, false, container.isMirrored);

                stateManager.isShooting = true;

                if (rangedWeapon.isHitscan)
                    FireHitscan(rangedWeapon, stateManager, hook, spreadDirection);
                else
                    FireProjectile(rangedWeapon, stateManager, hook, spreadDirection);

                // Gunfire generates heat for players
                NotifyGunfireHeat(stateManager);

                return;
            }

            // === Legacy path (no RangedWeaponHook) ===
            stateManager.PlayTargetAnimation(container.animName, true, container.isMirrored);

            if (rangedWeapon.isHitscan)
                FireHitscanLegacy(rangedWeapon, stateManager);
            else
                FireProjectileLegacy(rangedWeapon, stateManager);

            stateManager.ChangeState(stateManager.attackStateId);
        }

        /// <summary>
        /// Get aim direction — camera center for players, transform forward for AI.
        /// </summary>
        private Vector3 GetAimDirection(CharacterStateManager stateManager, RangedWeaponHook hook)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null && stateManager is PlayerStateManager)
            {
                Ray aimRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                Vector3 aimPoint;

                if (Physics.Raycast(aimRay, out RaycastHit aimHit, 200f))
                    aimPoint = aimHit.point;
                else
                    aimPoint = aimRay.GetPoint(200f);

                Vector3 bulletOrigin = hook.bulletOrigin != null
                    ? hook.bulletOrigin.position
                    : stateManager.transform.position + Vector3.up;

                return (aimPoint - bulletOrigin).normalized;
            }

            return stateManager.transform.forward;
        }

        /// <summary>
        /// Hitscan fire with RangedWeaponHook.
        /// </summary>
        private void FireHitscan(RangedWeaponItem weapon, CharacterStateManager stateManager,
            RangedWeaponHook hook, Vector3 direction)
        {
            Vector3 origin = hook.bulletOrigin != null
                ? hook.bulletOrigin.position
                : stateManager.transform.position + Vector3.up;

            float damageMultiplier = 1f;
            if (weapon.ammoDefinition != null)
                damageMultiplier = weapon.ammoDefinition.damageMultiplier;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, weapon.range))
            {
                IDamageable target = hit.collider.GetComponent<IDamageable>();
                if (target != null)
                {
                    DamageEvent dmg = new DamageEvent
                    {
                        attacker = stateManager,
                        damageType = weapon.primaryDamageType,
                        baseDamage = weapon.baseDamage * damageMultiplier,
                        motionValue = weapon.motionValueLight,
                        hitPoint = hit.point,
                        hitDirection = direction
                    };
                    target.OnDamage(dmg);

                    // Assault heat for player hitting characters
                    CharacterStateManager targetCSM = hit.collider.GetComponent<CharacterStateManager>();
                    if (targetCSM == null) targetCSM = hit.collider.GetComponentInParent<CharacterStateManager>();
                    if (targetCSM != null) NotifyAssaultHeat(stateManager, targetCSM);
                }

                IShootable shootable = hit.collider.GetComponent<IShootable>();
                if (shootable != null)
                {
                    float penetration = weapon.ammoDefinition != null
                        ? weapon.ammoDefinition.armorPenetration
                        : 0f;
                    shootable.OnBulletHit(hit.point, direction, penetration);
                }
            }
        }

        /// <summary>
        /// Projectile fire with RangedWeaponHook.
        /// </summary>
        private void FireProjectile(RangedWeaponItem weapon, CharacterStateManager stateManager,
            RangedWeaponHook hook, Vector3 direction)
        {
            GameObject prefab = weapon.ammoDefinition?.projectilePrefab ?? weapon.projectilePrefab;
            if (prefab == null) return;

            Vector3 spawnPos = hook.bulletOrigin != null
                ? hook.bulletOrigin.position
                : stateManager.transform.position + Vector3.up + stateManager.transform.forward * 0.5f;

            Quaternion spawnRot = Quaternion.LookRotation(direction);
            GameObject proj = Object.Instantiate(prefab, spawnPos, spawnRot);

            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Init(stateManager, weapon);
        }

        /// <summary>
        /// Gunfire in public generates heat. Player-only — AI gunfire doesn't generate police heat.
        /// </summary>
        private void NotifyGunfireHeat(CharacterStateManager stateManager)
        {
            PlayerStateManager player = stateManager as PlayerStateManager;
            if (player == null || player.wantedSystem == null) return;
            player.wantedSystem.AddHeat(WantedSystem.HeatValues.GunfireInPublic, "gunfire");
        }

        /// <summary>
        /// Hitting a character with bullets generates assault heat.
        /// </summary>
        private void NotifyAssaultHeat(CharacterStateManager attacker, CharacterStateManager target)
        {
            PlayerStateManager player = attacker as PlayerStateManager;
            if (player == null || player.wantedSystem == null) return;

            bool isPolice = target.gameObject.CompareTag("Police");
            float heat = isPolice
                ? WantedSystem.HeatValues.AssaultPolice
                : WantedSystem.HeatValues.AssaultCivilian;
            player.wantedSystem.AddHeat(heat, isPolice ? "shot officer" : "shot target");
        }

        #region Legacy Fire (no RangedWeaponHook)

        private void FireHitscanLegacy(RangedWeaponItem weapon, CharacterStateManager stateManager)
        {
            Transform origin = stateManager.transform;
            Vector3 direction = origin.forward;

            direction += new Vector3(
                Random.Range(-weapon.spread, weapon.spread),
                Random.Range(-weapon.spread, weapon.spread),
                Random.Range(-weapon.spread, weapon.spread)
            );

            if (Physics.Raycast(origin.position + Vector3.up, direction, out RaycastHit hit, weapon.range))
            {
                IDamageable target = hit.collider.GetComponent<IDamageable>();
                if (target != null)
                {
                    DamageEvent dmg = new DamageEvent
                    {
                        attacker = stateManager,
                        damageType = weapon.primaryDamageType,
                        baseDamage = weapon.baseDamage,
                        motionValue = weapon.motionValueLight,
                        hitPoint = hit.point,
                        hitDirection = direction
                    };
                    target.OnDamage(dmg);
                }
            }
        }

        private void FireProjectileLegacy(RangedWeaponItem weapon, CharacterStateManager stateManager)
        {
            if (weapon.projectilePrefab == null) return;

            Transform origin = stateManager.transform;
            Vector3 spawnPos = origin.position + Vector3.up + origin.forward * 0.5f;

            GameObject proj = Object.Instantiate(weapon.projectilePrefab, spawnPos, origin.rotation);
            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Init(stateManager, weapon);
        }

        #endregion
    }
}
