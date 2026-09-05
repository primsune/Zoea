using UnityEngine;
using UnityEngine.InputSystem;
using Zoea.Core;

namespace Zoea.CameraRig{
    /// <summary>
    /// Third-person orbit camera. Thin adapter over <see cref="OrbitCameraMath"/>:
    /// Update reads mouse input into yaw/pitch, LateUpdate turns yaw/pitch into a
    /// smoothed world position and looks back at the pivot.
    ///
    /// Rotation is recomputed with <see cref="Quaternion.LookRotation(Vector3, Vector3)"/>
    /// at the pivot every frame rather than set directly from yaw/pitch. Position
    /// is smoothed with SmoothDamp and therefore lags the desired position; if
    /// rotation were driven from yaw/pitch alone, fast mouse movement would let
    /// the target drift off-centre while position catches up. Looking at the
    /// pivot keeps the creature centred in frame.
    /// </summary>
    public class OrbitCamera : MonoBehaviour{
        [SerializeField] private Transform _target = null;
        [SerializeField] private float _distance = 5f;
        [SerializeField] private float _pivotHeight = 0.5f;
        [SerializeField] private float _mouseSensitivity = 0.12f;
        [SerializeField] private float _minPitch = -85f;
        [SerializeField] private float _maxPitch = 85f;

        /// <summary>
        /// Pitch, in degrees, the camera starts at before any mouse input.
        /// Positive pitch places the camera ABOVE the pivot looking down,
        /// because DesiredPosition rotates the backward offset by
        /// Quaternion.Euler(pitch, yaw, 0). Clamped on assignment so an
        /// out-of-range inspector value cannot put the camera outside the
        /// pitch limits on the first frame. Sets the starting value only —
        /// mouse input owns _pitch from the first Update onward.
        /// </summary>
        [SerializeField] private float _startingPitch = 0f;

        /// <summary>
        /// Degrees added to _pitch when orbiting the camera into position,
        /// without affecting where the creature aims. A positive offset lifts
        /// the camera above the creature and lets it look down without the
        /// creature nosing down to match. AimRotation deliberately does NOT
        /// include this offset — SwimController reads AimRotation, and if it
        /// saw the offset the creature would dive whenever the camera is
        /// raised.
        /// </summary>
        [SerializeField] private float _viewPitchOffset = 0f;

        [SerializeField] private float _positionSmoothTime = 0.12f;
        [SerializeField] private bool _invertY = false;
        [SerializeField] private bool _lockCursor = true;

        private float _yaw;
        private float _pitch;
        private Vector3 _followVelocity;
        private bool _snapped = false;

        /// <summary>Current aim rotation, for the movement controller to read.</summary>
        public Quaternion AimRotation => OrbitCameraMath.AimRotation(_yaw, _pitch);

        private void Start(){
            if (_target == null){
                Debug.LogError($"{nameof(OrbitCamera)}: {nameof(_target)} is not assigned.", this);
                enabled = false;
                return;
            }

            _pitch = OrbitCameraMath.ClampPitch(_startingPitch, _minPitch, _maxPitch);

            if (_lockCursor){
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update(){
            if (Mouse.current == null){
                return;
            }

            Vector2 delta = Mouse.current.delta.ReadValue();

            // Mouse delta is already a per-frame accumulation; do not scale by
            // Time.deltaTime or sensitivity would depend on framerate.
            _yaw += delta.x * _mouseSensitivity;
            float pitchDelta = delta.y * _mouseSensitivity;
            _pitch += _invertY ? pitchDelta : -pitchDelta;
            _pitch = OrbitCameraMath.ClampPitch(_pitch, _minPitch, _maxPitch);

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame){
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame
                && Cursor.lockState != CursorLockMode.Locked){
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate(){
            if (_target == null){
                return;
            }

            Vector3 pivot = _target.position + Vector3.up * _pivotHeight;

            // _pitch is where the CREATURE aims (see AimRotation); orbitPitch is
            // where the CAMERA sits. Keeping them separate lets the camera look
            // down on the creature from above without the creature diving to match.
            float orbitPitch = OrbitCameraMath.ClampPitch(_pitch + _viewPitchOffset,
                                                            _minPitch, _maxPitch);
            Vector3 desired = OrbitCameraMath.DesiredPosition(pivot, _yaw, orbitPitch, _distance);

            if (!_snapped){
                // No previous position is worth easing from on the first frame,
                // so snap straight to the orbit position instead of sliding in
                // from wherever the camera was left in the scene.
                transform.position = desired;
                _followVelocity = Vector3.zero;
                _snapped = true;
            }else{
                transform.position = Vector3.SmoothDamp(transform.position, desired,
                                                         ref _followVelocity, _positionSmoothTime);
            }

            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }
    }
}
