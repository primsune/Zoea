using NUnit.Framework;
using UnityEngine;
using Zoea.Core;

namespace Zoea.Tests.EditMode{
    /// <summary>
    /// EditMode tests for OrbitCameraMath: pitch clamping, orbit position, and aim rotation.
    /// </summary>
    [TestFixture]
    public class OrbitCameraMathTests{
        private const float MinPitch = -85f;
        private const float MaxPitch = 85f;

        [Test]
        public void ClampPitch_WithinRange_ReturnsUnchanged(){
            float result = OrbitCameraMath.ClampPitch(10f, MinPitch, MaxPitch);
            Assert.That(result, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void ClampPitch_BelowMin_ReturnsMin(){
            float result = OrbitCameraMath.ClampPitch(-90f, MinPitch, MaxPitch);
            Assert.That(result, Is.EqualTo(MinPitch).Within(0.0001f));
        }

        [Test]
        public void ClampPitch_AboveMax_ReturnsMax(){
            float result = OrbitCameraMath.ClampPitch(90f, MinPitch, MaxPitch);
            Assert.That(result, Is.EqualTo(MaxPitch).Within(0.0001f));
        }

        [Test]
        public void ClampPitch_AtMin_ReturnsMin(){
            float result = OrbitCameraMath.ClampPitch(MinPitch, MinPitch, MaxPitch);
            Assert.That(result, Is.EqualTo(MinPitch).Within(0.0001f));
        }

        [Test]
        public void ClampPitch_AtMax_ReturnsMax(){
            float result = OrbitCameraMath.ClampPitch(MaxPitch, MinPitch, MaxPitch);
            Assert.That(result, Is.EqualTo(MaxPitch).Within(0.0001f));
        }

        [Test]
        public void DesiredPosition_IsAlwaysDistanceFromPivot_AcrossYawPitchPairs(){
            Vector3 pivot = new Vector3(3f, 5f, -2f);
            float distance = 10f;
            float[] yaws = { 0f, 90f, 180f, -37f, 270f };
            float[] pitches = { 0f, 0f, 45f, -85f, 85f };

            for (int i = 0; i < yaws.Length; i++){
                Vector3 result = OrbitCameraMath.DesiredPosition(pivot, yaws[i], pitches[i], distance);
                float actualDistance = (result - pivot).magnitude;
                Assert.That(actualDistance, Is.EqualTo(distance).Within(0.0001f), $"yaw={yaws[i]} pitch={pitches[i]}");
            }
        }

        [Test]
        public void DesiredPosition_YawZeroPitchZero_ReturnsPivotMinusDistanceOnZ(){
            Vector3 pivot = new Vector3(3f, 5f, -2f);
            float distance = 10f;
            Vector3 expected = pivot + new Vector3(0f, 0f, -distance);

            Vector3 result = OrbitCameraMath.DesiredPosition(pivot, 0f, 0f, distance);

            Assert.That((result - expected).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void DesiredPosition_ZeroDistance_ReturnsPivot(){
            Vector3 pivot = new Vector3(3f, 5f, -2f);

            Vector3 result = OrbitCameraMath.DesiredPosition(pivot, 40f, -20f, 0f);

            Assert.That((result - pivot).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void AimRotation_YawZeroPitchZero_IsIdentity(){
            Quaternion result = OrbitCameraMath.AimRotation(0f, 0f);
            Assert.That(Quaternion.Angle(result, Quaternion.identity), Is.LessThan(0.01f));
        }

        [Test]
        public void AimRotation_Yaw90_RotatesForwardToRight(){
            Quaternion aim = OrbitCameraMath.AimRotation(90f, 0f);
            Vector3 rotated = aim * Vector3.forward;
            Assert.That((rotated - Vector3.right).magnitude, Is.LessThan(0.0001f));
        }
    }
}
