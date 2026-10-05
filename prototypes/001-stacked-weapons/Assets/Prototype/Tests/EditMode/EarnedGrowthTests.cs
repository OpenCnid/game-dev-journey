using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public class EarnedGrowthTests
    {
        SharedEncounter game;
        List<GrowthEvent> events;
        [SetUp] public void Setup()
        {
            game = new SharedEncounter(ScenarioConfiguration.Named(ScenarioKind.EarnedGrowth)); game.Start();
            events = new List<GrowthEvent>(); game.Growth.Changed += events.Add;
            for (int i = 0; i < 35; i++) game.Current.Enemies[i].Position = new Vector3(28, 0, 28);
        }
        SharedHit Death(int incarnation, Vector3 position) => new SharedHit(new SharedShot { Run = game.Current.Run, Player = 1, Weapon = 2, Event = incarnation },
            ShotDecision.Damage, new ShotResult(position, 0, incarnation, HitRegion.Head, 40, 20, 20, 0), position);
        void Earn()
        {
            for (int i = 1; i <= 10; i++) game.Growth.Death(game.Current, Death(i, SharedEncounter.Spawn(0)));
            game.Step();
        }
        GrowthChoice Choice(int p, UpgradeId upgrade) => new GrowthChoice { Run = game.Current.Run, Player = p,
            Offer = game.Current.Players[p].Offer.Id, Weapon = p + 1, Upgrade = upgrade };
        SharedShot Shot(int p, int id, bool head = false)
        {
            Vector3 origin = game.Current.Players[p].Position + Vector3.up * 1.65f;
            SharedEnemy enemy = game.Current.Enemies[0];
            Vector3 target = (head ? EnemyGeometry.Head(enemy.Position, enemy.Health < 60) : EnemyGeometry.Body(enemy.Position, enemy.Health < 60)).MultiplyPoint3x4(Vector3.zero);
            return new SharedShot { Run = game.Current.Run, Player = p, Weapon = p + 1, Life = game.Current.Players[p].Life,
                Event = id, PoseTick = game.Current.Tick, Ray = new Ray(origin, (target - origin).normalized) };
        }
        [Test] public void DeathCreatesOnceBeforeReuseButDoesNotCreditXp()
        {
            game.Current.Enemies[0].Position = new Vector3(0, 0, 8); game.Step();
            game.Submit(Shot(0, 1, true)); game.Submit(Shot(1, 1, true)); game.Step();
            Assert.That(game.Growth.Orbs.Count, Is.EqualTo(1)); Assert.That(game.Current.Xp, Is.Zero);
            GrowthEvent created = events[0]; Assert.That(created.Orb.Incarnation, Is.EqualTo(1));
            Assert.That(created.Orb.Position.z, Is.LessThan(9)); Assert.That(created.Orb.SourcePlayer, Is.EqualTo(1));
            Assert.That(game.Growth.Death(game.Current, Death(1, Vector3.zero)), Is.False);
            Assert.That(game.Current.Enemies[0].Incarnation, Is.EqualTo(2));
        }
        [Test] public void InclusiveRadiusStableCollectorAndDeadExclusion()
        {
            game.Current.Players[1].Position = game.Current.Players[0].Position;
            game.Growth.Death(game.Current, Death(1, SharedEncounter.Spawn(0) + Vector3.forward * 2)); game.Step();
            Assert.That(game.Current.Xp, Is.EqualTo(1)); Assert.That(events[1].Player, Is.Zero);
            game.Current.Players[0].Health = 0; game.Current.Players[0].ReturnAt = 50;
            game.Growth.Death(game.Current, Death(2, SharedEncounter.Spawn(0))); game.Step();
            Assert.That(events[3].Player, Is.EqualTo(1)); game.Step(); Assert.That(game.Current.Xp, Is.EqualTo(2));
            game.Growth.Death(game.Current, Death(3, SharedEncounter.Spawn(0) + Vector3.forward * 2.001f)); game.Step();
            Assert.That(game.Current.Xp, Is.EqualTo(2)); Assert.That(game.Growth.Orbs.Count, Is.EqualTo(1));
        }
        [Test] public void FixedRosterEarnsOneOfferIncludingDeadMemberAndXpContinuesAtCap()
        {
            game.Current.Players[0].Health = 0; game.Current.Players[0].ReturnAt = 50;
            game.Current.Players[1].Position = SharedEncounter.Spawn(0); Earn();
            Assert.That(game.Current.Level, Is.EqualTo(2));
            Assert.That(game.Current.Players[0].Offer.Pending, Is.True); Assert.That(game.Current.Players[1].Offer.Pending, Is.True);
            Assert.That(game.Current.Players[0].Offer.Id, Is.Not.EqualTo(game.Current.Players[1].Offer.Id));
            game.Growth.Death(game.Current, Death(11, SharedEncounter.Spawn(0))); game.Step();
            Assert.That(game.Current.Xp, Is.EqualTo(11)); Assert.That(events.FindAll(e => e.Kind == GrowthEventKind.Entitlement).Count, Is.EqualTo(2));
        }
        [Test] public void IndependentExplicitChoicesApplyNextTickOnceAndKeepOtherOffer()
        {
            Earn(); PersonalOffer b = game.Current.Players[1].Offer;
            Assert.That(game.Select(Choice(0, UpgradeId.StrongerHits)), Is.True);
            Assert.That(game.Select(Choice(0, UpgradeId.FasterFire)), Is.False);
            Assert.That(game.Current.Players[0].BodyDamage, Is.EqualTo(20)); game.Step();
            Assert.That(game.Current.Players[0].BodyDamage, Is.EqualTo(30)); Assert.That(game.Current.Players[0].RifleInterval, Is.EqualTo(.12f));
            Assert.That(game.Current.Players[1].Offer, Is.EqualTo(b));
            Assert.That(game.Select(Choice(0, UpgradeId.StrongerHits)), Is.False);
            Assert.That(game.Select(Choice(1, UpgradeId.FasterFire)), Is.True); game.Step();
            Assert.That(game.Current.Players[1].BodyDamage, Is.EqualTo(20)); Assert.That(game.Current.Players[1].RifleInterval, Is.EqualTo(.08f));
            Assert.That(events.FindAll(e => e.Kind == GrowthEventKind.Applied).Count, Is.EqualTo(2));
        }
        [Test] public void SelectionRevalidatesWeaponWithoutSpendingEntitlement()
        {
            Earn(); Assert.That(game.Select(Choice(0, UpgradeId.StrongerHits)), Is.True);
            game.Current.Players[0].Weapon = 99; game.Step(); Assert.That(game.Current.Players[0].Offer.Pending, Is.True);
            Assert.That(game.Current.Players[0].BodyDamage, Is.EqualTo(20));
        }
        [TestCase(false, UpgradeId.None, 3)]
        [TestCase(true, UpgradeId.None, 2)]
        [TestCase(false, UpgradeId.StrongerHits, 2)]
        [TestCase(true, UpgradeId.StrongerHits, 1)]
        [TestCase(false, UpgradeId.FasterFire, 3)]
        [TestCase(true, UpgradeId.FasterFire, 2)]
        public void DamageThresholdsUseEarnedBuild(bool head, UpgradeId upgrade, int required)
        {
            Earn(); if (upgrade != UpgradeId.None) { game.Select(Choice(0, upgrade)); game.Step(); }
            game.Current.Enemies[0].Position = new Vector3(0, 0, 8); game.Step();
            var hits = new List<SharedHit>(); game.Resolved += hits.Add;
            for (int i = 1; i <= required; i++)
            {
                game.Submit(Shot(0, i, head)); for (int t = 0; t < 8; t++) game.Step();
                Assert.That(game.Current.Players[0].Kills, Is.EqualTo(i == required ? 1 : 0));
            }
            Assert.That(hits.Count, Is.EqualTo(required)); Assert.That(hits[0].Shot.RequestedDamage, Is.EqualTo(GrowthUpgrade.Damage(upgrade) * (head ? 2 : 1)));
        }
        [Test] public void FasterFirePreservesFractionalCooldownWithoutRetroactiveBurst()
        {
            Earn(); game.Current.Enemies[0].Position = new Vector3(0, 0, 8); game.Step();
            game.Submit(Shot(0, 1)); game.Step(); game.Submit(Shot(0, 2)); game.Step();
            game.Select(Choice(0, UpgradeId.FasterFire)); game.Step(); Assert.That(game.Current.Players[0].Shots, Is.EqualTo(1));
            game.Step(); game.Step(); Assert.That(game.Current.Players[0].Shots, Is.EqualTo(1));
            game.Step(); Assert.That(game.Current.Players[0].Shots, Is.EqualTo(2));
        }
        [Test] public void DeadSelectionRetainsBuildOnReturnAndTerminalTickSuppressesCollection()
        {
            Earn(); game.Current.Players[0].Health = 0; game.Current.Players[0].ReturnAt = game.Current.Elapsed + .1f;
            Assert.That(game.Select(Choice(0, UpgradeId.StrongerHits)), Is.True);
            for (int i = 0; i < 7; i++) game.Step();
            Assert.That(game.Current.Players[0].Health, Is.EqualTo(100)); Assert.That(game.Current.Players[0].BodyDamage, Is.EqualTo(30));
            game.Growth.Death(game.Current, Death(11, SharedEncounter.Spawn(1))); game.Current.Players[0].Health = 0;
            game.Current.Players[0].ReturnAt = 60; game.Current.Players[1].Health = 10; game.Current.Tick = 3599;
            game.Current.Enemies[0].Position = SharedEncounter.Spawn(1); game.Step();
            Assert.That(game.Current.State, Is.EqualTo(SharedState.Defeat)); Assert.That(game.Current.Xp, Is.EqualTo(10));
            Assert.That(game.Select(Choice(1, UpgradeId.FasterFire)), Is.False);
        }
        [Test] public void RetryResetsAllGrowthAndDrawsAndRejectsOldChoice()
        {
            Earn(); PersonalOffer a = game.Current.Players[0].Offer, b = game.Current.Players[1].Offer;
            GrowthChoice stale = Choice(1, UpgradeId.FasterFire); game.Select(Choice(0, UpgradeId.StrongerHits)); game.Step();
            game.Growth.Death(game.Current, Death(11, Vector3.zero)); game.Current.Tick = 3599; game.Step();
            int run = game.Current.Run; game.Retry(run, 0); game.Retry(run, 1);
            Assert.That(game.Current.Run, Is.EqualTo(run + 1)); Assert.That(game.Current.Xp, Is.Zero); Assert.That(game.Current.Level, Is.EqualTo(1));
            Assert.That(game.Growth.Orbs.Count, Is.Zero); Assert.That(game.Select(stale), Is.False);
            foreach (SharedPlayer player in game.Current.Players) { Assert.That(player.Offer.Id, Is.Zero); Assert.That(player.BodyDamage, Is.EqualTo(20)); Assert.That(player.RifleInterval, Is.EqualTo(.12f)); }
            Earn(); Assert.That(game.Current.Players[0].Offer.First, Is.EqualTo(a.First)); Assert.That(game.Current.Players[1].Offer.First, Is.EqualTo(b.First));
        }
        [Test] public void SoloUsesSameThresholdAndOneClickRetry()
        {
            game = new SharedEncounter(ScenarioConfiguration.Named(ScenarioKind.EarnedGrowth), roster:1); game.Start(); Earn();
            Assert.That(game.Current.Players[0].Offer.Pending, Is.True); Assert.That(game.Current.Players[1].Offer.Id, Is.Zero);
            Assert.That(game.Current.Players[0].Position, Is.EqualTo(Vector3.zero)); game.Current.Players[0].Health = 0; game.Step();
            Assert.That(game.Current.State, Is.EqualTo(SharedState.Defeat)); Assert.That(game.Retry(1, 0), Is.True); Assert.That(game.Current.Run, Is.EqualTo(2));
        }
        [Test] public void LedgerExceedsPacketAndReliableRecordsAreBoundedIdempotentAndResettable()
        {
            var replica = new OrbReplica(); replica.Reset(game.Current.Run); game.Growth.Changed += e => {
                using (var stream = new MemoryStream())
                { var w = new BinaryWriter(stream); e.Write(w); Assert.That(stream.Length + 1, Is.LessThan(SharedConnection.MaxPacket));
                  stream.Position = 0; GrowthEvent read = GrowthEvent.Read(new BinaryReader(stream)); Assert.That(replica.Apply(read), Is.True); Assert.That(replica.Apply(read), Is.False); }
            };
            for (int i = 1; i <= 1502; i++) game.Growth.Death(game.Current, Death(i, Vector3.forward * 25));
            Assert.That(game.Growth.Orbs.Count, Is.EqualTo(1502)); Assert.That(replica.Orbs.Count, Is.EqualTo(1502));
            replica.Reset(game.Current.Run + 1); Assert.That(replica.Apply(events[0]), Is.False); Assert.That(replica.Orbs.Count, Is.Zero);
        }
        [Test] public void FrozenStartingConfigAcceptsLegalEarnedSnapshotRejectsForgedStats()
        {
            Earn(); game.Select(Choice(0, UpgradeId.StrongerHits)); game.Step(); var binding = new SharedScenarioBinding(game.Configuration);
            using (var stream = new MemoryStream()) { var w = new BinaryWriter(stream); binding.Write(w); stream.Position = 0; binding.AcceptAcknowledgement(new BinaryReader(stream)); }
            game.Current.Session = binding.Session; var clone = new SharedSnapshot();
            using (var stream = new MemoryStream()) { var w = new BinaryWriter(stream); SharedConnection.WriteSnapshot(w, game.Current);
                Assert.That(stream.Length + 1, Is.LessThanOrEqualTo(SharedConnection.MaxPacket)); stream.Position = 0; SharedConnection.ReadSnapshot(new BinaryReader(stream), clone); }
            Assert.That(binding.CanAdopt(clone, new SharedSnapshot(game.Configuration)), Is.True); Assert.That(clone.Configuration.BodyDamage, Is.EqualTo(20));
            clone.Players[0].BodyDamage = 31; Assert.Throws<ArgumentException>(() => binding.CanAdopt(clone, game.Current));
        }
        [Test] public void GrowthSettingsCannotBeOverriddenOrRateEdited()
        {
            Assert.Throws<ArgumentException>(() => ScenarioConfiguration.Named(ScenarioKind.EarnedGrowth).With(interval:.1f));
            Assert.Throws<ArgumentException>(() => ScenarioConfiguration.FromArguments(new[] { "-scenario", "earned-growth", "-scenarioHp", "40" }, ScenarioKind.SharedRifle));
        }
    }
}
