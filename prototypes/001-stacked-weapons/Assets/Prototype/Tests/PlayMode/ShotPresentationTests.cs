using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Journey.Tests
{
    public sealed class ShotPresentationTests
    {
        [UnityTest]
        public IEnumerator FastTurnUsesCurrentMuzzleAndDoesNotLeaveAnOldLine()
        {
            var root = new GameObject("Fast turn shot test");
            var sim = new CombatSimulation(); var view = new ArenaPresentation();
            try
            {
                view.Create(root.transform); sim.Reset(0, 137, false);
                view.Draw(sim, 1, 0, 0, 0);
                sim.Step(Vector3.right, CombatSimulation.Tick);
                view.BeginFrame(sim, .25f, 70, -20, 1f / 144);
                var ray = new Ray(view.Camera.transform.position, view.Camera.transform.forward);
                Assert.That(sim.TryFire(ray, out ShotResult result, .25f), Is.True);
                view.ShowShot(result.End);
                view.Draw(sim, .25f);
                var line = root.GetComponentInChildren<LineRenderer>();
                var muzzle = root.transform.Find("Player camera/Pulse rifle/Muzzle pulse");
                float originError = Vector3.Distance(line.GetPosition(0), muzzle.position);
                Vector3 endpoint = view.Camera.WorldToViewportPoint(line.GetPosition(1));
                bool birthVisible = line.enabled;
                yield return null;
                view.Draw(sim, .75f, 85, -10, 1f / 144);
                bool retainedAfterTurn = line.enabled;
                Debug.Log($"SHOT_ALIGNMENT_REPRO muzzleErrorMetres={originError:F6} endpointViewport={endpoint:F6} birthVisible={birthVisible} retainedAfterTurn={retainedAfterTurn} lineRenderers={root.GetComponentsInChildren<LineRenderer>().Length}");
                Assert.That(originError, Is.LessThan(.001f), "A fresh shot must start at this frame's rendered muzzle, including yaw/pitch and recoil.");
                Assert.That(birthVisible, Is.True);
                Assert.That(endpoint.x, Is.EqualTo(.5f).Within(.00001f));
                Assert.That(endpoint.y, Is.EqualTo(.5f).Within(.00001f));
                Assert.That(root.GetComponentsInChildren<LineRenderer>().Length, Is.EqualTo(1));
                Assert.That(retainedAfterTurn, Is.False, "A hitscan shot must not leave its old world line across later turning frames.");
            }
            finally { view.Dispose(); sim.Dispose(); Object.Destroy(root); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FreshShotsSurviveLongFrameButLinesClearOnPauseAndRestart()
        {
            var root = new GameObject("Shot lifetime test");
            var sim = new CombatSimulation(); var view = new ArenaPresentation();
            try
            {
                view.Create(root.transform); sim.Reset(1, 15, false);
                sim.Step(Vector3.right, CombatSimulation.Tick);
                Vector3 eye = sim.RenderPlayer(.5f) + Vector3.up * 1.65f;
                Quaternion aim = Quaternion.LookRotation(sim.RenderPosition(0, .5f) + Vector3.up * 1.1f - eye);
                view.BeginFrame(sim, .5f, aim.eulerAngles.y, aim.eulerAngles.x, .2f);
                Assert.That(sim.TryFire(new Ray(view.Camera.transform.position, view.Camera.transform.forward), out ShotResult shot, .5f), Is.True);
                view.ShowShot(shot.End); view.Feedback.RecordShot(sim, shot, .5f); view.Draw(sim, .5f);
                var line = root.GetComponentInChildren<LineRenderer>();
                var muzzle = root.transform.Find("Player camera/Pulse rifle/Muzzle pulse").gameObject;
                Assert.That(line.enabled && muzzle.activeSelf && view.Feedback.HitMarkerVisible, Is.True,
                    "A new shot is visible even after a frame longer than all short cue lifetimes.");
                Vector3 endpoint = view.Camera.WorldToViewportPoint(line.GetPosition(1));
                Assert.That(endpoint.x, Is.EqualTo(.5f).Within(.00001f));
                Assert.That(endpoint.y, Is.EqualTo(.5f).Within(.00001f));
                Assert.That(sim.EnemyHealth(0), Is.EqualTo(20), "Damage is already applied before rendering the shot.");
                yield return null;
                sim.TogglePause(); view.Draw(sim, .5f, aim.eulerAngles.y, aim.eulerAngles.x, 1);
                Assert.That(line.enabled, Is.False, "Pause must not retain a shot segment over its menu.");
                Assert.That(muzzle.activeSelf && view.Feedback.HitMarkerVisible, Is.True, "Persistent cues freeze.");
                view.ResetFeedback(); sim.Reset(0, 137, false);
                Assert.That(line.enabled || muzzle.activeSelf || view.Feedback.HitMarkerVisible, Is.False);
                sim.Reset(0, 137, true);
                view.ShowShot(Vector3.forward * 5, benchmark: true);
                view.Draw(sim, 1, 0, 0, .007f);
                Assert.That(line.enabled, Is.True); Assert.That(muzzle.activeSelf, Is.False);
                view.Draw(sim, 1, 10, 0, .007f);
                Assert.That(line.enabled, Is.True, "Benchmark preserves its legacy multi-frame tracer.");
                view.Draw(sim, 1, 20, 0, .04f); Assert.That(line.enabled, Is.False);
            }
            finally { view.Dispose(); sim.Dispose(); Object.Destroy(root); }
            yield return null; LogAssert.NoUnexpectedReceived();
        }
    }
}
