using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Journey.Tests
{
    public class SharedConnectionTests
    {
        [UnityTest] public IEnumerator LargeReliableSnapshotsRetainBoundedChunksAndAtomicOrderedState()
        {
            using (var host = new SharedConnection(true, "127.0.0.1", 17783, true))
            using (var client = new SharedConnection(false, "127.0.0.1", 17783))
            {
                var game = new SharedEncounter(ScenarioConfiguration.Named(ScenarioKind.CoopScale).Scale(1000, 8, 90)); game.Start();
                var destination = new SharedSnapshot(); var assembly = new SnapshotAssembly();
                int received = 0; string loss = "";
                host.Lost += r => loss = r; client.Lost += r => loss = r;
                client.Received += (message, reader) => {
                    if (message != SharedConnection.Message.SnapshotChunk) return;
                    int previous = destination.Tick;
                    if (assembly.Read(reader, destination, Time.realtimeSinceStartupAsDouble))
                    {
                        Assert.That(destination.Tick, Is.EqualTo(++received));
                        Assert.That(destination.Enemies.Length, Is.EqualTo(1000));
                        Assert.That(destination.Enemies[999].Incarnation, Is.EqualTo(received));
                        Assert.That(destination.Players[0].Arsenal, Is.EqualTo(255));
                    }
                    else Assert.That(destination.Tick, Is.EqualTo(previous));
                };
                double deadline = Time.realtimeSinceStartupAsDouble + 8;
                while (!host.Connected && Time.realtimeSinceStartupAsDouble < deadline) { host.Poll(); client.Poll(); yield return null; }
                Assert.That(host.Connected, Is.True);
                for (int i = 1; i <= 10; i++)
                { game.Current.Tick = i; game.Current.Enemies[999].Incarnation = i; Assert.That(host.SendSnapshot(game.Current), Is.True); }
                Assert.That(host.PendingMessages, Is.GreaterThan(0)); Assert.That(host.PendingBytes, Is.GreaterThan(0));
                Assert.That(host.BufferedHighWater, Is.LessThanOrEqualTo(SharedConnection.MaxBufferedMessages));
                while (received < 10 && Time.realtimeSinceStartupAsDouble < deadline) { host.Poll(); client.Poll(); yield return null; }
                Assert.That(loss, Is.Empty); Assert.That(received, Is.EqualTo(10));
                Assert.That(host.PendingMessages, Is.Zero); Assert.That(host.PendingBytes, Is.Zero); Assert.That(assembly.Pending, Is.False);
                Assert.That(host.SnapshotMessages, Is.GreaterThan(10));
            }
            yield return null; LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ReliableBurstRetainsEveryEventInOrderPastTransportWindow()
        {
            using (var host = new SharedConnection(true, "127.0.0.1", 17782, true))
            using (var client = new SharedConnection(false, "127.0.0.1", 17782))
            {
                int received = 0; string loss = "";
                host.Lost += r => loss = r; client.Lost += r => loss = r;
                client.Received += (message, reader) => {
                    if (message != SharedConnection.Message.Growth) return;
                    GrowthEvent record = GrowthEvent.Read(reader); Assert.That(record.Sequence, Is.EqualTo(++received));
                };
                double deadline = Time.realtimeSinceStartupAsDouble + 8;
                while (!host.Connected && Time.realtimeSinceStartupAsDouble < deadline) { host.Poll(); client.Poll(); yield return null; }
                Assert.That(host.Connected, Is.True);
                for (int i = 1; i <= 200; i++)
                { int sequence = i; Assert.That(host.Send(SharedConnection.Message.Growth, w => new GrowthEvent { Run = 1, Sequence = sequence }.Write(w)), Is.True); }
                Assert.That(host.PendingMessages, Is.GreaterThan(0)); Assert.That(host.DeferredSends, Is.GreaterThan(0));
                while (received < 200 && Time.realtimeSinceStartupAsDouble < deadline) { host.Poll(); client.Poll(); yield return null; }
                Assert.That(loss, Is.Empty); Assert.That(received, Is.EqualTo(200)); Assert.That(host.PendingMessages, Is.Zero);
                Assert.That(host.SentBytes, Is.EqualTo(200 * 105));
            }
            yield return null;
        }
        [UnityTest] public IEnumerator StartAndRetryExcludeWaitingFramesButActiveStallsStillAbort()
        {
            var root = new GameObject("Shared phase clock test");
            var view = new ArenaPresentation(); var sim = new CombatSimulation();
            var game = root.AddComponent<SharedGame>(); game.enabled = false;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var clock = typeof(SharedGame).GetField("previousClock", flags);
            var update = typeof(SharedGame).GetMethod("Update", flags);
            var publish = typeof(SharedGame).GetMethod("Publish", flags);
            typeof(SharedGame).GetField("output", flags).SetValue(game,
                System.IO.Path.Combine(Application.dataPath, "../artifacts/phase-clock-" + System.Guid.NewGuid().ToString("N")));
            using (var client = new SharedConnection(false, "127.0.0.1", 17778))
            try
            {
                LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("^SHARED_SESSION HOST "));
                var binding = new SharedScenarioBinding();
                client.Received += (message, reader) => {
                    if (message == SharedConnection.Message.Configuration) { binding.AcceptOffer(reader); client.Send(SharedConnection.Message.ConfigurationAck, binding.Write); }
                    else if (message == SharedConnection.Message.Snapshot) SharedConnection.ReadSnapshot(reader, new SharedSnapshot());
                    else if (message == SharedConnection.Message.Hit) SharedConnection.ReadHit(reader);
                    else if (message == SharedConnection.Message.Ping) { double stamp = reader.ReadDouble(); client.Send(SharedConnection.Message.Pong, w => w.Write(stamp)); }
                    else if (message == SharedConnection.Message.Abort) reader.ReadString();
                };
                view.Create(root.transform); game.Initialize(view, sim, true, "0.0.0.0", 17778);
                double deadline = Time.realtimeSinceStartupAsDouble + 4;
                while (game.Snapshot.State == SharedState.Waiting && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    client.Poll(); clock.SetValue(game, Time.realtimeSinceStartupAsDouble - 2);
                    update.Invoke(game, null); yield return null;
                }
                Assert.That(game.Snapshot.State, Is.EqualTo(SharedState.Running), "Hello must not spend a two-second Waiting frame in combat.");
                Assert.That(game.Snapshot.Tick, Is.Zero);
                clock.SetValue(game, Time.realtimeSinceStartupAsDouble - .02); update.Invoke(game, null);
                Assert.That(game.Snapshot.Tick, Is.GreaterThan(0), "Subsequent active frames still advance the authority.");

                var authority = (SharedEncounter)typeof(SharedGame).GetField("authority", flags).GetValue(game);
                authority.Current.Tick = 3599; authority.Step(); authority.Retry(1, 0); publish.Invoke(game, null);
                Assert.That(game.Snapshot.State, Is.EqualTo(SharedState.Complete));
                client.Send(SharedConnection.Message.Retry, writer => { writer.Write(1); binding.Write(writer); });
                while (game.Snapshot.Run == 1 && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    client.Poll(); clock.SetValue(game, Time.realtimeSinceStartupAsDouble - 2);
                    update.Invoke(game, null); yield return null;
                }
                Assert.That(game.Snapshot.Run, Is.EqualTo(2));
                Assert.That(game.Snapshot.State, Is.EqualTo(SharedState.Running), "Retry must not spend a terminal-screen frame in the next run.");
                Assert.That(game.Snapshot.Tick, Is.Zero);
                LogAssert.Expect(LogType.Log, "SHARED_ABORT Authority tick catch-up limit");
                LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("^SHARED_REPORT "));
                clock.SetValue(game, Time.realtimeSinceStartupAsDouble - .5); update.Invoke(game, null);
                Assert.That(game.Snapshot.State, Is.EqualTo(SharedState.Aborted), "A real active-run stall still hits the unchanged catch-up bound.");
            }
            finally { Object.DestroyImmediate(root); view.Dispose(); sim.Dispose(); }
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator RealLoopbackHandshakeSnapshotAndRetryRoundTrip()
        {
            var authority = new SharedEncounter(); var clientView = new SharedSnapshot();
            var confirmed = new System.Collections.Generic.List<SharedHit>(); bool shotReceived = false;
            using (var host = new SharedConnection(true, "0.0.0.0", 17777, loopbackHost: true))
            using (var client = new SharedConnection(false, "127.0.0.1", 17777))
            {
                Assert.That(host.BoundEndpoint, Is.EqualTo("127.0.0.1:17777"));
                string loss = ""; host.Lost += r => loss = r; client.Lost += r => loss = r;
                host.Received += (m, r) => {
                    if (m == SharedConnection.Message.Hello)
                    {
                        authority.Start();
                        for (int i = 0; i < 12; i++) authority.Current.Enemies[i].Position = new Vector3(28, 0, 28);
                        authority.Current.Enemies[0].Position = new Vector3(0, 0, 8); authority.Step();
                        host.Send(SharedConnection.Message.Snapshot, w => SharedConnection.WriteSnapshot(w, authority.Current));
                    }
                    if (m == SharedConnection.Message.Shot) { authority.Submit(SharedConnection.ReadShot(r)); shotReceived = true; }
                    if (m == SharedConnection.Message.Retry) { authority.Retry(r.ReadInt32(), 1); host.Send(SharedConnection.Message.Snapshot, w => SharedConnection.WriteSnapshot(w, authority.Current)); }
                };
                authority.Resolved += hit => host.Send(SharedConnection.Message.Hit, w => SharedConnection.WriteHit(w, hit));
                client.Received += (m, r) => {
                    if (m == SharedConnection.Message.Snapshot) SharedConnection.ReadSnapshot(r, clientView);
                    if (m == SharedConnection.Message.Hit) confirmed.Add(SharedConnection.ReadHit(r));
                };
                double deadline = Time.realtimeSinceStartupAsDouble + 4;
                while (clientView.Run == 0 && Time.realtimeSinceStartupAsDouble < deadline) { host.Poll(); client.Poll(); yield return null; }
                Assert.That(loss, Is.Empty); Assert.That(clientView.Run, Is.EqualTo(1)); Assert.That(clientView.Enemies[0].Health, Is.EqualTo(60));
                for (int p = 0; p < 2; p++)
                {
                    Vector3 origin = clientView.Players[p].Position + Vector3.up * 1.65f;
                    var shot = new SharedShot { Run = 1, Player = p, Event = 1, Life = 1, Weapon = p + 1, PoseTick = clientView.Tick,
                        Ray = new Ray(origin, (clientView.Enemies[0].Position + Vector3.up * (p == 0 ? .8f : 1.6f) - origin).normalized) };
                    if (p == 0) authority.Submit(shot); else client.Send(SharedConnection.Message.Shot, w => SharedConnection.WriteShot(w, shot));
                }
                while (!shotReceived && Time.realtimeSinceStartupAsDouble < deadline) { host.Poll(); client.Poll(); yield return null; }
                Assert.That(shotReceived, Is.True); authority.Step();
                host.Send(SharedConnection.Message.Snapshot, w => SharedConnection.WriteSnapshot(w, authority.Current));
                while (confirmed.Count < 2 && Time.realtimeSinceStartupAsDouble < deadline) { host.Poll(); client.Poll(); yield return null; }
                Assert.That(confirmed.Count, Is.EqualTo(2)); Assert.That(confirmed[0].Shot.Damage, Is.EqualTo(20)); Assert.That(confirmed[1].Shot.Damage, Is.EqualTo(40));
                Assert.That(confirmed[1].Shot.Killed, Is.True); Assert.That(clientView.Enemies[0].Incarnation, Is.EqualTo(2));
                authority.Current.Tick = 3599; authority.Step(); authority.Retry(1, 0);
                client.Send(SharedConnection.Message.Retry, w => w.Write(1));
                while (clientView.Run != 2 && Time.realtimeSinceStartupAsDouble < deadline) { host.Poll(); client.Poll(); yield return null; }
                Assert.That(loss, Is.Empty); Assert.That(clientView.Run, Is.EqualTo(2)); Assert.That(clientView.Tick, Is.Zero);
                Assert.That(clientView.Players, Is.EqualTo(authority.Current.Players)); Assert.That(clientView.Enemies, Is.EqualTo(authority.Current.Enemies));
                client.Dispose();
                while (loss.Length == 0 && Time.realtimeSinceStartupAsDouble < deadline) { host.Poll(); yield return null; }
                Assert.That(loss, Does.Contain("lost"));
            }
            yield return null; LogAssert.NoUnexpectedReceived();
        }
    }
}
