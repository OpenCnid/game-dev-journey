using NUnit.Framework;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChaseMovementITests
    {
        [Test]
        public void HeldHopRelaunchesOnLandingWithoutAnAirDoubleJump()
        {
            var movement = new ChaseMovement();
            movement.Step(Vector3.forward, true, true, 1f / 60);
            for (int frame = 0; frame < 40; frame++)
                movement.Step(Vector3.forward, false, true, 1f / 60);
            Assert.That(movement.JumpCount, Is.EqualTo(1));
            movement.Step(Vector3.forward, true, true, 1f / 60);
            Assert.That(movement.JumpCount, Is.EqualTo(2));
            Assert.That(movement.VerticalVelocity, Is.GreaterThan(7));
            Assert.That(movement.HorizontalVelocity.magnitude, Is.LessThanOrEqualTo(9.001f));
        }
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void RetainedForwardInputStillPermitsLateAirStrafing(int frequency)
        {
            float dt = 1f / frequency;
            var left = new ChaseMovement();
            var right = new ChaseMovement();
            left.Step(Vector3.forward, true, true, dt);
            right.Step(Vector3.forward, true, true, dt);
            for (int step = 0; step < frequency / 4; step++)
            {
                left.Step(Vector3.forward, false, false, dt);
                right.Step(Vector3.forward, false, false, dt);
            }
            for (int step = 0; step < frequency / 5; step++)
            {
                left.Step(Vector3.forward + Vector3.left, false, false, dt);
                right.Step(Vector3.forward + Vector3.right, false, false, dt);
            }
            Assert.That(left.HorizontalVelocity.x, Is.LessThan(-.5f));
            Assert.That(right.HorizontalVelocity.x, Is.GreaterThan(.5f));
            Assert.That(left.HorizontalVelocity.x, Is.EqualTo(-right.HorizontalVelocity.x).Within(.002f));
            Assert.That(left.HorizontalVelocity.z, Is.GreaterThan(5));
            Assert.That(left.HorizontalVelocity.magnitude, Is.LessThanOrEqualTo(9.001f));
            Assert.That(left.JumpCount, Is.EqualTo(1));
        }

        [Test]
        public void SmallMouseRelativeYawSteersWithoutImmediateReversal()
        {
            var movement = new ChaseMovement();
            movement.Step(Vector3.forward, true, true, 1f / 60);
            Vector3 yawed = Quaternion.Euler(0, 25, 0) * Vector3.forward;
            for (int step = 0; step < 20; step++)
                movement.Step(yawed, false, false, 1f / 60);
            Assert.That(movement.HorizontalVelocity.x, Is.GreaterThan(.5f));
            Assert.That(movement.HorizontalVelocity.z, Is.GreaterThan(5));
            Assert.That(movement.HorizontalVelocity.magnitude, Is.LessThanOrEqualTo(9.001f));
        }

        [Test]
        public void WindstepActuallyIncreasesAirCorrectionWithoutRemovingSpeedCap()
        {
            var normal = new ChaseMovement(); var empowered = new ChaseMovement();
            normal.Step(Vector3.forward, true, true, 1f / 60);
            empowered.Step(Vector3.forward, true, true, 1f / 60);
            for (int step = 0; step < 12; step++)
            {
                normal.Step(Vector3.forward + Vector3.right, false, false, 1f / 60);
                empowered.Step(Vector3.forward + Vector3.right, false, false, 1f / 60, 1, 1.5f);
            }
            Assert.That(empowered.HorizontalVelocity.x, Is.GreaterThan(normal.HorizontalVelocity.x));
            Assert.That(empowered.HorizontalVelocity.magnitude, Is.LessThanOrEqualTo(9.001f));
        }
    }
}
