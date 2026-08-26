using UnityEngine;

namespace Zoea.Core
{
    /// <summary>
    /// Pure functions behind the player creature's swimming controls. No
    /// MonoBehaviour state here — everything is inputs in, values out, so it
    /// can be unit tested and reasoned about without a scene.
    /// </summary>
    public static class SwimMath
    {
        /// <summary>
        /// Combines held-forward and strafe axis into a single world-space
        /// swim direction relative to the camera's aim, normalized. Returns
        /// zero when there is no meaningful input.
        /// </summary>
        public static Vector3 MoveDirection(Quaternion aimRotation, bool forwardHeld, float strafe)
        {
            Vector3 dir = Vector3.zero;
            if (forwardHeld)
            {
                dir += aimRotation * Vector3.forward;
            }
            dir += (aimRotation * Vector3.right) * strafe;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }

        /// <summary>
        /// True the instant the brake should hand off into a reverse turnaround
        /// rather than continuing to decelerate: brake is held, we are not
        /// already reversing, and speed has dropped to the entry threshold.
        /// </summary>
        public static bool ShouldStartReversing(bool brakeHeld, bool alreadyReversing, float speed, float entrySpeed)
        {
            return brakeHeld && !alreadyReversing && speed <= entrySpeed;
        }

        /// <summary>
        /// Rotation the creature should turn towards this frame. Falls back to
        /// the camera's own aim rotation when there is no move direction to
        /// face (e.g. braking to a stop).
        /// </summary>
        public static Quaternion FacingRotation(Vector3 moveDirection, Quaternion aimRotation)
        {
            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                return aimRotation;
            }

            // Do not substitute Vector3.up for the up argument: LookRotation
            // degenerates when the look direction is parallel to the up
            // reference, which happens whenever the creature swims straight
            // up or down. The camera's own up vector is always perpendicular
            // to every direction this game produces, so it is always safe.
            return Quaternion.LookRotation(moveDirection, aimRotation * Vector3.up);
        }
    }
}
