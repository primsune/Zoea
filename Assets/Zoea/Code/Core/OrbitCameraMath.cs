using UnityEngine;

namespace Zoea.Core{
    /// <summary>
    /// Pure math for an orbiting third-person camera: clamping pitch and
    /// converting yaw/pitch/distance around a pivot into a world position and
    /// aim rotation. No MonoBehaviour or scene dependency, so it is testable
    /// without a scene.
    /// </summary>
    public static class OrbitCameraMath{
        /// <summary>Clamps pitch between minPitch and maxPitch.</summary>
        public static float ClampPitch(float pitch, float minPitch, float maxPitch){
            return Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        /// <summary>
        /// Returns the camera position for the given orbit pivot, yaw (left and right), pitch (up and down),
        /// and distance: the pivot offset backward along the rotated camera
        /// axis by distance.
        /// </summary>
        public static Vector3 DesiredPosition(Vector3 pivot, float yaw, float pitch, float distance){
            return pivot + Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distance);
        }

        /// <summary>Returns the rotation corresponding to the given yaw and pitch.</summary>
        public static Quaternion AimRotation(float yaw, float pitch){
            return Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
