using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class FeedbackTests
    {
        [TestCase(1f)] [TestCase(1.5f)] [TestCase(2f)]
        public void HudRotationKeepsMarkersAndConnectorsAtTheirScaledAnchor(float scale)
        {
            Vector3 pivot = new Vector3(640, 360, 0);
            Matrix4x4 transform = CombatFeedback.RotateGUI(Matrix4x4.Scale(Vector3.one * scale), pivot, 45);
            Assert.That(Vector3.Distance(transform.MultiplyPoint3x4(pivot), pivot * scale), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(transform.MultiplyPoint3x4(pivot + Vector3.right * 10), pivot * scale), Is.EqualTo(10 * scale).Within(.001f));
        }

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
                    Vector3 woundedCoreTip = camera.WorldToViewportPoint(sim.Position(0) + new Vector3(.246f, 1.4775f, 0));
                    Assert.That(Vector2.Distance(feedback.Bar(0).OwnerViewport, woundedCoreTip), Is.LessThan(.001f), "Connector must end at the wounded silhouette, not at the old bar position.");
                    sim.TogglePause(); feedback.CollectBars(sim, camera, 1); Assert.That(feedback.BarCount, Is.EqualTo(1));
                    camera.transform.rotation = Quaternion.Euler(0, 180, 0); feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.BarCount, Is.Zero);
                    camera.transform.rotation = Quaternion.identity;
                    camera.transform.position -= Vector3.forward * 40; feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.BarCount, Is.Zero);
                    sim.Reset(1000, 137, false); camera.transform.position = Vector3.up * 1.65f;
                    feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.BarCount, Is.EqualTo(CombatFeedback.BarCapacity));
                    for (int i = 1; i < feedback.BarCount; i++)
                    {
                        Assert.That(feedback.Bar(i).Priority, Is.GreaterThanOrEqualTo(feedback.Bar(i - 1).Priority));
                        if (feedback.Bar(i).Priority == feedback.Bar(i - 1).Priority)
                            Assert.That(feedback.Bar(i).Distance, Is.GreaterThanOrEqualTo(feedback.Bar(i - 1).Distance));
                    }
                    sim.Reset(1000, 137, true); feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.BarCount, Is.Zero, "Benchmark workload must not acquire gameplay bars.");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RapidFireHitsRemainVisibleDuringLongerKillConfirmation()
        {
            using (var sim = new CombatSimulation())
            {
                sim.Reset(1, 15, false); var feedback = new CombatFeedback();
                sim.TryFire(AtEnemy(sim), out _); sim.Step(Vector3.zero, .12f);
                sim.TryFire(AtEnemy(sim), out ShotResult kill); feedback.RecordShot(sim, kill);
                sim.Step(Vector3.zero, .12f); feedback.Advance(sim, .12f);
                sim.TryFire(AtEnemy(sim), out ShotResult wound); feedback.RecordShot(sim, wound);
                Assert.That(wound.Killed, Is.False);
                Assert.That(feedback.HitMarkerVisible, Is.True); Assert.That(feedback.KillMarkerVisible, Is.True);
                feedback.Advance(sim, .13f);
                Assert.That(feedback.HitMarkerVisible, Is.False); Assert.That(feedback.KillMarkerVisible, Is.True);
                feedback.Reset(); Assert.That(feedback.HitMarkerVisible || feedback.KillMarkerVisible, Is.False);
            }
        }

        [Test]
        public void CrowdedLabelsDoNotOverlapAndPreserveWoundedAndAimedEnemies()
        {
            var root = new GameObject("Crowded labels");
            try
            {
                var camera = root.AddComponent<Camera>(); camera.fieldOfView = 85;
                camera.transform.position = Vector3.up * 1.65f;
                using (var sim = new CombatSimulation())
                {
                    sim.Reset(1000, 137, false); var feedback = new CombatFeedback();
                    feedback.CollectBars(sim, camera, 1);
                    var crowd = new int[feedback.BarCount];
                    for (int i = 0; i < crowd.Length; i++) crowd[i] = feedback.Bar(i).EnemyIndex;
                    // Wound a dense cluster so overlap checks exercise multiple eligible bars.
                    for (int i = 0; i < Mathf.Min(32, crowd.Length); i++)
                    {
                        sim.Step(Vector3.zero, .13f);
                        if (sim.EnemyHealth(crowd[i]) == CombatSimulation.EnemyMaxHealth)
                            sim.TryFire(new Ray(sim.Position(crowd[i]) + Vector3.up * 1.1f, Vector3.forward), out ShotResult _);
                    }
                    feedback.CollectBars(sim, camera, 1);
                    int visible = feedback.Bar(0).EnemyIndex;
                    camera.transform.rotation = Quaternion.LookRotation(sim.Position(visible) + Vector3.up * 1.1f - camera.transform.position);
                    int shots = sim.Shots, kills = sim.Kills, health = sim.EnemyHealth(visible);
                    feedback.CollectBars(sim, camera, 1); feedback.LayoutBars(1280, 720);
                    Assert.That(feedback.VisibleBarCount, Is.InRange(2, CombatFeedback.VisibleBarCapacity));
                    Assert.That(feedback.VisibleBarCount, Is.LessThan(feedback.BarCount));
                    int woundedCandidates = 0;
                    for (int i = 0; i < feedback.BarCount; i++) if (feedback.Bar(i).Health < CombatSimulation.EnemyMaxHealth) woundedCandidates++;
                    Assert.That(woundedCandidates, Is.GreaterThan(feedback.VisibleBarCount), "Overlapping wounded labels must actually yield space.");
                    bool woundedVisible = false;
                    int target = sim.AimTarget(new Ray(camera.transform.position, camera.transform.forward), out _);
                    if (target >= 0) Assert.That(feedback.FocusEnemy, Is.EqualTo(target));
                    var firstBounds = new Rect[feedback.VisibleBarCount];
                    for (int i = 0; i < feedback.VisibleBarCount; i++)
                    {
                        Assert.That(feedback.VisibleBar(i).Priority, Is.LessThan(2), "Healthy background labels must not fill the cluster.");
                        Assert.That(feedback.VisibleBarBounds(i).y, Is.EqualTo((1 - feedback.VisibleBar(i).Viewport.y) * 720 - 2).Within(.001), "Do not detach labels into floating rows.");
                        if (feedback.VisibleBar(i).Health < CombatSimulation.EnemyMaxHealth) woundedVisible = true;
                        firstBounds[i] = feedback.VisibleBarBounds(i);
                        for (int j = 0; j < i; j++) Assert.That(firstBounds[i].Overlaps(firstBounds[j]), Is.False);
                    }
                    Assert.That(woundedVisible, Is.True);
                    feedback.LayoutBars(1280, 720);
                    for (int i = 0; i < firstBounds.Length; i++) Assert.That(feedback.VisibleBarBounds(i), Is.EqualTo(firstBounds[i]), "Repeated layout must not shuffle static labels.");
                    var owners = new int[CombatFeedback.VisibleBarCapacity];
                    for (int i = 0; i < owners.Length; i++) owners[i] = feedback.LabelOwner(i);
                    camera.transform.position += Vector3.right * .01f;
                    feedback.CollectBars(sim, camera, 1); feedback.LayoutBars(1280, 720);
                    for (int i = 0; i < owners.Length; i++) Assert.That(feedback.LabelOwner(i), Is.EqualTo(owners[i]), "Small camera/distance changes cannot reassign live wounded labels.");
                    camera.fieldOfView = 179;
                    feedback.CollectBars(sim, camera, 1); feedback.LayoutBars(1280, 720);
                    Assert.That(feedback.VisibleBarCount, Is.EqualTo(feedback.TargetHealthBarVisible ? 0 : 1), "Only a wounded target bar reserves space; healthy aim brackets cannot hide wound labels.");
                    camera.fieldOfView = 85;
                    feedback.CollectBars(sim, camera, 1); feedback.LayoutBars(1280, 720);
                    for (int i = 0; i < feedback.VisibleBarCount; i++)
                        Assert.That(System.Array.IndexOf(owners, feedback.VisibleBar(i).EnemyIndex), Is.EqualTo(-1), "A brief clear frame cannot make suppressed owners flash back on; free slots may fill.");
                    feedback.Advance(sim, .3f); feedback.LayoutBars(1280, 720);
                    Assert.That(feedback.VisibleBarCount, Is.GreaterThanOrEqualTo(firstBounds.Length));
                    for (int i = 0; i < owners.Length; i++) if (owners[i] >= 0) Assert.That(feedback.LabelOwner(i), Is.EqualTo(owners[i]), "Overlap recovery must keep the same owners.");
                    Assert.That(sim.Shots, Is.EqualTo(shots)); Assert.That(sim.Kills, Is.EqualTo(kills));
                    Assert.That(sim.EnemyHealth(visible), Is.EqualTo(health), "Target preview and label layout cannot apply damage.");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void NormalTargetHealthAppearsAfterDamageAndRespawnHidesIt()
        {
            var root = new GameObject("Focus panel");
            try
            {
                var camera = root.AddComponent<Camera>();
                using (var sim = new CombatSimulation())
                {
                    sim.Reset(1, 137, false); var feedback = new CombatFeedback();
                    camera.transform.position = sim.Position(0) + Vector3.up * 1.1f - Vector3.forward * 3;
                    feedback.CollectBars(sim, camera, 1); feedback.LayoutBars(1280, 720);
                    Assert.That(feedback.FocusAimed, Is.True); Assert.That(feedback.FocusHealth, Is.EqualTo(40));
                    Assert.That(feedback.FocusVisible, Is.True, "Healthy targets retain body brackets.");
                    Assert.That(feedback.TargetHealthBarVisible, Is.False, "Aiming alone must not show normal enemy HP.");
                    Assert.That(feedback.VisibleBarCount, Is.Zero, "A target uses its own bar, without a duplicate wounded label.");
                    sim.TryFire(AtEnemy(sim), out ShotResult wound); feedback.RecordShot(sim, wound);
                    feedback.CollectBars(sim, camera, 1); Assert.That(feedback.FocusHealth, Is.EqualTo(20));
                    Assert.That(feedback.TargetHealthBarVisible, Is.True, "Actual health loss enables the bar.");
                    camera.transform.rotation = Quaternion.Euler(0, 180, 0);
                    feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.FocusAimed, Is.False); Assert.That(feedback.FocusVisible, Is.False);
                    Assert.That(feedback.FocusEnemy, Is.EqualTo(-1));
                    camera.transform.rotation = Quaternion.identity; feedback.CollectBars(sim, camera, 1);
                    sim.TogglePause(); feedback.Advance(sim, 1); Assert.That(feedback.FocusVisible, Is.True);
                    sim.TogglePause(); sim.Step(Vector3.zero, .13f);
                    sim.TryFire(AtEnemy(sim), out ShotResult kill); Assert.That(kill.Killed, Is.True);
                    feedback.RecordShot(sim, kill); feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.FocusVisible, Is.False, "A killed incarnation must not retain defeated HP UI.");
                    camera.transform.position = sim.Position(0) + Vector3.up * 1.1f - Vector3.forward * 3;
                    feedback.CollectBars(sim, camera, 1);
                    Assert.That(feedback.FocusVisible, Is.True); Assert.That(feedback.TargetHealthBarVisible, Is.False, "The fresh full-health respawn must not inherit the wounded bar.");
                    feedback.Reset(); Assert.That(feedback.FocusEnemy, Is.EqualTo(-1));
                    for (int i = 0; i < CombatFeedback.VisibleBarCapacity; i++) Assert.That(feedback.LabelOwner(i), Is.EqualTo(-1));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void WoundedCulledPoseChangesSilhouetteAndReturnsToNormalWithoutMask()
        {
            var root = new GameObject("Damage pose test");
            try
            {
                var camera = root.AddComponent<Camera>();
                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                var points = new[] { Vector3.forward * 5 };
                using (var instances = new BurstInstances())
                {
                    int count = instances.Prepare(points, points, 1, 1, planes, Vector3.up * .8f, new Vector3(.85f, 1.4f, .85f),
                        Vector3.up * 1.6f, Vector3.one * .45f, Vector3.up, 1.2f, true, 128, new byte[] { 1 });
                    Assert.That(count, Is.EqualTo(1));
                    Assert.That(instances.A[0].m11, Is.LessThan(1.4f)); Assert.That(instances.A[0].m01, Is.Not.Zero);
                    Assert.That(instances.B[0].m13, Is.LessThan(1.6f));
                    instances.Prepare(points, points, 1, 1, planes, Vector3.up * .8f, new Vector3(.85f, 1.4f, .85f),
                        Vector3.up * 1.6f, Vector3.one * .45f, Vector3.up, 1.2f, true, 128);
                    Assert.That(instances.A[0].m11, Is.EqualTo(1.4f)); Assert.That(instances.A[0].m01, Is.Zero);
                    Assert.That(instances.B[0].m13, Is.EqualTo(1.6f), "Unmasked reference/benchmark preparation cannot inherit a wound.");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
