using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class CombatTests
    {
        [Test]
        public void SameSeedReproducesCrowdAndDifferentSeedChangesIt()
        {
            var a = new CombatSimulation(); var b = new CombatSimulation(); var c = new CombatSimulation();
            a.Reset(100, 137, true); b.Reset(100, 137, true); c.Reset(100, 138, true);
            for (int i = 0; i < 120; i++) { a.Step(Vector3.zero, CombatSimulation.Tick); b.Step(Vector3.zero, CombatSimulation.Tick); }
            for (int i = 0; i < 100; i++) Assert.That(a.Position(i), Is.EqualTo(b.Position(i)));
            Assert.That(a.Position(0), Is.Not.EqualTo(c.Position(0)));
        }

        [Test]
        public void PauseFreezesMovementClockAndWeapon()
        {
            var sim = new CombatSimulation(); sim.Reset(10, 1, false);
            Vector3 enemy = sim.Position(0); sim.TogglePause();
            sim.Step(Vector3.forward, 10);
            Assert.That(sim.Elapsed, Is.Zero);
            Assert.That(sim.Player, Is.EqualTo(Vector3.zero));
            Assert.That(sim.Position(0), Is.EqualTo(enemy));
            Assert.That(sim.TryFire(new Ray(Vector3.up, Vector3.forward), out _, out _), Is.False);
            sim.TogglePause(); sim.Step(Vector3.forward, CombatSimulation.Tick);
            Assert.That(sim.Elapsed, Is.GreaterThan(0));
        }

        [TestCase(60)] [TestCase(144)] [TestCase(240)] [TestCase(360)]
        public void RenderRateDoesNotChangeFixedTickMovementOrShotCount(int fps)
        {
            var sim = new CombatSimulation(); sim.Reset(0, 1, false);
            double accumulator = 0;
            for (int frame = 0; frame < fps * 10; frame++)
            {
                accumulator += 1d / fps;
                while (accumulator + 1e-7 >= CombatSimulation.Tick)
                {
                    sim.Step(Vector3.right, CombatSimulation.Tick);
                    sim.TryFire(new Ray(Vector3.up, Vector3.forward), out _, out _);
                    accumulator -= CombatSimulation.Tick;
                }
            }
            Assert.That(sim.Shots, Is.EqualTo(84));
            Assert.That(sim.Player.x, Is.EqualTo(CombatSimulation.ArenaLimit));
            Assert.That(sim.Elapsed, Is.EqualTo(10).Within(.02));
        }

        [Test]
        public void TwoHitsKillAndOnlyOneKillIsAwardedBeforeRespawn()
        {
            var sim = new CombatSimulation(); sim.Reset(1, 15, false);
            Vector3 initial = sim.Position(0);
            Ray ray = new Ray(initial + Vector3.up * .8f - Vector3.forward * 3, Vector3.forward);
            Assert.That(sim.TryFire(ray, out _, out bool killed), Is.True); Assert.That(killed, Is.False);
            Assert.That(sim.TryFire(ray, out _, out _), Is.False, "Cannot fire twice without cooldown.");
            sim.Step(Vector3.zero, .12f);
            ray = new Ray(sim.Position(0) + Vector3.up * .8f - Vector3.forward * 3, Vector3.forward);
            sim.TryFire(ray, out _, out killed);
            Assert.That(killed, Is.True); Assert.That(sim.Kills, Is.EqualTo(1));
            Assert.That(sim.Count, Is.EqualTo(1));
            Assert.That((sim.Position(0) - sim.Player).sqrMagnitude, Is.GreaterThan(100));
        }

        [Test]
        public void RestartClearsRunStateAndDoesNotExceedCapacity()
        {
            var sim = new CombatSimulation(); sim.Reset(CombatSimulation.Capacity + 1, 1, true);
            Assert.That(sim.Count, Is.EqualTo(CombatSimulation.Capacity)); sim.Step(Vector3.zero, 1);
            sim.TryFire(new Ray(Vector3.up, Vector3.forward), out _, out _);
            sim.Reset(35, 1, false);
            Assert.That(sim.Elapsed, Is.Zero); Assert.That(sim.Shots, Is.Zero); Assert.That(sim.Kills, Is.Zero);
            Assert.That(sim.PlayerHealth, Is.EqualTo(100)); Assert.That(sim.State, Is.EqualTo(RunState.Running));
            Assert.That(sim.Benchmark, Is.False); Assert.That(sim.Count, Is.EqualTo(35));
        }

        [Test]
        public void ContactDamageHasGracePeriodAndCanEndTheRun()
        {
            var sim = new CombatSimulation(); sim.Reset(35, 137, false, 60);
            for (int tick = 0; tick < 1800 && sim.PlayerHealth == 100; tick++) sim.Step(Vector3.zero, CombatSimulation.Tick);
            Assert.That(sim.PlayerHealth, Is.EqualTo(90), "Crowd must be able to reach and damage the player.");
            for (int tick = 0; tick < 10; tick++) sim.Step(Vector3.zero, CombatSimulation.Tick);
            Assert.That(sim.PlayerHealth, Is.EqualTo(90), "Other enemies cannot bypass the damage grace period.");
            for (int tick = 0; tick < 1800 && sim.State == RunState.Running; tick++) sim.Step(Vector3.zero, CombatSimulation.Tick);
            Assert.That(sim.State, Is.EqualTo(RunState.Lost));
            Assert.That(sim.PlayerHealth, Is.Zero);
        }

        [Test]
        public void CombatEndsAndBenchmarkDoesNotUseCombatDuration()
        {
            var sim = new CombatSimulation(); sim.Reset(0, 1, false, 1);
            sim.Step(Vector3.zero, 1); Assert.That(sim.State, Is.EqualTo(RunState.Won));
            Assert.That(sim.TryFire(new Ray(Vector3.up, Vector3.forward), out _, out _), Is.False);
            sim.Reset(0, 1, true, 1); sim.Step(Vector3.zero, 2);
            Assert.That(sim.State, Is.EqualTo(RunState.Running));
        }

        [Test]
        public void RaySphereRejectsBehindAndZeroDirectionAndReturnsNearestSurface()
        {
            Assert.That(CombatSimulation.RaySphere(new Ray(Vector3.zero, Vector3.forward), new Vector3(0, 0, 5), 1, out float distance), Is.True);
            Assert.That(distance, Is.EqualTo(4).Within(.0001));
            Assert.That(CombatSimulation.RaySphere(new Ray(Vector3.zero, Vector3.forward), new Vector3(0, 0, -5), 1, out _), Is.False);
            Assert.That(CombatSimulation.RaySphere(new Ray(Vector3.zero, Vector3.zero), Vector3.one, 1, out _), Is.False);
        }

        [Test]
        public void NearestRankPercentilesExposeTailSpikes()
        {
            var values = new double[100]; for (int i = 0; i < 100; i++) values[i] = i + 1;
            Assert.That(BenchmarkCapture.Percentile(values, .5), Is.EqualTo(50));
            Assert.That(BenchmarkCapture.Percentile(values, .99), Is.EqualTo(99));
            Assert.Throws<System.ArgumentException>(() => BenchmarkCapture.Percentile(new double[0], .99));
        }

        [Test]
        public void DenseCrowdBoundsNeighborWorkAndKeepsAllEnemiesActive()
        {
            var sim = new CombatSimulation(); sim.Reset(20000, 137, true);
            sim.Step(Vector3.zero, CombatSimulation.Tick);
            Assert.That(sim.Count, Is.EqualTo(20000));
            Assert.That(sim.NeighborChecks, Is.InRange(1, 20000 * 9 * CombatSimulation.NeighborsPerCell));
            for (int i = 0; i < sim.Count; i++)
            {
                Vector3 p = sim.Position(i);
                Assert.That(float.IsNaN(p.x) || float.IsNaN(p.z), Is.False);
                Assert.That(Mathf.Abs(p.x), Is.LessThanOrEqualTo(CombatSimulation.ArenaLimit));
            }
        }

        [Test]
        public void BossesTelegraphBurstCollideAndResetWithoutSkippingStateUpdates()
        {
            var sim = new CombatSimulation(); sim.Reset(100, 137, true, bosses: true, synchronizedAbilities: true);
            for (int i = 0; i < 70; i++) sim.Step(Vector3.zero, CombatSimulation.Tick);
            Assert.That(sim.Abilities.Telegraphs, Is.EqualTo(50));
            Assert.That(sim.Abilities.Spawned, Is.EqualTo(400));
            sim.TogglePause(); int updates = sim.Abilities.StateUpdates;
            sim.Step(Vector3.zero, 1); Assert.That(sim.Abilities.StateUpdates, Is.EqualTo(updates)); sim.TogglePause();
            for (int i = 70; i < 300; i++) sim.Step(Vector3.zero, CombatSimulation.Tick);
            Assert.That(sim.Abilities.Activations, Is.EqualTo(100));
            Assert.That(sim.Abilities.Telegraphs, Is.Zero);
            Assert.That(sim.Abilities.Projectiles, Is.Zero, "Three-second projectile lifetime must expire.");
            Assert.That(sim.Abilities.StateUpdates, Is.EqualTo(30000));
            Assert.That(sim.Abilities.ProjectileUpdates, Is.GreaterThan(0));
            Assert.That(sim.Abilities.Dropped, Is.Zero);
            sim.Reset(35, 137, false);
            Assert.That(sim.Abilities.Enabled, Is.False); Assert.That(sim.Abilities.Spawned, Is.Zero);
            Assert.That(sim.Abilities.Projectiles, Is.Zero); Assert.That(sim.Abilities.Activations, Is.Zero);
        }

        [Test]
        public void SweptProjectileCollisionDoesNotTunnelAcrossPlayer()
        {
            Assert.That(BossAbilities.SegmentHitsPlayer(new Vector3(-10, 0, 0), new Vector3(10, 0, 0)), Is.True);
            Assert.That(BossAbilities.SegmentHitsPlayer(new Vector3(-10, 0, 2), new Vector3(10, 0, 2)), Is.False);
            Assert.That(BossAbilities.SegmentHitsPlayer(Vector3.zero, Vector3.zero), Is.True);
        }
    }
}
