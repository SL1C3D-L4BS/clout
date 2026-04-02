using UnityEngine;

namespace Clout.Combat
{
    /// <summary>
    /// Placed on a weapon model — provides the DamageCollider reference
    /// for animation events to open/close hitboxes.
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
}
