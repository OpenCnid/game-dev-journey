using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class ExperimentTests
    {
        [TestCase(100, false, 720)]
        [TestCase(20000, true, 190)]
        public void NativeOwnershipPreservesCombatProjectilesInterpolationAndReset(int count, bool synchronized, int ticks)
        {
            using (var staged = new CombatSimulation { UseBurst = true })
            using (var native = new CombatSimulation { UseBurst = true, UseNativeState = true })
            {
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    staged.Reset(count, 137, true, bosses: true, synchronizedAbilities: synchronized);
                    native.Reset(count, 137, true, bosses: true, synchronizedAbilities: synchronized);
                    for (int tick = 0; tick < ticks; tick++)
                    {
                        staged.Step(Vector3.zero, CombatSimulation.Tick); native.Step(Vector3.zero, CombatSimulation.Tick);
                        var ray = new Ray(staged.Player + Vector3.up * 1.65f, (-staged.Player).normalized);
                        Assert.That(native.TryFire(ray, out Vector3 nativeEnd, out bool nativeKill),
                            Is.EqualTo(staged.TryFire(ray, out Vector3 stagedEnd, out bool stagedKill)));
                        Assert.That(nativeKill, Is.EqualTo(stagedKill));
                        Assert.That(Vector3.Distance(nativeEnd, stagedEnd), Is.LessThan(.0001f));
                        Assert.That(native.NeighborChecks, Is.EqualTo(staged.NeighborChecks));
                        Assert.That(native.Abilities.Activations, Is.EqualTo(staged.Abilities.Activations));
                        Assert.That(native.Abilities.Projectiles, Is.EqualTo(staged.Abilities.Projectiles));
                        Assert.That(native.Abilities.PlayerHits, Is.EqualTo(staged.Abilities.PlayerHits));
                        Assert.That(native.Abilities.Telegraphs, Is.EqualTo(staged.Abilities.Telegraphs));
                        Assert.That(native.Abilities.Spawned, Is.EqualTo(staged.Abilities.Spawned));
                    }
                    Assert.That(native.Abilities.Dropped, Is.Zero);
                    Assert.That(native.Abilities.ProjectileUpdates, Is.EqualTo(staged.Abilities.ProjectileUpdates));
                    for (int i = 0; i < count; i++)
                        Assert.That(Vector3.Distance(native.RenderPosition(i, .3f), staged.RenderPosition(i, .3f)), Is.LessThan(.0001f));
                    for (int i = 0; i < native.Abilities.Projectiles; i++)
                        Assert.That(Vector3.Distance(native.Abilities.RenderProjectile(i, .3f), staged.Abilities.RenderProjectile(i, .3f)), Is.LessThan(.0001f));
                }
                native.UseNativeState = false; native.Reset(0, 137, true, bosses: true);
                native.Step(Vector3.zero, CombatSimulation.Tick);
                Assert.That(native.NativeState, Is.False); Assert.That(native.Abilities.Projectiles, Is.Zero);
            }
        }

        [Test]
        public void WarningDeduplicationPreservesDistinctTargetsAndDeadlines()
        {
            var collector = new WarningCollector(); collector.Begin();
            for (int i = 0; i < 10000; i++) collector.Add(Vector3.zero, .5f, true);
            collector.Add(Vector3.zero, .49f, true);
            collector.Add(new Vector3(.000001f, 0, 0), .5f, true);
            Assert.That(collector.Candidates, Is.EqualTo(10002)); Assert.That(collector.Count, Is.EqualTo(3));
            Assert.That(collector.Positions[2].x, Is.EqualTo(.000001f));
            collector.Begin(); collector.Add(Vector3.zero, .5f, false); collector.Add(Vector3.zero, .5f, false);
            Assert.That(collector.Count, Is.EqualTo(2));
        }

        [Test]
        public void ConcurrentPreparationMatchesSequentialAndCannotReusePendingBuffers()
        {
            var go = new GameObject("Experiment camera");
            try
            {
                var camera = go.AddComponent<Camera>();
                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                var points = new[] { new Vector3(0, 0, 10), new Vector3(0, 0, -10), new Vector3(1, 0, 5) };
                using (var a = new BurstInstances())
                using (var b = new BurstInstances())
                using (var reference = new BurstInstances())
                {
                    int count = reference.Prepare(points, points, 3, .5f, planes, Vector3.zero, Vector3.one,
                        Vector3.up, Vector3.one, Vector3.zero, 1, true, 128);
                    a.Schedule(points, points, 3, .5f, planes, Vector3.zero, Vector3.one, Vector3.up, Vector3.one, Vector3.zero, 1, true, 128);
                    b.Schedule(points, points, 3, .5f, planes, Vector3.zero, Vector3.one, Vector3.up, Vector3.one, Vector3.zero, 1, true, 128);
                    Assert.Throws<System.InvalidOperationException>(() => a.Schedule(points, points, 3, .5f, planes,
                        Vector3.zero, Vector3.one, Vector3.up, Vector3.one, Vector3.zero, 1, true, 128));
                    Assert.That(a.Complete(), Is.EqualTo(count)); Assert.That(b.Complete(), Is.EqualTo(count));
                    for (int i = 0; i < count; i++) { Assert.That(a.A[i], Is.EqualTo(reference.A[i])); Assert.That(b.B[i], Is.EqualTo(reference.B[i])); }
                    a.Schedule(points, points, 0, 1, planes, Vector3.zero, Vector3.one, Vector3.zero, Vector3.one, Vector3.zero, 1, false, 128);
                    Assert.That(a.Complete(), Is.Zero);
                }
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
