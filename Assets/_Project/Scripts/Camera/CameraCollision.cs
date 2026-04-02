using UnityEngine;

namespace Clout.Camera
{
    /// <summary>
    /// Camera collision prevention — SphereCast-based system.
    /// Prevents the camera from clipping through walls and geometry.
    /// </summary>
    public class CameraCollision : MonoBehaviour
    {
        [Header("Collision Settings")]
        public float sphereRadius = 0.2f;
        public float collisionOffset = 0.2f;
        public float minCollisionOffset = 0.2f;

        [Header("References")]
        public Transform cameraTransform;
        public Transform pivotTransform;

        [Header("Layer Mask")]
        public LayerMask collisionLayers = ~0;

        private float _defaultDistance;
        private float _adjustedDistance;

        private void Start()
        {
            if (cameraTransform != null && pivotTransform != null)
            {
                _defaultDistance = Vector3.Distance(pivotTransform.position, cameraTransform.position);
                _adjustedDistance = _defaultDistance;
            }
        }

        public float CheckCollision(Vector3 pivotPosition, Vector3 cameraDirection, float desiredDistance)
        {
            float targetDistance = desiredDistance;

            if (Physics.SphereCast(
                pivotPosition,
                sphereRadius,
                cameraDirection,
                out RaycastHit hit,
                desiredDistance,
                collisionLayers))
            {
                float hitDistance = hit.distance - collisionOffset;
                targetDistance = Mathf.Max(hitDistance, minCollisionOffset);
            }

            _adjustedDistance = Mathf.Lerp(_adjustedDistance, targetDistance, Time.deltaTime * 10f);
            return _adjustedDistance;
        }
    }
}
