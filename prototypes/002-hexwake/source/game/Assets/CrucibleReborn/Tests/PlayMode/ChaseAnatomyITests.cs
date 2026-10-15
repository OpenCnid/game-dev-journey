using System.Collections.Generic;
using System.Reflection;
using GameDevHarness.FantasyTrialB;
using NUnit.Framework;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChaseAnatomyITests
    {
        GameObject root;
        ArtPalette palette;
        ChaseSwarm swarm;
        ChaseEnemyRig[] templates;

        [OneTimeSetUp]
        public void BuildCachedAnatomy()
        {
            root = new GameObject("I anatomy tests");
            palette = new ArtPalette(Shader.Find("Standard"));
            swarm = ChaseSwarm.Create(root.transform, palette);
            templates = (ChaseEnemyRig[])typeof(ChaseSwarm).GetField("templates",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(swarm);
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            swarm.Dispose();
            Object.DestroyImmediate(root);
            palette.Dispose();
        }

        static Mesh LowMesh(ChaseEnemyRig rig)
        {
            object anatomy = typeof(ChaseEnemyRig).GetField("Anatomy",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rig);
            return (Mesh)anatomy.GetType().GetField("Low",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(anatomy);
        }

        [Test]
        public void BothResolutionsAreConnectedWeightedCompleteBodies()
        {
            foreach (var rig in templates)
            {
                Mesh detailed = rig.Root.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                Mesh low = LowMesh(rig);
                Assert.That(low.vertexCount, Is.LessThan(detailed.vertexCount));
                foreach (var mesh in new[] { detailed, low })
                {
                    Assert.That(mesh.vertexCount, Is.InRange(1000, 80000));
                    Assert.That(mesh.triangles.Length / 3, Is.LessThan(160000));
                    Assert.That(mesh.subMeshCount, Is.EqualTo(3));
                    Assert.That(mesh.boneWeights.Length, Is.EqualTo(mesh.vertexCount));
                    Assert.That(mesh.bindposes.Length, Is.EqualTo(6));
                    Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount));
                    Assert.That(mesh.tangents.Length, Is.EqualTo(mesh.vertexCount));
                    var points = mesh.vertices;
                    var weights = mesh.boneWeights;
                    bool leftHand = false, rightHand = false, leftFoot = false, rightFoot = false, head = false;
                    for (int i = 0; i < points.Length; i++)
                    {
                        var p = points[i];
                        Assert.That(float.IsNaN(p.x) || float.IsInfinity(p.x) ||
                            float.IsNaN(p.y) || float.IsInfinity(p.y) ||
                            float.IsNaN(p.z) || float.IsInfinity(p.z), Is.False);
                        Assert.That(weights[i].weight0 + weights[i].weight1, Is.EqualTo(1).Within(.00001f));
                        Assert.That(weights[i].boneIndex0, Is.InRange(0, 5));
                        leftHand |= p.x < -.49f && p.y < .96f && p.y > .78f;
                        rightHand |= p.x > .49f && p.y < .96f && p.y > .78f;
                        leftFoot |= p.x < -.12f && p.y < .10f;
                        rightFoot |= p.x > .12f && p.y < .10f;
                        head |= (p - ChaseEnemyRig.LocalHeadCenter(rig.Kind)).sqrMagnitude < .045f;
                    }
                    Assert.That(leftHand && rightHand && leftFoot && rightFoot && head, Is.True, mesh.name);
                    AssertConnectedClosed(mesh);
                }
            }
        }

        static int Find(int[] parents, int index)
        {
            while (parents[index] != index)
            { parents[index] = parents[parents[index]]; index = parents[index]; }
            return index;
        }

        static void AssertConnectedClosed(Mesh mesh)
        {
            var parents = new int[mesh.vertexCount];
            for (int i = 0; i < parents.Length; i++) parents[i] = i;
            var edges = new Dictionary<ulong, int>();
            int[] triangles = mesh.triangles;
            for (int t = 0; t < triangles.Length; t += 3)
                for (int side = 0; side < 3; side++)
                {
                    int a = triangles[t + side], b = triangles[t + (side + 1) % 3];
                    Assert.That(a, Is.Not.EqualTo(b));
                    parents[Find(parents, a)] = Find(parents, b);
                    ulong key = ((ulong)(uint)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);
                    edges.TryGetValue(key, out int count);
                    edges[key] = count + 1;
                }
            int component = Find(parents, 0);
            var components = new Dictionary<int, Bounds>();
            var sizes = new Dictionary<int, int>();
            var points = mesh.vertices;
            for (int i = 0; i < parents.Length; i++)
            {
                int root = Find(parents, i);
                if (!components.TryGetValue(root, out var bounds)) bounds = new Bounds(points[i], Vector3.zero);
                bounds.Encapsulate(points[i]); components[root] = bounds;
                sizes.TryGetValue(root, out int size); sizes[root] = size + 1;
            }
            if (components.Count > 1)
                foreach (var group in components)
                    Debug.Log("REBORN_TOPOLOGY " + mesh.name + " vertices=" + sizes[group.Key] +
                        " bounds=" + group.Value);
            for (int i = 1; i < parents.Length; i++)
                Assert.That(Find(parents, i), Is.EqualTo(component), mesh.name + " detached anatomy");
            foreach (var count in edges.Values)
                Assert.That(count, Is.EqualTo(2), mesh.name + " open or nonmanifold edge");
        }

        [Test]
        public void SpawnLocalHeadContractTracksVisibleAnimatedHeadAtFrozenScale()
        {
            foreach (var rig in templates)
            {
                var skin = rig.Root.GetComponent<SkinnedMeshRenderer>();
                Mesh original = skin.sharedMesh;
                rig.Root.SetPositionAndRotation(new Vector3(7, 3, -9), Quaternion.Euler(0, 53, 0));
                rig.Root.localScale = Vector3.one * 1.7f;
                try
                {
                    var local = ChaseEnemyRig.LocalHeadCenter(rig.Kind);
                    float radius = ChaseEnemyRig.LocalHeadRadius(rig.Kind);
                    Assert.That(local.y, Is.InRange(1.9f, 2.1f));
                    Assert.That(radius, Is.InRange(.15f, .20f));
                    for (int phase = 0; phase < 8; phase++)
                    {
                        rig.Animate(phase * Mathf.PI / (4 * 6.6f), 3, 0, false, false);
                        var expected = skin.bones[1].TransformPoint(local - new Vector3(0, 1.84f, 0));
                        Assert.That(Vector3.Distance(expected, rig.WorldHeadCenter), Is.LessThan(.00001f));
                        Assert.That(Vector3.Distance(rig.Root.TransformPoint(local), rig.WorldHeadCenter),
                            Is.LessThan(.027f * 1.7f));
                        Assert.That(rig.WorldHeadRadius, Is.EqualTo(radius * 1.7f).Within(.00001f));
                        Assert.That(skin.sharedMesh, Is.SameAs(original));
                    }
                    rig.Animate(1, 3, 0, true, false);
                    Assert.That(skin.sharedMaterials[0], Is.SameAs(skin.sharedMaterials[2]));
                }
                finally
                {
                    rig.Root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    rig.Root.localScale = Vector3.one;
                    rig.Animate(0, 0, 0, false, false);
                }
            }
        }

        [Test]
        public void AllActorsRemainPresentedAcrossLodsQualityFlashAndBoundStates()
        {
            var poses = new List<ChasePose>();
            for (int i = 0; i < 1201; i++)
                poses.Add(new ChasePose { Id = i + 1, Kind = (ChaseEnemyKind)(i % 3),
                    Position = new Vector3(i % 40 * 1.5f, 0, i / 40 * 1.5f),
                    Rotation = Quaternion.identity, SizeScale = 1.4f, Speed = 3,
                    FlashUntil = i % 7 == 0 ? 2 : 0, Bound = i % 11 == 0 });
            try
            {
                for (int quality = 0; quality < 3; quality++)
                {
                    swarm.SetQuality(quality);
                    swarm.Render(poses, Vector3.zero, 1);
                    Assert.That(swarm.DetailedCount, Is.EqualTo(swarm.DetailedLimit));
                    Assert.That(swarm.DetailedCount + swarm.InstancedCount, Is.EqualTo(poses.Count));
                    Assert.That(swarm.InstancedDrawCount, Is.GreaterThan(0));
                    Assert.That(swarm.RigidInstancedCount, Is.GreaterThan(0));
                    Assert.That(swarm.FlashingCount, Is.GreaterThan(0));
                    Assert.That(swarm.BoundCount, Is.GreaterThan(0));
                    var cached = new HashSet<Mesh>();
                    foreach (var template in templates)
                        cached.Add(template.Root.GetComponent<SkinnedMeshRenderer>().sharedMesh);
                    foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                        Assert.That(cached.Contains(skin.sharedMesh), Is.True);
                }
            }
            finally { swarm.Render(new List<ChasePose>(), Vector3.zero, 1); }
        }
    }
}
