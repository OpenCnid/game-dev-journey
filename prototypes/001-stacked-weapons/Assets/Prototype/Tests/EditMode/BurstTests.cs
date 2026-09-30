using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class BurstTests
    {
        [Test]
        public void JobsActuallyUseBurst()
        {
            BurstStatus.Verify(); Assert.That(BurstStatus.Compiled, Is.True);
        }

        [TestCase(64)] [TestCase(128)] [TestCase(256)]
        public void DenseCrowdJobMatchesReferenceAndDoesNotSkipNeighbors(int batch)
        {
            using (var managed = new CombatSimulation())
            using (var burst = new CombatSimulation { UseBurst = true, BatchSize = batch })
            {
                managed.Reset(20000, 137, true); burst.Reset(20000, 137, true);
                managed.Step(Vector3.zero, CombatSimulation.Tick); burst.Step(Vector3.zero, CombatSimulation.Tick);
                Assert.That(burst.NeighborChecks, Is.EqualTo(managed.NeighborChecks));
                for (int i = 0; i < 20000; i++)
                    Assert.That(Vector3.Distance(burst.Position(i), managed.Position(i)), Is.LessThan(.0001f));
            }
        }

        [Test]
        public void BossJobsPreserveActivationsProjectileOrderCollisionAndReset()
        {
            using (var managed = new CombatSimulation())
            using (var burst = new CombatSimulation { UseBurst = true })
            {
                managed.Reset(100, 137, true, bosses: true); burst.Reset(100, 137, true, bosses: true);
                for (int tick = 0; tick < 720; tick++)
                {
                    managed.Step(Vector3.zero, CombatSimulation.Tick); burst.Step(Vector3.zero, CombatSimulation.Tick);
                    Assert.That(burst.Abilities.Activations, Is.EqualTo(managed.Abilities.Activations));
                    Assert.That(burst.Abilities.Projectiles, Is.EqualTo(managed.Abilities.Projectiles));
                    Assert.That(burst.Abilities.PlayerHits, Is.EqualTo(managed.Abilities.PlayerHits));
                }
                Assert.That(burst.Abilities.StateUpdates, Is.EqualTo(managed.Abilities.StateUpdates));
                Assert.That(burst.Abilities.ProjectileUpdates, Is.EqualTo(managed.Abilities.ProjectileUpdates));
                for (int i = 0; i < burst.Abilities.Projectiles; i++)
                    Assert.That(Vector3.Distance(burst.Abilities.RenderProjectile(i, 1), managed.Abilities.RenderProjectile(i, 1)), Is.LessThan(.005f));
                burst.Reset(100, 137, true, bosses: true);
                Assert.That(burst.Abilities.Projectiles, Is.Zero); Assert.That(burst.Abilities.Activations, Is.Zero);
                burst.Step(Vector3.zero, CombatSimulation.Tick); // Persistent buffers remain reusable after reset.
            }
        }

        [Test]
        public void CulledInstancesKeepVisibleAndFrustumEdgeGeometryInStableOrder()
        {
            var go = new GameObject("Culling test camera");
            try
            {
                var camera = go.AddComponent<Camera>(); camera.fieldOfView = 60; camera.aspect = 1;
                camera.nearClipPlane = .3f; camera.farClipPlane = 100;
                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                var points = new[] { new Vector3(0, 0, 10), new Vector3(0, 0, -10), new Vector3(6, 0, 10) };
                using (var instances = new BurstInstances())
                {
                    int count = instances.Prepare(points, points, 3, 1, planes, Vector3.zero, Vector3.one,
                        Vector3.up, Vector3.one, Vector3.zero, 1, true, 64);
                    Assert.That(count, Is.EqualTo(2));
                    Assert.That(instances.A[0].m03, Is.EqualTo(0)); Assert.That(instances.A[1].m03, Is.EqualTo(6));
                    Assert.That(instances.B[1].m13, Is.EqualTo(1));
                }
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
