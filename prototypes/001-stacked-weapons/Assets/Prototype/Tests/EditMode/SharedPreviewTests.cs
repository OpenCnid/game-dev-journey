using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public class SharedPreviewTests
    {
        SharedSnapshot snapshot;
        [SetUp] public void SetUp() { var game = new SharedEncounter(); game.Start(); snapshot = game.Current; }
        SharedPreviewCommand Input() => new SharedPreviewCommand { sequence = 1, action = "input", run = snapshot.Run,
            life = snapshot.Players[1].Life, duration = .5f, moveX = 1, yaw = 20, pitch = -10, fire = true };
        [Test] public void OldRunAndLifeCannotControlAnotherAttemptOrReturnedPlayer()
        {
            var command = Input(); Assert.That(command.Validate(snapshot, 1), Is.Empty);
            command.run--; Assert.That(command.Validate(snapshot, 1), Does.Contain("Stale"));
            command.run++; command.life--; Assert.That(command.Validate(snapshot, 1), Does.Contain("Stale"));
        }
        [Test] public void InvalidOrUnboundedInputRejectsWithoutChangingAuthority()
        {
            Vector3 position = snapshot.Players[1].Position;
            var command = Input(); command.duration = 2.01f; Assert.That(command.Validate(snapshot, 1), Is.Not.Empty);
            command = Input(); command.yaw = float.NaN; Assert.That(command.Validate(snapshot, 1), Is.Not.Empty);
            command = Input(); command.moveX = float.PositiveInfinity; Assert.That(command.Validate(snapshot, 1), Is.Not.Empty);
            command = Input(); command.pitch = 81; Assert.That(command.Validate(snapshot, 1), Is.Not.Empty);
            Assert.That(snapshot.Players[1].Position, Is.EqualTo(position));
        }
        [Test] public void RetryAndInputRespectTerminalAndDeadStates()
        {
            var command = Input(); command.action = "retry"; Assert.That(command.Validate(snapshot, 1), Is.Not.Empty);
            snapshot.State = SharedState.Defeat; Assert.That(command.Validate(snapshot, 1), Is.Empty);
            command.action = "input"; Assert.That(command.Validate(snapshot, 1), Is.Not.Empty);
            snapshot.State = SharedState.Running; var p = snapshot.Players[1]; p.Health = 0; snapshot.Players[1] = p;
            Assert.That(command.Validate(snapshot, 1), Is.Not.Empty);
        }
    }
}
