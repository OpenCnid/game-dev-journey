using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public sealed class RangedThreatsTests
    {
        static SharedEncounter Fixture(int roster = 1)
        {
            var g = new SharedEncounter(ScenarioConfiguration.Named(ScenarioKind.MixedThreats), roster: roster); g.Start();
            for (int i = 0; i < g.Current.Enemies.Length; i++) g.Current.Enemies[i].Position = new Vector3(28, 0, 28);
            for (int i = 0; i < 5; i++) g.Current.Ranged.Attacks[i].Until = 500;
            g.Current.Enemies[0].Position = new Vector3(0, 0, 10); g.Current.Ranged.Attacks[0].Until = 0;
            return g;
        }
        static void Steps(SharedEncounter g, int ticks) { for (int i = 0; i < ticks; i++) g.Step(); }
        static void Bolt(SharedEncounter g, Vector3 start, Vector3 direction, int lifeA = 1, int lifeB = 1)
        {
            RangedState s = g.Current.Ranged;
            s.Bolts[s.Count++] = new EnemyBolt { Position = start, Direction = direction, Id = ++s.Fired,
                Remaining = RangedThreats.Lifetime, Source = 0, Incarnation = 1, LifeA = lifeA, LifeB = lifeB };
        }
        [Test] public void MixedDefinitionKeepsEarnedArsenalAndPureChaserReference()
        {
            var c = ScenarioConfiguration.FromArguments(new[] { "-scenario", "mixed-threats" }, ScenarioKind.SharedRifle);
            Assert.That(c.RangedCount, Is.EqualTo(5)); Assert.That(c.EnemyCount, Is.EqualTo(35)); Assert.That(c.Duration, Is.EqualTo(120));
            Assert.That(c.Growth && c.StackedGrowth && c.SharedSolo, Is.True); Assert.That(c.LevelCap, Is.EqualTo(3));
            Assert.Throws<ArgumentException>(() => c.With(count: 12));
            Assert.That(new SharedEncounter(ScenarioConfiguration.Named(ScenarioKind.WeaponStack)).Current.Ranged, Is.Null);
            var g = Fixture();
            for (int i = 0; i < 10; i++)
                g.Growth.Death(g.Current, new SharedHit(new SharedShot { Run = 1, Player = 0, Weapon = 1, Event = i + 1 }, ShotDecision.Damage,
                    new ShotResult(Vector3.zero, 0, i + 1, HitRegion.Body, 60, 60, 60, 0), Vector3.zero));
            g.Step(); Assert.That(g.Current.State, Is.EqualTo(SharedState.Choosing));
            PersonalOffer offer = g.Current.Players[0].Offer;
            Assert.That(offer.Contains(UpgradeId.AddScattergun) && offer.Contains(UpgradeId.AddPiercer), Is.True);
            Assert.That(g.Select(new GrowthChoice { Run = 1, Player = 0, Weapon = 1, Offer = offer.Id, Upgrade = UpgradeId.AddPiercer }), Is.True);
            g.CommitChoices(); Assert.That(g.Current.Players[0].Arsenal, Is.EqualTo(5));
            Assert.That(g.ContinueChoice(1, 1, 0), Is.True);
        }
        [Test] public void WindupLocksAimAndPositionThenMovingPlayerDodgesStraightBolt()
        {
            var g = Fixture(); g.Step();
            RangedAttack a = g.Current.Ranged.Attacks[0];
            Assert.That(a.Phase, Is.EqualTo(RangedPhase.Windup)); Assert.That(a.Direction, Is.EqualTo(Vector3.back));
            g.Current.Players[0].Position = new Vector3(4, 0, 0);
            Steps(g, 74); Assert.That(g.Current.Ranged.Count, Is.Zero);
            Assert.That(g.Current.Enemies[0].Position, Is.EqualTo(new Vector3(0, 0, 10)));
            g.Step(); Assert.That(g.Current.Ranged.Fired, Is.EqualTo(1));
            Assert.That(g.Current.Ranged.Bolts[0].Direction, Is.EqualTo(Vector3.back));
            Steps(g, 70); Assert.That(g.Current.Players[0].Health, Is.EqualTo(100)); Assert.That(g.Current.Ranged.Hits, Is.Zero);
        }
        [TestCase(1)] [TestCase(2)] public void CommittedShotHitsAppropriateLivingPlayerExactlyOnce(int roster)
        {
            var g = Fixture(roster); int target = roster - 1;
            g.Current.Players[target].Position = Vector3.zero;
            if (roster == 2) g.Current.Players[0].Position = new Vector3(-12, 0, 0);
            Steps(g, 140);
            Assert.That(g.Current.Players[target].Health, Is.EqualTo(85)); Assert.That(g.Current.Ranged.Hits, Is.EqualTo(1));
            Assert.That(g.Current.Ranged.Count, Is.Zero);
            if (roster == 2) Assert.That(g.Current.Players[0].Health, Is.EqualTo(100));
        }
        [Test] public void SweptCollisionIncludesCrossingPlayerAndSelectsNearestPlayer()
        {
            Assert.That(RangedThreats.SegmentEntry(new Vector3(-2, 0, 0), new Vector3(2, 0, 0), out float entry), Is.True);
            Assert.That(entry, Is.InRange(0f, .5f));
            Assert.That(RangedThreats.SegmentEntry(new Vector3(-2, 0, 2), new Vector3(2, 0, 2), out _), Is.False);
            var g = Fixture(2); g.Current.Players[0].Position = new Vector3(0, 0, .5f); g.Current.Players[1].Position = new Vector3(0, 0, -.5f);
            var ranged = new RangedThreats(g.Current); ranged.Reset();
            Bolt(g, new Vector3(0, 0, .9f), Vector3.back);
            g.Step(); Assert.That(g.Current.Players[0].Health, Is.EqualTo(85)); Assert.That(g.Current.Players[1].Health, Is.EqualTo(100));
            Bolt(g, new Vector3(2, 0, .5f), Vector3.zero + Vector3.back);
            g.Current.Players[0].Position = new Vector3(4, 0, .5f); g.Current.Players[0].ContactUntil = 0;
            g.Step(); Assert.That(g.Current.Players[0].Health, Is.EqualTo(70), "Relative sweep sees a player crossing a bolt between physical ticks");
        }
        [Test] public void ReturnLifeAndImmunityPreventStaleOrDuplicateDamage()
        {
            var g = Fixture(2); g.Current.Players[0].Position = Vector3.zero;
            g.Current.Players[1].Position = new Vector3(15, 0, 0);
            g.Current.Players[0].Health = 0; g.Current.Players[0].ReturnAt = CombatSimulation.Tick;
            Bolt(g, Vector3.zero, Vector3.forward, lifeA: 1); g.Step();
            Assert.That(g.Current.Players[0].Life, Is.EqualTo(2)); Assert.That(g.Current.Players[0].Health, Is.EqualTo(100));
            g.Current.Players[0].Position = Vector3.zero; g.Step();
            Assert.That(g.Current.Players[0].Health, Is.EqualTo(100)); Assert.That(g.Current.Ranged.Count, Is.EqualTo(1), "Old life bolt cannot attach to return");
            Bolt(g, Vector3.zero, Vector3.forward, lifeA: 2); g.Step();
            Assert.That(g.Current.Ranged.Blocked, Is.EqualTo(1)); Assert.That(g.Current.Players[0].Health, Is.EqualTo(100));
            g.Current.Players[0].ImmuneUntil = 0;
            Bolt(g, Vector3.zero, Vector3.forward, lifeA: 2); Bolt(g, Vector3.zero, Vector3.forward, lifeA: 2); g.Step();
            Assert.That(g.Current.Players[0].Health, Is.EqualTo(85)); Assert.That(g.Current.Ranged.Hits, Is.EqualTo(1)); Assert.That(g.Current.Ranged.Blocked, Is.EqualTo(2));
        }
        [Test] public void ChoiceFreezeAndRetryClearPhysicalAttacksAndBolts()
        {
            var g = Fixture(); g.Step(); Bolt(g, new Vector3(10, 0, 0), Vector3.forward);
            g.Current.State = SharedState.Choosing;
            int tick = g.Current.Tick; Vector3 position = g.Current.Ranged.Bolts[0].Position; float until = g.Current.Ranged.Attacks[0].Until;
            Steps(g, 100); Assert.That(g.Current.Tick, Is.EqualTo(tick)); Assert.That(g.Current.Ranged.Bolts[0].Position, Is.EqualTo(position));
            Assert.That(g.Current.Ranged.Attacks[0].Until, Is.EqualTo(until));
            g.Current.State = SharedState.Running; g.Step(); Assert.That(g.Current.Ranged.Bolts[0].Position, Is.Not.EqualTo(position));
            g.Current.Players[0].Health = 0; g.Step(); Assert.That(g.Current.State, Is.EqualTo(SharedState.Defeat)); Assert.That(g.Current.Ranged.Count, Is.Zero);
            Assert.That(g.Retry(1, 0), Is.True); Assert.That(g.Current.Run, Is.EqualTo(2)); Assert.That(g.Current.Ranged.Fired, Is.Zero);
            Assert.That(g.Current.Ranged.Attacks[0].Phase, Is.EqualTo(RangedPhase.Cooldown)); Assert.That(g.Current.Xp, Is.Zero); Assert.That(g.Current.Players[0].Arsenal, Is.EqualTo(1));
        }
        [Test] public void KillingSentinelCancelsWindupDropsNormalXpAndReleasedBoltSurvives()
        {
            var g = Fixture(); g.Current.Enemies[0].Health = 20; g.Step(); Bolt(g, new Vector3(12, 0, 0), Vector3.forward);
            Vector3 origin = Vector3.up * 1.65f;
            g.Submit(new SharedShot { Run = 1, Player = 0, Life = 1, Weapon = 1, Event = 1, PoseTick = g.Current.Tick,
                Ray = new Ray(origin, (g.Current.Enemies[0].Position + Vector3.up * .8f - origin).normalized) }); g.Step();
            Assert.That(g.Current.Enemies[0].Incarnation, Is.EqualTo(2)); Assert.That(g.Current.Ranged.Attacks[0].Phase, Is.EqualTo(RangedPhase.Cooldown));
            Assert.That(g.Growth.Orbs.Count, Is.EqualTo(1)); Assert.That(g.Current.Ranged.Count, Is.EqualTo(1));
            Steps(g, 220); Assert.That(g.Current.Ranged.Count, Is.Zero); Assert.That(g.Current.Ranged.Expired, Is.EqualTo(1));
        }
        [Test] public void ReleasedPoolIsBoundedAndTargetDeathCancelsPendingShot()
        {
            var g = Fixture(2); g.Current.Players[0].Position = Vector3.zero; g.Current.Players[1].Position = new Vector3(-20, 0, 0); g.Step();
            g.Current.Players[0].Health = 0; g.Current.Players[0].ReturnAt = 500; Steps(g, 75);
            Assert.That(g.Current.Ranged.Fired, Is.Zero); Assert.That(g.Current.Ranged.Attacks[0].Phase, Is.EqualTo(RangedPhase.Cooldown));
            g = Fixture(); g.Step(); Steps(g, 74);
            for (int i = 0; i < RangedThreats.Capacity; i++) Bolt(g, new Vector3(20, 0, 20), Vector3.forward);
            g.Step(); Assert.That(g.Current.Ranged.Count, Is.EqualTo(RangedThreats.Capacity)); Assert.That(g.Current.Ranged.Overflow, Is.EqualTo(1));
        }
        [Test] public void SnapshotRoundtripIsDeepAndMalformedRangedStateCannotReplaceDisplay()
        {
            var g = Fixture(); Steps(g, 76); var displayed = new SharedSnapshot();
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream); SharedConnection.WriteSnapshot(writer, g.Current); writer.Flush(); stream.Position = 0;
                SharedConnection.ReadSnapshot(new BinaryReader(stream), displayed);
                Assert.That(displayed.Ranged.Count, Is.EqualTo(1)); Assert.That(displayed.Ranged.Bolts[0].Id, Is.EqualTo(1));
                g.Current.Ranged.Bolts[0].Position = Vector3.zero; Assert.That(displayed.Ranged.Bolts[0].Position, Is.Not.EqualTo(Vector3.zero));
                stream.SetLength(0); stream.Position = 0; g.Current.Ranged.Count = 21;
                Assert.Throws<IndexOutOfRangeException>(() => SharedConnection.WriteSnapshot(writer, g.Current));
                stream.Position = 0; Assert.Throws<ArgumentException>(() => SharedConnection.ReadSnapshot(new BinaryReader(stream), displayed));
                Assert.That(displayed.Ranged.Count, Is.EqualTo(1));
            }
        }
    }
}
