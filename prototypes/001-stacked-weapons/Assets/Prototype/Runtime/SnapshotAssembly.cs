using System;
using System.IO;

namespace Journey
{
    // One bounded, ordered reliable assembly. No partial authority state is exposed.
    // Session/configuration/run/control validation happens after complete atomic parsing.
    public sealed class SnapshotAssembly
    {
        public const int MaxBytes = 32768;
        public const int ChunkBytes = SharedConnection.MaxPacket - 1 - 3 * sizeof(int);
        readonly byte[] bytes = new byte[MaxBytes];
        int sequence, total, received, completedSequence;
        double started;
        public bool Pending => received > 0;
        public double AgeMs(double now) => Pending ? (now - started) * 1000 : 0;
        public bool Read(BinaryReader reader, SharedSnapshot destination, double now)
        {
            int next = reader.ReadInt32(), length = reader.ReadInt32(), offset = reader.ReadInt32();
            int count = checked((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            if (next < 1 || length < 1 || length > MaxBytes || offset < 0 || offset >= length ||
                count != Math.Min(ChunkBytes, length - offset)) throw new ArgumentException("Invalid snapshot chunk bounds.");
            if (received == 0)
            {
                if (offset != 0 || next != completedSequence + 1) throw new ArgumentException("Missing/stale snapshot assembly start.");
                sequence = next; total = length; started = now;
            }
            if (next != sequence || length != total || offset != received) throw new ArgumentException("Snapshot chunks changed or arrived out of order.");
            int copied = 0;
            while (copied < count)
            {
                int n = reader.Read(bytes, offset + copied, count - copied);
                if (n == 0) throw new EndOfStreamException();
                copied += n;
            }
            received += count;
            if (received != total) return false;
            using (var full = new BinaryReader(new MemoryStream(bytes, 0, total, false)))
                SharedConnection.ReadSnapshot(full, destination);
            completedSequence = sequence; received = 0; return true;
        }
    }
}
