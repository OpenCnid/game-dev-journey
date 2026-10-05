using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Journey.Tests
{
    public sealed class ScenarioAgreementTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly MethodInfo Update = typeof(SharedGame).GetMethod("Update", Hidden);
        static readonly MethodInfo Publish = typeof(SharedGame).GetMethod("Publish", Hidden);
        static readonly FieldInfo Clock = typeof(SharedGame).GetField("previousClock", Hidden);
        static void Pump(SharedGame game)
        { Clock.SetValue(game, Time.realtimeSinceStartupAsDouble - .016); Update.Invoke(game, null); }

        [UnityTest] public IEnumerator SoloGrowthHiddenCardsCannotSelectAndRetryRestoresBaseline()
        {
            var root = new GameObject("Solo earned growth controls"); var view = new ArenaPresentation(); var sim = new CombatSimulation();
            try
            {
                view.Create(root.transform); var game = root.AddComponent<SharedGame>(); game.enabled = false;
                typeof(SharedGame).GetField("output", Hidden).SetValue(game, viewOutput());
                game.Initialize(view, sim, true, "", 0, ScenarioConfiguration.Named(ScenarioKind.EarnedGrowth), true);
                Assert.That(game.Snapshot.Roster, Is.EqualTo(1));
                Assert.That(game.Snapshot.State, Is.EqualTo(SharedState.Waiting));
                Clock.SetValue(game, Time.realtimeSinceStartupAsDouble - .5); Update.Invoke(game, null);
                Assert.That(game.Snapshot.State, Is.EqualTo(SharedState.Running));
                Assert.That(game.Snapshot.Tick, Is.Zero, "Initialization is not active combat time");
                var authority = (SharedEncounter)typeof(SharedGame).GetField("authority", Hidden).GetValue(game);
                for (int i = 1; i <= 10; i++) authority.Growth.Death(authority.Current,
                    new SharedHit(new SharedShot { Run = 1, Player = 0, Weapon = 1, Event = i }, ShotDecision.Damage,
                    new ShotResult(Vector3.zero, 0, i, HitRegion.Body, 30, 30, 30, 0), Vector3.zero));
                authority.Step(); Publish.Invoke(game, null); Pump(game);
                Assert.That(game.Snapshot.Players[0].Offer.Pending, Is.True);
                Assert.That(game.ChoiceControls(true, 1), Is.False, "Hiding cards disables number selection");
                Assert.That(game.ChoiceControls(false, 2), Is.False);
                UpgradeId first = game.Snapshot.Players[0].Offer.Card(1);
                Assert.That(game.ChoiceControls(true, 1), Is.True, "Reopening keeps the original offer");
                authority.Step(); Publish.Invoke(game, null); Pump(game);
                Assert.That(game.Snapshot.Players[0].Offer.Selected, Is.EqualTo(first));
                Assert.That(game.ChoiceControls(false, 2), Is.False);
                authority.Current.Tick = 3599;
                Clock.SetValue(game, Time.realtimeSinceStartupAsDouble - .06); Update.Invoke(game, null);
                Assert.That(game.Snapshot.State, Is.EqualTo(SharedState.Complete), "Leftover frame time cannot overwrite completion with catch-up abort");
                Assert.That(game.ApplyPreviewCommand(new SharedPreviewCommand { sequence = 1, run = 1, life = 1, action = "retry" }), Is.Empty);
                authority.Current.Players[0].Health = 0; authority.Step(); Publish.Invoke(game, null);
                Assert.That(game.Snapshot.State, Is.EqualTo(SharedState.Defeat));
                Assert.That(game.ApplyPreviewCommand(new SharedPreviewCommand { sequence = 2, run = 2, life = 1, action = "retry" }), Is.Empty);
                Assert.That(game.Snapshot.Run, Is.EqualTo(3)); Assert.That(game.Snapshot.Xp, Is.Zero);
                Assert.That(game.Snapshot.Players[0].BodyDamage, Is.EqualTo(20)); Assert.That(game.Snapshot.Players[0].RifleInterval, Is.EqualTo(.12f));
                yield return null;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); view.Dispose(); sim.Dispose(); }
        }
        static string viewOutput() => Path.Combine(Application.dataPath, "../artifacts/growth-solo-" + Guid.NewGuid().ToString("N"));

        [Test] public void StartAndRetryKeepCurrentRunFeedbackReceivedWithTheNewSnapshot()
        {
            var root = new GameObject("Batched generation feedback");
            var view = new ArenaPresentation();
            try
            {
                view.Create(root.transform);
                var game = root.AddComponent<SharedGame>(); game.enabled = false;
                typeof(SharedGame).GetField("view", Hidden).SetValue(game, view);
                var queue = (Queue<SharedHit>)typeof(SharedGame).GetField("feedback", Hidden).GetValue(game);
                var sync = typeof(SharedGame).GetMethod("SyncGeneration", Hidden);
                var encounter = new SharedEncounter();
                for (int run = 1; run <= 2; run++)
                {
                    encounter.Start(); game.Snapshot.CopyFrom(encounter.Current);
                    // One transport Poll can deliver the new snapshot, an old hit,
                    // and the new run's first hits before SyncGeneration executes.
                    foreach (var source in new[] {
                        new SharedShot { Run = run - 1, Event = 99 },
                        new SharedShot { Run = run, Event = 1 },
                        new SharedShot { Run = run, Event = 2 } })
                        queue.Enqueue(new SharedHit(source, ShotDecision.Miss, default, default));
                    sync.Invoke(game, null);
                    Assert.That(queue.Count, Is.EqualTo(2), "Start/Retry must retain new-run feedback and discard stale feedback.");
                    Assert.That(queue.Dequeue().Source.Event, Is.EqualTo(1));
                    Assert.That(queue.Dequeue().Source.Event, Is.EqualTo(2));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); view.Dispose(); }
        }

        [UnityTest] public IEnumerator ActualGamesAgreeBeforeStartAndBothReadyRetryRetainsDefaultsAndOverride()
        {
            foreach (var kind in new[] { ScenarioKind.SharedRifle, ScenarioKind.DeveloperParity, ScenarioKind.SoloPrecision, ScenarioKind.EarnedGrowth })
            {
                var configuration = ScenarioConfiguration.Named(kind);
                var roots = new[] { new GameObject("Scenario host"), new GameObject("Scenario client") };
                var views = new[] { new ArenaPresentation(), new ArenaPresentation() };
                var sims = new[] { new CombatSimulation(), new CombatSimulation() };
                var games = new SharedGame[2];
                try
                {
                    for (int i = 0; i < 2; i++)
                    {
                        views[i].Create(roots[i].transform); games[i] = roots[i].AddComponent<SharedGame>(); games[i].enabled = false;
                        typeof(SharedGame).GetField("output", Hidden).SetValue(games[i], Path.Combine(Application.dataPath, "../artifacts/scenario-pair-" + Guid.NewGuid().ToString("N")));
                        games[i].Initialize(views[i], sims[i], i == 0, "127.0.0.1", 17779, i == 0 ? configuration : ScenarioConfiguration.Named(ScenarioKind.SoloPrecision));
                    }
                    Assert.That(games[0].Snapshot.State, Is.EqualTo(SharedState.Waiting));
                    Assert.That(games[0].Snapshot.Run, Is.Zero, "A socket creation cannot start combat.");
                    double deadline = Time.realtimeSinceStartupAsDouble + 5;
                    while (games[1].Snapshot.Run == 0 && Time.realtimeSinceStartupAsDouble < deadline)
                    { Pump(games[0]); Pump(games[1]); yield return null; }
                    Assert.That(games[0].Snapshot.State, Is.EqualTo(SharedState.Running));
                    Assert.That(games[1].Snapshot.State, Is.EqualTo(SharedState.Running));
                    Assert.That(games[1].Snapshot.Configuration.Identity, Is.EqualTo(configuration.Identity));
                    Assert.That(games[1].Snapshot.Session, Is.EqualTo(games[0].Snapshot.Session));
                    Assert.That(games[1].Snapshot.Enemies.Length, Is.EqualTo(configuration.EnemyCount));
                    Assert.That(sims[1].HealthMaximum, Is.EqualTo(configuration.EnemyHealth));
                    foreach (var p in games[1].Snapshot.Players)
                    { Assert.That(p.BodyDamage, Is.EqualTo(configuration.BodyDamage)); Assert.That(p.RifleInterval, Is.EqualTo(configuration.RifleInterval)); }

                    var authority = (SharedEncounter)typeof(SharedGame).GetField("authority", Hidden).GetValue(games[0]);
                    if (configuration.Growth)
                    {
                        // Labelled diagnostic deaths isolate actual transport and UI selection, not earning/enjoyment.
                        for (int i = 1; i <= 10; i++) authority.Growth.Death(authority.Current,
                            new SharedHit(new SharedShot { Run = 1, Player = 0, Weapon = 1, Event = i }, ShotDecision.Damage,
                            new ShotResult(Vector3.zero, 0, i, HitRegion.Body, 30, 30, 30, 0), SharedEncounter.Spawn(0)));
                        authority.Step(); Publish.Invoke(games[0], null);
                        while (games[1].Snapshot.Level == 1 && Time.realtimeSinceStartupAsDouble < deadline)
                        { Pump(games[0]); Pump(games[1]); yield return null; }
                        Assert.That(games[1].Snapshot.Xp, Is.EqualTo(10)); Assert.That(games[1].Snapshot.Players[1].Offer.Pending, Is.True);
                        Assert.That(games[0].Choose(UpgradeId.StrongerHits), Is.True); Assert.That(games[1].Choose(UpgradeId.FasterFire), Is.True);
                        while (games[1].Snapshot.Players[1].Offer.Pending && Time.realtimeSinceStartupAsDouble < deadline)
                        { Pump(games[0]); Pump(games[1]); yield return null; }
                        Assert.That(games[1].Snapshot.Players[0].BodyDamage, Is.EqualTo(30));
                        Assert.That(games[1].Snapshot.Players[1].RifleInterval, Is.EqualTo(.08f));
                        Assert.That(games[1].Choose(UpgradeId.StrongerHits), Is.False);
                    }
                    authority.Current.Tick = 3599; authority.Step(); Publish.Invoke(games[0], null);
                    while (games[1].Snapshot.State == SharedState.Running && Time.realtimeSinceStartupAsDouble < deadline)
                    { Pump(games[0]); Pump(games[1]); yield return null; }
                    Assert.That(games[1].Snapshot.State, Is.EqualTo(SharedState.Complete));
                    Assert.That(games[0].ApplyPreviewCommand(new SharedPreviewCommand { sequence = 1, run = 1, life = 1, action = "retry" }), Is.Empty);
                    Pump(games[0]); Pump(games[1]);
                    Assert.That(authority.Current.Run, Is.EqualTo(1), "One readiness cannot restart the party.");
                    Assert.That(games[1].ApplyPreviewCommand(new SharedPreviewCommand { sequence = 1, run = 1, life = 1, action = "retry" }), Is.Empty);
                    while (games[1].Snapshot.Run == 1 && Time.realtimeSinceStartupAsDouble < deadline)
                    { Pump(games[0]); Pump(games[1]); yield return null; }
                    Assert.That(games[1].Snapshot.Run, Is.EqualTo(2));
                    Assert.That(games[1].Snapshot.Configuration.Identity, Is.EqualTo(configuration.Identity));
                    Assert.That(games[1].ApplyPreviewCommand(new SharedPreviewCommand { sequence = 2, run = 1, life = 1, action = "retry" }), Is.EqualTo("Stale run/life"));
                    Assert.That(games[1].Snapshot.Players[0].Health, Is.EqualTo(100));
                    Assert.That(games[1].Snapshot.Xp, Is.Zero); Assert.That(games[1].Snapshot.Level, Is.EqualTo(1));
                    Assert.That(games[1].Snapshot.Players[0].Offer.Id, Is.Zero);
                }
                finally
                {
                    for (int i = 0; i < 2; i++) { UnityEngine.Object.DestroyImmediate(roots[i]); views[i].Dispose(); sims[i].Dispose(); }
                }
                yield return null;
            }
            // Informational session/report logs are expected; Unity still fails on error/assert/exception logs.
        }
    }
}
