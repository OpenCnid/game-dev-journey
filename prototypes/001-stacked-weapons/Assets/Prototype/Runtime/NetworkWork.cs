using System;
using System.IO;

namespace Journey
{
    // Observations only. Inclusive parents overlap their explicitly named children.
    // Cumulative process counters survive Retry; capture deltas define the measured window.
    [Serializable] public sealed class NetworkWork
    {
        [NonSerialized] public bool Enabled;
        public double driverUpdateMs, receiveDispatchMs, flushMs, snapshotSendMs;
        public double packetPrepareMs, retainCopyMs, sendAttemptMs, sendBeginMs, sendCopyMs, sendEndMs, snapshotCopyMs;
        public long sendAttempts, beginFull, endFull, copiedBytes, snapshotCopiedBytes, queuedRecords, flushAttempts, flushedRecords;
        public long Begin() => Enabled ? StageProfile.Begin() : 0;
        public double End(long started) => Enabled ? StageProfile.End(started) : 0;
        public const string Header = "driver_update_ms,receive_dispatch_ms,flush_ms,snapshot_send_ms,packet_prepare_ms,retain_copy_ms,send_attempt_ms,send_begin_ms,send_copy_ms,send_end_ms,snapshot_copy_ms,send_attempts,begin_full,end_full,copied_bytes,snapshot_copied_bytes,queued_records,flush_attempts,flushed_records";
        public void Write(StreamWriter log) => log.Write(FormattableString.Invariant($"{driverUpdateMs:F6},{receiveDispatchMs:F6},{flushMs:F6},{snapshotSendMs:F6},{packetPrepareMs:F6},{retainCopyMs:F6},{sendAttemptMs:F6},{sendBeginMs:F6},{sendCopyMs:F6},{sendEndMs:F6},{snapshotCopyMs:F6},{sendAttempts},{beginFull},{endFull},{copiedBytes},{snapshotCopiedBytes},{queuedRecords},{flushAttempts},{flushedRecords}"));
    }
}
