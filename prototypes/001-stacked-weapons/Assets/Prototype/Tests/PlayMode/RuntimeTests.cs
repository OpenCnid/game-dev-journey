using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Journey.Tests
{
    public sealed class FeedbackHudProbe : MonoBehaviour
    {
        public CombatFeedback Feedback;
        void OnGUI() { Feedback?.DrawHUD(Screen.width, Screen.height); }
    }

    public sealed class RuntimeTests
    {
        [UnityTest]
        public IEnumerator DamagePresentationRendersBothOutcomesAndRestartClearsIt()
        {
            var root = new GameObject("Damage presentation test");
            var view = new ArenaPresentation(); var sim = new CombatSimulation { UseBurst = true };
            try
            {
                view.Create(root.transform); sim.Reset(1, 15, false);
                root.AddComponent<FeedbackHudProbe>().Feedback = view.Feedback;
                for (int outcome = 0; outcome < 2; outcome++)
                {
                    if (outcome > 0) sim.Step(Vector3.zero, .12f);
                    var ray = new Ray(sim.Position(0) + Vector3.up * 1.1f - Vector3.forward * 3, Vector3.forward);
                    Assert.That(sim.TryFire(ray, out ShotResult shot), Is.True);
                    view.ShowShot(shot.End); view.Feedback.RecordShot(sim, shot);
                    Quaternion aim = Quaternion.LookRotation(shot.End - (sim.Player + Vector3.up * 1.65f));
                    for (int mode = 0; mode < 2; mode++)
                    {
                        view.CullInstances = mode == 0;
                        view.Draw(sim, 1, aim.eulerAngles.y, aim.eulerAngles.x, CombatSimulation.Tick);
                        yield return null;
                    }
                    Assert.That(view.Feedback.ImpactCount, Is.EqualTo(1));
                    Assert.That(view.Feedback.FocusVisible, Is.True);
                    Assert.That(view.Feedback.HitMarkerVisible, Is.True);
                    if (outcome == 1) Assert.That(view.Feedback.KillMarkerVisible, Is.True);
                    if (outcome == 0)
                    {
                        Assert.That(view.Feedback.BarCount, Is.EqualTo(1));
                        Assert.That(view.Feedback.Bar(0).Health, Is.EqualTo(20));
                        Assert.That(view.Feedback.FocusHealth, Is.EqualTo(20));
                    }
                    Assert.That(sim.EnemyHealth(0), Is.EqualTo(outcome == 0 ? 20 : 40));
                    Assert.That(sim.Kills, Is.EqualTo(outcome));
                }
                view.Feedback.ObservePlayerHealth(90);
                sim.TogglePause(); view.Draw(sim, 1, 0, 0, 1);
                Assert.That(view.Feedback.KillMarkerVisible, Is.True);
                Assert.That(view.Feedback.PlayerDamage, Is.EqualTo(10));
                var muzzle = root.transform.Find("Player camera/Pulse rifle/Muzzle pulse").gameObject;
                Assert.That(muzzle.activeSelf, Is.True, "A shot pulse remains frozen during pause.");
                yield return null;
                view.ResetFeedback(); sim.Reset(1, 15, false);
                Assert.That(view.Feedback.ImpactCount, Is.Zero); Assert.That(view.Feedback.PlayerPulseRemaining, Is.Zero);
                Assert.That(view.Feedback.FocusVisible, Is.False);
                Assert.That(muzzle.activeSelf, Is.False);
                view.Draw(sim, 1, 0, 0, CombatSimulation.Tick);
                yield return null;
                view.ShowShot(Vector3.forward * 5); sim.Reset(1, 15, true);
                view.Draw(sim, 1, 0, 0, 0);
                Assert.That(muzzle.activeSelf, Is.False, "Synthetic benchmark must not acquire the new muzzle effect.");
                Assert.That(view.Feedback.FocusVisible, Is.False);
            }
            finally { view.Dispose(); sim.Dispose(); Object.Destroy(root); }
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ExperimentalRenderingPreservesSimulationAndDisposesBuffers()
        {
            for (int mode = 0; mode < 8; mode++)
            {
                var root = new GameObject("Experiment render test");
                var view = new ArenaPresentation { OverlapPreparation = (mode & 1) != 0, DeduplicateWarnings = true, CacheWarnings = true, CombinedCharacters = mode >= 4,
                    ProjectileRenderer = new[] { "cube", "octa", "indirect-cube", "indirect-octa" }[mode % 4] };
                var sim = new CombatSimulation { UseBurst = true, UseNativeState = true };
                try
                {
                    view.Create(root.transform);
                    sim.Reset(100, 137, true, bosses: true, synchronizedAbilities: true);
                    for (int tick = 0; tick < 65; tick++) sim.Step(Vector3.zero, CombatSimulation.Tick);
                    Assert.That(sim.Abilities.Telegraphs, Is.GreaterThan(0));
                    int hits = sim.Abilities.PlayerHits, shots = sim.Shots, attacks = sim.Abilities.Activations;
                    for (int hidden = 0; hidden < 4; hidden++)
                    {
                        view.HideWarnings = (hidden & 1) != 0; view.HideProjectiles = (hidden & 2) != 0;
                        view.Draw(sim, .5f, 270, 0, CombatSimulation.Tick);
                        yield return null;
                        Assert.That(sim.Abilities.PlayerHits, Is.EqualTo(hits));
                        Assert.That(sim.Abilities.Activations, Is.EqualTo(attacks));
                        Assert.That(sim.Shots, Is.EqualTo(shots));
                    }
                }
                finally { view.Dispose(); sim.Dispose(); Object.Destroy(root); }
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ArenaStartsFiresAndRestartsWithoutRuntimeErrors()
        {
            var root = new GameObject("Runtime smoke test");
            PrototypeGame game = root.AddComponent<PrototypeGame>();
            yield return null;
            Assert.That(Camera.main, Is.Not.Null);
            game.StartCombat();
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(game.State, Is.EqualTo(RunState.Running));
            Assert.That(game.ShotsFired, Is.GreaterThan(0));
            game.StartCombat();
            Assert.That(game.ShotsFired, Is.Zero);
            Object.Destroy(root);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
