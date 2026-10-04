using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Journey.Tests
{
    public class SharedEncounterTests
    {
        SharedEncounter game;
        List<SharedHit> hits;
        [SetUp] public void Setup()
        {
            game = new SharedEncounter(); game.Start(); hits = new List<SharedHit>(); game.Resolved += hits.Add;
            for (int i = 0; i < 12; i++) game.Current.Enemies[i].Position = new Vector3(28, 0, 28);
            game.Current.Enemies[0].Position = new Vector3(0, 0, 8); game.Step();
        }
        SharedShot Shot(int player, int id = 1, bool head = false) => new SharedShot {
            Run = game.Current.Run, Player = player, Event = id, Weapon = player + 1, Life = game.Current.Players[player].Life,
            PoseTick = game.Current.Tick, Ray = new Ray(game.Current.Players[player].Position + Vector3.up * 1.65f,
                (game.Current.Enemies[0].Position + Vector3.up * (head ? 1.6f : .8f) - (game.Current.Players[player].Position + Vector3.up * 1.65f)).normalized) };
        [Test] public void TwoSourcesShareHealthAndExactlyOneDeathSnapshot()
        {
            game.Submit(Shot(1, head:true)); game.Submit(Shot(0)); game.Step();
            Assert.That(hits.Count, Is.EqualTo(2)); Assert.That(hits[0].Source.Player, Is.Zero, "Stable source order");
            Assert.That(hits[0].Shot.Damage, Is.EqualTo(20)); Assert.That(hits[1].Shot.Damage, Is.EqualTo(40));
            Assert.That(hits.FindAll(h => h.Shot.Killed).Count, Is.EqualTo(1)); Assert.That(hits[1].Shot.HealthBefore, Is.EqualTo(40));
            Assert.That(game.Current.Enemies[0].Health, Is.EqualTo(60)); Assert.That(game.Current.Enemies[0].Incarnation, Is.EqualTo(2));
            Assert.That(hits[1].Shot.SpawnVersion, Is.EqualTo(1)); Assert.That(hits[1].DeathPosition.z, Is.LessThan(9));
            Assert.That(game.Current.Players[0].Damage, Is.EqualTo(20)); Assert.That(game.Current.Players[1].Kills, Is.EqualTo(1));
        }
        [Test] public void OverkillAttributesOnlyRemainingActualDamage()
        {
            game.Submit(Shot(0, head:true)); game.Submit(Shot(1, head:true)); game.Step();
            Assert.That(hits[0].Shot.Damage, Is.EqualTo(40)); Assert.That(hits[1].Shot.RequestedDamage, Is.EqualTo(40));
            Assert.That(hits[1].Shot.Damage, Is.EqualTo(20)); Assert.That(hits[1].Shot.HealthAfter, Is.Zero);
        }
        [Test] public void DuplicateAndOldIncarnationCannotDamageReplacement()
        {
            SharedShot a = Shot(0, head:true), b = Shot(1, head:true), stale = Shot(0, 2, true);
            game.Submit(a); game.Submit(a); game.Submit(b); game.Step();
            game.Submit(stale); for (int i = 0; i < 10; i++) game.Step();
            Assert.That(hits.Exists(h => h.Decision == ShotDecision.Duplicate), Is.True);
            Assert.That(hits.Exists(h => h.Decision == ShotDecision.StaleEnemy), Is.True);
            Assert.That(game.Current.Enemies[0].Health, Is.EqualTo(60)); Assert.That(game.Current.Players[0].Shots, Is.EqualTo(1));
        }
        [Test] public void HostRequeriesHistoricalDisplayedPoseRatherThanCurrentPosition()
        {
            SharedShot shot = Shot(0, head:true); game.Current.Enemies[0].Position = new Vector3(20, 0, 20);
            game.Submit(shot); game.Step(); Assert.That(hits[0].Shot.Damage, Is.EqualTo(40)); Assert.That(hits[0].Shot.Critical, Is.True);
        }
        [Test] public void ExpiredHistoryInvalidRayAndWrongWeaponReject()
        {
            SharedShot old = Shot(0); for (int i = 0; i < 31; i++) game.Step(); game.Submit(old);
            SharedShot invalid = Shot(1); invalid.Ray = new Ray(Vector3.zero, Vector3.zero); game.Submit(invalid);
            SharedShot weapon = Shot(1, 2); weapon.Weapon = 99; game.Submit(weapon); game.Step();
            Assert.That(hits.FindAll(h => h.Decision == ShotDecision.Invalid).Count, Is.EqualTo(2));
            Assert.That(hits.Exists(h => h.Decision == ShotDecision.StalePose), Is.True); Assert.That(game.Current.Players[0].Shots, Is.Zero);
        }
        [Test] public void ReadOnlyPreviewCannotGrantDamage()
        {
            int before = game.Current.Enemies[0].Health; for (int i = 0; i < 20; i++) Assert.That(game.Current.Query(Shot(0).Ray).Hit, Is.True);
            Assert.That(game.Current.Enemies[0].Health, Is.EqualTo(before)); Assert.That(game.Current.Players[0].Damage, Is.Zero);
        }
        [Test] public void NearestLivingUsesStableTieAndIgnoresDead()
        {
            Assert.That(game.NearestLiving(Vector3.zero), Is.Zero); game.Current.Players[0].Health = 0;
            Assert.That(game.NearestLiving(Vector3.zero), Is.EqualTo(1)); game.Current.Players[1].Health = 0;
            Assert.That(game.NearestLiving(Vector3.zero), Is.EqualTo(-1));
        }
        [Test] public void BothPlayersHaveIndependentContactGrace()
        {
            game.Current.Enemies[0].Position = SharedEncounter.Spawn(0); game.Current.Enemies[1].Position = SharedEncounter.Spawn(1);
            game.Step(); Assert.That(game.Current.Players[0].Health, Is.EqualTo(90)); Assert.That(game.Current.Players[1].Health, Is.EqualTo(90));
            for (int i = 0; i < 10; i++) game.Step();
            Assert.That(game.Current.Players[0].Health, Is.EqualTo(90)); Assert.That(game.Current.Players[1].Health, Is.EqualTo(90));
        }
        [Test] public void ReturnTakesFiveSecondsAndProvidesOneSecondImmunity()
        {
            game.Current.Players[1].Position = new Vector3(-28, 0, 28);
            game.Current.Players[0].Health = 10; game.Current.Enemies[0].Position = SharedEncounter.Spawn(0); game.Step();
            float due = game.Current.Players[0].ReturnAt; Assert.That(due - game.Current.Elapsed, Is.EqualTo(5).Within(.0001));
            for (int i = 0; i < 299; i++) game.Step(); Assert.That(game.Current.Players[0].Health, Is.Zero);
            game.Step(); Assert.That(game.Current.Players[0].Health, Is.EqualTo(100)); Assert.That(game.Current.Players[0].Life, Is.EqualTo(2));
            Assert.That(game.Current.Players[0].ImmuneUntil - game.Current.Elapsed, Is.EqualTo(1).Within(.0001));
            game.Current.Enemies[0].Position = SharedEncounter.Spawn(0); game.Step(); Assert.That(game.Current.Players[0].Health, Is.EqualTo(100));
        }
        [Test] public void LastDeathBeatsExpiringReturnAndCompletion()
        {
            game.Current.Tick = 3599; game.Current.Players[0].Health = 0; game.Current.Players[0].ReturnAt = 60;
            game.Current.Players[1].Health = 10; game.Current.Enemies[0].Position = SharedEncounter.Spawn(1); game.Step();
            Assert.That(game.Current.State, Is.EqualTo(SharedState.Defeat)); Assert.That(game.Current.Players[0].Health, Is.Zero);
        }
        [Test] public void TimedCompletionFreezesDeadReturnAndShots()
        {
            game.Current.Tick = 3599; game.Current.Players[0].Health = 0; game.Current.Players[0].ReturnAt = 60; game.Step();
            Assert.That(game.Current.State, Is.EqualTo(SharedState.Complete)); game.Step(); game.Submit(Shot(1));
            Assert.That(game.Current.Tick, Is.EqualTo(3600)); Assert.That(game.Current.Players[0].Health, Is.Zero);
            Assert.That(hits[0].Decision, Is.EqualTo(ShotDecision.Terminal));
        }
        [Test] public void BothReadyResetInvalidatesAllOldRunInputsShotsAndRetry()
        {
            SharedShot old = Shot(0); int run = game.Current.Run; game.Current.Tick = 3599; game.Step();
            Assert.That(game.Retry(run, 0), Is.True); Assert.That(game.Current.Run, Is.EqualTo(run));
            Assert.That(game.Retry(run, 1), Is.True); Assert.That(game.Current.Run, Is.EqualTo(run + 1));
            game.Submit(old); Assert.That(game.Retry(run, 0), Is.False);
            Assert.That(game.Input(run, 0, 1, 1, .02f, Vector3.zero, 0, 0), Is.False);
            Assert.That(hits[0].Decision, Is.EqualTo(ShotDecision.StaleRun));
            foreach (SharedPlayer p in game.Current.Players) { Assert.That(p.Health, Is.EqualTo(100)); Assert.That(p.Damage, Is.Zero); Assert.That(p.Ready, Is.False); Assert.That(p.BodyDamage, Is.EqualTo(20)); Assert.That(p.RifleInterval, Is.EqualTo(.12f)); }
            foreach (SharedEnemy e in game.Current.Enemies) { Assert.That(e.Health, Is.EqualTo(60)); Assert.That(e.Incarnation, Is.EqualTo(1)); }
        }
        [Test] public void LifeTokenRejectsShotsAndInputsAfterReturn()
        {
            SharedShot old = Shot(0); game.Current.Players[0].Health = 0; game.Current.Players[0].ReturnAt = game.Current.Elapsed;
            game.Step(); game.Submit(old);
            Assert.That(hits[0].Decision, Is.EqualTo(ShotDecision.StaleLife));
            Assert.That(game.Input(game.Current.Run, 0, 1, 1, .02f, SharedEncounter.Spawn(0), 0, 0), Is.False);
        }
        [Test] public void AbortNeverBecomesSuccessOrAllowsRetry()
        {
            game.Abort("peer lost"); game.Step(); Assert.That(game.Current.State, Is.EqualTo(SharedState.Aborted));
            Assert.That(game.AbortReason, Is.EqualTo("peer lost")); Assert.That(game.Retry(game.Current.Run, 0), Is.False);
        }
        [Test] public void SnapshotWireRoundTripPreservesBothPlayersAndIncarnations()
        {
            game.Submit(Shot(0, head:true)); game.Step(); var clone = new SharedSnapshot();
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream); SharedConnection.WriteSnapshot(writer, game.Current); writer.Flush();
                Assert.That(stream.Length + 1, Is.LessThan(SharedConnection.MaxPacket)); stream.Position = 0;
                SharedConnection.ReadSnapshot(new BinaryReader(stream), clone);
            }
            Assert.That(clone.Run, Is.EqualTo(game.Current.Run)); Assert.That(clone.Tick, Is.EqualTo(game.Current.Tick));
            Assert.That(clone.Players, Is.EqualTo(game.Current.Players)); Assert.That(clone.Enemies, Is.EqualTo(game.Current.Enemies));
        }
    }
}
