using System;
using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class RifleRateTests
    {
        [TestCase(.5f, 30)] [TestCase(2, 144)] [TestCase(CombatSimulation.DefaultRifleRate, 240)]
        [TestCase(30, 60)] [TestCase(120, 8)] [TestCase(120, 30)] [TestCase(120, 144)] [TestCase(120, 240)]
        public void ConfiguredCadenceMatchesAcrossCombatFramesAndBenchmarkTicks(float rate, int fps)
        {
            using (var combat = new CombatSimulation { RifleRate = rate })
            using (var benchmark = new CombatSimulation { RifleRate = rate })
            {
                combat.Reset(0, 137, false); benchmark.Reset(0, 137, true);
                var ray = new Ray(Vector3.up, Vector3.forward);
                double remainder = 0; int ticks = 0;
                for (int frame = 0; frame < fps * 10; frame++)
                {
                    remainder += 1d / fps; bool stepped = false;
                    while (remainder + 1e-6 >= CombatSimulation.Tick)
                    {
                        combat.Step(Vector3.zero, CombatSimulation.Tick);
                        benchmark.Step(Vector3.zero, CombatSimulation.Tick);
                        while (benchmark.TryFire(ray, out ShotResult _)) { }
                        remainder -= CombatSimulation.Tick; ticks++; stepped = true;
                    }
                    if (stepped) while (combat.TryFire(ray, out ShotResult _)) { }
                    Assert.That(combat.Shots, Is.EqualTo(benchmark.Shots), "Frame batching must not change due shots, including rates above 60/s.");
                }
                Assert.That(ticks, Is.EqualTo(600));
                int expected = (int)Math.Floor(ticks * (double)CombatSimulation.Tick / combat.RifleInterval) + 1;
                Assert.That(combat.Shots, Is.EqualTo(expected));
                Assert.That(combat.RifleRate, Is.EqualTo(rate));
                if (rate == CombatSimulation.DefaultRifleRate) Assert.That(combat.Shots, Is.EqualTo(84));
                if (rate == 120) Assert.That(benchmark.Shots, Is.EqualTo(1201), "Benchmark cannot silently cap fast guns at one shot per tick.");
            }
        }

        [Test]
        public void RateChangesPreserveCooldownProgressWhilePauseAndRestartKeepTheirRules()
        {
            using (var sim = new CombatSimulation())
            {
                sim.Reset(0, 137, false);
                var ray = new Ray(Vector3.up, Vector3.forward);
                Assert.That(sim.TryFire(ray, out ShotResult _), Is.True);
                sim.Step(Vector3.zero, .06f); sim.TogglePause();
                float elapsed = sim.Elapsed;
                sim.RifleRate = 2; sim.Step(Vector3.zero, 10);
                Assert.That(sim.Elapsed, Is.EqualTo(elapsed));
                Assert.That(sim.TryFire(ray, out ShotResult _), Is.False);
                sim.TogglePause(); sim.Step(Vector3.zero, .24f);
                Assert.That(sim.TryFire(ray, out ShotResult _), Is.False, "A half-finished cooldown becomes 0.25 s remaining at 2/s.");
                sim.Step(Vector3.zero, .011f);
                Assert.That(sim.TryFire(ray, out ShotResult _), Is.True);
                sim.Reset(35, 137, false);
                Assert.That(sim.RifleRate, Is.EqualTo(2)); Assert.That(sim.RifleInterval, Is.EqualTo(.5f));
                Assert.That(sim.Shots, Is.Zero); Assert.That(sim.Kills, Is.Zero);
                Assert.That(sim.PlayerHealth, Is.EqualTo(100)); Assert.That(sim.Elapsed, Is.Zero);
                sim.RifleRate = CombatSimulation.DefaultRifleRate;
                Assert.That(sim.RifleInterval, Is.EqualTo(.12f), "Baseline restores its exact original interval.");
            }
        }

        [Test]
        public void BaselineDecimalInputRestoresTheExactOriginalInterval()
        {
            using (var sim = new CombatSimulation { RifleRate = 120 })
            {
                sim.RifleRate = float.Parse("8.333333333333334", System.Globalization.CultureInfo.InvariantCulture);
                Assert.That(sim.RifleRate, Is.EqualTo(CombatSimulation.DefaultRifleRate));
                Assert.That(sim.RifleInterval, Is.EqualTo(.12f));
            }
        }

        [Test]
        public void InvalidRatesCannotPoisonTheWeaponClock()
        {
            using (var sim = new CombatSimulation())
            {
                foreach (float invalid in new[] { 0, -.5f, .49f, 121, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => sim.RifleRate = invalid);
                    Assert.That(sim.RifleInterval, Is.EqualTo(.12f));
                }
                sim.Reset(0, 137, false); sim.Step(Vector3.zero, CombatSimulation.Tick);
                Assert.That(sim.TryFire(new Ray(Vector3.up, Vector3.forward), out ShotResult _), Is.True);
                Assert.That(sim.Shots, Is.EqualTo(1));
            }
        }
    }
}
