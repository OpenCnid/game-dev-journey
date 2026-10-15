using System.Collections.Generic;
using System.Linq;
using GameDevHarness.FantasyTrialB;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChaseEnvironmentITests
    {
        [Test]
        public void PointedVoussoirsLeaveAnActualOpeningAndHaveBoundedCost()
        {
            var kit = new RebornEnvironmentGeometry();
            kit.PointedArch(Vector3.zero, 8, 5.5f, .75f, 1.2f, Quaternion.identity, 14);
            Mesh mesh = kit.Mesh("I arch fixture");
            var go = new GameObject("I arch fixture") { layer = 8 };
            MeshCollider collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            try
            {
                Physics.SyncTransforms();
                Assert.That(collider.Raycast(new Ray(new Vector3(0, 8, -3), Vector3.forward),
                    out _, 6), Is.False);
                Assert.That(collider.Raycast(new Ray(new Vector3(-8.4f, 5.55f, -3), Vector3.forward),
                    out _, 6), Is.True);
                Assert.That(mesh.bounds.max.y, Is.GreaterThan(19));
                Assert.That(mesh.triangles.Length / 3, Is.EqualTo(14 * 2 * 12));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void OrbitalRimHasAnOpenCenterAndADeepSolidLip()
        {
            var kit = new RebornEnvironmentGeometry();
            kit.Socket(Vector3.zero, 1, .8f, .2f, .5f, Quaternion.identity);
            Mesh mesh = kit.Mesh("I socket fixture");
            var go = new GameObject("I socket fixture");
            MeshCollider collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            try
            {
                Physics.SyncTransforms();
                Assert.That(collider.Raycast(new Ray(new Vector3(0, 0, 2), Vector3.back), out _, 4), Is.False);
                Assert.That(collider.Raycast(new Ray(new Vector3(1.1f, 0, 2), Vector3.back), out _, 4), Is.True);
                Assert.That(mesh.bounds.size.z, Is.GreaterThanOrEqualTo(.5f));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void AshFootingIsDeterministicAndInsideTheLiveJumpEnvelope()
        {
            var a = new RebornEnvironmentGeometry(); var b = new RebornEnvironmentGeometry();
            ChaseDebris.AshMound(a, Vector3.zero, 2.8f, 1.4f, 3);
            ChaseDebris.AshMound(b, Vector3.zero, 2.8f, 1.4f, 3);
            Mesh first = a.Mesh("I ash A"), second = b.Mesh("I ash B");
            try
            {
                Assert.That(second.vertices, Is.EqualTo(first.vertices));
                Assert.That(second.triangles, Is.EqualTo(first.triangles));
                Assert.That(first.bounds.max.y, Is.EqualTo(1.4f).Within(.001f));
                Assert.That(first.triangles.Length / 3, Is.EqualTo(108));
            }
            finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
        }

        [Test]
        public void ArenaSourcesMatchStaticPhysicsAndEveryTierHasTwoReachableApproaches()
        {
            var root = new GameObject("I environment integration fixture");
            var palette = new ArtPalette(Shader.Find("Standard"));
            ChaseArena arena = null;
            NavMeshData data = null;
            NavMeshDataInstance instance = default;
            try
            {
                arena = ChaseArena.Create(root.transform, palette, 47);
                Assert.That(arena.Portals.Length, Is.EqualTo(6));
                Assert.That(RenderSettings.customReflectionTexture, Is.TypeOf<Cubemap>());
                Assert.That(RenderSettings.customReflectionTexture.width, Is.EqualTo(128));
                Assert.That(RenderSettings.reflectionIntensity, Is.GreaterThan(0));
                Assert.That(arena.LevelHeights, Is.EqualTo(new[] { 0f, 2.5f, 5f }));
                // Static ownership counters include the player's already-built
                // arena. Measure this fixture's actual geometry, not both worlds.
                Assert.That(arena.Root.GetComponentsInChildren<MeshFilter>()
                    .Sum(filter => filter.sharedMesh.triangles.Length / 3),
                    Is.LessThan(180000));
                Assert.That(arena.Root.GetComponentsInChildren<MeshFilter>().Length, Is.LessThan(130));
                foreach (MeshCollider collider in arena.Root.GetComponentsInChildren<MeshCollider>())
                    Assert.That(arena.Sources.Exists(s => s.sourceObject == collider.sharedMesh &&
                        s.transform == collider.transform.localToWorldMatrix), Is.True);
                foreach (BoxCollider collider in arena.Root.GetComponentsInChildren<BoxCollider>())
                    Assert.That(arena.Sources.Exists(s => s.shape == NavMeshBuildSourceShape.Box &&
                        s.size == collider.size && s.transform == collider.transform.localToWorldMatrix), Is.True);

                var settings = NavMesh.GetSettingsByIndex(0);
                settings.agentRadius = .55f; settings.agentHeight = 2.4f; settings.agentClimb = .3f;
                settings.overrideVoxelSize = true; settings.voxelSize = .1f;
                data = NavMeshBuilder.BuildNavMeshData(settings, arena.Sources, arena.Bounds,
                    Vector3.zero, Quaternion.identity);
                Assert.That(data, Is.Not.Null);
                instance = NavMesh.AddNavMeshData(data);
                var path = new NavMeshPath();
                for (int landmark = 0; landmark < arena.ClimbPoints.Length; landmark++)
                    for (int approach = 0; approach < 2; approach++)
                    {
                        Assert.That(arena.TryGetClimbApproach(landmark, approach, out var from), Is.True,
                            "landmark=" + landmark + " approach=" + approach);
                        Assert.That(NavMesh.SamplePosition(arena.ClimbPoints[landmark], out var top,
                            .25f, NavMesh.AllAreas), Is.True);
                        Assert.That(top.position.y, Is.EqualTo(arena.ClimbPoints[landmark].y).Within(.15f));
                        Assert.That(NavMesh.CalculatePath(from, top.position, NavMesh.AllAreas, path), Is.True);
                        Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
                    }
                foreach (Vector3[] slots in arena.SpawnSlots)
                    foreach (Vector3 slot in slots)
                    {
                        Assert.That(NavMesh.SamplePosition(slot, out var spawn, .25f, NavMesh.AllAreas), Is.True);
                        Assert.That(spawn.position.y, Is.EqualTo(0).Within(.15f));
                        Assert.That(NavMesh.CalculatePath(spawn.position, arena.PlayerStart,
                            NavMesh.AllAreas, path), Is.True);
                        Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
                    }
                foreach (Vector3[] circuit in new[] { arena.OuterRoute, arena.KiteRoute, arena.FigureEightRoute })
                    for (int i = 1; i < circuit.Length; i++)
                    {
                        Assert.That(NavMesh.CalculatePath(circuit[i - 1], circuit[i], NavMesh.AllAreas, path), Is.True);
                        Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
                    }
                Vector3 vent = new Vector3(-10, 0, 2);
                Assert.That(arena.HazardDamageAt(vent, 1.99), Is.Zero);
                Assert.That(arena.HazardDamageAt(vent, 2), Is.EqualTo(8));
                Assert.That(arena.HazardDamageAt(vent, 4), Is.Zero);
                Assert.That(arena.HazardDamageAt(vent + Vector3.up, 3), Is.Zero);
                Assert.That(arena.HazardDamageAt(new Vector3(10, 0, 2), 10), Is.EqualTo(8));
                arena.Telegraph(0, 1); arena.Animate(1.5, 2);
                arena.SetQuality(0);
                foreach (Collider collider in arena.Root.GetComponentsInChildren<Collider>())
                    Assert.That(collider.enabled && collider.gameObject.activeInHierarchy, Is.True);
                arena.SetQuality(2);
            }
            finally
            {
                if (instance.valid) instance.Remove();
                if (data != null) Object.DestroyImmediate(data);
                Object.DestroyImmediate(root);
                arena?.Dispose(); ChaseDebris.Dispose(); ChaseScenery.Dispose();
                palette.Dispose();
            }
        }
    }
}
