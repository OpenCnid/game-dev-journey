using System;
using System.Collections.Generic;
using System.IO;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;
using TransportStatus = Unity.Networking.Transport.Error.StatusCode;

namespace Journey
{
    // One fixed peer, bounded packets, one reliable ordered pipeline. No scene/object replication.
    public sealed class SharedConnection : IDisposable
    {
        public const int Protocol = 6, MaxPacket = 1100;
        public const int MaxBufferedMessages = 1024;
        public enum Message : byte { Hello, Input, Shot, Snapshot, Hit, Retry, Abort, Ping, Pong, Configuration, ConfigurationAck, Growth, Choice, ContinueChoice, SnapshotChunk }
        NetworkDriver driver;
        NetworkPipeline pipeline;
        NetworkConnection peer;
        readonly MemoryStream buffer = new MemoryStream(MaxPacket);
        readonly BinaryWriter writer;
        readonly Queue<Retained> outgoing = new Queue<Retained>();
        readonly MemoryStream snapshotBuffer = new MemoryStream(SnapshotAssembly.MaxBytes);
        readonly BinaryWriter snapshotWriter;
        int snapshotSequence;
        public readonly NetworkWork Diagnostic = new NetworkWork();
        readonly struct Retained
        {
            public readonly byte[] Bytes;
            public readonly double At;
            public Retained(byte[] bytes) { Bytes = bytes; At = Time.realtimeSinceStartupAsDouble; }
        }
        public int PendingMessages => outgoing.Count;
        public int BufferedHighWater { get; private set; }
        public int DeferredSends { get; private set; }
        public int PendingBytes { get; private set; }
        public int BufferedBytesHighWater { get; private set; }
        public double PendingAgeMs => outgoing.Count == 0 ? 0 : (Time.realtimeSinceStartupAsDouble - outgoing.Peek().At) * 1000;
        public double OldestAgeHighWaterMs { get; private set; }
        public long SentMessages { get; private set; }
        public long ReceivedMessages { get; private set; }
        public long SnapshotBytes { get; private set; }
        public long SnapshotMessages { get; private set; }
        public double SerializationMs { get; private set; }
        double opened, received;
        bool aborted;
        public bool Host { get; }
        public bool Connected { get; private set; }
        public event Action<Message, BinaryReader> Received;
        public event Action<string> Lost;
        public long SentBytes { get; private set; }
        public long ReceivedBytes { get; private set; }
        public string BoundEndpoint => driver.GetLocalEndpoint().ToString();
        public SharedConnection(bool host, string address, ushort port, bool loopbackHost = false, bool diagnostics = false)
        {
            Diagnostic.Enabled = diagnostics;
            Host = host; writer = new BinaryWriter(buffer); snapshotWriter = new BinaryWriter(snapshotBuffer);
            var settings = new NetworkSettings();
            try
            {
                settings.WithNetworkConfigParameters(disconnectTimeoutMS: 5000, heartbeatTimeoutMS: 500,
                    connectTimeoutMS: 500, maxConnectAttempts: 10, reconnectionTimeoutMS: 0);
                driver = NetworkDriver.Create(settings);
            }
            finally { settings.Dispose(); }
            pipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            if (host)
            {
                NetworkEndpoint bind = loopbackHost ? NetworkEndpoint.LoopbackIpv4 : NetworkEndpoint.AnyIpv4;
                if (driver.Bind(bind.WithPort(port)) != 0 || driver.Listen() != 0)
                { driver.Dispose(); throw new InvalidOperationException("Cannot host on UDP port " + port); }
            }
            else
            {
                if (!NetworkEndpoint.TryParse(address, port, out NetworkEndpoint endpoint))
                { driver.Dispose(); throw new ArgumentException("Enter an IPv4 address"); }
                peer = driver.Connect(endpoint);
            }
            opened = received = Time.realtimeSinceStartupAsDouble;
        }
        public void Poll()
        {
            if (!driver.IsCreated || aborted) return;
            long stage = Diagnostic.Begin(); driver.ScheduleUpdate().Complete(); Diagnostic.driverUpdateMs += Diagnostic.End(stage);
            FlushOutgoing();
            if (aborted) return;
            stage = Diagnostic.Begin();
            try
            {
                if (Host)
                {
                    NetworkConnection incoming;
                    while ((incoming = driver.Accept()) != default)
                    {
                        if (peer.IsCreated) driver.Disconnect(incoming);
                        else peer = incoming;
                    }
                }
                if (peer.IsCreated)
                {
                    NetworkEvent.Type type;
                    while ((type = peer.PopEvent(driver, out var stream)) != NetworkEvent.Type.Empty)
                    {
                        if (type == NetworkEvent.Type.Connect)
                        {
                            Connected = true; received = Time.realtimeSinceStartupAsDouble;
                            if (!Host) Send(Message.Hello, w => w.Write(Protocol));
                        }
                        if (type == NetworkEvent.Type.Disconnect) { Fail("Peer connection lost"); return; }
                        if (type != NetworkEvent.Type.Data) continue;
                        if (stream.Length < 1 || stream.Length > MaxPacket) { Fail("Invalid packet length"); return; }
                        byte[] bytes = new byte[stream.Length];
                        for (int i = 0; i < bytes.Length; i++) bytes[i] = stream.ReadByte();
                        ReceivedBytes += bytes.Length; ReceivedMessages++; received = Time.realtimeSinceStartupAsDouble;
                        try
                        {
                            using (var reader = new BinaryReader(new MemoryStream(bytes)))
                            {
                                var message = (Message)reader.ReadByte();
                                if (Host && message == Message.Hello)
                                {
                                    if (reader.ReadInt32() != Protocol) { Fail("Protocol mismatch"); return; }
                                    SharedScenarioBinding.RequireEnd(reader);
                                    if (Connected) { Fail("Duplicate hello"); return; }
                                    Connected = true;
                                    // Notify host only after version handshake.
                                    Received?.Invoke(message, reader);
                                }
                                else
                                {
                                    if (!Enum.IsDefined(typeof(Message), message) || !Connected) { Fail("Unexpected packet before handshake"); return; }
                                    Received?.Invoke(message, reader);
                                }
                                SharedScenarioBinding.RequireEnd(reader);
                            }
                        }
                        catch (Exception e) when (e is EndOfStreamException || e is IOException || e is ArgumentException)
                        { Fail("Malformed packet: " + e.Message); return; }
                    }
                }
                double now = Time.realtimeSinceStartupAsDouble;
                if ((!Host && !Connected && now - opened > 6) || (Connected && now - received > 5)) Fail("Peer communication timeout");
            }
            finally { Diagnostic.receiveDispatchMs += Diagnostic.End(stage); }
        }
        public bool Send(Message message, Action<BinaryWriter> body)
        {
            if (!driver.IsCreated || !peer.IsCreated || aborted) return false;
            long stage = Diagnostic.Begin();
            buffer.Position = 0; buffer.SetLength(0); writer.Write((byte)message); body(writer); writer.Flush();
            Diagnostic.packetPrepareMs += Diagnostic.End(stage);
            if (buffer.Length > MaxPacket) { Fail("Packet exceeded bounded payload"); return false; }
            if (outgoing.Count == 0 && TrySend(buffer.GetBuffer(), (int)buffer.Length)) return true;
            if (aborted) return false;
            if (outgoing.Count >= MaxBufferedMessages) { Fail("Reliable outgoing backlog exceeded " + MaxBufferedMessages + " retained messages"); return false; }
            stage = Diagnostic.Begin();
            outgoing.Enqueue(new Retained(buffer.ToArray())); PendingBytes += (int)buffer.Length;
            Diagnostic.retainCopyMs += Diagnostic.End(stage);
            if (Diagnostic.Enabled) Diagnostic.queuedRecords++;
            BufferedHighWater = Math.Max(BufferedHighWater, outgoing.Count);
            BufferedBytesHighWater = Math.Max(BufferedBytesHighWater, PendingBytes); DeferredSends++; return true;
        }
        // Complete reliable snapshots remain the reference. Chunking only makes the larger
        // workload measurable; it neither coalesces authoritative outcomes nor raises the MTU.
        public bool SendSnapshot(SharedSnapshot snapshot)
        {
            long whole = Diagnostic.Begin();
            try
            {
                long started = StageProfile.Begin();
                snapshotBuffer.Position = 0; snapshotBuffer.SetLength(0);
                WriteSnapshot(snapshotWriter, snapshot); snapshotWriter.Flush();
                SerializationMs += StageProfile.End(started);
                int length = (int)snapshotBuffer.Length;
                if (length > SnapshotAssembly.MaxBytes) { Fail("Snapshot exceeded bounded assembly"); return false; }
                SnapshotBytes += length;
                byte[] data = snapshotBuffer.GetBuffer();
                if (length + 1 <= MaxPacket)
                { SnapshotMessages++; return Send(Message.Snapshot, w => w.Write(data, 0, length)); }
                int sequence = ++snapshotSequence;
                for (int offset = 0; offset < length; offset += SnapshotAssembly.ChunkBytes)
                {
                    int start = offset, count = Math.Min(SnapshotAssembly.ChunkBytes, length - offset);
                    SnapshotMessages++;
                    if (!Send(Message.SnapshotChunk, w => { w.Write(sequence); w.Write(length); w.Write(start); w.Write(data, start, count); })) return false;
                }
                return true;
            }
            finally { Diagnostic.snapshotSendMs += Diagnostic.End(whole); }
        }
        // The reliable window is finite. Backpressure retains complete records in FIFO order;
        // neither XP nor individual pellet/penetration outcomes are dropped or coalesced.
        void FlushOutgoing()
        {
            long stage = Diagnostic.Begin();
            try
            {
                OldestAgeHighWaterMs = Math.Max(OldestAgeHighWaterMs, PendingAgeMs);
                while (outgoing.Count > 0 && !aborted)
                { byte[] bytes = outgoing.Peek().Bytes; if (Diagnostic.Enabled) Diagnostic.flushAttempts++; if (!TrySend(bytes, bytes.Length)) return; outgoing.Dequeue(); PendingBytes -= bytes.Length; if (Diagnostic.Enabled) Diagnostic.flushedRecords++; }
            }
            finally { Diagnostic.flushMs += Diagnostic.End(stage); }
        }
        bool TrySend(byte[] bytes, int count)
        {
            long whole = Diagnostic.Begin(), stage = Diagnostic.Begin();
            if (Diagnostic.Enabled) Diagnostic.sendAttempts++;
            try
            {
                int status = driver.BeginSend(pipeline, peer, out var stream);
                Diagnostic.sendBeginMs += Diagnostic.End(stage);
                if (status == (int)TransportStatus.NetworkSendQueueFull) { if (Diagnostic.Enabled) Diagnostic.beginFull++; return false; }
                if (status != 0) { Fail("Transport send failed: " + status); return false; }
                stage = Diagnostic.Begin();
                for (int i = 0; i < count; i++) stream.WriteByte(bytes[i]);
                double copy = Diagnostic.End(stage); Diagnostic.sendCopyMs += copy;
                if (Diagnostic.Enabled)
                {
                    Diagnostic.copiedBytes += count;
                    if ((Message)bytes[0] == Message.Snapshot || (Message)bytes[0] == Message.SnapshotChunk)
                    { Diagnostic.snapshotCopyMs += copy; Diagnostic.snapshotCopiedBytes += count; }
                }
                stage = Diagnostic.Begin();
                status = driver.EndSend(stream);
                Diagnostic.sendEndMs += Diagnostic.End(stage);
                if (status == (int)TransportStatus.NetworkSendQueueFull) { if (Diagnostic.Enabled) Diagnostic.endFull++; return false; }
                if (status < 0) { Fail("Transport send failed: " + status); return false; }
                SentBytes += count; SentMessages++; return true;
            }
            finally { Diagnostic.sendAttemptMs += Diagnostic.End(whole); }
        }
        [Serializable] public sealed class PendingRecord
        {
            public string type;
            public int bytes;
            public double ageMs;
        }
        public PendingRecord[] ObservePending()
        {
            var result = new PendingRecord[outgoing.Count]; int i = 0;
            double now = Time.realtimeSinceStartupAsDouble;
            foreach (Retained record in outgoing)
                result[i++] = new PendingRecord { type = ((Message)record.Bytes[0]).ToString(), bytes = record.Bytes.Length, ageMs = (now - record.At) * 1000 };
            return result;
        }
        void Fail(string reason) { if (aborted) return; aborted = true; Connected = false; Lost?.Invoke(reason); }
        public void Dispose()
        {
            if (driver.IsCreated) { if (peer.IsCreated) driver.Disconnect(peer); driver.ScheduleUpdate().Complete(); driver.Dispose(); }
            writer.Dispose(); buffer.Dispose(); snapshotWriter.Dispose(); snapshotBuffer.Dispose();
            outgoing.Clear();
        }
        public static void Vector(BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        public static Vector3 Vector(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        public static void WriteShot(BinaryWriter w, SharedShot s)
        {
            w.Write(s.Run); w.Write(s.Player); w.Write(s.Life); w.Write(s.Weapon); w.Write(s.Event); w.Write(s.PoseTick);
            Vector(w, s.Ray.origin); Vector(w, s.Ray.direction);
        }
        public static SharedShot ReadShot(BinaryReader r) => new SharedShot { Run = r.ReadInt32(), Player = r.ReadInt32(), Life = r.ReadInt32(),
            Weapon = r.ReadInt32(), Event = r.ReadInt32(), PoseTick = r.ReadInt32(), Ray = new Ray(Vector(r), Vector(r)) };
        public static void WriteSnapshot(BinaryWriter w, SharedSnapshot s)
        {
            w.Write(s.Session.ToByteArray()); s.Configuration.Write(w);
            w.Write(s.Roster); w.Write(s.Xp); w.Write(s.Level);
            w.Write(s.ChoicePhase); w.Write(s.ControlRevision);
            w.Write(s.Run); w.Write(s.Tick); w.Write(s.Elapsed); w.Write((int)s.State);
            foreach (SharedPlayer p in s.Players)
            {
                Vector(w, p.Position); w.Write(p.Yaw); w.Write(p.Pitch); w.Write(p.Health); w.Write(p.Life);
                w.Write(p.ReturnAt); w.Write(p.ImmuneUntil); w.Write(p.ContactUntil); w.Write(p.Ready);
                w.Write(p.ChoiceReady);
                w.Write(p.Weapon); w.Write(p.BodyDamage); w.Write(p.RifleInterval);
                w.Write(p.Arsenal); w.Write((int)p.FirstAddition); w.Write((int)p.Specialization);
                GrowthEvent.WriteOffer(w, p.Offer);
                w.Write(p.Shots); w.Write(p.Hits); w.Write(p.Damage); w.Write(p.Kills);
            }
            foreach (SharedEnemy e in s.Enemies) { Vector(w, e.Position); w.Write(e.Health); w.Write(e.Incarnation); }
        }
        public static void ReadSnapshot(BinaryReader r, SharedSnapshot s)
        {
            Guid session = new Guid(r.ReadBytes(16)); var configuration = ScenarioConfiguration.Read(r);
            var parsed = new SharedSnapshot(configuration) { Session = session };
            ReadSnapshotBody(r, parsed); SharedScenarioBinding.RequireEnd(r); s.CopyFrom(parsed);
        }
        static void ReadSnapshotBody(BinaryReader r, SharedSnapshot s)
        {
            s.Roster = r.ReadInt32(); s.Xp = r.ReadInt32(); s.Level = r.ReadInt32();
            s.ChoicePhase = r.ReadInt32(); s.ControlRevision = r.ReadInt32();
            s.Run = r.ReadInt32(); s.Tick = r.ReadInt32(); s.Elapsed = r.ReadSingle(); s.State = (SharedState)r.ReadInt32();
            for (int i = 0; i < 2; i++) s.Players[i] = new SharedPlayer { Position = Vector(r), Yaw = r.ReadSingle(), Pitch = r.ReadSingle(),
                Health = r.ReadInt32(), Life = r.ReadInt32(), ReturnAt = r.ReadSingle(), ImmuneUntil = r.ReadSingle(), ContactUntil = r.ReadSingle(),
                Ready = r.ReadBoolean(), ChoiceReady = r.ReadBoolean(), Weapon = r.ReadInt32(), BodyDamage = r.ReadInt32(), RifleInterval = r.ReadSingle(),
                Arsenal = r.ReadInt32(), FirstAddition = (UpgradeId)r.ReadInt32(), Specialization = (UpgradeId)r.ReadInt32(),
                Offer = GrowthEvent.ReadOffer(r), Shots = r.ReadInt32(), Hits = r.ReadInt32(), Damage = r.ReadInt32(), Kills = r.ReadInt32() };
            for (int i = 0; i < s.Enemies.Length; i++) s.Enemies[i] = new SharedEnemy { Position = Vector(r), Health = r.ReadInt32(), Incarnation = r.ReadInt32() };
        }
        public static void WriteHit(BinaryWriter w, SharedHit hit)
        {
            WriteShot(w, hit.Source); w.Write((int)hit.Decision); ShotResult s = hit.Shot;
            Vector(w, s.End); w.Write(s.EnemyIndex); w.Write(s.SpawnVersion); w.Write((int)s.Region);
            w.Write(s.RequestedDamage); w.Write(s.HealthBefore); w.Write(s.Damage); w.Write(s.HealthAfter); Vector(w, hit.DeathPosition);
            w.Write(hit.Trigger); w.Write(hit.RayIndex); w.Write(hit.Rays); w.Write(hit.TargetTests);
        }
        public static SharedHit ReadHit(BinaryReader r)
        {
            SharedShot source = ReadShot(r); var decision = (ShotDecision)r.ReadInt32();
            var shot = new ShotResult(Vector(r), r.ReadInt32(), r.ReadInt32(), (HitRegion)r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
            return new SharedHit(source, decision, shot, Vector(r), r.ReadBoolean(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
        }
    }
}
