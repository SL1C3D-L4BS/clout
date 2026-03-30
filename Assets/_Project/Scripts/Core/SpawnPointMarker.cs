using UnityEngine;

namespace Clout.Core
{
    /// <summary>
    /// Tags a GameObject as a player spawn point.
    /// Visible as a green arrow gizmo in the scene view.
    /// Used by PlayerSpawnManager to pick spawn locations.
    /// </summary>
    public class SpawnPointMarker : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Sphere center at y+0.25 (= radius), so bottom hemisphere sits exactly on y=0 ground surface.
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.8f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.25f, 0.25f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.25f,
                           transform.forward * 0.8f);
        }
#endif
    }
}
