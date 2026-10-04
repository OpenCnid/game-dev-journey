using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class ShotAlignmentTests
    {
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(true, true)]
        public void HitsUseDisplayedEnemyPoseAndWoundSparksStartAtResolvedSurface(bool burst, bool native)
        {
            using (var sim = new CombatSimulation { UseBurst = burst, UseNativeState = native })
            {
                sim.Reset(1, 15, false);
                Vector3 previous = sim.Position(0);
                sim.Step(Vector3.right, CombatSimulation.Tick);
                const float alpha = .25f;
                Vector3 center = Vector3.Lerp(previous, sim.Position(0), alpha) + Vector3.up * 1.1f;
                Vector3 away = (previous - sim.Position(0)).normalized;
                Vector3 direction = Vector3.Cross(away, Vector3.up).normalized;
                var ray = new Ray(center + away * .61f - direction * 3, direction);
                Assert.That(sim.AimTarget(ray, out _), Is.EqualTo(-1), "The current fixed pose is outside this silhouette-edge ray.");
                Assert.That(sim.AimTarget(ray, out float distance, alpha), Is.EqualTo(0), "The visibly interpolated enemy occupies this ray.");
                Assert.That(CombatSimulation.RaySphere(ray, center, CombatSimulation.EnemyRadius, out float surface), Is.True);
                Assert.That(distance, Is.EqualTo(surface).Within(.00001f));
                Assert.That(sim.TryFire(ray, out ShotResult shot, alpha), Is.True);
                Assert.That(shot.Hit && !shot.Killed, Is.True);
                Assert.That(sim.EnemyHealth(0), Is.EqualTo(20));
                Assert.That(Vector3.Distance(shot.End, ray.GetPoint(surface)), Is.LessThan(.00001f));
                var feedback = new CombatFeedback(); feedback.RecordShot(sim, shot, alpha);
                var hits = new Matrix4x4[CombatFeedback.ImpactCapacity * 8];
                var kills = new Matrix4x4[hits.Length];
                feedback.BuildSparks(sim, alpha, hits, kills, out int count, out int deaths);
                Assert.That(count, Is.EqualTo(5)); Assert.That(deaths, Is.Zero);
                Assert.That(Vector3.Distance(hits[0].GetColumn(3), shot.End), Is.LessThan(.00001f),
                    "Interpolation must not offset a fresh hit spark away from the resolved surface.");
                Assert.That(sim.Shots, Is.EqualTo(1));
                sim.Step(Vector3.zero, .12f);
                ray = new Ray(sim.Position(0) + Vector3.up * 1.1f - Vector3.forward * 3, Vector3.forward);
                Assert.That(sim.TryFire(ray, out ShotResult death), Is.True);
                Assert.That(death.Killed, Is.True);
                feedback.RecordShot(sim, death);
                feedback.BuildSparks(sim, .5f, hits, kills, out count, out deaths);
                Assert.That(count, Is.Zero, "No stale wound may render on a respawn before the next feedback-advance call.");
                Assert.That(deaths, Is.EqualTo(8));
                Assert.That(Vector3.Distance(kills[0].GetColumn(3), death.End), Is.LessThan(.00001f));
            }
        }

        [TestCase(8)] [TestCase(30)] [TestCase(60)] [TestCase(144)] [TestCase(240)]
        public void FrameBatchedShotsMatchFixedTickCadenceIncludingCatchup(int fps)
        {
            using (var combat = new CombatSimulation())
            using (var reference = new CombatSimulation())
            {
                combat.Reset(0, 137, false); reference.Reset(0, 137, false);
                double remainder = 0;
                var ray = new Ray(Vector3.up * 1.65f, Vector3.forward);
                for (int frame = 0; frame < fps * 10; frame++)
                {
                    remainder += 1d / fps;
                    bool stepped = false;
                    while (remainder + 1e-7 >= CombatSimulation.Tick)
                    {
                        combat.Step(Vector3.right, CombatSimulation.Tick);
                        reference.Step(Vector3.right, CombatSimulation.Tick);
                        reference.TryFire(ray, out ShotResult _);
                        remainder -= CombatSimulation.Tick; stepped = true;
                    }
                    if (stepped) while (combat.TryFire(ray, out ShotResult _)) { }
                    Assert.That(combat.Shots, Is.EqualTo(reference.Shots), "A render frame cannot change accumulated cadence debt.");
                    Assert.That(combat.Player, Is.EqualTo(reference.Player));
                }
                Assert.That(combat.Shots, Is.EqualTo(84));
                Assert.That(combat.Elapsed, Is.EqualTo(10).Within(.02));
            }
        }
    }
}
