using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Journey.Tests
{
    public class SharedConnectionTests
    {
        [UnityTest] public IEnumerator RealLoopbackHandshakeSnapshotAndRetryRoundTrip()
        {
            var authority = new SharedEncounter(); var clientView = new SharedSnapshot();
            var confirmed = new System.Collections.Generic.List<SharedHit>(); bool shotReceived = false;
            using (var host = new SharedConnection(true, "0.0.0.0", 17777))
            using (var client = new SharedConnection(false, "127.0.0.1", 17777))
            {
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
