using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Journey
{
    // Opt-in local test bridge. It drives ordinary player input; authority still owns results.
    [Serializable] public sealed class SharedPreviewCommand
    {
        public long sequence;
        public string action;
        public int run, life;
        public RifleUpgrade upgrade;
        public float moveX, moveZ, yaw, pitch, duration;
        public bool fire;
        public string Validate(SharedSnapshot state, int local)
        {
            if (sequence <= 0) return "Invalid sequence";
            if (action == "quit") return "";
            if (run != state.Run || life != state.Players[local].Life) return "Stale run/life";
            if (action == "retry") return state.State == SharedState.Complete || state.State == SharedState.Defeat ? "" : "Run still active";
            if (action == "scripted" || action == "idle") return "";
            if (action == "choice") return state.State == SharedState.Running && state.Players[local].Offer.Pending && GrowthUpgrade.Valid(upgrade) ? "" : "No valid pending choice";
            if (action != "input") return "Unknown action";
            if (state.State != SharedState.Running || state.Players[local].Health <= 0) return "Player inactive";
            if (!Finite(moveX) || !Finite(moveZ) || !Finite(yaw) || !Finite(pitch) || !Finite(duration)
                || Mathf.Abs(moveX) > 1 || Mathf.Abs(moveZ) > 1 || Mathf.Abs(pitch) > 80 || duration <= 0 || duration > 2)
                return "Invalid input bounds";
            return "";
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public sealed class SharedPreview : MonoBehaviour
    {
        [Serializable] sealed class Receipt { public SharedPreviewCommand command; public string result; }
        [Serializable] sealed class State
        {
            public int run, life, local, tick, frameSequence, width, height;
            public long lastSequence;
            public string state, commandResult, captureError, source;
            public float elapsed, yaw, pitch;
            public double capturedWallSeconds;
            public bool scripted;
            public ScenarioEvidence scenario;
            public SharedPlayer[] players;
            public SharedEnemy[] enemies;
            public int xp, level;
        }
        SharedGame game;
        Camera camera;
        string output, stamp;
        RenderTexture full, thumbnail;
        long lastSequence;
        int frameSequence;
        double lastPoll, lastCapture, lastRequested;
        bool pending, closing;
        string result = "", captureError = "";
        public void Initialize(SharedGame owner, Camera target, string directory)
        {
            game = owner; camera = target; output = directory;
            var source = Resources.Load<TextAsset>("BuildInfo"); stamp = source == null ? "Editor" : source.text;
            // A camera texture keeps rendering independent of the hidden desktop's swap chain.
            full = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            full.Create(); camera.targetTexture = full;
            thumbnail = new RenderTexture(960, 540, 0, RenderTextureFormat.ARGB32); thumbnail.Create();
            StartCoroutine(Capture());
        }
        public void Poll()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (now - lastPoll < .1) return;
            lastPoll = now;
            string commandPath = Path.Combine(output, "command.json");
            try
            {
                if (File.Exists(commandPath) && new FileInfo(commandPath).Length <= 2048)
                {
                    SharedPreviewCommand command;
                    using (var stream = new FileStream(commandPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var reader = new StreamReader(stream)) command = JsonUtility.FromJson<SharedPreviewCommand>(reader.ReadToEnd());
                    if (command != null && command.sequence > lastSequence)
                    {
                        lastSequence = command.sequence;
                        result = game.ApplyPreviewCommand(command);
                        if (result.Length == 0) result = "Applied " + command.action;
                        File.AppendAllText(Path.Combine(output, "commands.jsonl"), JsonUtility.ToJson(new Receipt { command = command, result = result }) + "\n");
                    }
                }
                SharedSnapshot snapshot = game.Snapshot;
                var state = new State { run = snapshot.Run, life = snapshot.Players[game.LocalPlayer].Life,
                    local = game.LocalPlayer, tick = snapshot.Tick, elapsed = snapshot.Elapsed, state = snapshot.State.ToString(),
                    scenario = new ScenarioEvidence(snapshot.Configuration, snapshot.Session, snapshot.Run),
                    xp = snapshot.Xp, level = snapshot.Level, players = snapshot.Players, enemies = snapshot.Enemies, lastSequence = lastSequence, commandResult = result,
                    frameSequence = frameSequence, capturedWallSeconds = lastCapture, captureError = captureError,
                    source = stamp, width = Screen.width, height = Screen.height, scripted = game.Scripted,
                    yaw = game.LocalYaw, pitch = game.LocalPitch };
                WriteAtomic(Path.Combine(output, "state.json"), System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(state)));
            }
            catch (Exception error) when (error is IOException || error is ArgumentException)
            { result = error.GetType().Name; }
        }
        IEnumerator Capture()
        {
            var end = new WaitForEndOfFrame();
            while (!closing)
            {
                yield return end;
                if (pending || Time.realtimeSinceStartupAsDouble - lastRequested < 1.0 / 30) continue;
                lastRequested = Time.realtimeSinceStartupAsDouble;
                Graphics.Blit(full, thumbnail); pending = true;
                AsyncGPUReadback.Request(thumbnail, 0, TextureFormat.RGBA32, request => {
                    pending = false;
                    if (closing) return;
                    if (request.hasError) { captureError = "GPU readback failed"; return; }
                    try
                    {
                        byte[] jpeg = ImageConversion.EncodeArrayToJPG(request.GetData<byte>().ToArray(),
                            GraphicsFormat.R8G8B8A8_UNorm, 960, 540, 0, 75);
                        WriteAtomic(Path.Combine(output, "preview.jpg"), jpeg);
                        lastCapture = Time.realtimeSinceStartupAsDouble; frameSequence++; captureError = "";
                    }
                    catch (IOException error) { captureError = error.GetType().Name; }
                });
            }
        }
        static void WriteAtomic(string path, byte[] bytes)
        {
            string temporary = path + ".tmp"; File.WriteAllBytes(temporary, bytes);
            if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
        }
        void OnDestroy()
        {
            closing = true;
            if (pending) AsyncGPUReadback.WaitAllRequests();
            if (camera != null) camera.targetTexture = null;
            if (full != null) { full.Release(); Destroy(full); }
            if (thumbnail != null) { thumbnail.Release(); Destroy(thumbnail); }
        }
    }
}
