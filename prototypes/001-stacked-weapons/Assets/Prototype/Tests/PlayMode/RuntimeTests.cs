using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Journey.Tests
{
    public sealed class RuntimeTests
    {
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
