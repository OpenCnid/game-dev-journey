using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class ScenarioConfigurationTests
    {
        static BinaryReader Packet(Action<BinaryWriter> write)
        { var stream = new MemoryStream(); var writer = new BinaryWriter(stream); write(writer); writer.Flush(); stream.Position = 0; return new BinaryReader(stream); }
        static void Agree(SharedScenarioBinding host, SharedScenarioBinding client)
        {
            using (var r = Packet(host.Write)) client.AcceptOffer(r);
            using (var r = Packet(client.Write)) host.AcceptAcknowledgement(r);
        }
        [TestCase(ScenarioKind.SoloPrecision, 35, 40, 20, 2, .12f)]
        [TestCase(ScenarioKind.SharedRifle, 12, 60, 20, 2, .12f)]
        [TestCase(ScenarioKind.DeveloperParity, 12, 90, 15, 3, .25f)]
        public void NamedControlsRetainValuesAndBothPathsResolveBodyHeadAndRespawn(ScenarioKind kind, int count, int hp, int damage, int multiplier, float interval)
        {
            var config = ScenarioConfiguration.Named(kind);
            Assert.That(config.EnemyCount, Is.EqualTo(count)); Assert.That(config.EnemyHealth, Is.EqualTo(hp));
            Assert.That(config.BodyDamage, Is.EqualTo(damage)); Assert.That(config.HeadMultiplier, Is.EqualTo(multiplier));
            Assert.That(config.RifleInterval, Is.EqualTo(interval)); Assert.That(config.Overridden, Is.False);
            foreach (bool head in new[] { false, true }) using (var solo = new CombatSimulation())
            {
                solo.Reset(config); var shared = new SharedEncounter(config); shared.Start();
                Assert.That(solo.HealthMaximum, Is.EqualTo(hp)); Assert.That(solo.Duration, Is.EqualTo(config.Duration));
                var positions = (Vector3[])typeof(CombatSimulation).GetField("positions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(solo);
                for (int i = 0; i < count; i++)
                { Assert.That(solo.EnemyHealth(i), Is.EqualTo(shared.Current.Enemies[i].Health)); positions[i] = shared.Current.Enemies[i].Position = new Vector3(28, 0, 28); }
                positions[0] = shared.Current.Enemies[0].Position = new Vector3(0, 0, 8); shared.Step();
                Vector3 origin = SharedEncounter.Spawn(0) + Vector3.up * 1.65f;
                Ray soloRay = new Ray(origin, (positions[0] + Vector3.up * (head ? 1.6f : .8f) - origin).normalized);
                Ray sharedRay = new Ray(origin, (shared.Current.Enemies[0].Position + Vector3.up * (head ? 1.6f : .8f) - origin).normalized);
                SharedHit hit = default; shared.Resolved += h => hit = h;
                shared.Submit(new SharedShot { Run = 1, Player = 0, Life = 1, Weapon = 1, Event = 1, PoseTick = 1, Ray = sharedRay }); shared.Step();
                Assert.That(solo.TryFire(soloRay, out ShotResult shot), Is.True);
                Assert.That(shot.RequestedDamage, Is.EqualTo(damage * (head ? multiplier : 1)));
                Assert.That(hit.Shot.RequestedDamage, Is.EqualTo(shot.RequestedDamage));
                Assert.That(hit.Shot.HealthAfter, Is.EqualTo(shot.HealthAfter));
                Assert.That(solo.EnemyHealth(0), Is.EqualTo(shared.Current.Enemies[0].Health));
                typeof(CombatSimulation).GetMethod("ProjectShared", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(solo, new object[] { shared.Current, 0, Vector3.zero });
                Assert.That(solo.HealthMaximum, Is.EqualTo(hp)); Assert.That(solo.Configuration.Identity, Is.EqualTo(config.Identity));
            }
        }
        [TestCase(.5f)] [TestCase(2f)] [TestCase(4f)] [TestCase(CombatSimulation.DefaultRifleRate)] [TestCase(30f)] [TestCase(120f)]
        public void MatchingCadenceConsumesAllDueShotsIncludingAboveTickRate(float rate)
        {
            var config = ScenarioConfiguration.Named(ScenarioKind.SharedRifle).With(interval: ScenarioConfiguration.IntervalForRate(rate));
            using (var solo = new CombatSimulation())
            {
                solo.Reset(config); var shared = new SharedEncounter(config); shared.Start();
                int sequence = 0;
                for (int tick = 0; tick < 120; tick++)
                {
                    solo.Step(Vector3.zero, CombatSimulation.Tick);
                    var ray = new Ray(SharedEncounter.Spawn(0) + Vector3.up * 1.65f, Vector3.up);
                    while (solo.TryFire(ray, out ShotResult _)) shared.Submit(new SharedShot {
                        Run = 1, Player = 0, Life = 1, Weapon = 1, Event = ++sequence, PoseTick = shared.Current.Tick, Ray = ray });
                    shared.Step();
                    Assert.That(shared.Current.Players[0].Shots, Is.EqualTo(solo.Shots), "Cadence differs at tick " + tick);
                    Assert.That(shared.Rejected, Is.Zero);
                }
            }
        }
        [Test] public void ValidationRejectsNonfiniteBoundsVersionsAndMalformedArguments()
        {
            var config = ScenarioConfiguration.Named(ScenarioKind.SharedRifle);
            foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, 0, -1, 601 })
                Assert.Throws<ArgumentException>(() => config.With(duration: bad));
            foreach (float bad in new[] { float.NaN, float.PositiveInfinity, 0, -.1f, 2.1f, .001f })
                Assert.Throws<ArgumentException>(() => config.With(interval: bad));
            Assert.Throws<ArgumentException>(() => config.With(count: 36)); Assert.Throws<ArgumentException>(() => config.With(count: 0));
            Assert.Throws<ArgumentException>(() => config.With(hp: 0)); Assert.Throws<ArgumentException>(() => config.With(hp: 10001));
            Assert.Throws<ArgumentException>(() => config.With(damage: 0)); Assert.Throws<ArgumentException>(() => config.With(damage: 1001));
            Assert.Throws<ArgumentException>(() => config.With(head: 0)); Assert.Throws<ArgumentException>(() => config.With(head: 5));
            Assert.Throws<ArgumentException>(() => new ScenarioConfiguration(ScenarioConfiguration.Version + 1, ScenarioKind.SharedRifle, 137, 12, 60, 20, 2, 60, .12f));
            Assert.Throws<ArgumentException>(() => ScenarioConfiguration.Named((ScenarioKind)99));
            Assert.Throws<ArgumentException>(() => ScenarioConfiguration.FromArguments(new[] { "-scenario", "unknown" }, ScenarioKind.SharedRifle));
            Assert.Throws<FormatException>(() => ScenarioConfiguration.FromArguments(new[] { "-scenarioHp", "bad" }, ScenarioKind.SharedRifle));
            Assert.Throws<ArgumentException>(() => ScenarioConfiguration.FromArguments(new[] { "-scenarioHp" }, ScenarioKind.SharedRifle));
            Assert.Throws<ArgumentException>(() => ScenarioConfiguration.FromArguments(new[] { "-rifleRate", "NaN" }, ScenarioKind.SharedRifle));
        }
        [Test] public void OfferIsAtomicAndAcknowledgementCannotStartDifferentOrStaleConfiguration()
        {
            var config = ScenarioConfiguration.Named(ScenarioKind.DeveloperParity);
            var host = new SharedScenarioBinding(config); var client = new SharedScenarioBinding();
            using (var r = Packet(w => { host.Write(w); w.Write(42); })) Assert.Throws<ArgumentException>(() => client.AcceptOffer(r));
            Assert.That(client.Configuration, Is.Null); Assert.That(client.Agreed, Is.False);
            using (var r = Packet(w => { w.Write(Guid.NewGuid().ToByteArray()); w.Write(999); }))
                Assert.Catch(() => client.AcceptOffer(r));
            Assert.That(client.Configuration, Is.Null);
            using (var r = Packet(new SharedScenarioBinding(config.With(hp: 91)).Write))
                Assert.Throws<ArgumentException>(() => host.AcceptAcknowledgement(r));
            Assert.That(host.Agreed, Is.False);
            Agree(host, client);
            Assert.That(client.Configuration.Identity, Is.EqualTo(host.Configuration.Identity));
            using (var r = Packet(host.Write)) Assert.Throws<ArgumentException>(() => client.AcceptOffer(r));
            using (var r = Packet(client.Write)) Assert.Throws<ArgumentException>(() => host.AcceptAcknowledgement(r));
        }
        [Test] public void RetryRetainsFrozenConfigurationRejectsOldStateAndCannotAdoptForeignTuning()
        {
            var config = ScenarioConfiguration.Named(ScenarioKind.DeveloperParity).With(duration: 1);
            var host = new SharedScenarioBinding(config); var client = new SharedScenarioBinding(); Agree(host, client);
            var game = new SharedEncounter(config, host.Session); game.Start();
            var display = new SharedSnapshot(config); Assert.That(client.CanAdopt(game.Current, display), Is.True); display.CopyFrom(game.Current);
            for (int i = 0; i < 60; i++) game.Step();
            var old = new SharedSnapshot(); old.CopyFrom(game.Current); display.CopyFrom(old);
            Assert.That(game.Retry(1, 0), Is.True); Assert.That(game.Current.Run, Is.EqualTo(1));
            game.Retry(1, 1); Assert.That(client.CanAdopt(game.Current, display), Is.True); display.CopyFrom(game.Current);
            Assert.That(display.Run, Is.EqualTo(2)); Assert.That(display.Configuration.Identity, Is.EqualTo(config.Identity));
            Assert.That(client.CanAdopt(old, display), Is.False); Assert.That(game.Retry(1, 1), Is.False);
            var foreign = new SharedEncounter(config.With(hp: 91), host.Session); foreign.Start();
            Assert.Throws<ArgumentException>(() => client.CanAdopt(foreign.Current, display));
            foreign = new SharedEncounter(config); foreign.Start(); Assert.Throws<ArgumentException>(() => client.CanAdopt(foreign.Current, display));
            foreach (var enemy in display.Enemies) Assert.That(enemy.Health, Is.EqualTo(90));
        }
        [Test] public void LargestSnapshotFitsPacketAndFailedParseCannotMutateDestination()
        {
            var game = new SharedEncounter(ScenarioConfiguration.Named(ScenarioKind.SoloPrecision)); game.Start();
            var destination = new SharedSnapshot();
            using (var r = Packet(w => SharedConnection.WriteSnapshot(w, game.Current)))
            {
                Assert.That(r.BaseStream.Length + 1, Is.LessThanOrEqualTo(SharedConnection.MaxPacket));
                byte[] bytes = r.ReadBytes((int)r.BaseStream.Length);
                using (var truncated = new BinaryReader(new MemoryStream(bytes, 0, bytes.Length - 1)))
                    Assert.Throws<EndOfStreamException>(() => SharedConnection.ReadSnapshot(truncated, destination));
                Assert.That(destination.Run, Is.Zero); Assert.That(destination.Enemies.Length, Is.EqualTo(12));
                r.BaseStream.Position = 0; SharedConnection.ReadSnapshot(r, destination);
                Assert.That(destination.Enemies.Length, Is.EqualTo(35)); Assert.That(destination.Configuration.Identity, Is.EqualTo(game.Configuration.Identity));
            }
        }
    }
}
