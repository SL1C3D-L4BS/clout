using UnityEngine;

namespace Clout.World.Police
{
    /// <summary>
    /// Police station marker — spawn point for officers and arrest destination.
    ///
    /// Place these in the scene to define where police originate.
    /// HeatResponseManager spawns officers at the nearest station to the player.
    /// Arrested players are teleported to the nearest station's arrest point.
    /// </summary>
    public class PoliceStation : MonoBehaviour
    {
        [Header("Station Config")]
        public string stationName = "Police Station";

        [Tooltip("Offset from station position where officers spawn.")]
        public Vector3 spawnOffset = new Vector3(5f, 0f, 0f);

        [Tooltip("Offset from station position where arrested players appear.")]
        public Vector3 arrestPoint = new Vector3(0f, 0f, -3f);

        // ─── Properties ─────────────────────────────────────────────

        /// <summary>World-space officer spawn point.</summary>
        public Vector3 SpawnPoint => transform.position + transform.TransformDirection(spawnOffset);

        /// <summary>World-space arrest/detention point.</summary>
        public Vector3 ArrestPoint => transform.position + transform.TransformDirection(arrestPoint);

        // ─── Visual (Editor) ────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0.3f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(8f, 4f, 6f));

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(SpawnPoint, 1f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(ArrestPoint, 0.8f);
        }
    }
}
