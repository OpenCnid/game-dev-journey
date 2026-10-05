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
        SharedScenarioBinding binding;
        GameObject ally;
        Material allyMaterial;
        LineRenderer allyShot;
        int local, run, life, inputSequence, shotSequence, catchupLimits;
        float yaw, pitch, inputTime, inputRemaining, shotRemaining, accumulator, pingRemaining;
        Vector3 predicted;
        bool inputActive = true, scripted, quitAtEnd, saved, solo, cardsVisible = true;
        readonly OrbReplica orbReplica = new OrbReplica();
        readonly Dictionary<int, GameObject> orbViews = new Dictionary<int, GameObject>();
        Material orbMaterial;
        StreamWriter growthLog;
        float emissionInterval = .12f, confirmationUntil;
        RifleUpgrade scriptedChoice;
        double previousClock, lastRtt;
        string reason = "", output = "", screenshot = "";
        StreamWriter frameLog, hitLog;
        GUIStyle heading, small, lifeHeading, lifeDetail;
        const float ViewTransitionSeconds = .2f;
        float viewTransition;
        int previousWatched = -1;
        SharedPreview preview;
        Vector3 previewMovement;
        double previewInputUntil;
        bool previewFire;
        public SharedSnapshot Snapshot => display;
        public int LocalPlayer => local;
        public bool Scripted => scripted;
        public float LocalYaw => yaw;
        public float LocalPitch => pitch;
        public void Initialize(ArenaPresentation arena, CombatSimulation sim, bool host, string address, ushort port, ScenarioConfiguration configuration = null, bool soloGrowth = false)
        {
            solo = soloGrowth;
            binding = new SharedScenarioBinding(host ? configuration ?? ScenarioConfiguration.Named(ScenarioKind.SharedRifle) : null);
            view = arena; projection = sim; local = host ? 0 : 1;
            var args = Environment.GetCommandLineArgs();
            scripted = Array.IndexOf(args, "-sharedScripted") >= 0; quitAtEnd = Array.IndexOf(args, "-sharedQuit") >= 0;
            if (Array.IndexOf(args, "-sharedUncapped") >= 0) { Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0; }
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-sharedResults") output = Path.GetFullPath(args[i + 1]);
                if (args[i] == "-growthChoice") scriptedChoice = args[i + 1] == "stronger" ? RifleUpgrade.StrongerHits : args[i + 1] == "faster" ? RifleUpgrade.FasterFire : throw new ArgumentException("Unknown growth choice");
                if (args[i] == "-screenshot") screenshot = Path.GetFullPath(args[i + 1]);
            }
            if (output.Length == 0) output = Path.Combine(Application.persistentDataPath, "SharedCombat", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), host ? "host" : "client");
            Directory.CreateDirectory(output);
            if (Array.IndexOf(args, "-sharedPreview") >= 0)
            { preview = gameObject.AddComponent<SharedPreview>(); preview.Initialize(this, view.Camera, output); }
            frameLog = new StreamWriter(Path.Combine(output, "frames.csv"));
            frameLog.WriteLine("wall_seconds,frame_ms,run,tick,state,A_shots,B_shots,A_hits,B_hits,A_damage,B_damage,A_kills,B_kills,A_hp,B_hp,enemies,rtt_ms,xp,level,uncollected_orbs");
            hitLog = new StreamWriter(Path.Combine(output, "events.csv"));
            hitLog.WriteLine("wall_seconds,run,player,weapon,event,pose_tick,life,decision,enemy,incarnation,region,requested,before,actual,after,confirmed_delay_ms");
            growthLog = new StreamWriter(Path.Combine(output, "growth.jsonl"));
            orbMaterial = new Material(Resources.Load<Material>("Body")); orbMaterial.SetColor("_BaseColor", new Color(.3f, .75f, 1));
            if (!solo)
            {
                connection = new SharedConnection(host, address, port, Array.IndexOf(args, "-sharedLoopback") >= 0);
                connection.Received += Receive; connection.Lost += Abort;
            }
            if (host) { authority = new SharedEncounter(binding.Configuration, binding.Session, solo ? 1 : 2); display.CopyFrom(authority.Current); authority.Resolved += Resolve; if (authority.Growth != null) authority.Growth.Changed += GrowthChanged; }
            ally = GameObject.CreatePrimitive(PrimitiveType.Capsule); ally.name = "Teammate " + (host ? "B" : "A");
            Destroy(ally.GetComponent<Collider>()); ally.transform.SetParent(transform);
            allyMaterial = new Material(Resources.Load<Material>("Body"));
            allyMaterial.SetColor("_BaseColor", new Color(.1f, .9f, .8f));
            ally.GetComponent<Renderer>().sharedMaterial = allyMaterial; ally.SetActive(false);
            allyShot = new GameObject("Teammate shot").AddComponent<LineRenderer>(); allyShot.transform.SetParent(transform);
            allyShot.positionCount = 2; allyShot.startWidth = .018f; allyShot.endWidth = .004f;
            allyShot.sharedMaterial = new Material(Resources.Load<Material>("Trail")); allyShot.sharedMaterial.color = new Color(.1f, .9f, .8f);
            allyShot.enabled = false;
            if (solo) { authority.Start(); Publish(); SaveConfiguration(); }
            previousClock = Time.realtimeSinceStartupAsDouble;
            SetCursor(!scripted && preview == null); Debug.Log("SHARED_SESSION " + (host ? "HOST" : "CLIENT") + " " + address + ":" + port + " " + output);
        }
        void Receive(SharedConnection.Message message, BinaryReader r)
        {
            if (message == SharedConnection.Message.Hello && authority != null && display.State == SharedState.Waiting)
            { connection.Send(SharedConnection.Message.Configuration, binding.Write); }
            else if (message == SharedConnection.Message.Configuration && authority == null)
            {
                binding.AcceptOffer(r);
                display.CopyFrom(new SharedSnapshot(binding.Configuration) { Session = binding.Session });
                SaveConfiguration();
                connection.Send(SharedConnection.Message.ConfigurationAck, binding.Write);
            }
            else if (message == SharedConnection.Message.ConfigurationAck && authority != null)
            {
                binding.AcceptAcknowledgement(r); SaveConfiguration();
                authority.Start(); accumulator = 0; previousClock = Time.realtimeSinceStartupAsDouble; Publish();
            }
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
                if (binding.CanAdopt(incoming, display)) { if (incoming.Run != display.Run) orbReplica.Reset(incoming.Run); display.CopyFrom(incoming); }
            }
            else if (message == SharedConnection.Message.Growth && authority == null) GrowthChanged(GrowthEvent.Read(r));
            else if (message == SharedConnection.Message.Choice && authority != null)
            {
                authority.Select(new GrowthChoice { Run = r.ReadInt32(), Player = 1, Offer = r.ReadInt32(), Weapon = r.ReadInt32(), Upgrade = (RifleUpgrade)r.ReadInt32() });
            }
            else if (message == SharedConnection.Message.Hit && authority == null) feedback.Enqueue(SharedConnection.ReadHit(r));
            else if (message == SharedConnection.Message.Retry && authority != null)
            {
                int retryRun = r.ReadInt32(); Guid session = new Guid(r.ReadBytes(16)); var config = ScenarioConfiguration.Read(r); SharedScenarioBinding.RequireEnd(r);
                if (!binding.Agreed || session != binding.Session || !binding.Configuration.SameValues(config)) throw new ArgumentException("Retry configuration/session mismatch.");
                authority.Retry(retryRun, 1); Publish();
            }
            else if (message == SharedConnection.Message.Abort) Abort(r.ReadString());
            else if (message == SharedConnection.Message.Ping) { double stamp = r.ReadDouble(); connection.Send(SharedConnection.Message.Pong, w => w.Write(stamp)); }
            else if (message == SharedConnection.Message.Pong) lastRtt = (Time.realtimeSinceStartupAsDouble - r.ReadDouble()) * 1000;
        }
        void Resolve(SharedHit hit)
        {
            feedback.Enqueue(hit); connection?.Send(SharedConnection.Message.Hit, w => SharedConnection.WriteHit(w, hit));
        }
        void Publish()
        {
            if (display.Run != authority.Current.Run) orbReplica.Reset(authority.Current.Run);
            display.CopyFrom(authority.Current);
            connection?.Send(SharedConnection.Message.Snapshot, w => SharedConnection.WriteSnapshot(w, display));
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
                accumulator = 0; shotSequence = 0; emitted.Clear(); saved = false;
                // Poll can deliver the new snapshot and its first hits together.
                // Retain that run's feedback while discarding prior-run events.
                int queued = feedback.Count;
                while (queued-- > 0)
                {
                    SharedHit hit = feedback.Dequeue();
                    if (hit.Source.Run == run) feedback.Enqueue(hit);
                }
                frames.Clear(); confirms.Clear(); catchupLimits = 0;
                previousWatched = -1; viewTransition = 0; cardsVisible = true; confirmationUntil = 0; emissionInterval = player.RifleInterval;
            }
        }
        void Update()
        {
            if (binding == null) return;
            double now = Time.realtimeSinceStartupAsDouble, frame = (now - previousClock) * 1000; previousClock = now;
            bool runningAtFrameStart = display.State == SharedState.Running;
            int frameRun = display.Run;
            connection?.Poll();
            SyncGeneration();
            preview?.Poll();
            SharedPlayer player = display.Players[local];
            Keyboard keyboard = preview == null ? Keyboard.current : null;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { inputActive = !inputActive; SetCursor(inputActive && display.State == SharedState.Running); }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) Retry();
            PersonalOffer currentOffer = player.Offer;
            if (currentOffer.Pending && display.State == SharedState.Running)
            {
                if (keyboard != null) ChoiceControls(keyboard.tabKey.wasPressedThisFrame, keyboard.digit1Key.wasPressedThisFrame ? 1 : keyboard.digit2Key.wasPressedThisFrame ? 2 : 0);
                if (scripted && GrowthUpgrade.Valid(scriptedChoice)) Choose(scriptedChoice);
            }
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
                    if (display.Configuration.Growth && orbReplica.Orbs.Count > 0)
                    {
                        float nearest = float.PositiveInfinity;
                        foreach (XpOrb orb in orbReplica.Orbs.Values)
                        { float distance = (orb.Position - predicted).sqrMagnitude; if (distance < nearest) { nearest = distance; goal = orb.Position; } }
                    }
                    movement = (goal - predicted).normalized;
                    // Shared slot for the opening, then clear nearby threats. No invulnerability/fixture damage changes.
                    int target = 0;
                    if (t > 2) for (int i = 1; i < display.Enemies.Length; i++)
                        if ((display.Enemies[i].Position - predicted).sqrMagnitude < (display.Enemies[target].Position - predicted).sqrMagnitude) target = i;
                    SharedEnemy enemy = display.Enemies[target];
                    Vector3 targetCentre = (local == 0 ? EnemyGeometry.Body(enemy.Position, enemy.Health < display.Configuration.EnemyHealth) : EnemyGeometry.Head(enemy.Position, enemy.Health < display.Configuration.EnemyHealth)).MultiplyPoint3x4(Vector3.zero);
                    Vector3 to = targetCentre - (predicted + Vector3.up * 1.65f);
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
                if (authority.Current.State == SharedState.Running && accumulator >= CombatSimulation.Tick)
                { catchupLimits++; Abort("Authority tick catch-up limit"); accumulator = 0; }
                else if (authority.Current.State == SharedState.Aborted) Abort(authority.AbortReason);
            }
            SyncGeneration();
            player = display.Players[local];
            if (player.RifleInterval != emissionInterval)
            { shotRemaining = Mathf.Max(0, shotRemaining) / emissionInterval * player.RifleInterval; emissionInterval = player.RifleInterval; }
            active = display.State == SharedState.Running && player.Health > 0;
            int watched = player.Health == 0 && display.Players[1 - local].Health > 0 ? 1 - local : local;
            viewTransition = Mathf.Max(0, viewTransition - (float)(frame * .001));
            if (previousWatched >= 0 && watched != previousWatched) viewTransition = ViewTransitionSeconds;
            previousWatched = watched;
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
                while (shotRemaining <= 0)
                {
                    shotRemaining += player.RifleInterval;
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
            DrawOrbs();
            int other = 1 - local; ally.SetActive(!solo && display.State == SharedState.Running && display.Players[other].Health > 0 && watched == local);
            ally.transform.position = display.Players[other].Position + Vector3.up;
            pingRemaining -= dt;
            if (connection != null && connection.Connected && pingRemaining <= 0) { pingRemaining = .5f; connection.Send(SharedConnection.Message.Ping, w => w.Write(now)); }
            if (display.State == SharedState.Running)
            {
                if (display.Elapsed >= 3) frames.Add(frame);
                SharedPlayer a = display.Players[0], b = display.Players[1];
                frameLog.WriteLine(FormattableString.Invariant($"{now:F6},{frame:F4},{run},{display.Tick},{display.State},{a.Shots},{b.Shots},{a.Hits},{b.Hits},{a.Damage},{b.Damage},{a.Kills},{b.Kills},{a.Health},{b.Health},{display.Enemies.Length},{lastRtt:F3},{display.Xp},{display.Level},{orbReplica.Orbs.Count}"));
                if (screenshot.Length > 0 && display.Elapsed >= 5) { Directory.CreateDirectory(Path.GetDirectoryName(screenshot)); ScreenCapture.CaptureScreenshot(screenshot); screenshot = ""; }
            }
            else if (display.State != SharedState.Waiting && !saved)
            { Save(); SetCursor(false); if (quitAtEnd) Application.Quit(display.State == SharedState.Aborted ? 2 : 0); }
        }
        void GrowthChanged(GrowthEvent e)
        {
            if (!orbReplica.Apply(e)) return;
            growthLog.WriteLine(JsonUtility.ToJson(e));
            if (e.Kind == GrowthEventKind.Applied && e.Player == local) confirmationUntil = Time.unscaledTime + 3;
            if (authority != null) connection?.Send(SharedConnection.Message.Growth, e.Write);
        }
        public bool ChoiceControls(bool tab, int card)
        {
            PersonalOffer offer = display.Players[local].Offer;
            if (display.State != SharedState.Running || !offer.Pending) return false;
            if (tab) cardsVisible = !cardsVisible;
            return cardsVisible && card > 0 && card <= 2 && Choose(offer.Card(card));
        }
        public bool Choose(RifleUpgrade upgrade)
        {
            PersonalOffer offer = display.Players[local].Offer;
            if (!offer.Pending || display.State != SharedState.Running || !GrowthUpgrade.Valid(upgrade)) return false;
            var choice = new GrowthChoice { Run = display.Run, Player = local, Offer = offer.Id, Weapon = offer.Weapon, Upgrade = upgrade };
            if (authority != null) return authority.Select(choice);
            return connection.Send(SharedConnection.Message.Choice, w => { w.Write(choice.Run); w.Write(choice.Offer); w.Write(choice.Weapon); w.Write((int)choice.Upgrade); });
        }
        readonly List<int> removedOrbs = new List<int>();
        void DrawOrbs()
        {
            removedOrbs.Clear();
            foreach (var entry in orbViews) if (!orbReplica.Orbs.ContainsKey(entry.Key)) { Destroy(entry.Value); removedOrbs.Add(entry.Key); }
            foreach (int id in removedOrbs) orbViews.Remove(id);
            foreach (XpOrb orb in orbReplica.Orbs.Values) if (!orbViews.ContainsKey(orb.Id))
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere); obj.name = "XP " + orb.Id;
                Destroy(obj.GetComponent<Collider>()); obj.transform.SetParent(transform); obj.transform.position = orb.Position + Vector3.up * .25f;
                obj.transform.localScale = Vector3.one * .38f; obj.GetComponent<Renderer>().sharedMaterial = orbMaterial; orbViews.Add(orb.Id, obj);
            }
        }
        void GrowthHud(float w, float h)
        {
            if (!display.Configuration.Growth) return;
            SharedPlayer player = display.Players[local]; PersonalOffer offer = player.Offer;
            GUI.Label(new Rect(25, 139, 1100, 26), $"TEAM LEVEL {display.Level} · {display.Xp} XP · {(display.Level == 2 ? "Test level cap" : "10 collected XP to level 2")} · {orbReplica.Orbs.Count} ground orbs", small);
            GUI.Label(new Rect(25, 168, 1100, 26), $"YOUR RIFLE · Body {player.BodyDamage} / Head {player.BodyDamage * 2} · {player.RifleInterval:0.00}s · {GrowthUpgrade.Label(offer.Selected)}", small);
            if (!solo) GUI.Label(new Rect(25, 195, 1100, 26), $"A: {GrowthUpgrade.Label(display.Players[0].Offer.Selected)} · B: {GrowthUpgrade.Label(display.Players[1].Offer.Selected)}", small);
            if (offer.Pending)
            {
                GUI.Label(new Rect(w - 355, h - 363, 335, 40), display.State == SharedState.Running ? "Level 2 — choose your rifle upgrade\nTab hide/reopen · combat continues" : "Unspent choice · run ended (read only)", small);
                if (cardsVisible)
                    for (int card = 1; card <= 2; card++)
                    {
                        RifleUpgrade upgrade = offer.Card(card); Rect rect = new Rect(w - 355, h - 327 + (card - 1) * 120, 330, 110);
                        GUI.Box(rect, ""); GUI.Label(new Rect(rect.x + 12, rect.y + 6, 310, 28), $"{card} · {GrowthUpgrade.Label(upgrade)}", heading);
                        GUI.Label(new Rect(rect.x + 12, rect.y + 40, 310, 65), $"Your starting rifle\nBody 20 → {GrowthUpgrade.Damage(upgrade)} / Head 40 → {GrowthUpgrade.Damage(upgrade) * 2}\nInterval .12 → {GrowthUpgrade.Interval(upgrade):0.00}s", small);
                    }
                else GUI.Label(new Rect(w - 355, h - 305, 335, 40), "Choice saved · Tab to reopen · combat continues", small);
            }
            if (Time.unscaledTime < confirmationUntil) GUI.Label(new Rect(w - 360, 145, 340, 35), GrowthUpgrade.Label(offer.Selected) + " applied", heading);
        }
        void Retry()
        {
            if (display.State != SharedState.Complete && display.State != SharedState.Defeat) return;
            if (authority != null) { authority.Retry(display.Run, local); Publish(); if (display.Run != run) accumulator = 0; }
            else connection.Send(SharedConnection.Message.Retry, w => { w.Write(display.Run); binding.Write(w); });
            inputActive = true; SetCursor(!scripted && preview == null);
        }
        public string ApplyPreviewCommand(SharedPreviewCommand command)
        {
            string error = command.Validate(display, local);
            if (error.Length > 0) return error;
            if (command.action == "quit") { Application.Quit(); return ""; }
            if (command.action == "choice") { return Choose(command.upgrade) ? "" : "Choice rejected"; }
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
            public string role, source, machine, gpu, connection, reason, hostBindEndpoint;
            public ScenarioEvidence scenario;
            public bool scripted, development, previewEnabled;
            public int run, ticks, enemies, catchupLimits, frameSamples, confirmedHits, width, height, frameCap, vsync;
            public double frameP50, frameP95, frameP99, frameMax, confirmP50, confirmP95, confirmMax, rttMs;
            public SharedState state;
            public SharedPlayer[] players;
            public SharedEnemy[] enemyStates;
            public int roster, xp, level, uncollectedOrbs;
            public long sentBytes, receivedBytes;
        }
        void Save()
        {
            saved = true; frameLog.Flush(); hitLog.Flush(); growthLog.Flush();
            TextAsset stamp = Resources.Load<TextAsset>("BuildInfo");
            var report = new Report { role = local == 0 ? "host A" : "client B", source = stamp == null ? "Editor" : stamp.text,
                machine = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, connection = "Direct IPv4 UDP; declare actual address/machine/visibility separately",
                reason = reason, hostBindEndpoint = authority != null && connection != null ? connection.BoundEndpoint : "", scripted = scripted, development = Debug.isDebugBuild, previewEnabled = preview != null, run = run, ticks = display.Tick, enemies = display.Enemies.Length,
                scenario = new ScenarioEvidence(display.Configuration, display.Session, display.Run),
                catchupLimits = catchupLimits, width = Screen.width, height = Screen.height, frameCap = Application.targetFrameRate,
                vsync = QualitySettings.vSyncCount, frameSamples = frames.Count, confirmedHits = confirms.Count, frameP50 = Percentile(frames, .5),
                frameP95 = Percentile(frames, .95), frameP99 = Percentile(frames, .99), frameMax = Percentile(frames, 1),
                confirmP50 = Percentile(confirms, .5), confirmP95 = Percentile(confirms, .95), confirmMax = Percentile(confirms, 1), rttMs = lastRtt,
                state = display.State, players = display.Players, enemyStates = display.Enemies, roster = display.Roster, xp = display.Xp, level = display.Level, uncollectedOrbs = orbReplica.Orbs.Count, sentBytes = connection?.SentBytes ?? 0, receivedBytes = connection?.ReceivedBytes ?? 0 };
            File.WriteAllText(Path.Combine(output, "summary-run-" + run + ".json"), JsonUtility.ToJson(report, true));
            Debug.Log("SHARED_REPORT " + output);
        }
        void SaveConfiguration()
        {
            File.WriteAllText(Path.Combine(output, "scenario.json"), JsonUtility.ToJson(new ScenarioEvidence(binding.Configuration, binding.Session, 0), true));
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
            if (binding == null) return;
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold };
                small = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                lifeHeading = new GUIStyle(heading) { fontSize = 32, alignment = TextAnchor.UpperCenter };
                lifeDetail = new GUIStyle(small) { fontSize = 24, alignment = TextAnchor.UpperCenter };
            }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale); float w = Screen.width / scale, h = Screen.height / scale;
            SharedPlayer p = display.Players[local], a = display.Players[0], b = display.Players[1];
            if (viewTransition > 0)
            {
                GUI.color = new Color(0, 0, 0, viewTransition / ViewTransitionSeconds);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            }
            GUI.Label(new Rect(25, 18, 1000, 40), $"{(display.Configuration.Growth ? "EARNED GROWTH" : "SHARED RIFLE")} / {(solo ? "SOLO" : local == 0 ? "A · HOST" : "B · CLIENT")} / {display.State}", heading);
            GUI.Label(new Rect(25, 57, 1150, 28), $"Run {display.Run}   ·   A: {a.Health} HP, {a.Damage} damage, {a.Kills} kills   ·   B: {b.Health} HP, {b.Damage} damage, {b.Kills} kills", small);
            if (binding.Configuration != null)
            {
                GUI.Label(new Rect(25, 85, 1200, 25), binding.Configuration.Label + " · " + binding.Configuration.Settings, small);
                GUI.Label(new Rect(25, 110, 1200, 25), "Host settings locked for this session · Retry keeps them · " + binding.Configuration.Identity.Substring(0, 12), small);
            }
            GrowthHud(w, h);
            GUI.Label(new Rect(w - 210, h - 60, 200, 40), $"{Mathf.Max(0, display.Configuration.Duration - display.Elapsed):F1}s   RTT {lastRtt:F1}ms", small);
            if (display.State == SharedState.Running)
            {
                if (p.Health > 0)
                {
                    GUI.color = view.Feedback.CrosshairColor;
                    GUI.DrawTexture(new Rect(w / 2 - 7, h / 2 - 1, 14, 2), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(w / 2 - 1, h / 2 - 7, 2, 14), Texture2D.whiteTexture); GUI.color = Color.white;
                }
                if (p.Health == 0)
                    LifeMessage(w, h, "YOU DIED", $"Respawning in {Mathf.Max(0, p.ReturnAt - display.Elapsed):F1}s", $"Spectating teammate {(local == 0 ? "B" : "A")}");
                else if (display.Elapsed < p.ImmuneUntil)
                    LifeMessage(w, h, "BACK IN THE FIGHT", $"{p.Health} HP · Contact protection {p.ImmuneUntil - display.Elapsed:F1}s", "Your camera and controls are restored");
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
                LifeMessage(w, h, display.State == SharedState.Defeat ? "PARTY DEFEATED" : "RUN COMPLETE",
                    display.State == SharedState.Defeat ? (solo ? "You are down" : "Both players are down") : $"{display.Configuration.Duration:0.##}-second encounter complete",
                    solo ? "Retry starts a fresh run" : $"Retry readiness: A {(a.Ready ? "READY" : "waiting")} / B {(b.Ready ? "READY" : "waiting")}");
                if (GUI.Button(new Rect(w / 2 - 120, h / 2, 240, 50), "Retry (R)")) Retry();
            }
            else if (display.State == SharedState.Aborted)
                LifeMessage(w, h, "SESSION STOPPED", "The shared run was interrupted", "Start a new session to play again");
            else GUI.Label(new Rect(25, 145, 1150, 80), "Waiting for the fixed host/client pair…", heading);
        }
        void LifeMessage(float w, float h, string title, string detail, string context)
        {
            Rect panel = new Rect(w / 2 - 310, h * .20f, 620, 155);
            GUI.color = new Color(.02f, .035f, .06f, .92f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(panel.x, panel.y + 12, panel.width, 44), title, lifeHeading);
            GUI.Label(new Rect(panel.x, panel.y + 61, panel.width, 34), detail, lifeDetail);
            GUI.Label(new Rect(panel.x, panel.y + 107, panel.width, 30), context, lifeDetail);
        }
        static void SetCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnDestroy()
        {
            if (binding == null) return;
            if (!saved && display.State != SharedState.Waiting) { reason = "Local player closed"; display.State = SharedState.Aborted; Save(); }
            foreach (var orb in orbViews.Values) if (orb != null) Destroy(orb);
            connection?.Send(SharedConnection.Message.Abort, w => w.Write("Peer player closed")); connection?.Dispose();
            frameLog?.Dispose(); hitLog?.Dispose(); growthLog?.Dispose(); if (orbMaterial != null) Destroy(orbMaterial); if (allyMaterial != null) Destroy(allyMaterial);
            if (allyShot != null) Destroy(allyShot.sharedMaterial); SetCursor(false);
        }
    }
}
