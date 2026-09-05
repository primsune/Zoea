using UnityEngine;
using Zoea.Core;

namespace Zoea.Creatures{
    /// <summary>
    /// Thin adapter driving a chain of spine segments from
    /// <see cref="UndulationMath"/>. Update reads Time.time and writes it
    /// into the pure math each frame; no logic lives here.
    ///
    /// <see cref="_segments"/> holds only the driven tail segments, nearest
    /// the body first, tip last. The body segment is their parent and is
    /// deliberately excluded — it never rotates. Because each segment's
    /// parent is the previous segment (or the body), local rotations compound
    /// down the hierarchy: the tip's world-space displacement is the product
    /// of every joint's rotation above it, not just its own.
    ///
    /// This is VISUAL ONLY. Physics for the creature remains the single
    /// capsule Rigidbody on the Player; nothing here writes to a Rigidbody
    /// — <see cref="_body"/> is only ever read, to scale amplitude by speed.
    ///
    /// Frequency is fixed and never scaled with speed. The solver drives the
    /// wave from absolute time, not an accumulated phase, so changing
    /// frequency mid-swim would discontinuously shift the sine's argument
    /// and snap the tail to a new pose instead of smoothly changing cadence.
    /// Only amplitude is modulated.
    ///
    /// A turn-lag "trail" is added on top of the wave: the body's yaw rate
    /// is measured frame to frame and converted to a per-joint trail angle
    /// (see <see cref="UndulationMath.TrailAngle"/>), smoothed, and added to
    /// every segment's wave angle unmodified — it is not tapered along the
    /// spine and not scaled by the speed factor, so a creature turning on
    /// the spot still trails. The trail is measured from the body's own
    /// rotation rather than from input, so it does not care whether the
    /// turn came from the mouse, from A/D, or from the S turnaround — it
    /// reacts to however the body actually ends up rotating. The body's
    /// rotation rate is capped by SwimController's Turn Speed, so trail
    /// saturates rather than growing without bound. Trail decays back to
    /// zero via SmoothDamp when turning stops; it does not persist or
    /// spring back.
    /// </summary>
    public class SpineUndulator : MonoBehaviour{
        [SerializeField] private Transform[] _segments = null;
        [SerializeField] private float _amplitudeDegrees = 12f;
        [SerializeField] private float _frequencyHz = 1.2f;
        [SerializeField] private float _phaseOffsetDegrees = 45f;
        [SerializeField] private Rigidbody _body = null;
        [SerializeField] private float _speedForFullAmplitude = 4f;
        [SerializeField] private float _idleFraction = 0.25f;
        [SerializeField] private float _responseSmoothTime = 0.3f;
        [SerializeField] private float _trailDegreesPerYawRate = 0.05f;
        [SerializeField] private float _maxTrailDegrees = 8f;
        [SerializeField] private float _trailSmoothTime = 0.15f;

        private float _smoothedFactor;
        private float _factorVelocity;
        private Quaternion _previousRotation;
        private float _smoothedTrail;
        private float _trailVelocity;

        private void Start(){
            if (_segments == null || _segments.Length == 0){
                Debug.LogError($"{nameof(SpineUndulator)}: {nameof(_segments)} is not assigned on {gameObject.name}.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < _segments.Length; i++){
                if (_segments[i] == null){
                    Debug.LogError($"{nameof(SpineUndulator)}: {nameof(_segments)}[{i}] is not assigned on {gameObject.name}.", this);
                    enabled = false;
                    return;
                }
            }

            if (_body == null){
                Debug.LogError($"{nameof(SpineUndulator)}: {nameof(_body)} is not assigned on {gameObject.name}.", this);
                enabled = false;
                return;
            }

            _smoothedFactor = UndulationMath.SpeedFactor(0f, _speedForFullAmplitude, _idleFraction);
            _previousRotation = transform.rotation;
            _smoothedTrail = 0f;
        }

        private void Update(){
            // Reading _body.linearVelocity here is a READ ONLY and does not
            // violate the physics-in-FixedUpdate rule — only writes to a
            // Rigidbody (AddForce, velocity assignment, MoveRotation, etc.)
            // outside FixedUpdate cause jitter. This never writes to _body.
            float target = UndulationMath.SpeedFactor(_body.linearVelocity.magnitude,
                             _speedForFullAmplitude, _idleFraction);
            _smoothedFactor = Mathf.SmoothDamp(_smoothedFactor, target,
                                ref _factorVelocity, _responseSmoothTime);
            float amplitude = _amplitudeDegrees * _smoothedFactor;

            // transform.rotation, NOT _body.rotation: the Rigidbody has
            // interpolation enabled, so transform.rotation is the smoothly
            // interpolated visual rotation while Rigidbody.rotation steps at
            // the fixed timestep. Trail is presentation, so it follows the
            // visual rotation.
            float yawRate = UndulationMath.YawRateDegreesPerSecond(_previousRotation,
                              transform.rotation, transform.up, Time.deltaTime);
            _previousRotation = transform.rotation;
            float targetTrail = UndulationMath.TrailAngle(yawRate,
                                  _trailDegreesPerYawRate, _maxTrailDegrees);
            _smoothedTrail = Mathf.SmoothDamp(_smoothedTrail, targetTrail,
                               ref _trailVelocity, _trailSmoothTime);

            for (int i = 0; i < _segments.Length; i++){
                float angle = UndulationMath.SegmentAngle(i, Time.time,
                                amplitude, _frequencyHz, _phaseOffsetDegrees);
                _segments[i].localRotation = Quaternion.Euler(0f, angle + _smoothedTrail, 0f);
            }
        }
    }
}
