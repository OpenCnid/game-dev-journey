using System.Collections.Generic;
using System.Linq;
using GameDevHarness.FantasyTrialB;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChaseLootArtJTests
    {
        GameObject world;
        ArtPalette palette;
        Material beam;
        ChaseLootArt art;
        readonly List<GameObject> drops = new List<GameObject>();
        static IEnumerable<int> Kinds => Enumerable.Range(0, 22);

        [SetUp]
        public void SetUp()
        {
            world = new GameObject("J floor art test");
            palette = new ArtPalette(Shader.Find("Standard"));
            beam = new Material(Shader.Find("Sprites/Default"));
            art = new ChaseLootArt(world.transform, palette, beam);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var drop in drops) Object.DestroyImmediate(drop);
            drops.Clear(); art.Dispose(); palette.Dispose();
            Object.DestroyImmediate(beam); Object.DestroyImmediate(world);
        }

        [TestCase(ChaseLootKind.Weapon)]
        [TestCase(ChaseLootKind.Relic)]
        public void NearbyCueIsHiddenByWallWithoutChangingCollectionSelection(ChaseLootKind kind)
        {
            var loot = new ChaseLoot { Kind = kind, Position = new Vector3(0, .05f, 1.7f) };
            var wall = new GameObject("J physical cue blocker", typeof(BoxCollider));
            wall.transform.SetParent(world.transform, false);
            wall.layer = 8; wall.transform.position = new Vector3(0, .8f, .9f);
            wall.transform.localScale = new Vector3(2, 2, .15f);
            var eye = new Vector3(0, 1.6f, 0);
            Physics.SyncTransforms();
            Assert.That(Vector3.Distance(Vector3.up * .3f, loot.Position), Is.LessThan(2.2f));
            Assert.That(ChaseLootArt.CueVisible(eye, loot), Is.False);
            wall.SetActive(false); Physics.SyncTransforms();
            Assert.That(ChaseLootArt.CueVisible(eye, loot), Is.True);
        }

        [TestCaseSource(nameof(Kinds))]
        public void EveryDropHasLargePhysicalWorldArtAndCachedMeshes(int kind)
        {
            var loot = new ChaseLoot { Id = 1, Position = new Vector3(2, .1f, 5),
                Kind = kind < 16 ? ChaseLootKind.Weapon : ChaseLootKind.Relic,
                Weapon = (ChaseWeaponKind)(kind % 16),
                Relic = (ChaseRelicKind)(kind < 16 ? 0 : kind - 16) };
            var first = art.Create(loot, world.transform); drops.Add(first);
            var second = art.Create(loot, world.transform); drops.Add(second);
            Assert.That(first.transform.position, Is.EqualTo(loot.Position));
            Assert.That(first.GetComponentsInChildren<Collider>(), Is.Empty);
            var a = first.GetComponentsInChildren<MeshFilter>();
            var b = second.GetComponentsInChildren<MeshFilter>();
            Assert.That(a.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(a.Select(f => f.sharedMesh), Is.EqualTo(b.Select(f => f.sharedMesh)));
            Bounds bounds = a[0].GetComponent<Renderer>().bounds;
            foreach (var part in a)
            {
                Assert.That(part.gameObject.layer, Is.EqualTo(0)); // Not the through-wall viewmodel layer.
                Assert.That(part.sharedMesh.vertexCount, Is.GreaterThan(0));
                bounds.Encapsulate(part.GetComponent<Renderer>().bounds);
            }
            Assert.That(bounds.size.x, Is.GreaterThan(1));
            Assert.That(bounds.size.y, Is.GreaterThan(.7f));
            var locator = first.GetComponentInChildren<LineRenderer>();
            Assert.That(locator.positionCount, Is.EqualTo(2));
            Assert.That(locator.GetPosition(1).y, Is.GreaterThan(2));
            Assert.That(locator.useWorldSpace, Is.False);
            Assert.That(locator.gameObject.layer, Is.EqualTo(0));
        }
    }
}
