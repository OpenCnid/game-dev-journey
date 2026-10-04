using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class PrecisionAimTests
    {
        static void Place(CombatSimulation sim, int index, Vector3 p)
        {
            // Controlled geometry without adding a production teleport/test API.
            if (sim.NativeState)
            {
                var current = (Unity.Collections.NativeArray<Vector3>)typeof(CombatSimulation).GetField("NativePositions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sim);
                var previous = (Unity.Collections.NativeArray<Vector3>)typeof(CombatSimulation).GetField("NativePrevious", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sim);
                current[index] = previous[index] = p;
            }
            else
            {
                ((Vector3[])typeof(CombatSimulation).GetField("positions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sim))[index] = p;
                ((Vector3[])typeof(CombatSimulation).GetField("previous", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sim))[index] = p;
            }
        }
        static Ray BodyRay(CombatSimulation sim) => new Ray(sim.Position(0) + Vector3.up * .56f - Vector3.forward * 3, Vector3.forward);
        static Ray HeadRay(CombatSimulation sim) => new Ray(EnemyGeometry.Head(sim.Position(0), sim.EnemyHealth(0) < 40).MultiplyPoint3x4(Vector3.zero) - Vector3.forward * 3, Vector3.forward);

        [TestCase(false, false)] [TestCase(true, false)] [TestCase(true, true)]
        public void FreshCriticalAndWoundedCriticalCaptureActualDamageBeforeSingleRespawn(bool burst, bool native)
        {
            using (var sim = new CombatSimulation { UseBurst = burst, UseNativeState = native })
            {
                sim.Reset(1, 137, false); sim.Step(Vector3.zero, CombatSimulation.Tick);
                int version = sim.EnemySpawnVersion(0);
                AimResult preview = sim.QueryAim(HeadRay(sim));
                Assert.That(preview.Region, Is.EqualTo(HitRegion.Head)); Assert.That(sim.Shots, Is.Zero);
                Assert.That(sim.TryFire(HeadRay(sim), out ShotResult critical), Is.True);
                Assert.That(critical.Critical && critical.Killed, Is.True);
                Assert.That(critical.RequestedDamage, Is.EqualTo(40)); Assert.That(critical.Damage, Is.EqualTo(40));
                Assert.That(critical.HealthBefore, Is.EqualTo(40)); Assert.That(critical.HealthAfter, Is.Zero);
                Assert.That(critical.SpawnVersion, Is.EqualTo(version)); Assert.That(sim.Kills, Is.EqualTo(1));
                Assert.That(sim.EnemySpawnVersion(0), Is.EqualTo(version + 1)); Assert.That(sim.EnemyHealth(0), Is.EqualTo(40));
                Assert.That(sim.TryFire(HeadRay(sim), out ShotResult blocked), Is.False); Assert.That(blocked.Hit, Is.False);
                Assert.That(sim.QueryAim(HeadRay(sim)).Hit, Is.True, "Cooldown does not disable preview.");
                sim.Step(Vector3.zero, .12f);
                Assert.That(sim.TryFire(BodyRay(sim), out ShotResult wound), Is.True);
                Assert.That(wound.Region, Is.EqualTo(HitRegion.Body)); Assert.That(wound.Damage, Is.EqualTo(20));
                Assert.That(wound.HealthBefore, Is.EqualTo(40)); Assert.That(wound.HealthAfter, Is.EqualTo(20));
                sim.Step(Vector3.zero, .12f); version = sim.EnemySpawnVersion(0);
                Assert.That(sim.TryFire(HeadRay(sim), out ShotResult woundedCritical), Is.True);
                Assert.That(woundedCritical.Critical && woundedCritical.Killed, Is.True);
                Assert.That(woundedCritical.RequestedDamage, Is.EqualTo(40)); Assert.That(woundedCritical.Damage, Is.EqualTo(20));
                Assert.That(woundedCritical.HealthBefore, Is.EqualTo(20)); Assert.That(woundedCritical.HealthAfter, Is.Zero);
                Assert.That(woundedCritical.SpawnVersion, Is.EqualTo(version)); Assert.That(sim.Kills, Is.EqualTo(2));
            }
        }

        [TestCase(false, HitRegion.Body)] [TestCase(false, HitRegion.Head)]
        [TestCase(true, HitRegion.Body)] [TestCase(true, HitRegion.Head)]
        public void RegionEdgesAgreeWithRenderedCubesAndRejectOutsideSilhouette(bool wounded, HitRegion region)
        {
            using (var sim = new CombatSimulation())
            {
                sim.Reset(1, 137, false); Place(sim, 0, Vector3.zero);
                if (wounded) sim.TryFire(BodyRay(sim), out ShotResult _);
                Matrix4x4 pose = region == HitRegion.Head ? EnemyGeometry.Head(Vector3.zero, wounded) : EnemyGeometry.Body(Vector3.zero, wounded);
                foreach (float x in new[] { -.4995f, .4995f })
                {
                    Vector3 point = pose.MultiplyPoint3x4(new Vector3(x, 0, -.5f));
                    Ray ray = new Ray(point - Vector3.forward * 3, Vector3.forward);
                    AimResult aim = sim.QueryAim(ray);
                    Assert.That(aim.Region, Is.EqualTo(region)); Assert.That(aim.Distance, Is.EqualTo(3).Within(.00001f));
                }
                foreach (float x in new[] { -.501f, .501f })
                {
                    Vector3 point = pose.MultiplyPoint3x4(new Vector3(x, 0, -.5f));
                    Assert.That(sim.QueryAim(new Ray(point - Vector3.forward * 3, Vector3.forward)).Hit, Is.False);
                }
                // A side view also exercises wounded shear, rather than only the frontal extent.
                Vector3 surface = pose.MultiplyPoint3x4(new Vector3(-.5f, .3f, 0));
                AimResult side = sim.QueryAim(new Ray(surface - Vector3.right * 3, Vector3.right));
                Assert.That(side.Region, Is.EqualTo(region)); Assert.That(side.Distance, Is.EqualTo(3).Within(.0001f));
            }
        }

        [Test]
        public void WoundMovesHeadAndCompressesBodyInsteadOfKeepingHealthyHitRegions()
        {
            using (var sim = new CombatSimulation())
            {
                sim.Reset(1, 137, false); Place(sim, 0, Vector3.zero);
                var oldHead = new Ray(Vector3.up * 1.6f - Vector3.forward * 3, Vector3.forward);
                var oldBodyEdge = new Ray(new Vector3(-.4f, .56f, -3), Vector3.forward);
                Assert.That(sim.QueryAim(oldHead).Region, Is.EqualTo(HitRegion.Head)); Assert.That(sim.QueryAim(oldBodyEdge).Region, Is.EqualTo(HitRegion.Body));
                sim.TryFire(BodyRay(sim), out ShotResult _);
                Assert.That(sim.QueryAim(oldHead).Hit, Is.False); Assert.That(sim.QueryAim(oldBodyEdge).Hit, Is.False);
                Assert.That(sim.QueryAim(HeadRay(sim)).Region, Is.EqualTo(HitRegion.Head));
                // Wounded shear pushes the upper right body outside its old healthy x extent.
                var displacedBody = new Ray(new Vector3(.47f, .99f, -3), Vector3.forward);
                Assert.That(sim.QueryAim(displacedBody).Region, Is.EqualTo(HitRegion.Body));
                sim.Reset(1, 137, false); Place(sim, 0, Vector3.zero);
                Assert.That(sim.QueryAim(displacedBody).Hit, Is.False, "Reset restores healthy geometry, not a stale wound transform.");
            }
        }

        [Test]
        public void InclusiveBoundariesInsideBehindZeroAndRangeAreExplicit()
        {
            Matrix4x4 cube = Matrix4x4.identity;
            Assert.That(EnemyGeometry.RayCube(new Ray(new Vector3(.5f, .5f, -3), Vector3.forward), cube, out float edge), Is.True);
            Assert.That(edge, Is.EqualTo(2.5f));
            Assert.That(EnemyGeometry.RayCube(new Ray(Vector3.zero, Vector3.forward), cube, out float inside), Is.True); Assert.That(inside, Is.Zero);
            Assert.That(EnemyGeometry.RayCube(new Ray(Vector3.forward * 3, Vector3.forward), cube, out _), Is.False);
            Assert.That(EnemyGeometry.RayCube(new Ray(Vector3.zero, Vector3.zero), cube, out _), Is.False);
            using (var sim = new CombatSimulation())
            {
                // Straddle range by a millimetre; decimal placement at exactly 65.425
                // rounds the front surface slightly past 65 in float world coordinates.
                sim.Reset(1, 137, false); Place(sim, 0, new Vector3(0, 0, 65.424f));
                Assert.That(sim.QueryAim(new Ray(Vector3.up * .8f, Vector3.forward)).Hit, Is.True);
                Place(sim, 0, new Vector3(0, 0, 65.426f));
                Assert.That(sim.QueryAim(new Ray(Vector3.up * .8f, Vector3.forward)).Hit, Is.False);
                Assert.That(sim.QueryAim(new Ray(Vector3.zero, Vector3.zero)).Hit, Is.False);
            }
        }

        [Test]
        public void FirstSurfaceWinsOverlapHeadWinsExactRegionTieAndSlotWinsEnemyTie()
        {
            using (var sim = new CombatSimulation())
            {
                sim.Reset(2, 137, false);
                Place(sim, 0, Vector3.forward * 4); Place(sim, 1, Vector3.forward * 8);
                Ray rising = new Ray(Vector3.up, new Vector3(0, .075f, 1));
                AimResult nearest = sim.QueryAim(rising);
                Assert.That(nearest.EnemyIndex, Is.Zero); Assert.That(nearest.Region, Is.EqualTo(HitRegion.Body), "Near body occludes the farther exposed head.");
                Place(sim, 0, new Vector3(3, 0, 4));
                Assert.That(sim.QueryAim(rising).Region, Is.EqualTo(HitRegion.Head));
                Place(sim, 0, Vector3.forward * 8);
                Assert.That(sim.QueryAim(rising).EnemyIndex, Is.Zero, "Coincident enemy surfaces retain deterministic slot ordering.");
                Place(sim, 0, Vector3.zero); Place(sim, 1, Vector3.right * 5);
                Assert.That(sim.QueryAim(new Ray(new Vector3(0, 1.4f, -3), Vector3.forward)).Region, Is.EqualTo(HitRegion.Body), "Healthy body protrudes in front of the overlapping head base.");
                Assert.That(sim.QueryAim(new Ray(new Vector3(0, 1.4f, 0), Vector3.forward)).Region, Is.EqualTo(HitRegion.Head), "Inside both regions: zero-distance tie prefers head.");
                sim.TryFire(BodyRay(sim), out ShotResult _);
                Assert.That(sim.QueryAim(new Ray(new Vector3(.2f, 1.02f, -3), Vector3.forward)).Region, Is.EqualTo(HitRegion.Body), "Wounded body also protrudes in its small overlap with head.");
                Assert.That(sim.QueryAim(new Ray(new Vector3(.2f, 1.02f, 0), Vector3.forward)).Region, Is.EqualTo(HitRegion.Head));
            }
        }

        [Test]
        public void MissConsumesCooldownWithoutDamageAndPreviewDoesNotMutateState()
        {
            using (var sim = new CombatSimulation())
            {
                sim.Reset(1, 137, false); long state = sim.StateVersion;
                for (int i = 0; i < 100; i++) sim.QueryAim(HeadRay(sim), .5f);
                Assert.That(sim.StateVersion, Is.EqualTo(state)); Assert.That(sim.Shots, Is.Zero); Assert.That(sim.EnemyHealth(0), Is.EqualTo(40));
                Assert.That(sim.TryFire(new Ray(Vector3.up * 10, Vector3.up), out ShotResult miss), Is.True);
                Assert.That(miss.Region, Is.EqualTo(HitRegion.None)); Assert.That(miss.RequestedDamage, Is.Zero); Assert.That(miss.Damage, Is.Zero);
                Assert.That(sim.TryFire(HeadRay(sim), out ShotResult _), Is.False); Assert.That(sim.Kills, Is.Zero);
                sim.TogglePause(); Assert.That(sim.TryFire(HeadRay(sim), out ShotResult _), Is.False);
                sim.TogglePause(); sim.Step(Vector3.zero, .12f);
                Assert.That(sim.TryFire(HeadRay(sim), out ShotResult head), Is.True); Assert.That(head.Killed, Is.True);
            }
        }

        [Test]
        public void BenchmarkRetainsFixedSphereBodyDamageEvenOnVisibleHeadRay()
        {
            using (var sim = new CombatSimulation())
            {
                sim.Reset(1, 137, true); Place(sim, 0, Vector3.zero);
                Ray head = new Ray(Vector3.up * 1.6f - Vector3.forward * 3, Vector3.forward);
                Assert.That(CombatSimulation.RaySphere(head, Vector3.up * 1.1f, CombatSimulation.EnemyRadius, out float distance), Is.True);
                AimResult aim = sim.QueryAim(head); Assert.That(aim.Region, Is.EqualTo(HitRegion.Body)); Assert.That(aim.Distance, Is.EqualTo(distance));
                Assert.That(sim.TryFire(head, out ShotResult shot), Is.True); Assert.That(shot.Damage, Is.EqualTo(20)); Assert.That(shot.Critical, Is.False);
                Assert.That(sim.EnemyHealth(0), Is.EqualTo(20));
            }
        }
    }
}
