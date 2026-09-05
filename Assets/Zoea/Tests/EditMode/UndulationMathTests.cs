using NUnit.Framework;
using UnityEngine;
using Zoea.Core;

namespace Zoea.Tests.EditMode{
    /// <summary>
    /// EditMode tests for UndulationMath: amplitude bounding, wave travel
    /// direction, periodicity, and determinism of the segment angle formula.
    /// </summary>
    [TestFixture]
    public class UndulationMathTests{
        private const float Amplitude = 25f;
        private const float Frequency = 1.5f;
        private const float PhaseOffset = 40f;
        private const float Tolerance = 1e-4f;
        private const float SpeedForFullAmplitude = 4f;
        private const float IdleFraction = 0.25f;

        [Test]
        public void SegmentAngle_ZeroAmplitude_ReturnsExactlyZero(){
            for (int segmentIndex = 0; segmentIndex <= 7; segmentIndex++){
                for (int step = 0; step < 20; step++){
                    float time = step * 0.37f;
                    float angle = UndulationMath.SegmentAngle(segmentIndex, time, 0f, Frequency, PhaseOffset);
                    Assert.That(angle, Is.EqualTo(0f).Within(Tolerance));
                }
            }
        }

        [Test]
        public void SegmentAngle_AnyIndexOrTime_NeverExceedsAmplitude(){
            for (int segmentIndex = 0; segmentIndex <= 15; segmentIndex++){
                for (float time = 0f; time <= 10f; time += 0.01f){
                    float angle = UndulationMath.SegmentAngle(segmentIndex, time, Amplitude, Frequency, PhaseOffset);
                    Assert.That(Mathf.Abs(angle), Is.LessThanOrEqualTo(Amplitude + 1e-4f));
                }
            }
        }

        [Test]
        public void SegmentAngle_NonzeroPhaseOffset_WaveTravelsFromLowerToHigherIndex(){
            float lag = PhaseOffset / (360f * Frequency);

            for (int segmentIndex = 1; segmentIndex <= 6; segmentIndex++){
                for (int step = 0; step < 10; step++){
                    float time = 3f + step * 0.41f;
                    float current = UndulationMath.SegmentAngle(segmentIndex, time, Amplitude, Frequency, PhaseOffset);
                    float previousLagged = UndulationMath.SegmentAngle(segmentIndex - 1, time - lag, Amplitude, Frequency, PhaseOffset);
                    Assert.That(current, Is.EqualTo(previousLagged).Within(Tolerance));
                }
            }
        }

        [Test]
        public void SegmentAngle_OnePeriodLater_ReturnsSameAngle(){
            float period = 1f / Frequency;

            for (int segmentIndex = 0; segmentIndex <= 7; segmentIndex++){
                for (int step = 0; step < 10; step++){
                    float time = step * 0.9f;
                    float angle = UndulationMath.SegmentAngle(segmentIndex, time, Amplitude, Frequency, PhaseOffset);
                    float angleOnePeriodLater = UndulationMath.SegmentAngle(segmentIndex, time + period, Amplitude, Frequency, PhaseOffset);
                    Assert.That(angleOnePeriodLater, Is.EqualTo(angle).Within(Tolerance));
                }
            }
        }

        [Test]
        public void SegmentAngle_NonzeroPhaseOffsetAndAmplitude_AdjacentSegmentsDifferAtSomeTime(){
            bool foundDifference = false;

            for (float time = 0f; time <= 10f; time += 0.01f){
                float segment0 = UndulationMath.SegmentAngle(0, time, Amplitude, Frequency, PhaseOffset);
                float segment1 = UndulationMath.SegmentAngle(1, time, Amplitude, Frequency, PhaseOffset);
                if (Mathf.Abs(segment0 - segment1) > Tolerance){
                    foundDifference = true;
                    break;
                }
            }

            Assert.That(foundDifference, Is.True);
        }

        [Test]
        public void SegmentAngle_ZeroFrequency_IsTimeInvariant(){
            for (int segmentIndex = 0; segmentIndex <= 7; segmentIndex++){
                float angleAtZero = UndulationMath.SegmentAngle(segmentIndex, 0f, Amplitude, 0f, PhaseOffset);
                for (int step = 1; step < 20; step++){
                    float time = step * 0.53f;
                    float angle = UndulationMath.SegmentAngle(segmentIndex, time, Amplitude, 0f, PhaseOffset);
                    Assert.That(angle, Is.EqualTo(angleAtZero).Within(Tolerance));
                }
            }
        }

        [Test]
        public void SegmentAngle_IdenticalInputs_ReturnsSameOutputEveryCall(){
            for (int call = 0; call < 5; call++){
                float angle = UndulationMath.SegmentAngle(3, 1.234f, Amplitude, Frequency, PhaseOffset);
                Assert.That(angle, Is.EqualTo(UndulationMath.SegmentAngle(3, 1.234f, Amplitude, Frequency, PhaseOffset)).Within(1e-6f));
            }
        }

        [Test]
        public void SpeedFactor_ZeroSpeed_ReturnsIdleFraction(){
            float factor = UndulationMath.SpeedFactor(0f, SpeedForFullAmplitude, IdleFraction);
            Assert.That(factor, Is.EqualTo(IdleFraction).Within(Tolerance));
        }

