using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class FeedbackTests
    {
        static Ray AtEnemy(CombatSimulation sim) => new Ray(sim.Position(0) + Vector3.up * 1.1f - Vector3.forward * 3, Vector3.forward);

        [TestCase(false, false)] [TestCase(true, false)] [TestCase(true, true)]
        public void HitOutcomeMatchesHealthAndKeepsDeathSeparateFromRespawn(bool burst, bool native)
        {
            using (var sim = new CombatSimulation { UseBurst = burst, UseNativeState = native })
            {
                var feedback = new CombatFeedback(); sim.Reset(1, 15, false);
                int incarnation = sim.EnemySpawnVersion(0);
                Assert.That(sim.EnemyHealth(0), Is.EqualTo(40));
                Assert.That(sim.TryFire(AtEnemy(sim), out ShotResult wound), Is.True);
                Assert.That(wound.Hit, Is.True); Assert.That(wound.Killed, Is.False);
                Assert.That(wound.Damage, Is.EqualTo(20)); Assert.That(wound.HealthAfter, Is.EqualTo(20));
                Assert.That(sim.EnemyHealth(0), Is.EqualTo(wound.HealthAfter));
                feedback.RecordShot(sim, wound);
                Assert.That(sim.TryFire(AtEnemy(sim), out ShotResult blocked), Is.False);
                feedback.RecordShot(sim, blocked);
                Assert.That(feedback.ImpactCount, Is.EqualTo(1), "Cooldown must not create another damage cue.");
                sim.Step(Vector3.zero, .12f);
                Vector3 oldPosition = sim.Position(0);
                Assert.That(sim.TryFire(AtEnemy(sim), out ShotResult death), Is.True);
                Assert.That(death.Killed, Is.True); Assert.That(death.HealthAfter, Is.Zero);
                Assert.That(death.SpawnVersion, Is.EqualTo(incarnation));
                Assert.That(Vector3.Distance(death.End, oldPosition + Vector3.up * 1.1f), Is.EqualTo(CombatSimulation.EnemyRadius).Within(.001));
                Assert.That(sim.EnemyHealth(0), Is.EqualTo(40));
                Assert.That(sim.EnemySpawnVersion(0), Is.Not.EqualTo(incarnation));
                feedback.RecordShot(sim, death); feedback.Advance(sim, 0);
                Assert.That(feedback.ImpactCount, Is.EqualTo(1), "Old wound must not follow the fresh enemy.");
                Assert.That(feedback.KillMarkerVisible, Is.True);
                var hits = new Matrix4x4[128]; var kills = new Matrix4x4[128];
                feedback.BuildSparks(sim, 1, hits, kills, out int hitCount, out int killCount);
                Assert.That(hitCount, Is.Zero); Assert.That(killCount, Is.GreaterThan(0));
                Assert.That((Vector3)kills[0].GetColumn(3), Is.EqualTo(death.End));
                Assert.That(sim.Kills, Is.EqualTo(1));
                feedback.Reset(); sim.Reset(1, 15, false);
                Assert.That(feedback.ImpactCount, Is.Zero); Assert.That(feedback.KillMarkerVisible, Is.False);
                Assert.That(sim.EnemyHealth(0), Is.EqualTo(40));
            }
        }

        [Test]
        public void MissDoesNotCreateDamageAndPausedOrTerminalShotsAreRejected()
        {
            using (var sim = new CombatSimulation())
            {
                var feedback = new CombatFeedback(); sim.Reset(1, 15, false, .2f);
                Assert.That(sim.TryFire(new Ray(Vector3.up * 10, Vector3.up), out ShotResult miss), Is.True);
                Assert.That(miss.Hit, Is.False); feedback.RecordShot(sim, miss);
                Assert.That(feedback.ImpactCount, Is.Zero); Assert.That(sim.EnemyHealth(0), Is.EqualTo(40));
                sim.TogglePause(); Assert.That(sim.TryFire(AtEnemy(sim), out _), Is.False);
                sim.TogglePause(); sim.Step(Vector3.zero, .2f);
                Assert.That(sim.State, Is.EqualTo(RunState.Won)); Assert.That(sim.TryFire(AtEnemy(sim), out _), Is.False);
            }
        }

        [Test]
        public void ActualPlayerHealthLossPulsesOnceFreezesOnPauseAndClearsOnRestart()
        {
            using (var sim = new CombatSimulation())
            {
                var feedback = new CombatFeedback(); sim.Reset(35, 137, false);
                for (int i = 0; i < 1800 && sim.PlayerHealth == 100; i++) sim.Step(Vector3.zero, CombatSimulation.Tick);
                feedback.ObservePlayerHealth(sim.PlayerHealth);
                Assert.That(feedback.PlayerDamage, Is.EqualTo(10));
                feedback.Advance(sim, .1f); float remaining = feedback.PlayerPulseRemaining;
                feedback.ObservePlayerHealth(sim.PlayerHealth);
                Assert.That(feedback.PlayerPulseRemaining, Is.EqualTo(remaining), "Health that has not changed must not retrigger the pulse.");
                sim.TogglePause(); feedback.Advance(sim, 2);
                Assert.That(feedback.PlayerPulseRemaining, Is.EqualTo(remaining));
                sim.TogglePause(); feedback.Advance(sim, 1);
                Assert.That(feedback.PlayerPulseRemaining, Is.Zero); Assert.That(feedback.PlayerLabelRemaining, Is.Zero);
                for (int i = 0; i < 1800 && sim.State == RunState.Running; i++) sim.Step(Vector3.zero, CombatSimulation.Tick);
                feedback.ObservePlayerHealth(sim.PlayerHealth); Assert.That(sim.State, Is.EqualTo(RunState.Lost));
                feedback.Advance(sim, 1); Assert.That(feedback.PlayerLabelRemaining, Is.Zero, "Final damage should expire on the loss screen.");
                feedback.Reset(); sim.Reset(35, 137, false); feedback.ObservePlayerHealth(sim.PlayerHealth);
                Assert.That(feedback.PlayerDamage, Is.Zero); Assert.That(feedback.PlayerPulseRemaining, Is.Zero);
                sim.Reset(100, 137, true); feedback.Advance(sim, 0);
                Assert.That(feedback.ImpactCount, Is.Zero); Assert.That(feedback.BarCount, Is.Zero);
            }
        }

        [Test]
        public void BarsReadAuthoritativeWoundsAndFilterBehindCameraAndDistance()
        {
            var root = new GameObject("Bar projection test");
            try
            {
                Camera camera = root.AddComponent<Camera>(); camera.fieldOfView = 85;
                using (var sim = new CombatSimulation())
                {
                    sim.Reset(1, 15, false); var feedback = new CombatFeedback();
                    camera.transform.position = sim.Position(0) - Vector3.forward * 5 + Vector3.up * 1.65f;
                    feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.BarCount, Is.EqualTo(1)); Assert.That(feedback.Bar(0).Health, Is.EqualTo(40));
                    sim.TryFire(AtEnemy(sim), out _); feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.Bar(0).Health, Is.EqualTo(20));
                    sim.TogglePause(); feedback.CollectBars(sim, camera, 1); Assert.That(feedback.BarCount, Is.EqualTo(1));
                    camera.transform.rotation = Quaternion.Euler(0, 180, 0); feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.BarCount, Is.Zero);
                    camera.transform.rotation = Quaternion.identity;
                    camera.transform.position -= Vector3.forward * 40; feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.BarCount, Is.Zero);
                    sim.Reset(1000, 137, false); camera.transform.position = Vector3.up * 1.65f;
                    feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.BarCount, Is.EqualTo(CombatFeedback.BarCapacity));
                    for (int i = 1; i < feedback.BarCount; i++) Assert.That(feedback.Bar(i).Distance, Is.GreaterThanOrEqualTo(feedback.Bar(i - 1).Distance));
                    sim.Reset(1000, 137, true); feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.BarCount, Is.Zero, "Benchmark workload must not acquire gameplay bars.");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
