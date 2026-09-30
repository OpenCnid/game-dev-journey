using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Journey
{
    [Serializable]
    public sealed class BuildStamp
    {
        public string revision = "unknown", builtUtc = "unknown";
        public bool dirty;
    }

    [Serializable]
    public sealed class BenchmarkSummary
    {
        public string schema = "journey-benchmark-v1";
        public string utc, unity, operatingSystem, cpu, gpu, graphicsApi, mode, quality;
        public BuildStamp source;
        public int cpuThreads, systemMemoryMB, gpuMemoryMB, width, height, crowd, seed, repeat;
        public int frameCap, vSync, samples, framesOverBudget, framesOverDoubleBudget, catchupLimitFrames;
        public bool editor, developmentBuild, gpuTimingAvailable, standardDuration, batchMode;
        public float warmupSeconds, captureSeconds;
        public double medianMs, p95Ms, p99Ms, maxMs, averageFps, meanSimulationMs, meanCpuFrameMs, meanGpuFrameMs;
        public int cpuTimingSamples, gpuTimingSamples, shots, kills;
        public string note = "Local observation only. A graybox crowd is not the final peak encounter or validation on the i7-8700K/GTX 1080 Ti baseline.";
    }

    public sealed class BenchmarkCapture
    {
        struct Sample
        {
            public double ms, simulationMs, cpuMs, gpuMs;
            public int count;
            public bool catchupLimit;
        }
        readonly List<Sample> frames = new List<Sample>(65536);
        public bool Recording { get; private set; }
        public int Samples => frames.Count;
        public void Begin() { frames.Clear(); Recording = true; }
        public void Cancel() { Recording = false; frames.Clear(); }
        public void Add(double ms, double simulationMs, double cpuMs, double gpuMs, int count, bool catchupLimit)
        {
            if (Recording && ms > 0 && !double.IsNaN(ms))
                frames.Add(new Sample { ms = ms, simulationMs = simulationMs, cpuMs = cpuMs,
                    gpuMs = gpuMs, count = count, catchupLimit = catchupLimit });
        }

        public string Save(string folder, int crowd, int seed, int repeat, float warmup, float seconds, int shots, int kills)
        {
            Recording = false;
            if (frames.Count == 0) throw new InvalidOperationException("No frames captured; no report written.");
            Directory.CreateDirectory(folder);
            var values = new double[frames.Count];
            TextAsset stamp = Resources.Load<TextAsset>("BuildInfo");
            var summary = new BenchmarkSummary {
                utc = DateTime.UtcNow.ToString("o"), unity = Application.unityVersion,
                operatingSystem = SystemInfo.operatingSystem, cpu = SystemInfo.processorType,
                gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                cpuThreads = SystemInfo.processorCount, systemMemoryMB = SystemInfo.systemMemorySize,
                gpuMemoryMB = SystemInfo.graphicsMemorySize, width = Screen.width, height = Screen.height,
                crowd = crowd, seed = seed, repeat = repeat, frameCap = Application.targetFrameRate,
                vSync = QualitySettings.vSyncCount, samples = frames.Count, editor = Application.isEditor,
                developmentBuild = Debug.isDebugBuild, mode = "scripted-orbit-autofire-invulnerable",
                source = stamp != null ? JsonUtility.FromJson<BuildStamp>(stamp.text) : new BuildStamp(), batchMode = Application.isBatchMode,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                warmupSeconds = warmup, captureSeconds = seconds, standardDuration = warmup >= 30 && seconds >= 60,
                shots = shots, kills = kills
            };
            var csv = new StringBuilder("frame,frame_ms,simulation_ms,cpu_frame_ms,gpu_frame_ms,enemy_count,catchup_limit\n");
            double total = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                Sample f = frames[i]; values[i] = f.ms; total += f.ms;
                summary.meanSimulationMs += f.simulationMs;
                if (f.cpuMs > 0) { summary.meanCpuFrameMs += f.cpuMs; summary.cpuTimingSamples++; }
                if (f.gpuMs > 0) { summary.meanGpuFrameMs += f.gpuMs; summary.gpuTimingSamples++; }
                if (f.ms > 1000d / 144) summary.framesOverBudget++;
                if (f.ms > 2000d / 144) summary.framesOverDoubleBudget++;
                if (f.catchupLimit) summary.catchupLimitFrames++;
                csv.Append(i).Append(',').Append(F(f.ms)).Append(',').Append(F(f.simulationMs)).Append(',')
                    .Append(f.cpuMs > 0 ? F(f.cpuMs) : "").Append(',').Append(f.gpuMs > 0 ? F(f.gpuMs) : "")
                    .Append(',').Append(f.count).Append(',').Append(f.catchupLimit ? 1 : 0).Append('\n');
            }
            Array.Sort(values);
            summary.medianMs = Percentile(values, .5);
            summary.p95Ms = Percentile(values, .95);
            summary.p99Ms = Percentile(values, .99);
            summary.maxMs = values[values.Length - 1];
            summary.averageFps = frames.Count * 1000d / total;
            summary.meanSimulationMs /= frames.Count;
            if (summary.cpuTimingSamples > 0) summary.meanCpuFrameMs /= summary.cpuTimingSamples;
            if (summary.gpuTimingSamples > 0) summary.meanGpuFrameMs /= summary.gpuTimingSamples;
            summary.gpuTimingAvailable = summary.gpuTimingSamples > 0;
            string stem = Path.Combine(folder, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-crowd" + crowd + "-r" + repeat);
            File.WriteAllText(stem + ".csv", csv.ToString());
            File.WriteAllText(stem + ".json", JsonUtility.ToJson(summary, true));
            return stem + ".json";
        }
        static string F(double value) => value.ToString("F5", CultureInfo.InvariantCulture);
        // Nearest-rank percentile, input sorted ascending; same convention in tests and reports.
        public static double Percentile(double[] sorted, double percentile)
        {
            if (sorted == null || sorted.Length == 0) throw new ArgumentException("Empty samples.");
            return sorted[Math.Max(0, Math.Min(sorted.Length - 1, (int)Math.Ceiling(percentile * sorted.Length) - 1))];
        }
    }
}
