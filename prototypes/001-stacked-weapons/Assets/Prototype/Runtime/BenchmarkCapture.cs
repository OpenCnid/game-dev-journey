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
        public string schema = "journey-benchmark-v4";
        public string simulationBackend, renderingBackend;
        public bool nativeState, hideWarnings, hideProjectiles, deduplicateWarnings, overlapPreparation, diagnosticOnly;
        public double meanWarningCandidates, meanUniqueWarnings;
        public bool burstCompiled, profilerCapture;
        public int jobBatch, instanceBatchLimit, simulationTicks;
        public double meanCrowdMsPerTick, meanAbilityMsPerTick, meanProjectileMsPerTick,
            meanWeaponMsPerFrame, meanRenderPrepareMs, meanRenderSubmitMs, meanSubmittedInstances, meanInstanceBatches;
        public double actualCaptureSeconds;
        public float captureStartSimulationSeconds, captureEndSimulationSeconds;
        public string workload, avoidance = "rotating-4-candidates-per-adjacent-cell-v1";
        public bool synchronizedAbilities;
        public int bossCount, peakProjectiles, peakTelegraphs, abilityActivations, projectilesSpawned,
            droppedProjectilesIncludingWarmup, abilityPlayerHits, abilityStateUpdates, projectileUpdates, maxNeighborChecksPerTick;
        public float simulatedCaptureSeconds, arenaHalfExtent = CombatSimulation.ArenaLimit;
        public float abilityCooldownSeconds = BossAbilities.Cooldown, telegraphSeconds = BossAbilities.TelegraphSeconds,
            areaRadius = BossAbilities.AreaRadius, projectileLifetimeSeconds = BossAbilities.ProjectileLifetime,
            projectileSpeed = BossAbilities.ProjectileSpeed;
        public int projectilePoolCapacity = BossAbilities.ProjectileCapacity, burstSize = BossAbilities.BurstSize;
        public string utc, unity, operatingSystem, cpu, gpu, graphicsApi, mode, quality;
        public BuildStamp source;
        public int cpuThreads, systemMemoryMB, gpuMemoryMB, width, height, crowd, seed, repeat;
        public int frameCap, vSync, samples, framesOverBudget, framesOverDoubleBudget, catchupLimitFrames;
        public bool editor, developmentBuild, gpuTimingAvailable, standardDuration, batchMode;
        public float warmupSeconds, captureSeconds;
        public double medianMs, p95Ms, p99Ms, maxMs, averageFps, meanSimulationMs, meanCpuFrameMs, meanGpuFrameMs;
        public int cpuTimingSamples, gpuTimingSamples, shots, kills;
        public string note = "Measured on the hardware recorded in this report. A graybox crowd does not validate the final gameplay workload. Current development benchmarks use the owner's desktop; older-hardware testing is deferred until the game is more polished.";
    }

    public sealed class BenchmarkCapture
    {
        struct Sample
        {
            public double ms, simulationMs, cpuMs, gpuMs;
            public int count, projectiles, telegraphs, neighborChecks;
            public bool catchupLimit;
            public double crowdMs, abilityMs, projectileMs, weaponMs, renderPrepareMs, renderSubmitMs;
            public int ticks, submittedInstances, instanceBatches, warningCandidates, uniqueWarnings;
        }
        readonly List<Sample> frames = new List<Sample>(65536);
        public bool Recording { get; private set; }
        public int Samples => frames.Count;
        int startActivations, startSpawned, startHits, startStateUpdates, startProjectileUpdates;
        float startSimulationTime;
        public void Begin(CombatSimulation sim)
        {
            frames.Clear(); Recording = true;
            var a = sim.Abilities;
            startActivations = a.Activations; startSpawned = a.Spawned; startHits = a.PlayerHits;
            startStateUpdates = a.StateUpdates; startProjectileUpdates = a.ProjectileUpdates;
            startSimulationTime = sim.Elapsed;
        }
        public void Cancel() { Recording = false; frames.Clear(); }
        public void Add(double ms, double simulationMs, double cpuMs, double gpuMs, CombatSimulation sim, bool catchupLimit)
        {
            if (Recording && ms > 0 && !double.IsNaN(ms))
                frames.Add(new Sample { ms = ms, simulationMs = simulationMs, cpuMs = cpuMs,
                    gpuMs = gpuMs, count = sim.Count, catchupLimit = catchupLimit,
                    projectiles = sim.Abilities.Projectiles, telegraphs = sim.Abilities.Telegraphs,
                    neighborChecks = sim.NeighborChecks,
                    crowdMs = StageProfile.CrowdMs, abilityMs = StageProfile.AbilityMs, projectileMs = StageProfile.ProjectileMs,
                    weaponMs = StageProfile.WeaponMs, renderPrepareMs = StageProfile.RenderPrepareMs,
                    renderSubmitMs = StageProfile.RenderSubmitMs, ticks = StageProfile.Ticks,
                    submittedInstances = StageProfile.SubmittedInstances, instanceBatches = StageProfile.InstanceBatches, warningCandidates = StageProfile.WarningCandidates, uniqueWarnings = StageProfile.UniqueWarnings });
        }

        public string Save(string folder, int crowd, int seed, int repeat, float warmup, float seconds, int shots, int kills,
            CombatSimulation sim, bool culled, bool profiled, double actualSeconds, ArenaPresentation view = null)
        {
            Recording = false;
            if (frames.Count == 0) throw new InvalidOperationException("No frames captured; no report written.");
            Directory.CreateDirectory(folder);
            var values = new double[frames.Count];
            TextAsset stamp = Resources.Load<TextAsset>("BuildInfo");
            var summary = new BenchmarkSummary {
                nativeState = sim.NativeState, hideWarnings = view?.HideWarnings ?? false, hideProjectiles = view?.HideProjectiles ?? false,
                deduplicateWarnings = view?.DeduplicateWarnings ?? false, overlapPreparation = view?.OverlapPreparation ?? false,
                diagnosticOnly = view?.DiagnosticOnly ?? false,
                utc = DateTime.UtcNow.ToString("o"), unity = Application.unityVersion,
                simulationBackend = sim.UseBurst ? "burst-jobs-v1" : "managed-reference",
                renderingBackend = culled ? "burst-culled-v1" : "legacy-instanced",
                burstCompiled = BurstStatus.Compiled, profilerCapture = profiled, jobBatch = sim.BatchSize,
                instanceBatchLimit = culled ? 500 : 250,
                actualCaptureSeconds = actualSeconds,
                captureStartSimulationSeconds = startSimulationTime, captureEndSimulationSeconds = sim.Elapsed,
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
                shots = shots, kills = kills,
                workload = sim.Abilities.Enabled ? "boss-mixed-v1" : "regular-v2",
                bossCount = sim.Abilities.Enabled ? sim.Count : 0, synchronizedAbilities = sim.Abilities.Synchronized,
                abilityActivations = sim.Abilities.Activations - startActivations,
                projectilesSpawned = sim.Abilities.Spawned - startSpawned,
                droppedProjectilesIncludingWarmup = sim.Abilities.Dropped,
                abilityPlayerHits = sim.Abilities.PlayerHits - startHits,
                abilityStateUpdates = sim.Abilities.StateUpdates - startStateUpdates,
                projectileUpdates = sim.Abilities.ProjectileUpdates - startProjectileUpdates,
                simulatedCaptureSeconds = sim.Elapsed - startSimulationTime
            };
            var csv = new StringBuilder("frame,frame_ms,simulation_ms,cpu_frame_ms,gpu_frame_ms,enemy_count,catchup_limit,projectiles,telegraphs,neighbor_checks_last_tick,ticks,crowd_ms,ability_ms,projectile_ms,weapon_ms,render_prepare_ms,render_submit_ms,submitted_instances,instance_batches,warning_candidates,unique_warnings\n");
            double total = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                Sample f = frames[i]; values[i] = f.ms; total += f.ms;
                summary.meanSimulationMs += f.simulationMs;
                summary.meanWarningCandidates += f.warningCandidates; summary.meanUniqueWarnings += f.uniqueWarnings;
                summary.simulationTicks += f.ticks;
                summary.meanCrowdMsPerTick += f.crowdMs; summary.meanAbilityMsPerTick += f.abilityMs;
                summary.meanProjectileMsPerTick += f.projectileMs; summary.meanWeaponMsPerFrame += f.weaponMs;
                summary.meanRenderPrepareMs += f.renderPrepareMs; summary.meanRenderSubmitMs += f.renderSubmitMs;
                summary.meanSubmittedInstances += f.submittedInstances; summary.meanInstanceBatches += f.instanceBatches;
                if (f.cpuMs > 0) { summary.meanCpuFrameMs += f.cpuMs; summary.cpuTimingSamples++; }
                if (f.gpuMs > 0) { summary.meanGpuFrameMs += f.gpuMs; summary.gpuTimingSamples++; }
                if (f.ms > 1000d / 144) summary.framesOverBudget++;
                if (f.ms > 2000d / 144) summary.framesOverDoubleBudget++;
                if (f.catchupLimit) summary.catchupLimitFrames++;
                summary.peakProjectiles = Math.Max(summary.peakProjectiles, f.projectiles);
                summary.peakTelegraphs = Math.Max(summary.peakTelegraphs, f.telegraphs);
                summary.maxNeighborChecksPerTick = Math.Max(summary.maxNeighborChecksPerTick, f.neighborChecks);
                csv.Append(i).Append(',').Append(F(f.ms)).Append(',').Append(F(f.simulationMs)).Append(',')
                    .Append(f.cpuMs > 0 ? F(f.cpuMs) : "").Append(',').Append(f.gpuMs > 0 ? F(f.gpuMs) : "")
                    .Append(',').Append(f.count).Append(',').Append(f.catchupLimit ? 1 : 0)
                    .Append(',').Append(f.projectiles).Append(',').Append(f.telegraphs).Append(',').Append(f.neighborChecks)
                    .Append(',').Append(f.ticks).Append(',').Append(F(f.crowdMs)).Append(',').Append(F(f.abilityMs))
                    .Append(',').Append(F(f.projectileMs)).Append(',').Append(F(f.weaponMs)).Append(',').Append(F(f.renderPrepareMs))
                    .Append(',').Append(F(f.renderSubmitMs)).Append(',').Append(f.submittedInstances).Append(',').Append(f.instanceBatches).Append(',').Append(f.warningCandidates).Append(',').Append(f.uniqueWarnings).Append('\n');
            }
            Array.Sort(values);
            summary.medianMs = Percentile(values, .5);
            summary.p95Ms = Percentile(values, .95);
            summary.p99Ms = Percentile(values, .99);
            summary.maxMs = values[values.Length - 1];
            summary.averageFps = frames.Count * 1000d / total;
            summary.meanSimulationMs /= frames.Count;
            summary.meanWarningCandidates /= frames.Count; summary.meanUniqueWarnings /= frames.Count;
            int ticks = Math.Max(1, summary.simulationTicks);
            summary.meanCrowdMsPerTick /= ticks; summary.meanAbilityMsPerTick /= ticks; summary.meanProjectileMsPerTick /= ticks;
            summary.meanWeaponMsPerFrame /= frames.Count; summary.meanRenderPrepareMs /= frames.Count;
            summary.meanRenderSubmitMs /= frames.Count; summary.meanSubmittedInstances /= frames.Count; summary.meanInstanceBatches /= frames.Count;
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
