using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Journey
{
    // Pair orchestration/input/presentation; never calculates authoritative damage.
    public sealed class SharedGame : MonoBehaviour
    {
        readonly SharedSnapshot display = new SharedSnapshot(), incoming = new SharedSnapshot();
        readonly Queue<SharedHit> feedback = new Queue<SharedHit>();
        readonly Dictionary<int, double> emitted = new Dictionary<int, double>();
        readonly List<double> frames = new List<double>(), confirms = new List<double>();
        ArenaPresentation view;
        CombatSimulation projection;
        SharedConnection connection;
        SharedEncounter authority;
        GameObject ally;
        LineRenderer allyShot;
        int local, run, life, inputSequence, shotSequence, catchupLimits;
        float yaw, pitch, inputTime, inputRemaining, shotRemaining, accumulator, pingRemaining;
        Vector3 predicted;
        bool inputActive = true, scripted, quitAtEnd, saved;
        double previousClock, lastRtt;
        string reason = "", output = "", screenshot = "";
        StreamWriter frameLog, hitLog;
        GUIStyle heading, small;
        SharedPreview preview;
        Vector3 previewMovement;
        double previewInputUntil;
        bool previewFire;
        public SharedSnapshot Snapshot => display;
        public int LocalPlayer => local;
        public bool Scripted => scripted;
        public float LocalYaw => yaw;
        public float LocalPitch => pitch;
        public void Initialize(ArenaPresentation arena, CombatSimulation sim, bool host, string address, ushort port)
        {
            view = arena; projection = sim; local = host ? 0 : 1;
            var args = Environment.GetCommandLineArgs();
            scripted = Array.IndexOf(args, "-sharedScripted") >= 0; quitAtEnd = Array.IndexOf(args, "-sharedQuit") >= 0;
            if (Array.IndexOf(args, "-sharedUncapped") >= 0) { Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0; }
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-sharedResults") output = Path.GetFullPath(args[i + 1]);
                if (args[i] == "-screenshot") screenshot = Path.GetFullPath(args[i + 1]);
            }
            if (output.Length == 0) output = Path.Combine(Application.persistentDataPath, "SharedCombat", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), host ? "host" : "client");
            Directory.CreateDirectory(output);
            if (Array.IndexOf(args, "-sharedPreview") >= 0)
            { preview = gameObject.AddComponent<SharedPreview>(); preview.Initialize(this, view.Camera, output); }
            frameLog = new StreamWriter(Path.Combine(output, "frames.csv"));
            frameLog.WriteLine("wall_seconds,frame_ms,run,tick,state,A_shots,B_shots,A_hits,B_hits,A_damage,B_damage,A_kills,B_kills,A_hp,B_hp,enemies,rtt_ms");
            hitLog = new StreamWriter(Path.Combine(output, "events.csv"));
            hitLog.WriteLine("wall_seconds,run,player,weapon,event,pose_tick,life,decision,enemy,incarnation,region,requested,before,actual,after,confirmed_delay_ms");
            connection = new SharedConnection(host, address, port);
            connection.Received += Receive; connection.Lost += Abort;
            if (host) { authority = new SharedEncounter(); authority.Resolved += Resolve; }
            ally = GameObject.CreatePrimitive(PrimitiveType.Capsule); ally.name = "Teammate " + (host ? "B" : "A");
            Destroy(ally.GetComponent<Collider>()); ally.transform.SetParent(transform);
            ally.GetComponent<Renderer>().material.color = new Color(.1f, .9f, .8f); ally.SetActive(false);
            allyShot = new GameObject("Teammate shot").AddComponent<LineRenderer>(); allyShot.transform.SetParent(transform);
            allyShot.positionCount = 2; allyShot.startWidth = .018f; allyShot.endWidth = .004f;
            allyShot.sharedMaterial = new Material(Resources.Load<Material>("Trail")); allyShot.sharedMaterial.color = new Color(.1f, .9f, .8f);
            allyShot.enabled = false;
            previousClock = Time.realtimeSinceStartupAsDouble;
            SetCursor(!scripted && preview == null); Debug.Log("SHARED_SESSION " + (host ? "HOST" : "CLIENT") + " " + address + ":" + port + " " + output);
        }
        void Receive(SharedConnection.Message message, BinaryReader r)
        {
            if (message == SharedConnection.Message.Hello && authority != null && display.State == SharedState.Waiting)
            { authority.Start(); accumulator = 0; previousClock = Time.realtimeSinceStartupAsDouble; Publish(); }
            else if (message == SharedConnection.Message.Input && authority != null)
            {
                int commandRun = r.ReadInt32(), commandLife = r.ReadInt32(), sequence = r.ReadInt32();
                float time = r.ReadSingle(); Vector3 position = SharedConnection.Vector(r); float y = r.ReadSingle(), p = r.ReadSingle();
                authority.Input(commandRun, 1, commandLife, sequence, time, position, y, p);
            }
            else if (message == SharedConnection.Message.Shot && authority != null)
            { SharedShot shot = SharedConnection.ReadShot(r); shot.Player = 1; authority.Submit(shot); }
            else if (message == SharedConnection.Message.Snapshot && authority == null)
            {
                SharedConnection.ReadSnapshot(r, incoming);
                if (incoming.Run > display.Run || incoming.Run == display.Run && incoming.Tick >= display.Tick) display.CopyFrom(incoming);
            }
            else if (message == SharedConnection.Message.Hit && authority == null) feedback.Enqueue(SharedConnection.ReadHit(r));
            else if (message == SharedConnection.Message.Retry && authority != null) { authority.Retry(r.ReadInt32(), 1); Publish(); }
            else if (message == SharedConnection.Message.Abort) Abort(r.ReadString());
            else if (message == SharedConnection.Message.Ping) { double stamp = r.ReadDouble(); connection.Send(SharedConnection.Message.Pong, w => w.Write(stamp)); }
            else if (message == SharedConnection.Message.Pong) lastRtt = (Time.realtimeSinceStartupAsDouble - r.ReadDouble()) * 1000;
        }
        void Resolve(SharedHit hit)
        {
            feedback.Enqueue(hit); connection.Send(SharedConnection.Message.Hit, w => SharedConnection.WriteHit(w, hit));
        }
        void Publish()
        {
            display.CopyFrom(authority.Current);
            connection.Send(SharedConnection.Message.Snapshot, w => SharedConnection.WriteSnapshot(w, display));
        }
        void Abort(string value)
        {
            if (display.State == SharedState.Aborted) return;
            reason = value; authority?.Abort(value); display.State = SharedState.Aborted;
            connection?.Send(SharedConnection.Message.Abort, w => w.Write(value));
            SetCursor(false); Debug.Log("SHARED_ABORT " + value);
        }
        void SyncGeneration()
        {
            SharedPlayer player = display.Players[local];
            if (display.Run == run && player.Life == life) return;
            bool newRun = display.Run != run; run = display.Run; life = player.Life;
            predicted = player.Position; yaw = player.Yaw; pitch = player.Pitch;
            inputTime = inputRemaining = shotRemaining = 0; inputSequence = 0; view.ResetFeedback();
            previewInputUntil = 0; previewMovement = Vector3.zero; previewFire = false;
            if (newRun)
            {
                shotSequence = 0; emitted.Clear(); feedback.Clear(); saved = false;
                frames.Clear(); confirms.Clear(); catchupLimits = 0;
            }
        }
        void Update()
        {
            if (connection == null) return;
            double now = Time.realtimeSinceStartupAsDouble, frame = (now - previousClock) * 1000; previousClock = now;
            bool runningAtFrameStart = display.State == SharedState.Running;
            int frameRun = display.Run;
            connection.Poll();
            SyncGeneration();
            preview?.Poll();
            SharedPlayer player = display.Players[local];
            Keyboard keyboard = preview == null ? Keyboard.current : null;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { inputActive = !inputActive; SetCursor(inputActive && display.State == SharedState.Running); }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) Retry();
            float dt = Mathf.Min(.05f, (float)(frame * .001));
            bool active = display.State == SharedState.Running && player.Health > 0;
            Vector3 movement = Vector3.zero;
            bool previewActive = preview != null && now <= previewInputUntil;
            if (active && (scripted || (preview == null ? inputActive : previewActive)))
            {
                if (scripted)
                {
                    float t = display.Elapsed;
                    Vector3 goal = new Vector3(Mathf.Cos(t * .30f + local * Mathf.PI) * 22, 0, Mathf.Sin(t * .30f + local * Mathf.PI) * 22);
                    movement = (goal - predicted).normalized;
                    // Shared slot for the opening, then clear nearby threats. No invulnerability/fixture damage changes.
                    int target = 0;
                    if (t > 2) for (int i = 1; i < display.Enemies.Length; i++)
                        if ((display.Enemies[i].Position - predicted).sqrMagnitude < (display.Enemies[target].Position - predicted).sqrMagnitude) target = i;
                    Vector3 to = display.Enemies[target].Position + Vector3.up * (local == 0 ? .8f : 1.6f) - (predicted + Vector3.up * 1.65f);
                    Quaternion aim = Quaternion.LookRotation(to); yaw = aim.eulerAngles.y; pitch = aim.eulerAngles.x; if (pitch > 180) pitch -= 360;
                }
                else if (preview != null) movement = previewMovement;
                else
                {
                    if (Mouse.current != null) { Vector2 mouse = Mouse.current.delta.ReadValue(); yaw += mouse.x * .09f; pitch = Mathf.Clamp(pitch - mouse.y * .09f, -80, 80); }
                    if (keyboard != null)
                    {
                        float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                        float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
                        movement = Quaternion.Euler(0, yaw, 0) * new Vector3(x, 0, z);
                    }
                }
            }
            if (active)
            {
                predicted = SharedEncounter.Move(predicted, movement, dt); inputTime += dt;
                inputRemaining -= dt;
                if (inputRemaining <= 0)
                {
                    inputRemaining += CombatSimulation.Tick; inputSequence++;
                    if (authority != null) authority.Input(run, local, life, inputSequence, inputTime, predicted, yaw, pitch);
                    else connection.Send(SharedConnection.Message.Input, w => {
                        w.Write(run); w.Write(life); w.Write(inputSequence); w.Write(inputTime); SharedConnection.Vector(w, predicted); w.Write(yaw); w.Write(pitch); });
                }
            }
            if (authority != null && display.State == SharedState.Running)
            {
                // Hello/Retry can start a run inside Poll. Its preceding Waiting/terminal frame is not combat time.
                if (runningAtFrameStart && frameRun == display.Run) accumulator += (float)(frame * .001);
                int ticks = 0;
                while (accumulator >= CombatSimulation.Tick && ticks < 8 && authority.Current.State == SharedState.Running)
                { authority.Step(); accumulator -= CombatSimulation.Tick; ticks++; Publish(); }
                if (accumulator >= CombatSimulation.Tick) { catchupLimits++; Abort("Authority tick catch-up limit"); accumulator = 0; }
                else if (authority.Current.State == SharedState.Aborted) Abort(authority.AbortReason);
            }
            SyncGeneration();
            player = display.Players[local]; active = display.State == SharedState.Running && player.Health > 0;
            int watched = player.Health == 0 && display.Players[1 - local].Health > 0 ? 1 - local : local;
            Vector3 cameraPosition = watched == local ? predicted : display.Players[watched].Position;
            float cameraYaw = watched == local ? yaw : display.Players[watched].Yaw, cameraPitch = watched == local ? pitch : display.Players[watched].Pitch;
            projection.ProjectShared(display, local, cameraPosition);
            view.BeginFrame(projection, 1, cameraYaw, cameraPitch, dt); view.Feedback.ObservePlayerHealth(player.Health);
            allyShot.enabled = false;
            while (feedback.Count > 0)
            {
                SharedHit hit = feedback.Dequeue();
                if (hit.Source.Run != run) continue;
                double delay = -1;
                if (hit.Source.Player == local && emitted.TryGetValue(hit.Source.Event, out double emission))
                {
                    delay = (now - emission) * 1000; emitted.Remove(hit.Source.Event);
                    if (hit.Decision == ShotDecision.Damage) confirms.Add(delay);
                }
                ShotResult s = hit.Shot;
                hitLog.WriteLine(FormattableString.Invariant($"{now:F6},{run},{hit.Source.Player},{hit.Source.Weapon},{hit.Source.Event},{hit.Source.PoseTick},{hit.Source.Life},{hit.Decision},{s.EnemyIndex},{s.SpawnVersion},{s.Region},{s.RequestedDamage},{s.HealthBefore},{s.Damage},{s.HealthAfter},{delay:F3}"));
                if (s.Hit)
                {
                    if (hit.Source.Player == local && hit.Source.Life == life) view.Feedback.RecordShot(projection, s);
                    else { allyShot.SetPosition(0, hit.Source.Ray.origin); allyShot.SetPosition(1, s.End); allyShot.enabled = true; }
                }
            }
            if (active && (scripted || (preview == null ? inputActive : previewActive && previewFire)))
            {
                shotRemaining -= dt;
                if (shotRemaining <= 0)
                {
                    shotRemaining = Mathf.Max(0, shotRemaining + player.RifleInterval);
                    Ray ray = new Ray(view.Camera.transform.position, view.Camera.transform.forward);
                    AimResult preview = display.Query(ray);
                    view.ShowShot(ray.GetPoint(preview.Distance)); // Immediate cue only, no hit grant.
                    var shot = new SharedShot { Run = run, Player = local, Life = life, Weapon = player.Weapon, Event = ++shotSequence, PoseTick = display.Tick, Ray = ray };
                    emitted[shot.Event] = now;
                    if (authority != null) authority.Submit(shot);
                    else connection.Send(SharedConnection.Message.Shot, w => SharedConnection.WriteShot(w, shot));
                }
            }
            view.Draw(projection, 1);
            int other = 1 - local; ally.SetActive(display.State == SharedState.Running && display.Players[other].Health > 0 && watched == local);
            ally.transform.position = display.Players[other].Position + Vector3.up;
            pingRemaining -= dt;
            if (connection.Connected && pingRemaining <= 0) { pingRemaining = .5f; connection.Send(SharedConnection.Message.Ping, w => w.Write(now)); }
            if (display.State == SharedState.Running)
            {
                if (display.Elapsed >= 3) frames.Add(frame);
                SharedPlayer a = display.Players[0], b = display.Players[1];
                frameLog.WriteLine(FormattableString.Invariant($"{now:F6},{frame:F4},{run},{display.Tick},{display.State},{a.Shots},{b.Shots},{a.Hits},{b.Hits},{a.Damage},{b.Damage},{a.Kills},{b.Kills},{a.Health},{b.Health},12,{lastRtt:F3}"));
                if (screenshot.Length > 0 && display.Elapsed >= 5) { Directory.CreateDirectory(Path.GetDirectoryName(screenshot)); ScreenCapture.CaptureScreenshot(screenshot); screenshot = ""; }
            }
            else if (display.State != SharedState.Waiting && !saved)
            { Save(); SetCursor(false); if (quitAtEnd) Application.Quit(display.State == SharedState.Aborted ? 2 : 0); }
        }
        void Retry()
        {
            if (display.State != SharedState.Complete && display.State != SharedState.Defeat) return;
            if (authority != null) { authority.Retry(run, local); Publish(); }
            else connection.Send(SharedConnection.Message.Retry, w => w.Write(run));
            inputActive = true; SetCursor(!scripted && preview == null);
        }
        public string ApplyPreviewCommand(SharedPreviewCommand command)
        {
            string error = command.Validate(display, local);
            if (error.Length > 0) return error;
            if (command.action == "quit") { Application.Quit(); return ""; }
            if (command.action == "retry") { Retry(); return ""; }
            scripted = command.action == "scripted";
            previewInputUntil = 0; previewMovement = Vector3.zero; previewFire = false;
            if (command.action == "input")
            {
                previewMovement = new Vector3(command.moveX, 0, command.moveZ);
                yaw = Mathf.Repeat(command.yaw, 360); pitch = command.pitch; previewFire = command.fire;
                previewInputUntil = Time.realtimeSinceStartupAsDouble + command.duration;
            }
            SetCursor(false); return "";
        }
        [Serializable] sealed class Report
        {
            public string role, source, machine, gpu, connection, reason;
            public bool scripted, development, previewEnabled;
            public int run, ticks, enemies, catchupLimits, frameSamples, confirmedHits, width, height, frameCap, vsync;
            public double frameP50, frameP95, frameP99, frameMax, confirmP50, confirmP95, confirmMax, rttMs;
            public SharedState state;
            public SharedPlayer[] players;
            public SharedEnemy[] enemyStates;
            public long sentBytes, receivedBytes;
        }
        void Save()
        {
            saved = true; frameLog.Flush(); hitLog.Flush();
            TextAsset stamp = Resources.Load<TextAsset>("BuildInfo");
            var report = new Report { role = local == 0 ? "host A" : "client B", source = stamp == null ? "Editor" : stamp.text,
                machine = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, connection = "Direct IPv4 UDP; declare actual address/machine/visibility separately",
                reason = reason, scripted = scripted, development = Debug.isDebugBuild, previewEnabled = preview != null, run = run, ticks = display.Tick, enemies = 12,
                catchupLimits = catchupLimits, width = Screen.width, height = Screen.height, frameCap = Application.targetFrameRate,
                vsync = QualitySettings.vSyncCount, frameSamples = frames.Count, confirmedHits = confirms.Count, frameP50 = Percentile(frames, .5),
                frameP95 = Percentile(frames, .95), frameP99 = Percentile(frames, .99), frameMax = Percentile(frames, 1),
                confirmP50 = Percentile(confirms, .5), confirmP95 = Percentile(confirms, .95), confirmMax = Percentile(confirms, 1), rttMs = lastRtt,
                state = display.State, players = display.Players, enemyStates = display.Enemies, sentBytes = connection.SentBytes, receivedBytes = connection.ReceivedBytes };
            File.WriteAllText(Path.Combine(output, "summary-run-" + run + ".json"), JsonUtility.ToJson(report, true));
            Debug.Log("SHARED_REPORT " + output);
        }
        public static double Percentile(List<double> values, double percentile)
        {
            if (values.Count == 0) return -1;
            double[] copy = values.ToArray(); Array.Sort(copy);
            return copy[Math.Max(0, (int)Math.Ceiling(percentile * copy.Length) - 1)];
        }
        void OnApplicationFocus(bool focused)
        { if (preview == null && !scripted && !focused) { inputActive = false; SetCursor(false); } }
        void OnGUI()
        {
            if (connection == null) return;
            if (heading == null) { heading = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold }; small = new GUIStyle(GUI.skin.label) { fontSize = 15 }; }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale); float w = Screen.width / scale, h = Screen.height / scale;
            SharedPlayer p = display.Players[local], a = display.Players[0], b = display.Players[1];
            GUI.Label(new Rect(25, 18, 1000, 40), $"SHARED RIFLE / {(local == 0 ? "A · HOST" : "B · CLIENT")} / {display.State}", heading);
            GUI.Label(new Rect(25, 57, 1150, 28), $"Run {display.Run}   ·   12 drones / 60 HP   ·   A: {a.Health} HP, {a.Damage} damage, {a.Kills} kills   ·   B: {b.Health} HP, {b.Damage} damage, {b.Kills} kills", small);
            GUI.Label(new Rect(w - 210, h - 60, 200, 40), $"{Mathf.Max(0, 60 - display.Elapsed):F1}s   RTT {lastRtt:F1}ms", small);
            if (display.State == SharedState.Running)
            {
                GUI.color = view.Feedback.CrosshairColor;
                GUI.DrawTexture(new Rect(w / 2 - 7, h / 2 - 1, 14, 2), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(w / 2 - 1, h / 2 - 7, 2, 14), Texture2D.whiteTexture); GUI.color = Color.white;
                GUI.Label(new Rect(25, h - 68, 950, 32), p.Health == 0 ? $"Following teammate · Return in {Mathf.Max(0, p.ReturnAt - display.Elapsed):F1}s" :
                    display.Elapsed < p.ImmuneUntil ? $"RETURN IMMUNITY {p.ImmuneUntil - display.Elapsed:F1}s" : "WASD · Mouse aim · Auto rifle", heading);
                if (!inputActive && !scripted && p.Health > 0)
                {
                    if (GUI.Button(new Rect(w / 2 - 120, h / 2 + 85, 240, 40), "Resume local input")) { inputActive = true; SetCursor(true); }
                    GUI.Label(new Rect(w / 2 - 220, h / 2 + 135, 440, 28), "Encounter continues while input is released", small);
                }
                view.Feedback.DrawHUD(w, h);
                if (ally.activeSelf)
                {
                    Vector3 vp = view.Camera.WorldToViewportPoint(ally.transform.position + Vector3.up * 1.3f);
                    if (vp.z > 0) GUI.Label(new Rect(vp.x * w - 20, (1 - vp.y) * h, 60, 25), local == 0 ? "B" : "A", small);
                }
            }
            else if (display.State == SharedState.Complete || display.State == SharedState.Defeat)
            {
                GUI.Label(new Rect(w / 2 - 220, h / 2 - 50, 440, 40), $"Retry readiness: A {(a.Ready ? "READY" : "waiting")} / B {(b.Ready ? "READY" : "waiting")}", small);
                if (GUI.Button(new Rect(w / 2 - 120, h / 2, 240, 50), "Retry (R)")) Retry();
            }
            else GUI.Label(new Rect(25, 110, 1150, 80), display.State == SharedState.Waiting ? "Waiting for the fixed host/client pair…" : "Test aborted: " + reason, heading);
        }
        static void SetCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnDestroy()
        {
            if (connection == null) return;
            if (!saved && display.State != SharedState.Waiting) { reason = "Local player closed"; display.State = SharedState.Aborted; Save(); }
            connection.Send(SharedConnection.Message.Abort, w => w.Write("Peer player closed")); connection.Dispose();
            frameLog?.Dispose(); hitLog?.Dispose(); if (allyShot != null) Destroy(allyShot.sharedMaterial); SetCursor(false);
        }
    }
}
