using NUnit.Framework;
using UnityEngine;
using Zoea.Core;

namespace Zoea.Tests.EditMode{
    /// <summary>
    /// EditMode tests for SwimMath: move direction composition, the body-facing
    /// direction while braking/reversing, the brake-to-reverse handoff, and
    /// facing rotation including the straight up/down edge cases.
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
        public void FacingDirection_BrakeHeldIdentityAim_ReturnsAimBack(){
            Vector3 result = SwimMath.FacingDirection(Quaternion.identity, true, false, 0f);
            Assert.That((result - Vector3.back).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void FacingDirection_BrakeHeldNonIdentityAimWithPitch_ReturnsAimBack(){
            Quaternion aim = Quaternion.Euler(30f, 45f, 0f);
            Vector3 result = SwimMath.FacingDirection(aim, true, false, 0f);
            Vector3 expected = aim * Vector3.back;

            Assert.That((result - expected).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void FacingDirection_BrakeHeldZeroStrafe_ReturnsAimBackRegardlessOfForward(){
            Quaternion aim = Quaternion.Euler(15f, 60f, 0f);
            Vector3 expected = aim * Vector3.back;

            Vector3 resultForwardNotHeld = SwimMath.FacingDirection(aim, true, false, 0f);
            Vector3 resultForwardHeld = SwimMath.FacingDirection(aim, true, true, 0f);

            Assert.That((resultForwardNotHeld - expected).magnitude, Is.LessThan(0.0001f));
            Assert.That((resultForwardHeld - expected).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void FacingDirection_BrakeHeldStrafePositive_Is45DegreesTowardRight(){
            Quaternion aim = Quaternion.Euler(15f, 60f, 0f);
            Vector3 back = aim * Vector3.back;
            Vector3 right = aim * Vector3.right;

            Vector3 resultForwardNotHeld = SwimMath.FacingDirection(aim, true, false, 1f);
            Vector3 resultForwardHeld = SwimMath.FacingDirection(aim, true, true, 1f);

            Assert.That(Vector3.Angle(resultForwardNotHeld, back), Is.EqualTo(45f).Within(0.01f));
            Assert.That(Vector3.Dot(resultForwardNotHeld, right), Is.GreaterThan(0f));
            Assert.That((resultForwardNotHeld - resultForwardHeld).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void FacingDirection_BrakeHeldStrafeNegative_Is45DegreesTowardLeft(){
            Quaternion aim = Quaternion.Euler(15f, 60f, 0f);
            Vector3 back = aim * Vector3.back;
            Vector3 right = aim * Vector3.right;

            Vector3 resultForwardNotHeld = SwimMath.FacingDirection(aim, true, false, -1f);
            Vector3 resultForwardHeld = SwimMath.FacingDirection(aim, true, true, -1f);

            Assert.That(Vector3.Angle(resultForwardNotHeld, back), Is.EqualTo(45f).Within(0.01f));
            Assert.That(Vector3.Dot(resultForwardNotHeld, right), Is.LessThan(0f));
            Assert.That((resultForwardNotHeld - resultForwardHeld).magnitude, Is.LessThan(0.0001f));
        }

        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(1f)]
        public void FacingDirection_BrakeHeld_ResultIsNormalized(float strafe){
            Quaternion aim = Quaternion.Euler(15f, 60f, 0f);
            Vector3 result = SwimMath.FacingDirection(aim, true, false, strafe);
            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void FacingDirection_BrakeNotHeldForwardHeld_MatchesMoveDirection(){
            Quaternion aim = Quaternion.Euler(10f, 20f, 0f);
            Vector3 result = SwimMath.FacingDirection(aim, false, true, 0f);
            Vector3 expected = SwimMath.MoveDirection(aim, true, 0f);

            Assert.That((result - expected).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void FacingDirection_BrakeNotHeldNoInput_ReturnsZero(){
            Vector3 result = SwimMath.FacingDirection(Quaternion.identity, false, false, 0f);
            Assert.That(result.magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void ShouldStartReversing_BrakeHeldNotReversingBelowEntryAligned_ReturnsTrue(){
            bool result = SwimMath.ShouldStartReversing(true, false, 1f, EntrySpeed, true);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldStartReversing_BrakeHeldNotReversingAtEntryAligned_ReturnsTrue(){
            bool result = SwimMath.ShouldStartReversing(true, false, EntrySpeed, EntrySpeed, true);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldStartReversing_BrakeHeldNotReversingAboveEntryAligned_ReturnsFalse(){
            bool result = SwimMath.ShouldStartReversing(true, false, 3f, EntrySpeed, true);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldStartReversing_BrakeHeldAlreadyReversingBelowEntryAligned_ReturnsFalse(){
            bool result = SwimMath.ShouldStartReversing(true, true, 1f, EntrySpeed, true);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldStartReversing_BrakeNotHeldNotReversingBelowEntryAligned_ReturnsFalse(){
            bool result = SwimMath.ShouldStartReversing(false, false, 1f, EntrySpeed, true);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldStartReversing_BrakeNotHeldAlreadyReversingBelowEntryAligned_ReturnsFalse(){
            bool result = SwimMath.ShouldStartReversing(false, true, 1f, EntrySpeed, true);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldStartReversing_BrakeHeldNotReversingBelowEntryNotAligned_ReturnsFalse(){
            bool result = SwimMath.ShouldStartReversing(true, false, 1f, EntrySpeed, false);
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
