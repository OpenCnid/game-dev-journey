using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class CoopScaleTests
    {
        static byte[] Snapshot(SharedSnapshot snapshot)
        {
            using (var stream = new MemoryStream())
            { SharedConnection.WriteSnapshot(new BinaryWriter(stream), snapshot); return stream.ToArray(); }
        }
        static BinaryReader Chunk(byte[] data, int offset, int sequence = 1, int? length = null)
        {
            var stream = new MemoryStream(); var writer = new BinaryWriter(stream);
            writer.Write(sequence); writer.Write(length ?? data.Length); writer.Write(offset);
            writer.Write(data, offset, Math.Min(SnapshotAssembly.ChunkBytes, data.Length - offset));
            writer.Flush(); stream.Position = 0; return new BinaryReader(stream);
        }
        [Test] public void DiagnosticBoundsAndIdentityCannotChangeOrdinaryGameplay()
        {
            var named = ScenarioConfiguration.Named(ScenarioKind.CoopScale);
            var large = named.Scale(1000, 8, 90);
            Assert.That(large.Identity, Is.Not.EqualTo(named.Identity));
            Assert.That(large.Identity, Is.Not.EqualTo(large.Scale(1000, 3, 90).Identity));
            Assert.That(large.Growth, Is.False); Assert.That(large.Pickups, Is.True);
            Assert.That(large.SharedSolo, Is.True); Assert.That(large.StartingArsenal, Is.EqualTo(255));
            Assert.Throws<ArgumentException>(() => large.Scale(1001, 8, 90));
            Assert.Throws<ArgumentException>(() => large.Scale(0, 8, 90));
            Assert.Throws<ArgumentException>(() => large.Scale(1000, 4, 90));
            Assert.Throws<ArgumentException>(() => large.With(hp: 61));
            Assert.Throws<ArgumentException>(() => large.With(interval: .1f));
            Assert.Throws<ArgumentException>(() => ScenarioConfiguration.Named(ScenarioKind.WeaponStack).Scale(35, 8, 90));
            Assert.Throws<ArgumentException>(() => ScenarioConfiguration.Named(ScenarioKind.SharedRifle).With(count: 250));
            var parsed = ScenarioConfiguration.FromArguments(new[] { "-scenario", "co-op-scale", "-scenarioEnemies", "1000", "-scaleWeapons", "8", "-scenarioDuration", "300" }, ScenarioKind.SoloPrecision);
            Assert.That(parsed.Identity, Is.EqualTo(large.Scale(1000, 8, 300).Identity));
        }
        [Test] public void EightInstancesKeepOwnershipArchetypeAndIndependentCadence()
        {
            var game = new SharedEncounter(ScenarioConfiguration.Named(ScenarioKind.CoopScale).Scale(35, 8, 2));
            game.Start(); game.Step(); var hits = new List<SharedHit>(); game.Resolved += hits.Add;
            int sequence = 0;
            for (int instance = 0; instance < 8; instance++)
            {
                int id = WeaponArsenal.Id(0, instance);
                Assert.That(WeaponArsenal.Owns(game.Current.Players[0], 0, id), Is.True);
                Assert.That(WeaponArsenal.Owns(game.Current.Players[0], 0, id + 1), Is.False);
                Assert.That(WeaponArsenal.Instance(id), Is.EqualTo(instance));
                Assert.That(WeaponArsenal.Slot(id), Is.EqualTo((WeaponSlot)(instance % 3)));
                game.Submit(new SharedShot { Run = 1, Player = 0, Life = 1, Weapon = id, Event = ++sequence,
                    PoseTick = game.Current.Tick, Ray = new Ray(game.Current.Players[0].Position + Vector3.up * 1.65f, Vector3.up) });
            }
            game.Step(); Assert.That(game.Current.Players[0].Shots, Is.EqualTo(8));
            foreach (int id in new[] { 1, 7, 13 })
                game.Submit(new SharedShot { Run = 1, Player = 0, Life = 1, Weapon = id, Event = ++sequence,
                    PoseTick = game.Current.Tick, Ray = new Ray(game.Current.Players[0].Position + Vector3.up * 1.65f, Vector3.up) });
            for (int i = 0; i < 10; i++) game.Step();
            Assert.That(game.Current.Players[0].Shots, Is.EqualTo(11), "Repeated rifles must not share the primary cooldown.");
            int rays = 0, triggers = 0;
            foreach (var hit in hits) { rays += hit.Rays; if (hit.Trigger) triggers++; }
            Assert.That(rays, Is.EqualTo(23)); Assert.That(triggers, Is.EqualTo(11));
            Assert.That(WeaponArsenal.Owns(game.Current.Players[0], 0, 17), Is.False);
            game.Current.State = SharedState.Complete; game.Retry(1, 0); game.Retry(1, 1);
            Assert.That(game.Current.Players[0].Arsenal, Is.EqualTo(255)); Assert.That(game.Current.Players[0].Shots, Is.Zero);
            game.Submit(new SharedShot { Run = 1, Player = 0, Life = 1, Weapon = 15, Event = ++sequence });
            Assert.That(hits[hits.Count - 1].Decision, Is.EqualTo(ShotDecision.StaleRun));
        }
        [Test] public void DiagnosticXpUsesConservedLedgerWithoutOffersAndStillTestsContacts()
        {
            var game = new SharedEncounter(ScenarioConfiguration.Named(ScenarioKind.CoopScale).Scale(35, 8, 2));
            game.Start(); var replica = new OrbReplica(); replica.Reset(1); game.Growth.Changed += e => replica.Apply(e);
            // Synthetic deaths are a rule fixture, never injected into native measurements.
            for (int i = 1; i <= 40; i++)
                game.Growth.Death(game.Current, new SharedHit(new SharedShot { Run = 1, Player = 0, Weapon = 1, Event = i },
                    ShotDecision.Damage, new ShotResult(game.Current.Players[0].Position, 0, i, 1, 0), game.Current.Players[0].Position));
            game.Current.Enemies[0].Position = game.Current.Players[0].Position;
            game.Step();
            Assert.That(game.Current.State, Is.EqualTo(SharedState.Running)); Assert.That(game.Current.Level, Is.EqualTo(1));
            Assert.That(game.Current.Xp, Is.EqualTo(40)); Assert.That(game.Growth.EarnedXp, Is.EqualTo(40));
            Assert.That(replica.UncollectedXp, Is.Zero); Assert.That(replica.Drops, Is.EqualTo(40));
            Assert.That(game.Current.Players[0].Offer.Id, Is.Zero); Assert.That(game.Current.Players[0].Health, Is.EqualTo(100));
            Assert.That(game.Work.contactTests, Is.EqualTo(70)); Assert.That(game.Work.contactApplications, Is.GreaterThan(0));
            Assert.That(game.Work.steeringChecks, Is.EqualTo(35 * 35));
        }
        [Test] public void ChunkedSnapshotIsBoundedAtomicAndSurvivesRetryGeneration()
        {
            var game = new SharedEncounter(ScenarioConfiguration.Named(ScenarioKind.CoopScale).Scale(1000, 8, 90));
            game.Start(); game.Current.Tick = 123; game.Current.ControlRevision = 4;
            byte[] data = Snapshot(game.Current); Assert.That(data.Length, Is.GreaterThan(SharedConnection.MaxPacket));
            var assembly = new SnapshotAssembly(); var destination = new SharedSnapshot();
            for (int offset = 0; offset < data.Length; offset += SnapshotAssembly.ChunkBytes)
            using (var reader = Chunk(data, offset))
            {
                Assert.That(reader.BaseStream.Length + 1, Is.LessThanOrEqualTo(SharedConnection.MaxPacket));
                bool complete = assembly.Read(reader, destination, 1);
                Assert.That(complete, Is.EqualTo(offset + SnapshotAssembly.ChunkBytes >= data.Length));
                if (!complete) Assert.That(destination.Run, Is.Zero);
            }
            Assert.That(destination.Enemies, Is.EqualTo(game.Current.Enemies));
            Assert.That(destination.Players, Is.EqualTo(game.Current.Players)); Assert.That(destination.Tick, Is.EqualTo(123));
            Assert.That(assembly.Pending, Is.False);
            game.Current.State = SharedState.Complete; game.Retry(1, 0); game.Retry(1, 1); data = Snapshot(game.Current);
            for (int offset = 0; offset < data.Length; offset += SnapshotAssembly.ChunkBytes)
                using (var reader = Chunk(data, offset, 2)) assembly.Read(reader, destination, 2);
            Assert.That(destination.Run, Is.EqualTo(2)); Assert.That(destination.Tick, Is.Zero);
        }
        [Test] public void ChunkForgeryDuplicatesGapsAndMalformedCompletionCannotMutateDisplayedState()
        {
            var game = new SharedEncounter(ScenarioConfiguration.Named(ScenarioKind.CoopScale).Scale(1000, 8, 90)); game.Start();
            byte[] data = Snapshot(game.Current); var destination = new SharedSnapshot();
            var assembly = new SnapshotAssembly();
            using (var r = Chunk(data, 0, length: SnapshotAssembly.MaxBytes + 1)) Assert.Throws<ArgumentException>(() => assembly.Read(r, destination, 1));
            using (var r = Chunk(data, SnapshotAssembly.ChunkBytes)) Assert.Throws<ArgumentException>(() => assembly.Read(r, destination, 1));
            using (var r = Chunk(data, 0)) Assert.That(assembly.Read(r, destination, 1), Is.False);
            Assert.That(assembly.AgeMs(2), Is.EqualTo(1000));
            using (var r = Chunk(data, 0)) Assert.Throws<ArgumentException>(() => assembly.Read(r, destination, 2));
            using (var r = Chunk(data, SnapshotAssembly.ChunkBytes, 2)) Assert.Throws<ArgumentException>(() => assembly.Read(r, destination, 2));
            Assert.That(destination.Run, Is.Zero);
            byte[] malformed = Snapshot(new SharedSnapshot()); Array.Resize(ref malformed, malformed.Length + 1);
            using (var r = Chunk(malformed, 0)) Assert.Throws<ArgumentException>(() => new SnapshotAssembly().Read(r, destination, 1));
            Assert.That(destination.Run, Is.Zero); Assert.That(destination.Enemies.Length, Is.EqualTo(12));
        }
    }
}
