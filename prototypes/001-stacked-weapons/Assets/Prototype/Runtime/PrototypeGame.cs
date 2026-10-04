using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace Journey
{
    public sealed class PrototypeGame : MonoBehaviour
    {
        static readonly ProfilerMarker SimulationMarker = new ProfilerMarker("Journey.Simulation");
        static readonly ProfilerMarker WeaponMarker = new ProfilerMarker("Journey.Weapon");
        readonly CombatSimulation sim = new CombatSimulation();
        readonly ArenaPresentation view = new ArenaPresentation();
        readonly BenchmarkCapture capture = new BenchmarkCapture();
        readonly FrameTiming[] timings = new FrameTiming[1];
        bool benchmark, quitAfterBenchmark, screenshotSaved, screenshotAtMenu;
        bool bosses = true, synchronizedAbilities;
        int crowd = 100, seed = 137, repeat = 1, repeats = 3;
        int savedCap = 144, savedVSync, captureStartShots, captureStartKills;
        float yaw, pitch, accumulator;
        float warmup = 30, captureSeconds = 60, frameMs = 6.94f;
        double runStarted, previousClock, captureStarted;
        string outputDirectory, lastResult = "", error = "", screenshotPath;
        string customCap = "144";
        string customRifleRate = CombatSimulation.DefaultRifleRate.ToString("G8", CultureInfo.InvariantCulture), rateError = "";
        Vector2 menuScroll;
        string profilePath;
        float profileSeconds = 5;
        double profileStarted;
        bool profileActive, profileSaved;
        GUIStyle title, label, small, heading, button;
        public RunState State => sim.State;
        public int ShotsFired => sim.Shots;
        public float RifleRate => sim.RifleRate;

        void Awake()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 144;
            view.Create(transform);
            outputDirectory = Path.Combine(Application.persistentDataPath, "BenchmarkResults");
            previousClock = Time.realtimeSinceStartupAsDouble;
            sim.UseBurst = true;
            ParseArguments();
            BurstStatus.Verify();
            if (!benchmark) SetCursor(false);
        }

        void ParseArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool autoBenchmark = false, autoPlay = false;
            for (int i = 0; i < args.Length; i++)
            {
                string value = i + 1 < args.Length ? args[i + 1] : "";
                if (args[i] == "-benchmark") autoBenchmark = true;
                if (args[i] == "-playtest") autoPlay = true;
                if (args[i] == "-quitAfterBenchmark") quitAfterBenchmark = true;
                if (args[i] == "-screenshotAtMenu") screenshotAtMenu = true;
                if (args[i] == "-crowd" && int.TryParse(value, out int c)) crowd = Mathf.Clamp(c, 0, CombatSimulation.Capacity);
                if (args[i] == "-workload") bosses = value == "bosses";
                if (args[i] == "-hideWarnings") view.HideWarnings = true;
                if (args[i] == "-hideProjectiles") view.HideProjectiles = true;
                if (args[i] == "-deduplicateWarnings") view.DeduplicateWarnings = true;
                if (args[i] == "-overlapPreparation") view.OverlapPreparation = true;
                if (args[i] == "-nativeState") sim.UseNativeState = true;
                if (args[i] == "-projectileRenderer") view.ProjectileRenderer = value;
                if (args[i] == "-combinedCharacters") view.CombinedCharacters = true;
                if (args[i] == "-cacheWarnings") view.CacheWarnings = true;
                if (args[i] == "-synchronizedAbilities") synchronizedAbilities = true;
                if (args[i] == "-simulation") sim.UseBurst = value == "burst";
                if (args[i] == "-rendering") view.CullInstances = value == "culled";
                if (args[i] == "-jobBatch" && int.TryParse(value, out int batch)) sim.BatchSize = view.BatchSize = Mathf.Clamp(batch, 16, 1024);
                if (args[i] == "-profile") profilePath = Path.GetFullPath(value);
                if (args[i] == "-profileSeconds" && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float ps)) profileSeconds = Mathf.Clamp(ps, 1, 30);
                if (args[i] == "-seed" && int.TryParse(value, out int s)) seed = s;
                if (args[i] == "-rifleRate" && !TrySetRifleRate(value)) throw new ArgumentException(rateError);
                if (args[i] == "-repeats" && int.TryParse(value, out int r)) repeats = Mathf.Clamp(r, 1, 20);
                if (args[i] == "-warmup" && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float w)) warmup = Mathf.Clamp(w, 0, 300);
                if (args[i] == "-seconds" && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float t)) captureSeconds = Mathf.Clamp(t, 1, 600);
                if (args[i] == "-results" && !string.IsNullOrWhiteSpace(value)) outputDirectory = Path.GetFullPath(value);
                if (args[i] == "-screenshot" && !string.IsNullOrWhiteSpace(value)) screenshotPath = Path.GetFullPath(value);
            }
            if (view.ProjectileRenderer != "cube" && view.ProjectileRenderer != "octa" && view.ProjectileRenderer != "indirect-cube" && view.ProjectileRenderer != "indirect-octa")
                throw new InvalidOperationException("Unknown projectile renderer.");
            if (view.ProjectileRenderer.StartsWith("indirect-") && (!sim.UseNativeState || !sim.UseBurst))
                throw new InvalidOperationException("Indirect projectiles require native Burst state.");
            if (view.DiagnosticOnly && !autoBenchmark) throw new InvalidOperationException("Hidden effects require -benchmark.");
            if (!view.CullInstances && (view.DiagnosticOnly || view.DeduplicateWarnings || view.OverlapPreparation || view.ProjectileRenderer != "cube" || view.CacheWarnings || view.CombinedCharacters))
                throw new InvalidOperationException("Rendering experiments require -rendering culled.");
            if (autoBenchmark) StartBenchmark(crowd);
            else if (autoPlay) StartCombat();
        }

        void Update()
        {
            StageProfile.Reset();
            double now = Time.realtimeSinceStartupAsDouble;
            double interval = (now - previousClock) * 1000; previousClock = now;
            frameMs = Mathf.Lerp(frameMs, (float)interval, .05f);
            FrameTimingManager.CaptureFrameTimings();
            uint timingCount = FrameTimingManager.GetLatestTimings(1, timings);
            double cpuMs = timingCount > 0 ? timings[0].cpuFrameTime : 0;
            double gpuMs = timingCount > 0 ? timings[0].gpuFrameTime : 0;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (benchmark) CancelBenchmark();
                else { sim.TogglePause(); SetCursor(sim.State == RunState.Running); }
            }
            if (!benchmark && keyboard != null && keyboard.rKey.wasPressedThisFrame) StartCombat();
            if (!benchmark && sim.State == RunState.Running && Mouse.current != null)
            {
                Vector2 mouse = Mouse.current.delta.ReadValue();
                yaw += mouse.x * .09f; pitch = Mathf.Clamp(pitch - mouse.y * .09f, -80, 80);
            }
            Vector3 movement = Vector3.zero;
            if (!benchmark && keyboard != null)
            {
                float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
                movement = Quaternion.Euler(0, yaw, 0) * new Vector3(x, 0, z);
            }
            long begin = Stopwatch.GetTimestamp();
            bool catchupLimit = false, stepped = false;
            using (SimulationMarker.Auto())
            {
                if (sim.State == RunState.Running)
                {
                    accumulator += (float)(interval * .001);
                    int ticks = 0;
                    while (accumulator >= CombatSimulation.Tick && ticks < 8 && sim.State == RunState.Running)
                    {
                        sim.Step(movement, CombatSimulation.Tick);
                        stepped = true;
                        if (benchmark)
                        {
                            Vector3 direction = -sim.Player; direction.y = -.35f;
                            Quaternion aim = Quaternion.LookRotation(direction);
                            yaw = aim.eulerAngles.y; pitch = aim.eulerAngles.x;
                        }
                        if (benchmark)
                        {
                            Ray ray = new Ray(sim.Player + Vector3.up * 1.65f, Quaternion.Euler(pitch, yaw, 0) * Vector3.forward);
                            long weaponStart = StageProfile.Begin();
                            using (WeaponMarker.Auto())
                                while (sim.TryFire(ray, out ShotResult shot)) view.ShowShot(shot.End, benchmark: true);
                            StageProfile.WeaponMs += StageProfile.End(weaponStart);
                        }
                        accumulator -= CombatSimulation.Tick; ticks++;
                    }
                    if (accumulator >= CombatSimulation.Tick)
                    { catchupLimit = true; accumulator %= CombatSimulation.Tick; }
                    if (sim.State != RunState.Running) { SetCursor(false); accumulator = 0; }
                }
            }
            double simulationMs = (Stopwatch.GetTimestamp() - begin) * 1000d / Stopwatch.Frequency;
            float alpha = accumulator / CombatSimulation.Tick;
            // Prepare the displayed pose and age old cues before consuming new shot events.
            view.BeginFrame(sim, alpha, yaw, pitch, Time.unscaledDeltaTime);
            if (!benchmark) view.Feedback.ObservePlayerHealth(sim.PlayerHealth);
            if (!benchmark && stepped)
            {
                Ray ray = new Ray(view.Camera.transform.position, view.Camera.transform.forward);
                long weaponStart = StageProfile.Begin();
                using (WeaponMarker.Auto())
                    while (sim.TryFire(ray, out ShotResult shot, alpha))
                    { view.ShowShot(shot.End); view.Feedback.RecordShot(sim, shot, alpha); }
                StageProfile.WeaponMs += StageProfile.End(weaponStart);
            }
            view.Draw(sim, alpha);

            if (benchmark)
            {
                double elapsed = now - runStarted;
                // Align the gameplay phase as well as wall-clock warm-up. Initial shader/
                // driver stalls may hit the catch-up bound before capture begins.
                if (!capture.Recording && elapsed >= warmup && sim.Elapsed >= warmup)
                { capture.Begin(sim); captureStarted = now; captureStartShots = sim.Shots; captureStartKills = sim.Kills; StartProfile(); }
                else if (capture.Recording)
                {
                    capture.Add(interval, simulationMs, cpuMs, gpuMs, sim, catchupLimit, timingCount > 0 ? timings[0].cpuMainThreadFrameTime : 0, timingCount > 0 ? timings[0].cpuRenderThreadFrameTime : 0);
                    if (now - captureStarted >= captureSeconds) FinishCapture(now - captureStarted);
                }
            }
            if (profileActive && now - profileStarted >= profileSeconds) StopProfile();
            if (!screenshotSaved && screenshotPath != null && (screenshotAtMenu ? sim.State == RunState.Ready : sim.Elapsed >= 3))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath));
                ScreenCapture.CaptureScreenshot(screenshotPath); screenshotSaved = true;
            }
        }

        public void StartCombat()
        {
            if (benchmark) RestoreFrameSettings();
            benchmark = false; capture.Cancel(); error = "";
            yaw = pitch = accumulator = 0;
            view.ResetFeedback(); sim.Reset(35, seed, false); SetCursor(true);
        }
        public bool TrySetRifleRate(string value)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float rate) ||
                !(rate >= CombatSimulation.MinRifleRate && rate <= CombatSimulation.MaxRifleRate))
            { rateError = "Enter a rifle rate from 0.5 to 120 shots/sec."; return false; }
            SetRifleRate(rate); return true;
        }
        void SetRifleRate(float rate)
        {
            sim.RifleRate = rate;
            customRifleRate = sim.RifleRate.ToString("G8", CultureInfo.InvariantCulture); rateError = "";
        }
        void RifleRateControls()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Rifle rate (shots/sec)", small, GUILayout.Width(160));
            customRifleRate = GUILayout.TextField(customRifleRate, 12, GUILayout.Width(90), GUILayout.Height(28));
            if (GUILayout.Button("Apply", GUILayout.Height(28))) TrySetRifleRate(customRifleRate);
            GUILayout.Label($"Active: {sim.RifleRate:0.##}/s", small, GUILayout.Width(110));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Slow 2/s", GUILayout.Height(26))) SetRifleRate(2);
            if (GUILayout.Button("Baseline 8.33/s", GUILayout.Height(26))) SetRifleRate(CombatSimulation.DefaultRifleRate);
            if (GUILayout.Button("Rapid 30/s", GUILayout.Height(26))) SetRifleRate(30);
            if (GUILayout.Button("Stress 120/s", GUILayout.Height(26))) SetRifleRate(120);
            GUILayout.EndHorizontal();
            GUILayout.Label(rateError.Length > 0 ? rateError : "Combat + benchmark · 0.5–120 shots/sec · Restart keeps the selected rate.", small);
        }
        void StartBenchmark(int count)
        {
            if (!benchmark) { savedCap = Application.targetFrameRate; savedVSync = QualitySettings.vSyncCount; }
            benchmark = true; crowd = count; repeat = 1; error = "";
            Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0;
            ResetCapture(); SetCursor(false);
        }
        void ResetCapture()
        {
            accumulator = 0; capture.Cancel(); view.ResetFeedback();
            sim.Reset(crowd, seed, true, bosses: bosses, synchronizedAbilities: synchronizedAbilities);
            runStarted = Time.realtimeSinceStartupAsDouble;
            previousClock = runStarted;
        }
        void FinishCapture(double actualSeconds)
        {
            try
            {
                lastResult = capture.Save(outputDirectory, crowd, seed, repeat, warmup, captureSeconds,
                    sim.Shots - captureStartShots, sim.Kills - captureStartKills, sim, view.CullInstances, profilePath != null, actualSeconds, view);
                Debug.Log("BENCHMARK_REPORT " + lastResult);
                if (repeat++ < repeats) { ResetCapture(); return; }
                CancelBenchmark();
                if (quitAfterBenchmark) Application.Quit(0);
            }
            catch (Exception exception)
            {
                error = "Could not save benchmark: " + exception.Message; Debug.LogException(exception);
                CancelBenchmark(); if (quitAfterBenchmark) Application.Quit(2);
            }
        }
        void CancelBenchmark()
        {
            StopProfile();
            capture.Cancel(); benchmark = false; RestoreFrameSettings();
            if (sim.State == RunState.Running) sim.TogglePause(); SetCursor(false);
        }
        void RestoreFrameSettings() { Application.targetFrameRate = savedCap; QualitySettings.vSyncCount = savedVSync; }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && !benchmark && sim.State == RunState.Running) { sim.TogglePause(); SetCursor(false); }
        }
        void StartProfile()
        {
            if (profilePath == null || profileSaved) return;
            if (!Debug.isDebugBuild) throw new InvalidOperationException("-profile requires a Development player build.");
            Directory.CreateDirectory(Path.GetDirectoryName(profilePath));
            UnityEngine.Profiling.Profiler.logFile = profilePath;
            UnityEngine.Profiling.Profiler.enableBinaryLog = true;
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.GPU, true);
            UnityEngine.Profiling.Profiler.enabled = true;
            profileStarted = Time.realtimeSinceStartupAsDouble; profileActive = true;
        }
        void StopProfile()
        {
            if (!profileActive) return;
            UnityEngine.Profiling.Profiler.enabled = false;
            UnityEngine.Profiling.Profiler.logFile = "";
            profileActive = false; profileSaved = true;
            Debug.Log("PROFILER_REPORT " + profilePath);
        }
        void OnDestroy() { StopProfile(); view.Dispose(); sim.Dispose(); SetCursor(false); }
        static void SetCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(.45f, .95f, .96f);
            heading = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            small.normal.textColor = new Color(.65f, .75f, .8f);
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, fixedHeight = 38 };
        }
        void OnGUI()
        {
            Styles();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.Label(new Rect(30, 20, 500, 50), "STACK / LAB", title);
            GUI.Label(new Rect(32, 69, 700, 24), $"01   /   PULSE RIFLE     ·     {sim.RifleRate:0.##} SHOTS/SEC     ·     CROWD SIMULATION", small);
            GUI.Label(new Rect(width - 320, 26, 300, 30), $"{1000 / Mathf.Max(.01f, frameMs):F0} FPS   /   {frameMs:F2} ms", heading);
            GUI.Label(new Rect(width - 320, 59, 300, 25), "144 FPS TARGET  ·  6.94 ms BUDGET", small);
            if (sim.State == RunState.Running)
            {
                GUI.color = benchmark ? Color.white : view.Feedback.CrosshairColor;
                GUI.DrawTexture(new Rect(width / 2 - 7, height / 2 - 1, 14, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(width / 2 - 1, height / 2 - 7, 2, 14), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(32, height - 70, 1100, 32), $"{(benchmark ? "BENCH" : "HP " + sim.PlayerHealth)} / {sim.Count} {(sim.Abilities.Enabled ? "BOSSES" : "DRONES")} / {sim.Kills} KILLS / {sim.Abilities.Projectiles} PROJECTILES", heading);
                GUI.Label(new Rect(32, height - 38, 700, 28), benchmark ? "SCRIPTED ORBIT + AUTOFIRE  /  ESC CANCELS" : "WASD MOVE  /  MOUSE AIM  /  AUTO FIRE  /  ESC PAUSE  /  R RESTART", small);
                if (!benchmark) GUI.Label(new Rect(width - 200, height - 65, 180, 32), $"{Mathf.Max(0, 60 - sim.Elapsed):F1}s", heading);
            }
            if (!benchmark) view.Feedback.DrawHUD(width, height);
            if (benchmark)
            {
                if (view.DiagnosticOnly) GUI.Label(new Rect(32, 100, 800, 30), "DIAGNOSTIC: EFFECTS HIDDEN / NOT AN ACCEPTANCE RUN", heading);
                float remaining = capture.Recording ? Mathf.Max(0, captureSeconds - (float)(Time.realtimeSinceStartupAsDouble - captureStarted)) :
                    Mathf.Max(0, warmup - (float)(Time.realtimeSinceStartupAsDouble - runStarted)) + captureSeconds;
                GUI.Box(new Rect(width / 2 - 260, 110, 520, 80), "");
                GUI.Label(new Rect(width / 2 - 240, 120, 480, 26), $"{(capture.Recording ? "CAPTURING" : "WARMING UP")} · {crowd} {(bosses ? "BOSSES" : "DRONES")} · {repeat}/{repeats}", label);
                GUI.Label(new Rect(width / 2 - 240, 153, 480, 25), $"{remaining:F1}s left this repeat  /  {capture.Samples} samples  /  uncapped", small);
            }
            else if (sim.State != RunState.Running)
            {
                GUILayout.BeginArea(new Rect(width / 2 - 280, 130, 560, height - 155), GUI.skin.box);
                menuScroll = GUILayout.BeginScrollView(menuScroll);
                GUILayout.Space(18);
                string status = sim.State == RunState.Won ? "ENCOUNTER COMPLETE" : sim.State == RunState.Lost ? "RIG OFFLINE" : sim.State == RunState.Paused ? "SIMULATION PAUSED" : "ONE WEAPON. HOLD THE LINE.";
                GUILayout.Label(status, heading);
                GUILayout.Label("Survive 60 seconds. Two body hits or one head hit destroy a fresh drone. Crosshair: green body ×1, gold head ×2, white miss. Movement keeps you alive.", label);
                GUILayout.Space(12);
                RifleRateControls();
                GUILayout.Space(8);
                if (sim.State == RunState.Paused && !sim.Benchmark && GUILayout.Button("RESUME", button)) { sim.TogglePause(); SetCursor(true); }
                if (GUILayout.Button("START / RESTART COMBAT", button)) StartCombat();
                GUILayout.Space(12);
                GUILayout.Label("REPEATABLE CROWD BENCHMARK", heading);
                sim.UseBurst = GUILayout.Toggle(sim.UseBurst, " Jobs/Burst simulation");
                if (!sim.UseBurst) sim.UseNativeState = false;
                view.CullInstances = GUILayout.Toggle(view.CullInstances, " Burst instance preparation + frustum culling");
                GUILayout.Label($"{warmup:0}s warm-up + {captureSeconds:0}s capture, {repeats} repeats. Automated movement, autofire, invulnerability. Writes CSV + JSON.", small);
                bosses = GUILayout.Toggle(bosses, " Boss workload: area attacks + projectile bursts");
                synchronizedAbilities = GUILayout.Toggle(synchronizedAbilities, " Synchronized ability spike stress");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("1,000", button)) StartBenchmark(1000);
                if (GUILayout.Button("10,000", button)) StartBenchmark(10000);
                if (GUILayout.Button("15,000", button)) StartBenchmark(15000);
                if (GUILayout.Button("20,000", button)) StartBenchmark(20000);
                GUILayout.EndHorizontal();
                GUILayout.Space(8);
                GUILayout.BeginHorizontal(); GUILayout.Label("FPS cap", label, GUILayout.Width(80));
                customCap = GUILayout.TextField(customCap, 4, GUILayout.Width(75), GUILayout.Height(30));
                if (GUILayout.Button("Apply", GUILayout.Height(30)) && int.TryParse(customCap, out int cap)) Application.targetFrameRate = Mathf.Clamp(cap, 30, 1000);
                if (GUILayout.Button("144", GUILayout.Height(30))) { Application.targetFrameRate = 144; customCap = "144"; }
                if (GUILayout.Button("240", GUILayout.Height(30))) { Application.targetFrameRate = 240; customCap = "240"; }
                if (GUILayout.Button("Uncapped", GUILayout.Height(30))) Application.targetFrameRate = -1;
                GUILayout.EndHorizontal();
                bool sync = GUILayout.Toggle(QualitySettings.vSyncCount > 0, " VSync (display refresh takes precedence over the cap)"); QualitySettings.vSyncCount = sync ? 1 : 0;
                if (lastResult.Length > 0) GUILayout.Label("Saved: " + lastResult, small);
                if (error.Length > 0) GUILayout.Label(error, small);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }
    }
}
