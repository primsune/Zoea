using UnityEngine;

namespace Zoea.Core{
    /// <summary>
    /// Pure math behind the body-segment undulation wave used to animate
    /// creature spines. No MonoBehaviour state here — everything is inputs
    /// in, values out, so it can be unit tested without a scene.
    /// </summary>
    public static class UndulationMath{
        /// <summary>
        /// Angle, in degrees, that a single spine segment should be offset
        /// from rest at a given moment, as part of a traveling sine wave
        /// along the spine.
        ///
        /// <paramref name="time"/> is a PARAMETER, not Time.time — never read
        /// Time.time inside this function. The caller supplies time so the
        /// result stays pure and testable.
        ///
        /// The formula is:
        ///   amplitudeDegrees * sin(2*PI*frequencyHz*time
        ///                          - segmentIndex * phaseOffsetDegrees * Deg2Rad)
        ///
        /// The minus sign on the phase term is deliberate: it makes the wave
        /// travel from segment 0 toward higher indices, so segment i lags
        /// segment i-1 by phaseOffsetDegrees / (360 * frequencyHz) seconds.
        /// Do not change the sign.
        /// </summary>
        /// <param name="segmentIndex">Index of the spine segment, 0 at the head end.</param>
        /// <param name="time">Simulation time in seconds, supplied by the caller.</param>
        /// <param name="amplitudeDegrees">Peak deflection of the wave, in degrees.</param>
        /// <param name="frequencyHz">Wave frequency in cycles per second.</param>
        /// <param name="phaseOffsetDegrees">Phase lag applied per segment index, in degrees.</param>
        /// <returns>The segment's angular offset from rest, in degrees.</returns>
        public static float SegmentAngle(int segmentIndex, float time, float amplitudeDegrees, float frequencyHz, float phaseOffsetDegrees){
            return amplitudeDegrees * Mathf.Sin(2f * Mathf.PI * frequencyHz * time - segmentIndex * phaseOffsetDegrees * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Multiplier, in [idleFraction, 1], scaling undulation amplitude by
        /// swimming speed: a stationary creature returns idleFraction, one at
        /// or above <paramref name="speedForFullAmplitude"/> returns 1, and
        /// speeds in between lerp linearly. Negative speed clamps to
        /// idleFraction rather than extrapolating below it.
        ///
        /// Guards against <paramref name="speedForFullAmplitude"/> left at 0
        /// in the inspector (which would otherwise divide by zero) by
        /// returning 1 in that case.
        /// </summary>
        /// <param name="speed">Current swimming speed, in the same units as speedForFullAmplitude.</param>
        /// <param name="speedForFullAmplitude">Speed at which amplitude reaches its full value.</param>
        /// <param name="idleFraction">Amplitude multiplier at zero (or negative) speed.</param>
        /// <returns>An amplitude multiplier in [idleFraction, 1].</returns>
        public static float SpeedFactor(float speed, float speedForFullAmplitude, float idleFraction){
            if (speedForFullAmplitude <= 0f){
                return 1f;
            }

            float t = Mathf.Clamp01(speed / speedForFullAmplitude);
            return Mathf.Lerp(idleFraction, 1f, t);
        }

        /// <summary>
        /// Signed yaw rate, in degrees per second, of the rotation from
        /// <paramref name="previous"/> to <paramref name="current"/> about
        /// the body's own <paramref name="upAxis"/>.
        ///
        /// <paramref name="previous"/>, <paramref name="current"/>, and
        /// <paramref name="deltaTime"/> are all PARAMETERS — nothing here
        /// reads Time.time or a Transform. The caller supplies two sampled
        /// rotations and the time between them, so the result stays pure
        /// and testable.
        ///
        /// eulerAngles is deliberately not used to extract yaw: it is
        /// discontinuous (wraps at 360) and gimbal-ambiguous. Instead the
        /// delta rotation between the two samples is converted to an
        /// angle-axis pair, and that axis is dotted against the body's own
        /// up to isolate the yaw component from any pitch or roll present
        /// in the delta.
        /// </summary>
        /// <param name="previous">The body's rotation at the previous sample.</param>
        /// <param name="current">The body's rotation at the current sample.</param>
        /// <param name="upAxis">The body's own up axis, used to isolate yaw from pitch/roll.</param>
        /// <param name="deltaTime">Seconds between the two samples.</param>
        /// <returns>Signed yaw rate in degrees per second; positive is turning right about upAxis.</returns>
        public static float YawRateDegreesPerSecond(Quaternion previous, Quaternion current, Vector3 upAxis, float deltaTime){
            if (deltaTime <= 0f){
                return 0f;
            }

            Quaternion delta = current * Quaternion.Inverse(previous);
            delta.ToAngleAxis(out float angle, out Vector3 axis);

            // ToAngleAxis returns angle in [0, 360]; normalize to [-180, 180]
            // so a near-identity delta reported the "long way around" doesn't
            // read as a near-360-degree turn.
            if (angle > 180f){
                angle -= 360f;
            }

            return angle * Vector3.Dot(axis, upAxis) / deltaTime;
        }

        /// <summary>
        /// Per-joint trail angle, in degrees, that a spine segment should
        /// add to its wave angle so the tail lags behind a turning body.
        ///
        /// The NEGATION of <paramref name="yawRateDegreesPerSecond"/> is
        /// deliberate and must not be removed: the tail trails behind the
        /// turn, so a body turning right (positive yaw) bends its tail
        /// left (negative angle).
        ///
        /// The returned angle is PER JOINT, not per tip. Spine segments are
        /// parented in a chain, so a uniform per-joint angle compounds down
        /// the hierarchy — a tail of N segments deflects roughly N times
        /// this angle at the tip. <paramref name="maxTrailDegrees"/>
        /// therefore clamps the JOINT, not the tip.
        ///
        /// Assumes <paramref name="maxTrailDegrees"/> is non-negative. A
        /// negative value would make Clamp throw; that is the caller's
        /// contract, not guarded here.
        /// </summary>
        /// <param name="yawRateDegreesPerSecond">Signed body yaw rate, in degrees per second.</param>
        /// <param name="degreesPerYawRate">Trail angle produced per degree-per-second of yaw rate.</param>
        /// <param name="maxTrailDegrees">Maximum magnitude of the per-joint trail angle, in degrees.</param>
        /// <returns>The per-joint trail angle, in degrees, clamped to +/- maxTrailDegrees.</returns>
        public static float TrailAngle(float yawRateDegreesPerSecond, float degreesPerYawRate, float maxTrailDegrees){
            float raw = -yawRateDegreesPerSecond * degreesPerYawRate;
            return Mathf.Clamp(raw, -maxTrailDegrees, maxTrailDegrees);
        }
    }
}
