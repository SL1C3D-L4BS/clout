using UnityEngine;
using Clout.Utils;

namespace Clout.World.Districts
{
    /// <summary>
    /// Trigger zone that fires DistrictEnteredEvent when the player enters.
    /// Attached to district root GameObjects with a BoxCollider (isTrigger = true).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DistrictTriggerZone : MonoBehaviour
    {
        [Header("District")]
        public string districtId;
        public string districtName;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            string previousDistrict = DistrictManager.Instance != null
                ? DistrictManager.Instance.CurrentDistrictId
                : "";

            if (previousDistrict == districtId) return;

            EventBus.Publish(new DistrictEnteredEvent
            {
                districtId = districtId,
                previousDistrictId = previousDistrict
            });

            if (DistrictManager.Instance != null)
                DistrictManager.Instance.OnPlayerEnteredDistrict(districtId);

            Debug.Log($"[District] Player entered: {districtName} (from {previousDistrict})");
        }
    }
}
