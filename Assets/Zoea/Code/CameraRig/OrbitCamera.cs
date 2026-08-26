using UnityEngine;
using UnityEngine.InputSystem;
using Zoea.Core;

namespace Zoea.CameraRig
{
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
    public class OrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target = null;
        [SerializeField] private float _distance = 5f;
        [SerializeField] private float _pivotHeight = 0.5f;
        [SerializeField] private float _mouseSensitivity = 0.12f;
        [SerializeField] private float _minPitch = -85f;
        [SerializeField] private float _maxPitch = 85f;
        [SerializeField] private float _positionSmoothTime = 0.12f;
        [SerializeField] private bool _invertY = false;
        [SerializeField] private bool _lockCursor = true;

        private float _yaw;
        private float _pitch;
        private Vector3 _followVelocity;

        /// <summary>Current aim rotation, for the movement controller to read.</summary>
        public Quaternion AimRotation => OrbitCameraMath.AimRotation(_yaw, _pitch);

        private void Start()
        {
            if (_lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (_target == null)
            {
                Debug.LogError($"{nameof(OrbitCamera)}: {nameof(_target)} is not assigned.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (Mouse.current == null)
            {
                return;
            }

            Vector2 delta = Mouse.current.delta.ReadValue();

            // Mouse delta is already a per-frame accumulation; do not scale by
            // Time.deltaTime or sensitivity would depend on framerate.
            _yaw += delta.x * _mouseSensitivity;
            float pitchDelta = delta.y * _mouseSensitivity;
            _pitch += _invertY ? pitchDelta : -pitchDelta;
            _pitch = OrbitCameraMath.ClampPitch(_pitch, _minPitch, _maxPitch);

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame
                && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 pivot = _target.position + Vector3.up * _pivotHeight;
            Vector3 desired = OrbitCameraMath.DesiredPosition(pivot, _yaw, _pitch, _distance);
            transform.position = Vector3.SmoothDamp(transform.position, desired,
                                                     ref _followVelocity, _positionSmoothTime);
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }
    }
}
