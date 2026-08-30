using UnityEngine;
using UnityEngine.InputSystem;
using Zoea.CameraRig;
using Zoea.Core;

namespace Zoea.Movement{
    /// <summary>
    /// Thin adapter driving the player creature's Rigidbody from keyboard
    /// input and the camera's aim rotation. Update only polls input;
    /// FixedUpdate does all physics, per Unity execution order. All actual
    /// direction/rotation logic lives in <see cref="SwimMath"/>.
    ///
    /// S is a brake first and a turnaround second: holding it decelerates the
    /// creature to a near-stop, then flips into reversing along the camera's
    /// aim. A latch (<see cref="_reversing"/>) prevents the speed drop caused
    /// by braking from re-triggering the brake once reversing has begun. W
    /// and A/D are ignored entirely while S is held.
    ///
    /// Reverse thrust is gated on body alignment: once reversing begins, the
    /// body still has to rotate up to 180 degrees to face the reverse
    /// direction. Firing thrust immediately would shove the creature backward
    /// while it is still broadside to its direction of travel. Instead the
    /// creature keeps braking (shedding momentum) until its rotation is
    /// within <see cref="_thrustAlignmentAngle"/> of the reverse facing, and
    /// only then switches to thrust. This gate applies only to reversing —
    /// forward and strafe thrust always apply immediately regardless of body
    /// angle.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SwimController : MonoBehaviour{
        [SerializeField] private OrbitCamera _camera = null;
        [SerializeField] private float _swimForce = 8f;
        [SerializeField] private float _brakeStrength = 4f;
        [SerializeField] private float _reverseEntrySpeed = 0.5f;
        [SerializeField] private float _turnSpeed = 180f; // degrees per second
        [SerializeField] private float _thrustAlignmentAngle = 8f;

        private Rigidbody _rb;
        private bool _forwardHeld;
        private bool _brakeHeld;
        private float _strafe;
        private bool _reversing;

        private void Awake(){
            _rb = GetComponent<Rigidbody>();
        }

        private void Start(){
            if (_camera == null){
                Debug.LogError($"{nameof(SwimController)}: {nameof(_camera)} is not assigned.", this);
                enabled = false;
            }
        }

        private void Update(){
            if (Keyboard.current == null){
                _forwardHeld = false;
                _brakeHeld = false;
                _strafe = 0f;
                return;
            }

            _forwardHeld = Keyboard.current.wKey.isPressed;
            _brakeHeld = Keyboard.current.sKey.isPressed;
            _strafe = (Keyboard.current.dKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.aKey.isPressed ? 1f : 0f);
        }

        private void FixedUpdate(){
            if (_camera == null){
                return;
            }

            Quaternion aim = _camera.AimRotation;

            if (!_brakeHeld){
                _reversing = false;
            }else if (SwimMath.ShouldStartReversing(_brakeHeld, _reversing,
                     _rb.linearVelocity.magnitude, _reverseEntrySpeed)){
                _reversing = true;
            }

            bool braking = _brakeHeld && !_reversing;

            Vector3 moveDirection;
            if (braking){
                moveDirection = Vector3.zero;
            }else if (_reversing){
                moveDirection = aim * Vector3.back;
            }else{
                moveDirection = SwimMath.MoveDirection(aim, _forwardHeld, _strafe);
            }

            Quaternion target = SwimMath.FacingRotation(moveDirection, aim);
            float angleToTarget = Quaternion.Angle(_rb.rotation, target);
            bool aligned = angleToTarget <= _thrustAlignmentAngle;

            if (braking){
                _rb.AddForce(-_rb.linearVelocity * _brakeStrength, ForceMode.Acceleration);
            }else if (_reversing){
                if (aligned){
                    _rb.AddForce(moveDirection * _swimForce, ForceMode.Acceleration);
                }else if (_rb.linearVelocity.magnitude > _reverseEntrySpeed){
                    _rb.AddForce(-_rb.linearVelocity * _brakeStrength, ForceMode.Acceleration);
                }
            }else if (moveDirection.sqrMagnitude > 0.0001f){
                _rb.AddForce(moveDirection * _swimForce, ForceMode.Acceleration);
            }

            _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, target,
                              _turnSpeed * Time.fixedDeltaTime));
        }
    }
}
