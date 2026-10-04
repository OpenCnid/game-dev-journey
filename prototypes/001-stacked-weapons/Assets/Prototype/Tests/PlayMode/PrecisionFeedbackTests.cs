using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Journey.Tests
{
    public sealed class PrecisionFeedbackTests
    {
        [UnityTest]
        public IEnumerator PreviewTracksDisplayedPoseMissCooldownPauseAndResetWithIndependentCriticalCue()
        {
            var root = new GameObject("Precision feedback test");
            var sim = new CombatSimulation(); var view = new ArenaPresentation();
            try
            {
                view.Create(root.transform); sim.Reset(1, 137, false); sim.Step(Vector3.right, CombatSimulation.Tick);
                root.AddComponent<FeedbackHudProbe>().Feedback = view.Feedback;
                const float alpha = .25f;
                Vector3 eye = sim.RenderPlayer(alpha) + Vector3.up * 1.65f;
                Quaternion aim = Quaternion.LookRotation(sim.RenderPosition(0, alpha) + Vector3.up * .56f - eye);
                view.Draw(sim, alpha, aim.eulerAngles.y, aim.eulerAngles.x, 0);
                Assert.That(view.Feedback.PreviewRegion, Is.EqualTo(HitRegion.Body)); Assert.That(view.Feedback.CrosshairColor, Is.Not.EqualTo(Color.white));
                var ray = new Ray(view.Camera.transform.position, view.Camera.transform.forward);
                Assert.That(sim.TryFire(ray, out ShotResult wound, alpha), Is.True);
                view.Feedback.RecordShot(sim, wound, alpha); view.Draw(sim, alpha);
                Assert.That(view.Feedback.PreviewRegion, Is.EqualTo(HitRegion.Body), "Preview remains during cooldown.");
                Assert.That(view.Feedback.TargetHealthBarVisible, Is.True);
                yield return null;
                view.Draw(sim, alpha, aim.eulerAngles.y + 90, 0, 0);
                Assert.That(view.Feedback.PreviewRegion, Is.EqualTo(HitRegion.None)); Assert.That(view.Feedback.CrosshairColor, Is.EqualTo(Color.white));
                Assert.That(sim.Shots, Is.EqualTo(1)); Assert.That(sim.Kills, Is.Zero);
                sim.Step(Vector3.zero, .12f);
                eye = sim.RenderPlayer(alpha) + Vector3.up * 1.65f;
                aim = Quaternion.LookRotation(EnemyGeometry.Head(sim.RenderPosition(0, alpha), true).MultiplyPoint3x4(Vector3.zero) - eye);
                view.Draw(sim, alpha, aim.eulerAngles.y, aim.eulerAngles.x, 0);
                Assert.That(view.Feedback.PreviewRegion, Is.EqualTo(HitRegion.Head));
                Assert.That(sim.TryFire(new Ray(view.Camera.transform.position, view.Camera.transform.forward), out ShotResult critical, alpha), Is.True);
                Assert.That(critical.Critical && critical.Killed, Is.True);
                view.ShowShot(critical.End); view.Feedback.RecordShot(sim, critical, alpha); view.Draw(sim, alpha);
                Assert.That(view.Feedback.CriticalMarkerVisible && view.Feedback.KillMarkerVisible && view.Feedback.HitMarkerVisible, Is.True);
                yield return null;
                Assert.That(view.Feedback.TargetHealthBarVisible, Is.False, "The old wounded/dead owner cannot remain focused after respawn.");
                sim.TogglePause(); view.Draw(sim, alpha, aim.eulerAngles.y, aim.eulerAngles.x, 1);
                Assert.That(view.Feedback.CrosshairColor, Is.EqualTo(Color.white));
                Assert.That(view.Feedback.CriticalMarkerVisible && view.Feedback.KillMarkerVisible && view.Feedback.HitMarkerVisible, Is.True, "Independent cues freeze on pause.");
                sim.TogglePause(); view.Draw(sim, alpha, aim.eulerAngles.y, aim.eulerAngles.x, .13f);
                Assert.That(view.Feedback.HitMarkerVisible, Is.False); Assert.That(view.Feedback.CriticalMarkerVisible && view.Feedback.KillMarkerVisible, Is.True);
                view.Draw(sim, alpha, aim.eulerAngles.y, aim.eulerAngles.x, .13f);
                Assert.That(view.Feedback.CriticalMarkerVisible, Is.False); Assert.That(view.Feedback.KillMarkerVisible, Is.True);
                view.ResetFeedback(); sim.Reset(1, 137, false);
                Assert.That(view.Feedback.PreviewRegion, Is.EqualTo(HitRegion.None));
                Assert.That(view.Feedback.HitMarkerVisible || view.Feedback.CriticalMarkerVisible || view.Feedback.KillMarkerVisible, Is.False);
                const string repro = "PRECISION_FEEDBACK_REPRO bodyPreview=true missWhite=true woundedHeadCritical=true actualDamage=20 pauseFreeze=true resetClear=true";
                LogAssert.Expect(LogType.Log, repro); Debug.Log(repro);
            }
            finally { view.Dispose(); sim.Dispose(); Object.Destroy(root); }
            yield return null; LogAssert.NoUnexpectedReceived();
        }
    }
}
