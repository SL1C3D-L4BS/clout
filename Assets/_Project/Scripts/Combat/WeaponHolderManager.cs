using UnityEngine;

namespace Clout.Combat
{
    /// <summary>
    /// Manages weapon model instances on character hands.
    /// Discovers WeaponHolderHook children and loads/unloads weapon models.
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
                Clout.Core.CharacterStateManager owner = GetComponentInParent<Clout.Core.CharacterStateManager>();
                weapon.weaponHook.damageCollider.Init(owner, weapon);
            }

            if (isLeft)
                leftItem = weapon;
            else
                rightItem = weapon;
        }
    }
}
