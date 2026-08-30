using NUnit.Framework;
using UnityEngine;
using Zoea.Core;

namespace Zoea.Tests.EditMode{
    /// <summary>
    /// EditMode tests for SwimMath: move direction composition, the brake-to-reverse
    /// handoff, and facing rotation including the straight up/down edge cases.
    /// </summary>
    [TestFixture]
    public class SwimMathTests{
        private const float EntrySpeed = 2f;

        [Test]
        public void MoveDirection_NoForwardZeroStrafe_ReturnsZeroVector(){
            Vector3 result = SwimMath.MoveDirection(Quaternion.identity, false, 0f);
            Assert.That(result.magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void MoveDirection_ForwardHeldZeroStrafeIdentityAim_ReturnsForward(){
            Vector3 result = SwimMath.MoveDirection(Quaternion.identity, true, 0f);
            Assert.That((result - Vector3.forward).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void MoveDirection_NotForwardStrafeOne_ReturnsRight(){
            Vector3 result = SwimMath.MoveDirection(Quaternion.identity, false, 1f);
            Assert.That((result - Vector3.right).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void MoveDirection_NotForwardStrafeNegativeOne_ReturnsLeft(){
            Vector3 result = SwimMath.MoveDirection(Quaternion.identity, false, -1f);
            Assert.That((result - Vector3.left).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void MoveDirection_ForwardAndStrafeOne_IsNormalizedDiagonal(){
            Vector3 result = SwimMath.MoveDirection(Quaternion.identity, true, 1f);
            Vector3 expected = (Vector3.forward + Vector3.right).normalized;

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That((result - expected).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void MoveDirection_ForwardHeldNonIdentityAim_EqualsAimTimesForward(){
            Quaternion aim = Quaternion.Euler(30f, 45f, 0f);
            Vector3 result = SwimMath.MoveDirection(aim, true, 0f);
            Vector3 expected = aim * Vector3.forward;

            Assert.That((result - expected).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void ShouldStartReversing_BrakeHeldNotReversingBelowEntry_ReturnsTrue(){
            bool result = SwimMath.ShouldStartReversing(true, false, 1f, EntrySpeed);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldStartReversing_BrakeHeldNotReversingAtEntry_ReturnsTrue(){
            bool result = SwimMath.ShouldStartReversing(true, false, EntrySpeed, EntrySpeed);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldStartReversing_BrakeHeldNotReversingAboveEntry_ReturnsFalse(){
            bool result = SwimMath.ShouldStartReversing(true, false, 3f, EntrySpeed);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldStartReversing_BrakeHeldAlreadyReversingBelowEntry_ReturnsFalse(){
            bool result = SwimMath.ShouldStartReversing(true, true, 1f, EntrySpeed);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldStartReversing_BrakeNotHeldNotReversingBelowEntry_ReturnsFalse(){
            bool result = SwimMath.ShouldStartReversing(false, false, 1f, EntrySpeed);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldStartReversing_BrakeNotHeldAlreadyReversingBelowEntry_ReturnsFalse(){
            bool result = SwimMath.ShouldStartReversing(false, true, 1f, EntrySpeed);
            Assert.That(result, Is.False);
        }

        [Test]
        public void FacingRotation_ZeroMoveDirection_ReturnsAimRotationUnchanged(){
            Quaternion aim = Quaternion.Euler(10f, 20f, 30f);
            Quaternion result = SwimMath.FacingRotation(Vector3.zero, aim);
            Assert.That(Quaternion.Angle(result, aim), Is.LessThan(0.01f));
        }

        [Test]
        public void FacingRotation_TinyMoveDirection_ReturnsAimRotationUnchanged(){
            Quaternion aim = Quaternion.Euler(10f, 20f, 30f);
            Quaternion result = SwimMath.FacingRotation(new Vector3(0.001f, 0f, 0f), aim);
            Assert.That(Quaternion.Angle(result, aim), Is.LessThan(0.01f));
        }

        [Test]
        public void FacingRotation_HorizontalMoveDirection_ForwardPointsAlongMoveDirection(){
            Vector3 moveDirection = new Vector3(1f, 0f, 1f);
            Quaternion result = SwimMath.FacingRotation(moveDirection, Quaternion.identity);
            Vector3 expected = moveDirection.normalized;

            Assert.That((result * Vector3.forward - expected).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void FacingRotation_StraightUp_ForwardPointsUp(){
            // Regression guard: FacingRotation must pass the camera's own up
            // vector (aimRotation * Vector3.up) into Quaternion.LookRotation,
            // not Vector3.up, because LookRotation degenerates when the look
            // direction is parallel to the up reference -- which is exactly
            // what happens when the creature swims straight up. Do not
            // "simplify" this to use Vector3.up as the up reference.
            Quaternion aim = Quaternion.Euler(-89f, 0f, 0f);
            Quaternion result = SwimMath.FacingRotation(Vector3.up, aim);

            Assert.That((result * Vector3.forward - Vector3.up).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void FacingRotation_StraightDown_ForwardPointsDown(){
            // Same degenerate case as straight up, mirrored: the look
            // direction (down) must not be parallel to the up reference
            // passed to Quaternion.LookRotation, which is why FacingRotation
            // uses the camera's up vector instead of Vector3.up.
            Quaternion aim = Quaternion.Euler(89f, 0f, 0f);
            Quaternion result = SwimMath.FacingRotation(Vector3.down, aim);

            Assert.That((result * Vector3.forward - Vector3.down).magnitude, Is.LessThan(0.0001f));
        }
    }
}