        [Test]
        public void SpeedFactor_SpeedEqualToSpeedForFullAmplitude_ReturnsOne(){
            float factor = UndulationMath.SpeedFactor(SpeedForFullAmplitude, SpeedForFullAmplitude, IdleFraction);
            Assert.That(factor, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void SpeedFactor_SpeedWellAboveSpeedForFullAmplitude_ReturnsOneNotMore(){
            float factor = UndulationMath.SpeedFactor(SpeedForFullAmplitude * 5f, SpeedForFullAmplitude, IdleFraction);
            Assert.That(factor, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void SpeedFactor_NegativeSpeed_ReturnsIdleFractionNotLess(){
            float factor = UndulationMath.SpeedFactor(-3f, SpeedForFullAmplitude, IdleFraction);
            Assert.That(factor, Is.EqualTo(IdleFraction).Within(Tolerance));
        }

        [Test]
        public void SpeedFactor_AcrossSpeedSweep_NeverOutsideIdleFractionToOne(){
            for (float speed = -5f; speed <= 20f; speed += 0.1f){
                float factor = UndulationMath.SpeedFactor(speed, SpeedForFullAmplitude, IdleFraction);
                Assert.That(factor, Is.GreaterThanOrEqualTo(IdleFraction - Tolerance));
                Assert.That(factor, Is.LessThanOrEqualTo(1f + Tolerance));
            }
        }

        [Test]
        public void SpeedFactor_SpeedForFullAmplitudeZero_ReturnsOne(){
            float factor = UndulationMath.SpeedFactor(2f, 0f, IdleFraction);
            Assert.That(factor, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void SpeedFactor_AcrossSpeedSweep_IsNonDecreasing(){
            float previous = UndulationMath.SpeedFactor(-5f, SpeedForFullAmplitude, IdleFraction);
            for (float speed = -4.9f; speed <= 20f; speed += 0.1f){
                float current = UndulationMath.SpeedFactor(speed, SpeedForFullAmplitude, IdleFraction);
                Assert.That(current, Is.GreaterThanOrEqualTo(previous - Tolerance));
                previous = current;
            }
        }

        [Test]
        public void YawRateDegreesPerSecond_ZeroDeltaTime_ReturnsExactlyZero(){
            Quaternion previous = Quaternion.identity;
            Quaternion current = Quaternion.Euler(0f, 30f, 0f);
            float yawRate = UndulationMath.YawRateDegreesPerSecond(previous, current, Vector3.up, 0f);
            Assert.That(yawRate, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void YawRateDegreesPerSecond_IdenticalRotations_ReturnsApproximatelyZero(){
            Quaternion rotation = Quaternion.Euler(10f, 20f, 5f);
            float yawRate = UndulationMath.YawRateDegreesPerSecond(rotation, rotation, Vector3.up, 0.5f);
            Assert.That(yawRate, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void YawRateDegreesPerSecond_KnownYaw_ReturnsExpectedRate(){
            Quaternion previous = Quaternion.identity;
            Quaternion current = Quaternion.Euler(0f, 30f, 0f);
            float yawRate = UndulationMath.YawRateDegreesPerSecond(previous, current, Vector3.up, 0.5f);
            Assert.That(yawRate, Is.EqualTo(60f).Within(0.01f));
        }

        [Test]
        public void YawRateDegreesPerSecond_ReversedRotation_FlipsSign(){
            Quaternion previous = Quaternion.identity;
            Quaternion current = Quaternion.Euler(0f, -30f, 0f);
            float yawRate = UndulationMath.YawRateDegreesPerSecond(previous, current, Vector3.up, 0.5f);
            Assert.That(yawRate, Is.EqualTo(-60f).Within(0.01f));
        }

        [Test]
        public void YawRateDegreesPerSecond_PurePitch_ReturnsApproximatelyZero(){
            Quaternion previous = Quaternion.identity;
            Quaternion current = Quaternion.Euler(30f, 0f, 0f);
            float yawRate = UndulationMath.YawRateDegreesPerSecond(previous, current, Vector3.up, 0.5f);
            Assert.That(yawRate, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void YawRateDegreesPerSecond_YawAboutPitchedBodyOwnUp_ReturnsExpectedRate(){
            Quaternion previous = Quaternion.Euler(40f, 0f, 0f);
            Quaternion current = previous * Quaternion.Euler(0f, 30f, 0f);
            Vector3 upAxis = previous * Vector3.up;
            float yawRate = UndulationMath.YawRateDegreesPerSecond(previous, current, upAxis, 0.5f);
            Assert.That(yawRate, Is.EqualTo(60f).Within(0.01f));
        }

        [Test]
        public void TrailAngle_SignIsOppositeYawRate(){
            for (float yawRate = -300f; yawRate <= 300f; yawRate += 5f){
                if (Mathf.Abs(yawRate) < Tolerance){
                    continue;
                }

                float trail = UndulationMath.TrailAngle(yawRate, 0.05f, 8f);
                Assert.That(Mathf.Sign(trail), Is.EqualTo(-Mathf.Sign(yawRate)));
            }
        }

        [Test]
        public void TrailAngle_AcrossYawRateSweep_NeverExceedsMaxTrailDegrees(){
            const float maxTrailDegrees = 8f;
            for (float yawRate = -1000f; yawRate <= 1000f; yawRate += 10f){
                float trail = UndulationMath.TrailAngle(yawRate, 0.05f, maxTrailDegrees);
                Assert.That(Mathf.Abs(trail), Is.LessThanOrEqualTo(maxTrailDegrees + Tolerance));
            }
        }

        [Test]
        public void TrailAngle_ZeroYawRate_ReturnsExactlyZero(){
            float trail = UndulationMath.TrailAngle(0f, 0.05f, 8f);
            Assert.That(trail, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void TrailAngle_ZeroDegreesPerYawRate_ReturnsZeroForAnyYawRate(){
            for (float yawRate = -300f; yawRate <= 300f; yawRate += 5f){
                float trail = UndulationMath.TrailAngle(yawRate, 0f, 8f);
                Assert.That(trail, Is.EqualTo(0f).Within(Tolerance));
            }
        }
    }
}
