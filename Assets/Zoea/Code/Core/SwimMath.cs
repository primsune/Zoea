using UnityEngine;

namespace Zoea.Core{
    /// <summary>
    /// Pure functions behind the player creature's swimming controls. No
    /// MonoBehaviour state here — everything is inputs in, values out, so it
    /// can be unit tested and reasoned about without a scene.
    /// </summary>
    public static class SwimMath{
        /// <summary>
        /// Combines held-forward and strafe axis into a single world-space
        /// swim direction relative to the camera's aim, normalized. Returns
        /// zero when there is no meaningful input.
        /// </summary>
        public static Vector3 MoveDirection(Quaternion aimRotation, bool forwardHeld, float strafe){
            Vector3 dir = Vector3.zero;
            if (forwardHeld){
                dir += aimRotation * Vector3.forward;
            }
            dir += (aimRotation * Vector3.right) * strafe;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }

        /// <summary>
        /// Direction the creature's BODY should turn to face this frame. This
        /// is not always the direction thrust is applied in: while braking
        /// (brakeHeld), the body turns to face the reverse heading
        /// (aimRotation * Vector3.back), and A/D now steer that reverse
        /// heading — strafe is added in exactly as it is for MoveDirection,
        /// producing diagonal reverse headings. W remains ignored entirely
        /// while braking.
        /// </summary>
        public static Vector3 FacingDirection(Quaternion aimRotation, bool brakeHeld, bool forwardHeld, float strafe){
            if (brakeHeld){
                Vector3 dir = aimRotation * Vector3.back;
                dir += (aimRotation * Vector3.right) * strafe;
                return dir.normalized;
            }
            return MoveDirection(aimRotation, forwardHeld, strafe);
        }

        /// <summary>
        /// True the instant the brake should hand off into a reverse turnaround
        /// rather than continuing to decelerate: brake is held, we are not
        /// already reversing, speed has dropped to the entry threshold, AND
        /// the body has finished rotating to face the reverse heading
        /// (aligned). The caller latches the result and never re-evaluates
        /// this while the brake stays held, so the alignment check is
        /// one-shot: it gates only the initial turnaround, and steering with
        /// A/D afterward is not gated by it.
        /// </summary>
        public static bool ShouldStartReversing(bool brakeHeld, bool alreadyReversing,
                                                float speed, float entrySpeed,
                                                bool aligned){
            return brakeHeld && !alreadyReversing && speed <= entrySpeed && aligned;
        }

        /// <summary>
        /// Rotation the creature should turn towards this frame. Falls back to
        /// the camera's own aim rotation when there is no move direction to
        /// face (e.g. braking to a stop).
        /// </summary>
        public static Quaternion FacingRotation(Vector3 moveDirection, Quaternion aimRotation){
            if (moveDirection.sqrMagnitude <= 0.0001f){
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
