using System.Diagnostics;

namespace Journey
{
    // Reset once per rendered frame; simulation stages accumulate across catch-up ticks.
    // Stopwatch measurements remain available in release builds without Deep Profiling.
    public static class StageProfile
    {
        public static double CrowdMs, AbilityMs, ProjectileMs, WeaponMs, RenderPrepareMs, RenderSubmitMs;
        public static int Ticks, SubmittedInstances, InstanceBatches, WarningCandidates, UniqueWarnings;
        public static long Begin() => Stopwatch.GetTimestamp();
        public static double End(long start) => (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
        public static void Reset()
        {
            CrowdMs = AbilityMs = ProjectileMs = WeaponMs = RenderPrepareMs = RenderSubmitMs = 0;
            Ticks = SubmittedInstances = InstanceBatches = WarningCandidates = UniqueWarnings = 0;
        }
    }
}
