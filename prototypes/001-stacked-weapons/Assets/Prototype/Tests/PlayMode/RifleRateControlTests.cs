using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Journey.Tests
{
    public sealed class RifleRateControlTests
    {
        [UnityTest]
        public IEnumerator MenuRateValidationAndCombatRestartUseTheChosenRate()
        {
            var root = new GameObject("Rifle rate control test");
            var game = root.AddComponent<PrototypeGame>();
            try
            {
                yield return null;
                Assert.That(game.State, Is.EqualTo(RunState.Ready));
                Assert.That(game.TrySetRifleRate("8.333333"), Is.True);
                Assert.That(game.RifleRate, Is.EqualTo(CombatSimulation.DefaultRifleRate));
                Assert.That(game.TrySetRifleRate("2"), Is.True);
                Assert.That(game.TrySetRifleRate("NaN"), Is.False);
                Assert.That(game.TrySetRifleRate("121"), Is.False);
                Assert.That(game.TrySetRifleRate("unfinished input"), Is.False);
                Assert.That(game.RifleRate, Is.EqualTo(2));
                Assert.That(game.TrySetRifleRate("2"), Is.True);
                game.StartCombat(); yield return new WaitForSecondsRealtime(.3f);
                Assert.That(game.ShotsFired, Is.EqualTo(1), "Slow setting must affect actual runtime firing.");
                Assert.That(game.TrySetRifleRate("120"), Is.True);
                int before = game.ShotsFired;
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(game.ShotsFired - before, Is.GreaterThan(25), "Actual combat must support more than 60 shots/sec.");
                game.StartCombat();
                Assert.That(game.RifleRate, Is.EqualTo(120)); Assert.That(game.ShotsFired, Is.Zero);
            }
            finally { Object.Destroy(root); }
            yield return null; LogAssert.NoUnexpectedReceived();
        }
    }
}
