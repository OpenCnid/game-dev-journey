using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Journey.Tests
{
    public sealed class RuntimeTests
    {
        [UnityTest]
        public IEnumerator ExperimentalRenderingPreservesSimulationAndDisposesBuffers()
        {
            for (int mode = 0; mode < 4; mode++)
            {
                var root = new GameObject("Experiment render test");
                var view = new ArenaPresentation { OverlapPreparation = (mode & 1) != 0, DeduplicateWarnings = true };
                var sim = new CombatSimulation { UseBurst = true, UseNativeState = (mode & 2) != 0 };
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
