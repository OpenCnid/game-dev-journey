using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public class WeaponStackTests
    {
        SharedEncounter game;
        readonly List<SharedHit> hits = new List<SharedHit>();
        readonly List<GrowthEvent> growth = new List<GrowthEvent>();
        void Start(string reference = null)
        {
            var config = ScenarioConfiguration.Named(reference == null ? ScenarioKind.WeaponStack : ScenarioKind.StackReference);
            if (reference != null) config = config.Reference(reference);
            game = new SharedEncounter(config); game.Start(); hits.Clear(); growth.Clear(); game.Resolved += hits.Add;
            if (game.Growth != null) game.Growth.Changed += growth.Add;
            for (int i = 0; i < game.Current.Enemies.Length; i++) game.Current.Enemies[i].Position = new Vector3(28, 0, 28);
            game.Step();
        }
        SharedShot Shot(int id, int weapon, float y = .8f) => new SharedShot { Run = game.Current.Run, Player = 0, Life = game.Current.Players[0].Life,
            Event = id, Weapon = weapon, PoseTick = game.Current.Tick, Ray = new Ray(game.Current.Players[0].Position + Vector3.up * y, Vector3.forward) };
        void Earn(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                Vector3 p = game.Current.Players[1].Position;
                game.Growth.Death(game.Current, new SharedHit(new SharedShot { Run = game.Current.Run, Player = 1, Weapon = 2, Event = i },
                    ShotDecision.Damage, new ShotResult(p, 0, i, 1, 0), p));
            }
            game.Step();
        }
        GrowthChoice Choice(int player, UpgradeId upgrade) => new GrowthChoice { Run = game.Current.Run, Player = player,
            Offer = game.Current.Players[player].Offer.Id, Weapon = player + 1, Upgrade = upgrade };
        void SelectBoth(UpgradeId a, UpgradeId b)
        {
            Assert.That(game.Select(Choice(0, a)), Is.True); Assert.That(game.Select(Choice(1, b)), Is.True);
            int tick = game.Current.Tick; game.CommitChoices(); Assert.That(game.Current.Tick, Is.EqualTo(tick));
            Assert.That(game.ContinueChoice(game.Current.Run, game.Current.ChoicePhase, 0), Is.True);
            Assert.That(game.Current.State, Is.EqualTo(SharedState.Choosing));
            Assert.That(game.ContinueChoice(game.Current.Run, game.Current.ChoicePhase, 1), Is.True);
        }
        [Test] public void ReferenceLoadoutsAreFrozenDistinctAndCannotEnterEarnedScenario()
        {
            var baseline = ScenarioConfiguration.Named(ScenarioKind.StackReference);
            var all = baseline.Reference("all"); var specialized = baseline.Reference("scatter");
            Assert.That(all.Identity, Is.Not.EqualTo(baseline.Identity)); Assert.That(specialized.Identity, Is.Not.EqualTo(all.Identity));
            Assert.That(all.Growth, Is.False); Assert.That(all.StartingArsenal, Is.EqualTo(7));
            Assert.Throws<ArgumentException>(() => ScenarioConfiguration.FromArguments(new[] { "-scenario", "weapon-stack", "-stackLoadout", "all" }, ScenarioKind.SharedRifle));
            Assert.Throws<ArgumentException>(() => all.With(hp: 61));
            using (var stream = new MemoryStream())
            { all.Write(new BinaryWriter(stream)); stream.Position = 0; Assert.That(ScenarioConfiguration.Read(new BinaryReader(stream)).Identity, Is.EqualTo(all.Identity)); }
            Start("all"); Assert.That(game.Growth, Is.Null); Assert.That(game.Current.Level, Is.EqualTo(1));
        }
        [Test] public void InstalledSlotsHaveIndependentCooldownsAndRejectForeignOrAbsentIds()
        {
            Start("all"); game.Submit(Shot(1, 1)); game.Submit(Shot(2, 3)); game.Submit(Shot(3, 5)); game.Step();
            Assert.That(game.Current.Players[0].Shots, Is.EqualTo(3));
            game.Submit(Shot(4, 1));
            for (int i = 0; i < 9; i++) game.Step();
            Assert.That(game.Current.Players[0].Shots, Is.EqualTo(4));
            for (int i = 0; i < 27; i++) game.Step();
            game.Submit(Shot(5, 5)); for (int i = 0; i < 4; i++) game.Step();
            Assert.That(hits.FindAll(h => h.Trigger && h.Source.Weapon == 5).Count, Is.EqualTo(2));
            Assert.That(hits.FindAll(h => h.Trigger && h.Source.Weapon == 3).Count, Is.EqualTo(1));
            game.Submit(Shot(6, 3)); for (int i = 0; i < 8; i++) game.Step();
            Assert.That(hits.FindAll(h => h.Trigger && h.Source.Weapon == 3).Count, Is.EqualTo(2));
            game.Submit(Shot(7, 4)); Assert.That(hits[hits.Count - 1].Decision, Is.EqualTo(ShotDecision.Invalid));
            Start("rifle"); game.Submit(Shot(1, 3)); Assert.That(hits[0].Decision, Is.EqualTo(ShotDecision.Invalid));
        }
        [TestCase("all", .8f, 3, 16)]
        [TestCase("pierce", .8f, 5, 16)]
        [TestCase("all", 1.6f, 3, 32)]
        public void PiercingHitsNearestDistinctEnemiesOnceWithBoundedDepth(string reference, float y, int count, int damage)
        {
            Start(reference);
            // Reverse slot/distance order; body and head of the same enemy must not count twice.
            for (int i = 0; i < 5; i++) game.Current.Enemies[i].Position = game.Current.Players[0].Position + Vector3.forward * (14 - i * 2);
            game.Step(); game.Submit(Shot(1, 5, y)); game.Step();
            var damaged = hits.FindAll(h => h.Decision == ShotDecision.Damage);
            Assert.That(damaged.Count, Is.EqualTo(count)); Assert.That(damaged[0].Shot.EnemyIndex, Is.EqualTo(4));
            var indices = new HashSet<int>(); foreach (SharedHit hit in damaged) { Assert.That(indices.Add(hit.Shot.EnemyIndex), Is.True); Assert.That(hit.Shot.RequestedDamage, Is.EqualTo(damage)); }
            Assert.That(game.Current.Players[0].Shots, Is.EqualTo(1)); Assert.That(game.Current.Players[0].Damage, Is.EqualTo(count * damage));
            Assert.That(damaged.FindAll(h => h.Trigger).Count, Is.EqualTo(1)); Assert.That(damaged[0].Rays, Is.EqualTo(1));
        }
        [TestCase("all", 5)] [TestCase("scatter", 7)]
        public void ScatterUsesHorizontalFanAndExactRangeWithoutPerPelletTriggers(string reference, int rays)
        {
            Start(reference); var center = Shot(1, 3).Ray;
            Assert.That(Vector3.SignedAngle(center.direction, WeaponArsenal.Fan(center, 0, rays).direction, Vector3.up), Is.EqualTo(-8).Within(.01));
            Assert.That(Vector3.SignedAngle(center.direction, WeaponArsenal.Fan(center, rays - 1, rays).direction, Vector3.up), Is.EqualTo(8).Within(.01));
            game.Current.Players[0].Position += Vector3.back * 10;
            game.Current.Enemies[0].Position = game.Current.Players[0].Position + Vector3.forward * 32;
            game.Step(); game.Submit(Shot(1, 3)); game.Step();
            Assert.That(game.Current.Players[0].Shots, Is.EqualTo(1)); Assert.That(game.Current.Players[0].Damage, Is.Zero);
            Assert.That(hits.Count, Is.EqualTo(rays)); Assert.That(hits[0].Rays, Is.EqualTo(rays)); Assert.That(hits[0].TargetTests, Is.EqualTo(rays * 35));
        }
        [Test] public void ScatterCannotDamageRespawnFromEarlierPelletAndCreatesOneDeathXp()
        {
            Start(); game.Current.Players[0].Arsenal = 3; // Diagnostic rule fixture, never a playable grant.
            game.Current.Enemies[0].Position = game.Current.Players[0].Position + Vector3.forward * 2;
            game.Current.Enemies[0].Health = 12; game.Step(); game.Submit(Shot(1, 3)); game.Step();
            Assert.That(game.Current.Players[0].Kills, Is.EqualTo(1)); Assert.That(game.Current.Enemies[0].Incarnation, Is.EqualTo(2));
            Assert.That(game.Current.Enemies[0].Health, Is.EqualTo(60)); Assert.That(growth.FindAll(e => e.Kind == GrowthEventKind.DeathOrb).Count, Is.EqualTo(1));
            Assert.That(hits.FindAll(h => h.Decision == ShotDecision.StaleEnemy).Count, Is.GreaterThan(0));
            Assert.That(game.Growth.EarnedXp, Is.EqualTo(1));
        }
        [Test] public void BankedXpGrantsTwoSeparateFrozenStagesWithIndependentBroadAndSpecializedBuilds()
        {
            Start(); Earn(25); Assert.That(game.Current.Level, Is.EqualTo(2)); Assert.That(game.Current.Xp, Is.EqualTo(25));
            Assert.That(game.Select(Choice(0, UpgradeId.StrongerHits)), Is.False); int tick = game.Current.Tick;
            for (int i = 0; i < 120; i++) game.Step(); Assert.That(game.Current.Tick, Is.EqualTo(tick));
            SelectBoth(UpgradeId.AddScattergun, UpgradeId.AddPiercer); game.Step();
            Assert.That(game.Current.Level, Is.EqualTo(3)); Assert.That(game.Current.ChoicePhase, Is.EqualTo(2));
            Assert.That(game.Current.Players[0].ChoiceReady, Is.False); Assert.That(game.Current.Players[1].ChoiceReady, Is.False);
            Assert.That(game.Select(Choice(0, UpgradeId.DeeperPierce)), Is.False);
            SelectBoth(UpgradeId.AddPiercer, UpgradeId.DeeperPierce);
            Assert.That(game.Current.Players[0].Arsenal, Is.EqualTo(7)); Assert.That(game.Current.Players[0].Specialization, Is.EqualTo(UpgradeId.None));
            Assert.That(game.Current.Players[1].Arsenal, Is.EqualTo(5)); Assert.That(game.Current.Players[1].Specialization, Is.EqualTo(UpgradeId.DeeperPierce));
            Assert.That(WeaponArsenal.ValidBuild(game.Current, 0), Is.True); Assert.That(WeaponArsenal.ValidBuild(game.Current, 1), Is.True);
            Assert.That(growth.FindAll(e => e.Kind == GrowthEventKind.Entitlement).Count, Is.EqualTo(4));
            game.Step(); Assert.That(game.Current.State, Is.EqualTo(SharedState.Running));
        }
        [Test] public void DeadMemberKeepsTwoStageChoicesAndArsenalOnReturnThenRetryClearsAllSlots()
        {
            Start(); game.Current.Players[0].Health = 0; game.Current.Players[0].ReturnAt = .15f;
            Earn(25); SelectBoth(UpgradeId.AddScattergun, UpgradeId.AddPiercer); game.Step();
            SelectBoth(UpgradeId.WiderScatter, UpgradeId.AddScattergun);
            for (int i = 0; i < 10; i++) game.Step();
            Assert.That(game.Current.Players[0].Health, Is.EqualTo(100)); Assert.That(game.Current.Players[0].Arsenal, Is.EqualTo(3));
            Assert.That(game.Current.Players[0].Specialization, Is.EqualTo(UpgradeId.WiderScatter));
            int oldRun = game.Current.Run; game.Current.State = SharedState.Defeat; game.Retry(oldRun, 0); game.Retry(oldRun, 1);
            foreach (SharedPlayer player in game.Current.Players) { Assert.That(player.Arsenal, Is.EqualTo(1)); Assert.That(player.FirstAddition, Is.EqualTo(UpgradeId.None)); Assert.That(player.Specialization, Is.EqualTo(UpgradeId.None)); Assert.That(player.Offer.Id, Is.Zero); }
            var stale = Shot(1, 3); stale.Run = oldRun; game.Submit(stale); Assert.That(hits[hits.Count - 1].Decision, Is.EqualTo(ShotDecision.StaleRun));
            Assert.That(game.ContinueChoice(oldRun, 2, 0), Is.False);
        }
        [Test] public void StackSnapshotsAndMultiTargetHitsRoundTripUnderPacketLimitAndRejectForgedBuilds()
        {
            Start(); Earn(25); SelectBoth(UpgradeId.AddScattergun, UpgradeId.AddPiercer); game.Step(); SelectBoth(UpgradeId.WiderScatter, UpgradeId.AddScattergun);
            var copy = new SharedSnapshot();
            using (var stream = new MemoryStream())
            { SharedConnection.WriteSnapshot(new BinaryWriter(stream), game.Current); Assert.That(stream.Length + 1, Is.LessThanOrEqualTo(1100)); stream.Position = 0; SharedConnection.ReadSnapshot(new BinaryReader(stream), copy); }
            Assert.That(copy.Players[0], Is.EqualTo(game.Current.Players[0])); Assert.That(copy.Players[1], Is.EqualTo(game.Current.Players[1]));
            Assert.That(WeaponArsenal.ValidBuild(copy, 0), Is.True); copy.Players[0].Arsenal = 7; Assert.That(WeaponArsenal.ValidBuild(copy, 0), Is.False);
            Start("all"); game.Submit(Shot(1, 5)); game.Step(); SharedHit hit = hits[0];
            using (var stream = new MemoryStream())
            { SharedConnection.WriteHit(new BinaryWriter(stream), hit); Assert.That(stream.Length + 1, Is.LessThanOrEqualTo(1100)); stream.Position = 0; SharedHit read = SharedConnection.ReadHit(new BinaryReader(stream)); Assert.That(read.Rays, Is.EqualTo(1)); Assert.That(read.TargetTests, Is.EqualTo(35)); Assert.That(read.Trigger, Is.True); }
        }
    }
}
