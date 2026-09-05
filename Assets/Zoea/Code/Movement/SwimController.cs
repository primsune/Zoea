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
    /// W is ignored while S is held; A and D steer the reverse heading,
    /// producing diagonal reverse movement exactly as they produce diagonal
    /// forward movement. The body begins rotating toward the reverse heading
    /// the instant S is pressed, but reverse thrust is withheld until the
    /// body has actually come round: alignment, not the facing target, is
    /// the gate. Until aligned the creature keeps decelerating in place
    /// while it turns; once aligned it switches to reverse thrust.
    ///
    /// A latch (<see cref="_reversing"/>) prevents the speed drop caused by
    /// braking from re-triggering the brake once reversing has begun.
    /// Because _reversing latches until S is released, the alignment
    /// condition gates only the initial turnaround — it is not re-evaluated
    /// while the brake stays held. Steering with A or D while already
    /// reversing therefore behaves like strafing while swimming forward,
    /// which is never gated on alignment.
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

            Vector3 facingDirection = SwimMath.FacingDirection(aim, _brakeHeld, _forwardHeld, _strafe);
            Quaternion target = SwimMath.FacingRotation(facingDirection, aim);
            float angleToTarget = Quaternion.Angle(_rb.rotation, target);
            bool aligned = angleToTarget <= _thrustAlignmentAngle;

            if (!_brakeHeld){
                _reversing = false;
            }else if (SwimMath.ShouldStartReversing(_brakeHeld, _reversing,
                     _rb.linearVelocity.magnitude, _reverseEntrySpeed, aligned)){
                _reversing = true;
            }

            Vector3 thrustDirection;
            if (_brakeHeld){
                thrustDirection = _reversing ? facingDirection : Vector3.zero;
            }else{
                thrustDirection = SwimMath.MoveDirection(aim, _forwardHeld, _strafe);
            }

            if (_brakeHeld){
                if (_reversing){
                    _rb.AddForce(thrustDirection * _swimForce, ForceMode.Acceleration);
                }else{
                    _rb.AddForce(-_rb.linearVelocity * _brakeStrength, ForceMode.Acceleration);
                }
            }else if (thrustDirection.sqrMagnitude > 0.0001f){
                _rb.AddForce(thrustDirection * _swimForce, ForceMode.Acceleration);
            }

            _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, target,
                              _turnSpeed * Time.fixedDeltaTime));
        }
    }
}
