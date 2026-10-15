using System;
using System.Collections.Generic;
using GameDevHarness.FantasyTrialB;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChaseWeaponArtITests
    {
        GameObject camera;
        ArtPalette palette;
        readonly List<ChaseGunRig> guns = new List<ChaseGunRig>();
        ChaseArmsArt arms;

        [SetUp]
        public void SetUp()
        {
            camera = new GameObject("I weapon art test camera");
            camera.transform.SetPositionAndRotation(new Vector3(7,3,-9),Quaternion.Euler(11,32,0));
            palette = new ArtPalette(Shader.Find("Standard"));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var gun in guns) gun.Dispose();
            guns.Clear();
            arms?.Dispose(); arms = null;
            Object.DestroyImmediate(camera);
            palette.Dispose();
        }

        ChaseGunRig Create(ChaseWeaponKind kind, int slot = 0)
        {
            var gun = ChaseGunRig.Create(camera.transform,palette,kind,guns.Count+1,slot);
            guns.Add(gun); return gun;
        }

        static IEnumerable<ChaseWeaponKind> Kinds => (ChaseWeaponKind[])Enum.GetValues(typeof(ChaseWeaponKind));

        [TestCaseSource(nameof(Kinds))]
        public void EveryEngineeredAssemblyHasFiniteMappedGeometryAndVisibleAction(ChaseWeaponKind kind)
        {
            var gun = Create(kind);
            Assert.That(gun.Root.parent,Is.SameAs(camera.transform));
            Assert.That(gun.Muzzle.parent,Is.SameAs(gun.Root));
            Assert.That(gun.Root.GetComponentsInChildren<Collider>(),Is.Empty);
            var filters = gun.Root.GetComponentsInChildren<MeshFilter>();
            Assert.That(filters.Length,Is.InRange(4,20));
            foreach (var filter in filters) Validate(filter);
            Transform action = gun.Root.GetChild(1);
            Assert.That(action.GetComponentsInChildren<MeshFilter>().Length,Is.GreaterThan(0));
            // The exposed action is forward of the rear grip and above its center.
            Assert.That(action.localPosition.z,Is.GreaterThan(.15f));
            Assert.That(action.localPosition.y,Is.GreaterThan(0));
            Assert.That(gun.Muzzle.localPosition.z,Is.GreaterThan(.5f));
        }

        [TestCaseSource(nameof(Kinds))]
        public void FireReloadAndPauseMoveTransformsWithoutReplacingMeshes(ChaseWeaponKind kind)
        {
            var gun = Create(kind);
            var filters = gun.Root.GetComponentsInChildren<MeshFilter>();
            var meshes = new Mesh[filters.Length];
            for(int i=0;i<filters.Length;i++)meshes[i]=filters[i].sharedMesh;
            Transform action=gun.Root.GetChild(1),feed=gun.Root.GetChild(2);
            Vector3 rest=action.localPosition;
            gun.Fire();gun.Animate(.02f,1,0,camera.transform.TransformPoint(Vector3.forward*25));
            Assert.That(action.localPosition!=rest || action.localRotation!=Quaternion.identity ||
                action.localScale!=Vector3.one,Is.True);
            Vector3 position=gun.Root.localPosition;
            Quaternion rotation=action.localRotation;
            gun.Animate(0,100,1,Vector3.zero);
            Assert.That(gun.Root.localPosition,Is.EqualTo(position));
            Assert.That(action.localRotation,Is.EqualTo(rotation));
            Vector3 feedRest=feed.localPosition;
            gun.Reload(.5f);
            Assert.That(feed.localPosition.y,Is.LessThan(feedRest.y-.1f));
            gun.Reload(-1);
            Assert.That(feed.localPosition,Is.EqualTo(feedRest));
            gun.Animate(1,2,0,camera.transform.TransformPoint(Vector3.forward*25));
            for(int i=0;i<filters.Length;i++)Assert.That(filters[i].sharedMesh,Is.SameAs(meshes[i]));
            Assert.That(action.localPosition,Is.EqualTo(rest));
        }

        [Test]
        public void SixCopiesHaveDistinctCameraBandsAndIndependentActions()
        {
            var cameraComponent=camera.AddComponent<Camera>();
            cameraComponent.fieldOfView=60;cameraComponent.aspect=16f/9;
            var projected=new List<Vector3>();
            for(int slot=0;slot<6;slot++)
            {
                var gun=Create(ChaseWeaponKind.Stormsplit,slot);
                Vector3 center=gun.Root.TransformPoint(new Vector3(0,.055f,.2f));
                Vector3 screen=cameraComponent.WorldToViewportPoint(center);
                Assert.That(screen.x,Is.InRange(.02f,.98f));
                Assert.That(screen.y,Is.InRange(.02f,.98f));
                foreach(Vector3 previous in projected)
                    Assert.That(Vector2.Distance(screen,previous),Is.GreaterThan(.075f));
                projected.Add(screen);
            }
            guns[0].Fire();guns[0].Reload(.5f);
            Assert.That(guns[1].Root.GetChild(2).localPosition,Is.EqualTo(new Vector3(0,-.025f,.13f)));
            Assert.That(guns[0].Root.GetChild(2).localPosition,Is.Not.EqualTo(guns[1].Root.GetChild(2).localPosition));
        }

        [Test]
        public void HandsAreBatchedBelowSightsAndPoseIsAbsolute()
        {
            arms=ChaseArmsArt.Create(camera.transform,palette);
            var filters=arms.Root.GetComponentsInChildren<MeshFilter>();
            Assert.That(filters.Length,Is.EqualTo(4));
            foreach(var filter in filters)
            {
                Validate(filter);
                foreach(Vector3 p in filter.sharedMesh.vertices)Assert.That(p.y,Is.LessThan(-.18f));
            }
            arms.Animate(3,.8f);
            Vector3 pose=arms.Root.localPosition;
            arms.Animate(12,.2f);arms.Animate(3,.8f);
            Assert.That(arms.Root.localPosition,Is.EqualTo(pose));
            arms.Dispose();arms.Dispose();
            Assert.That(arms.Root,Is.Null);
        }

        static void Validate(MeshFilter filter)
        {
            Mesh mesh=filter.sharedMesh;
            Assert.That(mesh.vertexCount,Is.GreaterThan(0));
            Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
            Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount));
            Assert.That(mesh.tangents.Length,Is.EqualTo(mesh.vertexCount));
            Assert.That(filter.gameObject.layer,Is.EqualTo(13));
            Assert.That(filter.GetComponent<MeshRenderer>().shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));
            foreach(Vector3 p in mesh.vertices)
                Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||
                    float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z),Is.False);
            Vector3[] vertices=mesh.vertices;
            int[] indices=mesh.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                for(int j=0;j<3;j++)Assert.That(indices[i+j],Is.InRange(0,vertices.Length-1));
                Assert.That(Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],
                    vertices[indices[i+2]]-vertices[indices[i]]).sqrMagnitude,Is.GreaterThan(1e-16f));
            }
        }
    }
}
