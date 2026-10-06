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
        readonly SnapshotAssembly snapshotAssembly = new SnapshotAssembly();
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
        float yaw, pitch, inputTime, inputRemaining, accumulator, pingRemaining;
        readonly float[] shotRemaining = new float[WeaponArsenal.MaxInstances];
        readonly WeaponWork[] weaponWork = new WeaponWork[WeaponArsenal.MaxOwnedIds];
        readonly AimResult[] cueTargets = new AimResult[ScenarioConfiguration.MaxEnemies];
        readonly LineRenderer[] secondaryLines = new LineRenderer[WeaponArsenal.MaxInstances * 5];
        Material scatterTrail, pierceTrail, rifleTrail;
        int secondaryCue;
        Vector3 predicted;
        bool inputActive = true, scripted, quitAtEnd, saved, solo, soloPrepared, cardsVisible = true;
        readonly OrbReplica orbReplica = new OrbReplica();
        XpPresentation pickups;
        RangedPresentation rangedView;
        SharedState previousState;
        bool pickupCue, levelCue, screenshotAtLevelUp;
        double pickupCueAt, pickupFlashUntil;
        StreamWriter growthLog;
        float emissionInterval = .12f, confirmationUntil;
        UpgradeId scriptedChoice;
        string scriptedStackPath = "";
        double previousClock, lastRtt;
        double updateLastMs, updateMaxMs, abortFrameMs, abortRemainingMs, abortPreviousUpdateMs;
        int abortCatchupTicks;
        int rttSequence;
        long presentationProbes, presentationRays;
        double pollMs, presentationMs, pickupRenderMs;
        double loggingMs, loggingMaxMs;
        bool diagnostics, terminalObserved;
        StreamWriter diagnosticLog;
        readonly NetworkWork noConnectionWork = new NetworkWork();
        double publishMs, stateCopyMs, inputAimMs, diagnosticLoggingMs, terminalAt = -1, firstQueueEmptyAfterSave = -1;
        long publishCount;
        bool exactSteering;
        double memoryAt, memorySampleMs, memorySamplingMs;
        long sampledHeapBytes, sampledWorkingSetBytes;
        int memorySamples;
        string reason = "", output = "", screenshot = "";
        StreamWriter frameLog, hitLog;
        GUIStyle heading, small, lifeHeading, lifeDetail, choiceCard;
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
            diagnostics = Array.IndexOf(args, "-sharedDiagnostics") >= 0;
            if (Array.IndexOf(args, "-sharedMute") >= 0) AudioListener.volume = 0;
            scripted = Array.IndexOf(args, "-sharedScripted") >= 0; quitAtEnd = Array.IndexOf(args, "-sharedQuit") >= 0;
            screenshotAtLevelUp = Array.IndexOf(args, "-screenshotAtLevelUp") >= 0;
            if (Array.IndexOf(args, "-sharedUncapped") >= 0) { Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0; }
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-sharedResults") output = Path.GetFullPath(args[i + 1]);
                if (args[i] == "-sharedSteering") exactSteering = args[i + 1] == "grid" ? true : args[i + 1] == "pairwise" ? false : throw new ArgumentException("Steering must be grid or pairwise");
                if (args[i] == "-growthChoice") scriptedChoice = args[i + 1] == "stronger" ? UpgradeId.StrongerHits : args[i + 1] == "faster" ? UpgradeId.FasterFire : throw new ArgumentException("Unknown growth choice");
                if (args[i] == "-stackPath") scriptedStackPath = args[i + 1] == "broad" || args[i + 1] == "scatter" || args[i + 1] == "pierce" ? args[i + 1] : throw new ArgumentException("Unknown stack path");
                if (args[i] == "-screenshot") screenshot = Path.GetFullPath(args[i + 1]);
            }
            if (output.Length == 0) output = Path.Combine(Application.persistentDataPath, "SharedCombat", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), host ? "host" : "client");
            if (File.Exists(Path.Combine(output, "frames.csv"))) output = Path.Combine(output, "session-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            if (Array.IndexOf(args, "-sharedPreview") >= 0)
            { preview = gameObject.AddComponent<SharedPreview>(); preview.Initialize(this, view.Camera, output); }
            frameLog = new StreamWriter(Path.Combine(output, "frames.csv"));
            frameLog.Write("wall_seconds,frame_ms,run,tick,state,A_shots,B_shots,A_hits,B_hits,A_damage,B_damage,A_kills,B_kills,A_hp,B_hp,enemies,rtt_ms,xp,level,uncollected_orbs");
            for (int id = 1; id <= WeaponArsenal.MaxOwnedIds; id++) frameLog.Write($",weapon{id}_triggers,weapon{id}_rays,weapon{id}_target_tests,weapon{id}_hits,weapon{id}_damage,weapon{id}_kills");
            frameLog.Write(",rtt_sequence,sim_ticks,sim_ms,crowd_ms,weapon_ms,xp_ms,steering_checks,contact_tests,attraction_queries,presentation_rays,presentation_probes,presentation_ms,render_prepare_ms,render_submit_ms,pickup_render_ms,submitted_instances,instance_batches,pickup_carriers,secondary_cues,net_poll_ms,sent_bytes,received_bytes,sent_messages,received_messages,snapshot_bytes,snapshot_messages,serialization_ms,queue_records,queue_bytes,queue_age_ms,queue_high_water,queue_deferred,allocated_thread_bytes,gc0,gc1,gc2,installed_instances,active_instances,xp_drops,xp_merges,xp_move_events,xp_collections,uncollected_xp");
            frameLog.Write(",memory_samples,managed_heap_bytes,working_set_bytes,memory_sample_ms");
            frameLog.WriteLine();
            hitLog = new StreamWriter(Path.Combine(output, "events.csv"));
            hitLog.WriteLine("wall_seconds,run,player,weapon,event,pose_tick,life,decision,enemy,incarnation,region,requested,before,actual,after,confirmed_delay_ms,trigger,ray_index,rays,target_tests");
            growthLog = new StreamWriter(Path.Combine(output, "growth.jsonl"));
            if (diagnostics)
            {
                diagnosticLog = new StreamWriter(Path.Combine(output, "publication.csv"));
                diagnosticLog.WriteLine("wall_seconds,frame_ms,run,tick,state,update_cpu_ms,sim_ms,crowd_ms,weapon_ms,xp_ms,presentation_ms,input_aim_ms,publish_ms,state_copy_ms,publish_count,serialization_ms,logging_ms,diagnostic_logging_ms,memory_sample_ms," + NetworkWork.Header + ",queue_records,queue_bytes,queue_age_ms");
            }
            pickups = new XpPresentation(transform);
            if (binding.Configuration != null && binding.Configuration.RangedCount > 0) rangedView = new RangedPresentation(transform);
            if (!solo)
            {
                connection = new SharedConnection(host, address, port, Array.IndexOf(args, "-sharedLoopback") >= 0, diagnostics);
                connection.Received += Receive; connection.Lost += Abort;
            }
            if (host) { authority = new SharedEncounter(binding.Configuration, binding.Session, solo ? 1 : 2, exactSteering); display.CopyFrom(authority.Current); authority.Resolved += Resolve; if (authority.Growth != null) authority.Growth.Changed += GrowthChanged; }
            ally = GameObject.CreatePrimitive(PrimitiveType.Capsule); ally.name = "Teammate " + (host ? "B" : "A");
            Destroy(ally.GetComponent<Collider>()); ally.transform.SetParent(transform);
            allyMaterial = new Material(Resources.Load<Material>("Body"));
            allyMaterial.SetColor("_BaseColor", new Color(.1f, .9f, .8f));
            ally.GetComponent<Renderer>().sharedMaterial = allyMaterial; ally.SetActive(false);
            allyShot = new GameObject("Teammate shot").AddComponent<LineRenderer>(); allyShot.transform.SetParent(transform);
            allyShot.positionCount = 2; allyShot.startWidth = .018f; allyShot.endWidth = .004f;
            allyShot.sharedMaterial = new Material(Resources.Load<Material>("Trail")); allyShot.sharedMaterial.color = new Color(.1f, .9f, .8f);
            allyShot.enabled = false;
            scatterTrail = new Material(Resources.Load<Material>("Trail")); scatterTrail.color = new Color(1, .55f, .1f);
            pierceTrail = new Material(Resources.Load<Material>("Trail")); pierceTrail.color = new Color(.8f, .35f, 1);
            rifleTrail = new Material(Resources.Load<Material>("Trail")); rifleTrail.color = new Color(.1f, .9f, 1);
            for (int i = 0; i < secondaryLines.Length; i++)
            {
                var line = new GameObject("Secondary shot " + i).AddComponent<LineRenderer>(); line.transform.SetParent(transform);
                line.positionCount = 2; line.startWidth = .025f; line.endWidth = .008f; line.enabled = false; secondaryLines[i] = line;
            }
            if (solo) { authority.Start(preparing: true); Publish(); SaveConfiguration(); }
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
                if (binding.Configuration.RangedCount > 0) rangedView = new RangedPresentation(transform);
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
                AdoptSnapshot();
            }
            else if (message == SharedConnection.Message.SnapshotChunk && authority == null)
            { if (snapshotAssembly.Read(r, incoming, Time.realtimeSinceStartupAsDouble)) AdoptSnapshot(); }
            else if (message == SharedConnection.Message.Growth && authority == null) GrowthChanged(GrowthEvent.Read(r));
            else if (message == SharedConnection.Message.Choice && authority != null)
            {
                authority.Select(new GrowthChoice { Run = r.ReadInt32(), Player = 1, Offer = r.ReadInt32(), Weapon = r.ReadInt32(), Upgrade = (UpgradeId)r.ReadInt32() });
            }
            else if (message == SharedConnection.Message.ContinueChoice && authority != null)
            { authority.ContinueChoice(r.ReadInt32(), r.ReadInt32(), 1); Publish(); }
            else if (message == SharedConnection.Message.Hit && authority == null) feedback.Enqueue(SharedConnection.ReadHit(r));
            else if (message == SharedConnection.Message.Retry && authority != null)
            {
                int retryRun = r.ReadInt32(); Guid session = new Guid(r.ReadBytes(16)); var config = ScenarioConfiguration.Read(r); SharedScenarioBinding.RequireEnd(r);
                if (!binding.Agreed || session != binding.Session || !binding.Configuration.SameValues(config)) throw new ArgumentException("Retry configuration/session mismatch.");
                authority.Retry(retryRun, 1); Publish();
            }
            else if (message == SharedConnection.Message.Abort) Abort(r.ReadString());
            else if (message == SharedConnection.Message.Ping) { double stamp = r.ReadDouble(); connection.Send(SharedConnection.Message.Pong, w => w.Write(stamp)); }
            else if (message == SharedConnection.Message.Pong) { lastRtt = (Time.realtimeSinceStartupAsDouble - r.ReadDouble()) * 1000; rttSequence++; }
        }
        void AdoptSnapshot()
        { if (binding.CanAdopt(incoming, display)) { if (incoming.Run != display.Run) orbReplica.Reset(incoming.Run); display.CopyFrom(incoming); } }
        void Resolve(SharedHit hit)
        {
            feedback.Enqueue(hit); connection?.Send(SharedConnection.Message.Hit, w => SharedConnection.WriteHit(w, hit));
        }
        void Publish()
        {
            long whole = diagnostics ? StageProfile.Begin() : 0, copy = diagnostics ? StageProfile.Begin() : 0;
            if (display.Run != authority.Current.Run) orbReplica.Reset(authority.Current.Run);
            display.CopyFrom(authority.Current);
            if (diagnostics) stateCopyMs += StageProfile.End(copy);
            connection?.SendSnapshot(display);
            if (diagnostics) { publishMs += StageProfile.End(whole); publishCount++; }
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
            inputTime = inputRemaining = 0; Array.Clear(shotRemaining, 0, shotRemaining.Length); inputSequence = 0; view.ResetFeedback();
            previewInputUntil = 0; previewMovement = Vector3.zero; previewFire = false;
            if (newRun)
            {
                accumulator = 0; shotSequence = 0; emitted.Clear(); saved = false;
                terminalAt = firstQueueEmptyAfterSave = -1; terminalObserved = false;
                // Poll can deliver the new snapshot and its first hits together.
                // Retain that run's feedback while discarding prior-run events.
                int queued = feedback.Count;
                while (queued-- > 0)
                {
                    SharedHit hit = feedback.Dequeue();
                    if (hit.Source.Run == run) feedback.Enqueue(hit);
                }
                frames.Clear(); confirms.Clear(); catchupLimits = 0;
                presentationRays = presentationProbes = 0;
                abortFrameMs = abortRemainingMs = abortPreviousUpdateMs = 0; abortCatchupTicks = 0;
                for (int i = 0; i < weaponWork.Length; i++) weaponWork[i] = new WeaponWork(i + 1);
                previousWatched = -1; viewTransition = 0; cardsVisible = true; confirmationUntil = 0; emissionInterval = player.RifleInterval;
            }
        }
        void Update()
        {
            if (binding == null) return;
            long updateStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            StageProfile.Reset(); authority?.Work.BeginFrame();
            double now = Time.realtimeSinceStartupAsDouble, frame = (now - previousClock) * 1000; previousClock = now;
            memorySampleMs = 0;
            if (now >= memoryAt)
            {
                long memoryStarted = StageProfile.Begin();
                sampledHeapBytes = GC.GetTotalMemory(false);
                using (var process = System.Diagnostics.Process.GetCurrentProcess()) sampledWorkingSetBytes = process.WorkingSet64;
                memorySamples++; memoryAt = now + 1;
                memorySampleMs = StageProfile.End(memoryStarted); memorySamplingMs += memorySampleMs;
            }
            bool runningAtFrameStart = display.State == SharedState.Running;
            int frameRun = display.Run;
            long pollStarted = StageProfile.Begin(); connection?.Poll(); pollMs = StageProfile.End(pollStarted);
            if (snapshotAssembly.AgeMs(now) > 1000) Abort("Snapshot assembly timed out");
            if (previousState == SharedState.Choosing && display.State == SharedState.Running)
            { frame = accumulator = 0; inputActive = true; predicted = display.Players[local].Position; previewInputUntil = 0; confirmationUntil = (float)now + 2; SetCursor(!scripted && preview == null); }
            if (solo && display.State == SharedState.Waiting && soloPrepared)
            {
                authority.Begin(); accumulator = 0; Publish();
                // One complete prepared render precedes active time; first-use GPU setup is not combat.
                frame = 0; previousClock = now;
            }
            SyncGeneration();
            preview?.Poll();
            if (authority != null && authority.CommitChoices()) Publish();
            SharedPlayer player = display.Players[local];
            Keyboard keyboard = preview == null ? Keyboard.current : null;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { inputActive = !inputActive; SetCursor(inputActive && display.State == SharedState.Running); }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) Retry();
            PersonalOffer currentOffer = player.Offer;
            if (currentOffer.Pending && (display.State == SharedState.Running || display.State == SharedState.Choosing))
            {
                if (keyboard != null) ChoiceControls(keyboard.tabKey.wasPressedThisFrame, keyboard.digit1Key.wasPressedThisFrame ? 1 : keyboard.digit2Key.wasPressedThisFrame ? 2 : 0);
                if (scripted && GrowthUpgrade.Valid(ScriptedUpgrade())) Choose(ScriptedUpgrade());
            }
            if (keyboard != null && keyboard.enterKey.wasPressedThisFrame) ContinueChoice();
            if (authority != null && authority.CommitChoices()) Publish();
            if (scripted && GrowthUpgrade.Valid(ScriptedUpgrade()) && display.State == SharedState.Choosing && display.Players[local].Offer.Selected != UpgradeId.None) ContinueChoice();
            long inputStarted = diagnostics ? StageProfile.Begin() : 0;
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
                    if (display.Configuration.Pickups && orbReplica.Orbs.Count > 0)
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
            if (diagnostics) inputAimMs = StageProfile.End(inputStarted);
            if (authority != null && display.State == SharedState.Running)
            {
                if (previousState == SharedState.Choosing) { frame = accumulator = 0; }
                // Hello/Retry can start a run inside Poll. Its preceding Waiting/terminal frame is not combat time.
                if (runningAtFrameStart && frameRun == display.Run) accumulator += (float)(frame * .001);
                int ticks = 0;
                while (accumulator >= CombatSimulation.Tick && ticks < 8 && authority.Current.State == SharedState.Running)
                { authority.Step(); accumulator -= CombatSimulation.Tick; ticks++; Publish(); }
                if (authority.Current.State == SharedState.Running && accumulator >= CombatSimulation.Tick)
                {
                    catchupLimits++;
                    // This MVP fixture carries short stalls over subsequent bounded frames.
                    // Keep strict reference/diagnostic behavior and a finite backlog guard.
                    if (display.Configuration.Kind != ScenarioKind.MixedThreats || accumulator > 1)
                    {
                        abortFrameMs = frame; abortRemainingMs = accumulator * 1000; abortCatchupTicks = ticks;
                        abortPreviousUpdateMs = updateLastMs;
                        Abort("Authority tick catch-up limit"); accumulator = 0;
                    }
                }
                else if (authority.Current.State == SharedState.Aborted) Abort(authority.AbortReason);
            }
            SyncGeneration();
            player = display.Players[local];
            long presentationStarted = StageProfile.Begin();
            if (player.RifleInterval != emissionInterval)
            { shotRemaining[0] = Mathf.Max(0, shotRemaining[0]) / emissionInterval * player.RifleInterval; emissionInterval = player.RifleInterval; }
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
            secondaryCue = 0; foreach (LineRenderer line in secondaryLines) line.enabled = false;
            while (feedback.Count > 0)
            {
                SharedHit hit = feedback.Dequeue();
                if (hit.Source.Run != run) continue;
                if (hit.Source.Weapon >= 1 && hit.Source.Weapon <= WeaponArsenal.MaxOwnedIds) weaponWork[hit.Source.Weapon - 1].Record(hit);
                double delay = -1;
                if (hit.Source.Player == local && emitted.TryGetValue(hit.Source.Event, out double emission))
                {
                    delay = (now - emission) * 1000; emitted.Remove(hit.Source.Event);
                    if (hit.Decision == ShotDecision.Damage) confirms.Add(delay);
                }
                ShotResult s = hit.Shot;
                hitLog.WriteLine(FormattableString.Invariant($"{now:F6},{run},{hit.Source.Player},{hit.Source.Weapon},{hit.Source.Event},{hit.Source.PoseTick},{hit.Source.Life},{hit.Decision},{s.EnemyIndex},{s.SpawnVersion},{s.Region},{s.RequestedDamage},{s.HealthBefore},{s.Damage},{s.HealthAfter},{delay:F3},{hit.Trigger},{hit.RayIndex},{hit.Rays},{hit.TargetTests}"));
                if (s.Hit)
                {
                    if (hit.Source.Player == local && hit.Source.Life == life) view.Feedback.RecordShot(projection, s);
                    else { allyShot.SetPosition(0, hit.Source.Ray.origin); allyShot.SetPosition(1, s.End); allyShot.sharedMaterial.color = WeaponArsenal.Slot(hit.Source.Weapon) == WeaponSlot.Scattergun ? scatterTrail.color : WeaponArsenal.Slot(hit.Source.Weapon) == WeaponSlot.Piercer ? pierceTrail.color : new Color(.1f, .9f, .8f); allyShot.enabled = true; }
                }
            }
            if (active && (scripted || (preview == null ? inputActive : previewActive && previewFire)))
            {
                for (int s = 0; s < WeaponArsenal.MaxInstances; s++)
                {
                    int weapon = WeaponArsenal.Id(local, s); WeaponSlot slot = WeaponArsenal.Slot(weapon);
                    if (!WeaponArsenal.Owns(player, local, weapon)) { shotRemaining[s] = 0; continue; }
                    shotRemaining[s] -= dt;
                    while (shotRemaining[s] <= 0)
                    {
                        shotRemaining[s] += WeaponArsenal.Interval(player, slot);
                        Ray ray = new Ray(view.Camera.transform.position, view.Camera.transform.forward);
                        if (s == 0) { view.ShowShot(ray.GetPoint(display.Query(ray).Distance)); presentationRays++; presentationProbes += display.Enemies.Length; }
                        else SecondaryCue(player, slot, ray);
                        var shot = new SharedShot { Run = run, Player = local, Life = life, Weapon = weapon, Event = ++shotSequence, PoseTick = display.Tick, Ray = ray };
                        emitted[shot.Event] = now;
                        if (authority != null) authority.Submit(shot);
                        else connection.Send(SharedConnection.Message.Shot, w => SharedConnection.WriteShot(w, shot));
                    }
                }
            }
            view.Draw(projection, 1);
            rangedView?.Draw(display);
            // CollectBars performs one full head/body precision query per presented running frame.
            if (projection.State == RunState.Running || projection.State == RunState.Paused) { presentationRays++; presentationProbes += projection.Count; }
            if (solo && display.State == SharedState.Waiting) soloPrepared = true;
            if (display.State == SharedState.Choosing) { predicted = display.Players[local].Position; inputActive = false; SetCursor(false); }
            long pickupStarted = StageProfile.Begin();
            pickups.Draw(orbReplica, view.Camera, display.Players[local].Position, display.Configuration.Pickups && player.Health > 0 && (display.State == SharedState.Running || display.State == SharedState.Choosing));
            pickupRenderMs = StageProfile.End(pickupStarted);
            presentationMs = StageProfile.End(presentationStarted);
            if (levelCue || pickupCue && now >= pickupCueAt)
            { view.PlayGrowthCue(levelCue); pickupCueAt = now + .075; levelCue = pickupCue = false; }
            previousState = display.State;
            int other = 1 - local; ally.SetActive(!solo && (display.State == SharedState.Running || display.State == SharedState.Choosing) && display.Players[other].Health > 0 && watched == local);
            ally.transform.position = display.Players[other].Position + Vector3.up;
            pingRemaining -= dt;
            if (connection != null && connection.Connected && pingRemaining <= 0) { pingRemaining = .5f; connection.Send(SharedConnection.Message.Ping, w => w.Write(now)); }
            if (display.State == SharedState.Running || display.State != SharedState.Waiting && display.State != SharedState.Choosing && !saved)
            {
                long loggingStarted = StageProfile.Begin();
                if (display.State == SharedState.Running && display.Elapsed >= 3 && frame > 0) frames.Add(frame);
                SharedPlayer a = display.Players[0], b = display.Players[1];
                frameLog.Write(FormattableString.Invariant($"{now:F6},{frame:F4},{run},{display.Tick},{display.State},{a.Shots},{b.Shots},{a.Hits},{b.Hits},{a.Damage},{b.Damage},{a.Kills},{b.Kills},{a.Health},{b.Health},{display.Enemies.Length},{lastRtt:F3},{display.Xp},{display.Level},{orbReplica.Orbs.Count}"));
                foreach (WeaponWork work in weaponWork) frameLog.Write($",{work.triggers},{work.rays},{work.targetTests},{work.hits},{work.damage},{work.kills}");
                WriteWorkFrame();
                frameLog.WriteLine();
                double logMs = StageProfile.End(loggingStarted); loggingMs += logMs; loggingMaxMs = Math.Max(loggingMaxMs, logMs);
            }
            if (display.State != SharedState.Running && display.State != SharedState.Waiting && display.State != SharedState.Choosing && !saved)
            { Save(); SetCursor(false); if (quitAtEnd) Application.Quit(display.State == SharedState.Aborted ? 2 : 0); }
            if (screenshot.Length > 0 && (screenshotAtLevelUp ? display.State == SharedState.Choosing : display.State == SharedState.Running && display.Elapsed >= 5))
            { Directory.CreateDirectory(Path.GetDirectoryName(screenshot)); ScreenCapture.CaptureScreenshot(screenshot); screenshot = ""; }
            updateLastMs = (System.Diagnostics.Stopwatch.GetTimestamp() - updateStarted) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            updateMaxMs = Math.Max(updateMaxMs, updateLastMs);
            if (diagnostics && !terminalObserved)
            {
                long diagnosticStarted = StageProfile.Begin();
                WritePublicationFrame(now, frame);
                if (saved)
                {
                    if (firstQueueEmptyAfterSave < 0 && (connection == null || connection.PendingMessages == 0))
                        firstQueueEmptyAfterSave = Time.realtimeSinceStartupAsDouble - terminalAt;
                    if (Time.realtimeSinceStartupAsDouble - terminalAt >= 2)
                    { ObserveTerminal("terminal-after-2s.json"); terminalObserved = true; diagnosticLog.Flush(); hitLog.Flush(); growthLog.Flush(); }
                }
                diagnosticLoggingMs += StageProfile.End(diagnosticStarted);
            }
        }
        void WritePublicationFrame(double now, double frame)
        {
            SharedWork w = authority?.Work;
            diagnosticLog.Write(FormattableString.Invariant($"{now:F6},{frame:F4},{run},{display.Tick},{display.State},{updateLastMs:F6},{w?.frameSimulationMs ?? 0:F6},{w?.frameCrowdMs ?? 0:F6},{w?.frameWeaponMs ?? 0:F6},{w?.frameXpMs ?? 0:F6},{presentationMs:F6},{inputAimMs:F6},{publishMs:F6},{stateCopyMs:F6},{publishCount},{connection?.SerializationMs ?? 0:F6},{loggingMs:F6},{diagnosticLoggingMs:F6},{memorySampleMs:F6},"));
            (connection?.Diagnostic ?? noConnectionWork).Write(diagnosticLog);
            diagnosticLog.WriteLine(FormattableString.Invariant($",{connection?.PendingMessages ?? 0},{connection?.PendingBytes ?? 0},{connection?.PendingAgeMs ?? 0:F6}"));
        }
        [Serializable] sealed class TerminalObservation
        {
            public string source, role, method;
            public int run, tick, xp, uncollectedXp, pendingRecords, pendingBytes;
            public SharedState state;
            public double secondsAfterSave, firstQueueEmptyAfterSave, diagnosticLoggingMs;
            public long sentBytes, sentMessages, receivedBytes, receivedMessages;
            public SharedConnection.PendingRecord[] pending;
            public NetworkWork network;
            public WeaponWork[] weapons;
            public SharedPlayer[] players;
        }
        void ObserveTerminal(string file)
        {
            TextAsset stamp = Resources.Load<TextAsset>("BuildInfo");
            var observation = new TerminalObservation { source = stamp == null ? "Editor" : stamp.text, role = local == 0 ? "host A" : "client B",
                method = "Observational probe only; original first-terminal summary unchanged. Empty application FIFO does not prove transport acknowledgement.",
                run = run, tick = display.Tick, state = display.State, xp = display.Xp, uncollectedXp = orbReplica.UncollectedXp,
                secondsAfterSave = Time.realtimeSinceStartupAsDouble - terminalAt, firstQueueEmptyAfterSave = firstQueueEmptyAfterSave,
                diagnosticLoggingMs = diagnosticLoggingMs, pendingRecords = connection?.PendingMessages ?? 0, pendingBytes = connection?.PendingBytes ?? 0,
                sentBytes = connection?.SentBytes ?? 0, sentMessages = connection?.SentMessages ?? 0, receivedBytes = connection?.ReceivedBytes ?? 0, receivedMessages = connection?.ReceivedMessages ?? 0,
                pending = connection?.ObservePending(), network = connection?.Diagnostic, weapons = weaponWork, players = display.Players };
            File.WriteAllText(Path.Combine(output, file), JsonUtility.ToJson(observation, true));
        }
        void WriteWorkFrame()
        {
            SharedWork work = authority?.Work;
            int installed = 0, active = 0;
            for (int p = 0; p < display.Roster; p++) for (int instance = 0; instance < WeaponArsenal.MaxInstances; instance++)
                if ((display.Players[p].Arsenal & (1 << instance)) != 0) { installed++; if (display.Players[p].Health > 0) active++; }
            frameLog.Write(FormattableString.Invariant($",{rttSequence},{work?.frameTicks ?? 0},{work?.frameSimulationMs ?? 0:F6},{work?.frameCrowdMs ?? 0:F6},{work?.frameWeaponMs ?? 0:F6},{work?.frameXpMs ?? 0:F6},{work?.steeringChecks ?? 0},{work?.contactTests ?? 0},{work?.attractionQueries ?? 0},{presentationRays},{presentationProbes},{presentationMs:F6},{StageProfile.RenderPrepareMs:F6},{StageProfile.RenderSubmitMs:F6},{pickupRenderMs:F6},{StageProfile.SubmittedInstances},{StageProfile.InstanceBatches},{pickups.SubmittedCarriers},{secondaryCue},{pollMs:F6},{connection?.SentBytes ?? 0},{connection?.ReceivedBytes ?? 0},{connection?.SentMessages ?? 0},{connection?.ReceivedMessages ?? 0},{connection?.SnapshotBytes ?? 0},{connection?.SnapshotMessages ?? 0},{connection?.SerializationMs ?? 0:F6},{connection?.PendingMessages ?? 0},{connection?.PendingBytes ?? 0},{connection?.PendingAgeMs ?? 0:F6},{connection?.BufferedHighWater ?? 0},{connection?.DeferredSends ?? 0},{GC.GetAllocatedBytesForCurrentThread()},{GC.CollectionCount(0)},{GC.CollectionCount(1)},{GC.CollectionCount(2)},{installed},{active},{orbReplica.Drops},{orbReplica.MergeEvents},{orbReplica.MoveEvents},{orbReplica.Collections},{orbReplica.UncollectedXp}"));
            frameLog.Write(FormattableString.Invariant($",{memorySamples},{sampledHeapBytes},{sampledWorkingSetBytes},{memorySampleMs:F6}"));
        }
        void GrowthChanged(GrowthEvent e)
        {
            if (!orbReplica.Apply(e)) return;
            growthLog.WriteLine(JsonUtility.ToJson(e));
            if (e.Kind == GrowthEventKind.Applied && e.Player == local) confirmationUntil = Time.unscaledTime + 3;
            if (e.Kind == GrowthEventKind.Collected && e.Player == local) { pickupCue = true; pickupFlashUntil = Time.realtimeSinceStartupAsDouble + .35; }
            if (e.Kind == GrowthEventKind.Entitlement && e.Player == local) levelCue = true;
            if (authority != null) connection?.Send(SharedConnection.Message.Growth, e.Write);
        }
        UpgradeId ScriptedUpgrade()
        {
            if (!display.Configuration.StackedGrowth) return scriptedChoice;
            if (scriptedStackPath.Length == 0) return UpgradeId.None;
            if (display.Level == 2) return scriptedStackPath == "pierce" ? UpgradeId.AddPiercer : UpgradeId.AddScattergun;
            if (display.Level == 3) return scriptedStackPath == "broad" ? WeaponArsenal.Missing(display.Players[local].FirstAddition) : WeaponArsenal.Enhancement(display.Players[local].FirstAddition);
            return UpgradeId.None;
        }
        void SecondaryCue(SharedPlayer player, WeaponSlot slot, Ray aim)
        {
            int rays = WeaponArsenal.Rays(player, slot); float range = WeaponArsenal.Range(slot);
            for (int r = 0; r < rays && secondaryCue < secondaryLines.Length; r++)
            {
                Ray ray = slot == WeaponSlot.Scattergun ? WeaponArsenal.Fan(aim, r, rays) : aim;
                int count = WeaponArsenal.Query(display, ray, range, cueTargets);
                presentationRays++; presentationProbes += display.Enemies.Length;
                float distance = count == 0 ? range : cueTargets[slot == WeaponSlot.Piercer ? Math.Min(count, WeaponArsenal.PierceLimit(player)) - 1 : 0].Distance;
                LineRenderer line = secondaryLines[secondaryCue++]; line.sharedMaterial = slot == WeaponSlot.Scattergun ? scatterTrail : slot == WeaponSlot.Piercer ? pierceTrail : rifleTrail;
                line.SetPosition(0, aim.origin + view.Camera.transform.right * .25f - Vector3.up * .18f);
                line.SetPosition(1, ray.GetPoint(distance)); line.enabled = true;
            }
        }
        public bool ChoiceControls(bool tab, int card)
        {
            PersonalOffer offer = display.Players[local].Offer;
            if ((display.State != SharedState.Running && display.State != SharedState.Choosing) || !offer.Pending) return false;
            if (display.State == SharedState.Choosing) cardsVisible = true;
            else if (tab) cardsVisible = !cardsVisible;
            return cardsVisible && card > 0 && card <= 2 && Choose(offer.Card(card));
        }
        public bool Choose(UpgradeId upgrade)
        {
            PersonalOffer offer = display.Players[local].Offer;
            if (!offer.Pending || (display.State != SharedState.Running && display.State != SharedState.Choosing) || !offer.Contains(upgrade)) return false;
            var choice = new GrowthChoice { Run = display.Run, Player = local, Offer = offer.Id, Weapon = offer.Weapon, Upgrade = upgrade };
            if (authority != null) return authority.Select(choice);
            return connection.Send(SharedConnection.Message.Choice, w => { w.Write(choice.Run); w.Write(choice.Offer); w.Write(choice.Weapon); w.Write((int)choice.Upgrade); });
        }
        public bool ContinueChoice()
        {
            if (display.State != SharedState.Choosing || display.Players[local].Offer.Selected == UpgradeId.None || display.Players[local].ChoiceReady) return false;
            if (authority != null) { bool accepted = authority.ContinueChoice(display.Run, display.ChoicePhase, local); Publish(); return accepted; }
            return connection.Send(SharedConnection.Message.ContinueChoice, w => { w.Write(display.Run); w.Write(display.ChoicePhase); });
        }
        void GrowthHud(float w, float h)
        {
            if (display.Configuration.Scaling)
            {
                GUI.Label(new Rect(25, 139, 1100, 26), $"XP · {display.Xp} collected · {orbReplica.Orbs.Count} pickups hold {orbReplica.UncollectedXp} XP", heading);
                return;
            }
            if (!display.Configuration.Growth) return;
            SharedPlayer player = display.Players[local]; PersonalOffer offer = player.Offer;
            int goal = display.Level < display.Configuration.LevelCap ? EarnedGrowth.ThresholdFor(display.Level + 1) : EarnedGrowth.ThresholdFor(display.Configuration.LevelCap);
            int previous = display.Level == 1 ? 0 : EarnedGrowth.ThresholdFor(display.Level);
            GUI.Label(new Rect(25, 139, 1100, 26), $"XP · TEAM LEVEL {display.Level} · {(display.Level < display.Configuration.LevelCap ? display.Xp + " / " + goal + " to level up" : display.Xp + " collected · test level cap")}", heading);
            GUI.color = new Color(.08f, .18f, .28f); GUI.DrawTexture(new Rect(25, 173, 320, 12), Texture2D.whiteTexture);
            GUI.color = Time.realtimeSinceStartupAsDouble < pickupFlashUntil ? Color.white : new Color(.15f, .65f, 1);
            GUI.DrawTexture(new Rect(25, 173, 320 * (goal == previous ? 1 : Mathf.Clamp01((display.Xp - previous) / (float)(goal - previous))), 12), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(25, 193, 1100, 26), $"Collect XP inside the blue {EarnedGrowth.PickupRadius:0}m ring · {orbReplica.Orbs.Count} pickups hold {orbReplica.UncollectedXp} XP", small);
            GUI.Label(new Rect(25, 219, 1100, 26), display.Configuration.Stacking ? "ARSENAL · " + WeaponArsenal.Label(player.Arsenal) + (player.Specialization == UpgradeId.None ? "" : " · " + GrowthUpgrade.Label(player.Specialization)) + " · orange spread / purple piercing" : $"YOUR RIFLE · Body {player.BodyDamage} / Head {player.BodyDamage * 2} · {player.RifleInterval:0.00}s · {GrowthUpgrade.Label(offer.Selected)}", small);
            if (!solo) GUI.Label(new Rect(25, 245, 1100, 26), $"A: {GrowthUpgrade.Label(display.Players[0].Offer.Selected)} · B: {GrowthUpgrade.Label(display.Players[1].Offer.Selected)}", small);
            GUI.Label(new Rect(25, h - 105, 900, 25), "XP pickups: blue 1 · larger teal 2–4 · largest gold 5+ · nearby drops pull together and merge", small);
            if (Time.unscaledTime < confirmationUntil) GUI.Label(new Rect(w - 360, 145, 340, 35), GrowthUpgrade.Label(offer.Selected) + " applied", heading);
        }
        void ChoiceDialog(float w, float h)
        {
            GUI.color = new Color(.015f, .025f, .045f, .88f); GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            Rect panel = new Rect(w / 2 - 390, h / 2 - 180, 780, 360); GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x, panel.y + 15, panel.width, 45), "LEVEL UP — CHOOSE YOUR UPGRADE", lifeHeading);
            GUI.Label(new Rect(panel.x, panel.y + 64, panel.width, 35), "Combat is paused. Take your time.", lifeDetail);
            PersonalOffer offer = display.Players[local].Offer;
            if (offer.Pending)
            {
                for (int card = 1; card <= 2; card++)
                {
                    UpgradeId upgrade = offer.Card(card);
                    string text = card + " · " + GrowthUpgrade.Label(upgrade) + "\n\n" + GrowthUpgrade.Summary(upgrade);
                    if (GUI.Button(new Rect(panel.x + 20 + (card - 1) * 380, panel.y + 117, 360, 142), text, choiceCard)) Choose(upgrade);
                }
                GUI.Label(new Rect(panel.x, panel.y + 280, panel.width, 35), "Click a card or press 1 / 2.", lifeDetail);
            }
            else
            {
                GUI.Label(new Rect(panel.x, panel.y + 124, panel.width, 45), GrowthUpgrade.Label(offer.Selected) + " selected", lifeHeading);
                GUI.Label(new Rect(panel.x, panel.y + 174, panel.width, 35), GrowthUpgrade.Summary(offer.Selected), lifeDetail);
                if (!display.Players[local].ChoiceReady)
                { if (GUI.Button(new Rect(w / 2 - 160, panel.y + 223, 320, 50), "Continue (Enter)", choiceCard)) ContinueChoice(); }
                else GUI.Label(new Rect(panel.x, panel.y + 232, panel.width, 35), "Ready — waiting for your teammate", lifeDetail);
                if (!solo) GUI.Label(new Rect(panel.x, panel.y + 291, panel.width, 35), "A: " + ChoiceStatus(0) + "   ·   B: " + ChoiceStatus(1), lifeDetail);
            }
            if (GUI.Button(new Rect(w - 205, 18, 180, 38), "Back to menu")) BackToMenu();
        }
        string ChoiceStatus(int p) => display.Players[p].ChoiceReady ? "ready" : display.Players[p].Offer.Pending ? "choosing" : "chosen";
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
            if (command.action == "continue") return ContinueChoice() ? "" : "Continue rejected";
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
            public string role, source, machine, gpu, connection, reason, hostBindEndpoint, steering;
            public ScenarioEvidence scenario;
            public bool scripted, development, previewEnabled;
            public float audioVolume;
            public int run, ticks, enemies, catchupLimits, frameSamples, confirmedHits, width, height, frameCap, vsync;
            public double frameP50, frameP95, frameP99, frameMax, confirmP50, confirmP95, confirmMax, rttMs;
            public double updateLastMs, updateMaxMs, abortFrameMs, abortRemainingMs, abortPreviousUpdateMs;
            public double previewCaptureLastMs, previewCaptureMaxMs, previewCaptureCompletedAt, previewPollMaxMs;
            public int abortCatchupTicks;
            public SharedState state;
            public SharedPlayer[] players;
            public SharedEnemy[] enemyStates;
            public RangedState ranged;
            public WeaponWork[] weaponWork;
            public int roster, xp, level, uncollectedOrbs, uncollectedXp, choicePhase, controlRevision;
            public float pickupRadius, attractionRadius, mergeRadius;
            public long sentBytes, receivedBytes;
            public int outgoingPending, outgoingHighWater, outgoingDeferred, outgoingLimit;
            public SharedWork authorityWork;
            public long sentMessages, receivedMessages, snapshotBytes, snapshotMessages, allocatedThreadBytes, managedHeapBytes, workingSetBytes;
            public long presentationRays, presentationProbes;
            public double serializationMs, outgoingAgeMs, outgoingMaxAgeMs;
            public double loggingMs, loggingMaxMs;
            public int memorySamples;
            public double memorySamplingMs;
            public int outgoingBytes, outgoingBytesHighWater, xpDrops, xpMerges, xpMoveEvents, xpCollections, physicalXpMoves;
        }
        void Save()
        {
            if (diagnostics) { terminalAt = Time.realtimeSinceStartupAsDouble; ObserveTerminal("terminal-save.json"); }
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
            report.uncollectedXp = orbReplica.UncollectedXp; report.choicePhase = display.ChoicePhase; report.controlRevision = display.ControlRevision;
            report.weaponWork = weaponWork;
            report.authorityWork = authority?.Work;
            report.ranged = display.Ranged;
            report.presentationRays = presentationRays; report.presentationProbes = presentationProbes;
            report.sentMessages = connection?.SentMessages ?? 0; report.receivedMessages = connection?.ReceivedMessages ?? 0;
            report.snapshotBytes = connection?.SnapshotBytes ?? 0; report.snapshotMessages = connection?.SnapshotMessages ?? 0;
            report.serializationMs = connection?.SerializationMs ?? 0;
            report.loggingMs = loggingMs; report.loggingMaxMs = loggingMaxMs;
            report.steering = authority == null ? "client; host owns steering" : authority.ExactSteering ? "exact grid" : "pairwise";
            report.memorySamples = memorySamples; report.memorySamplingMs = memorySamplingMs;
            report.outgoingBytes = connection?.PendingBytes ?? 0; report.outgoingBytesHighWater = connection?.BufferedBytesHighWater ?? 0;
            report.outgoingAgeMs = connection?.PendingAgeMs ?? 0; report.outgoingMaxAgeMs = connection?.OldestAgeHighWaterMs ?? 0;
            report.xpDrops = orbReplica.Drops; report.xpMerges = orbReplica.MergeEvents; report.xpMoveEvents = orbReplica.MoveEvents;
            report.xpCollections = orbReplica.Collections; report.physicalXpMoves = authority?.Growth?.Moves ?? 0;
            report.allocatedThreadBytes = GC.GetAllocatedBytesForCurrentThread(); report.managedHeapBytes = GC.GetTotalMemory(false);
            using (var process = System.Diagnostics.Process.GetCurrentProcess()) report.workingSetBytes = process.WorkingSet64;
            report.updateLastMs = updateLastMs; report.updateMaxMs = updateMaxMs;
            report.abortFrameMs = abortFrameMs; report.abortRemainingMs = abortRemainingMs;
            report.abortPreviousUpdateMs = abortPreviousUpdateMs; report.abortCatchupTicks = abortCatchupTicks;
            report.previewCaptureLastMs = preview?.CaptureLastMs ?? 0; report.previewCaptureMaxMs = preview?.CaptureMaxMs ?? 0;
            report.previewCaptureCompletedAt = preview?.CaptureCompletedAt ?? 0; report.previewPollMaxMs = preview?.PollMaxMs ?? 0;
            report.audioVolume = AudioListener.volume;
            report.outgoingPending = connection?.PendingMessages ?? 0; report.outgoingHighWater = connection?.BufferedHighWater ?? 0;
            report.outgoingDeferred = connection?.DeferredSends ?? 0; report.outgoingLimit = SharedConnection.MaxBufferedMessages;
            report.pickupRadius = EarnedGrowth.PickupRadius; report.attractionRadius = EarnedGrowth.AttractionRadius; report.mergeRadius = EarnedGrowth.MergeRadius;
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
                choiceCard = new GUIStyle(GUI.skin.button) { fontSize = 20, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale); float w = Screen.width / scale, h = Screen.height / scale;
            SharedPlayer p = display.Players[local], a = display.Players[0], b = display.Players[1];
            if (viewTransition > 0)
            {
                GUI.color = new Color(0, 0, 0, viewTransition / ViewTransitionSeconds);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture); GUI.color = Color.white;
            }
            GUI.Label(new Rect(25, 18, 1000, 40), $"{(display.Configuration.Stacking ? display.Configuration.Name.ToUpperInvariant() : display.Configuration.Growth ? "EARNED GROWTH" : "SHARED RIFLE")} / {(solo ? "SOLO" : local == 0 ? "A · HOST" : "B · CLIENT")} / {display.State}", heading);
            if (display.State != SharedState.Choosing && (display.State != SharedState.Running || !inputActive))
                if (GUI.Button(new Rect(w - 205, 18, 180, 38), "Back to menu")) { BackToMenu(); return; }
            GUI.Label(new Rect(25, 57, 1150, 28), solo ? $"Run {display.Run} · {a.Health} HP · {a.Damage} damage · {a.Kills} kills" :
                $"Run {display.Run}   ·   A: {a.Health} HP, {a.Damage} damage, {a.Kills} kills   ·   B: {b.Health} HP, {b.Damage} damage, {b.Kills} kills", small);
            if (binding.Configuration != null)
            {
                GUI.Label(new Rect(25, 85, 1200, 25), (display.Configuration.Growth ? "START · " : "") + binding.Configuration.Label + " · " + binding.Configuration.Settings, small);
                GUI.Label(new Rect(25, 110, 1200, 25), (display.Configuration.Growth ? "Starting fixture locked · Retry resets earned builds · " : "Host settings locked for this session · Retry keeps them · ") + binding.Configuration.Identity.Substring(0, 12), small);
            }
            GrowthHud(w, h);
            GUI.Label(new Rect(w - 210, h - 60, 200, 40), $"{Mathf.Max(0, display.Configuration.Duration - display.Elapsed):F1}s" + (solo ? "" : $"   RTT {lastRtt:F1}ms"), small);
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
                    display.Elapsed < p.ImmuneUntil ? $"RETURN IMMUNITY {p.ImmuneUntil - display.Elapsed:F1}s" : "WASD · Mouse aim · Auto arsenal", heading);
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
            else if (display.State == SharedState.Choosing) ChoiceDialog(w, h);
            else if (display.State == SharedState.Complete || display.State == SharedState.Defeat)
            {
                LifeMessage(w, h, display.State == SharedState.Defeat ? (solo ? "DEFEATED" : "PARTY DEFEATED") : "RUN COMPLETE",
                    display.State == SharedState.Defeat ? (solo ? "You are down" : "Both players are down") : $"{display.Configuration.Duration:0.##}-second encounter complete",
                    solo ? "Retry starts a fresh run" : $"Retry readiness: A {(a.Ready ? "READY" : "waiting")} / B {(b.Ready ? "READY" : "waiting")}");
                if (GUI.Button(new Rect(w / 2 - 120, h / 2, 240, 50), "Retry (R)")) Retry();
            }
            else if (display.State == SharedState.Aborted)
                LifeMessage(w, h, "SESSION STOPPED", "The shared run was interrupted", "Start a new session to play again");
            else GUI.Label(new Rect(25, 280, 1150, 80), solo ? "Preparing the arena…" : "Waiting for the fixed host/client pair…", heading);
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
        public void BackToMenu()
        {
            if (binding == null) return;
            CloseSession("Returned to menu");
            GetComponent<PrototypeGame>()?.ReturnFromShared();
            Destroy(this);
        }
        internal void CloseSession(string closeReason)
        {
            if (binding == null) return;
            if (!saved && display.State != SharedState.Waiting && frameLog != null)
            { reason = closeReason; authority?.Abort(closeReason); display.State = SharedState.Aborted; Save(); }
            if (connection != null)
            {
                connection.Send(SharedConnection.Message.Abort, w => w.Write(closeReason));
                connection.Received -= Receive; connection.Lost -= Abort;
                connection.Dispose(); connection = null;
            }
            if (preview != null) { preview.Close(); Destroy(preview); }
            pickups?.Dispose();
            rangedView?.Dispose();
            frameLog?.Dispose(); hitLog?.Dispose(); growthLog?.Dispose(); diagnosticLog?.Dispose(); if (allyMaterial != null) Destroy(allyMaterial);
            if (ally != null) Destroy(ally);
            if (allyShot != null) { Destroy(allyShot.sharedMaterial); Destroy(allyShot.gameObject); }
            foreach (LineRenderer line in secondaryLines) if (line != null) Destroy(line.gameObject);
            if (scatterTrail != null) Destroy(scatterTrail); if (pierceTrail != null) Destroy(pierceTrail);
            if (rifleTrail != null) Destroy(rifleTrail);
            if (authority != null) { authority.Resolved -= Resolve; if (authority.Growth != null) authority.Growth.Changed -= GrowthChanged; }
            authority = null; binding = null; enabled = false; SetCursor(false);
        }
        void OnDestroy() => CloseSession("Peer player closed");
    }
}
