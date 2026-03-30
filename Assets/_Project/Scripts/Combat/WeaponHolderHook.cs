using UnityEngine;

namespace Clout.Combat
{
    /// <summary>
    /// Transform hook on character skeleton — anchor point where weapon models attach.
    /// Set isLeftHook = true for the off-hand.
    /// </summary>
    public class WeaponHolderHook : MonoBehaviour
    {
        public bool isLeftHook;

        public void ClearWeapon()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
