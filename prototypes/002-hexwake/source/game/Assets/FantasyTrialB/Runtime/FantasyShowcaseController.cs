using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GameDevHarness.FantasyTrialB
{
    public sealed class FantasyShowcaseController : MonoBehaviour
    {
        public Shader worldShader, effectShader;
        public FantasyRun State { get; } = new FantasyRun();
        public Camera ViewCamera { get; private set; }
        public bool ScriptedControl { get; set; }
        public int Seed { get; private set; }
        public int PickupCount => pickups.Count;
        public int VisibleGunCount => guns.Count;
        public Vector3 PlayerPosition => player.transform.position;
        public IReadOnlyDictionary<int, GunArtRig> Guns => guns;
        public float MovementAmount { get; private set; }
        public int ObservedMoves, ObservedLooks, ObservedJumps;

        readonly Dictionary<int, EnemyView> enemies = new Dictionary<int, EnemyView>();
        readonly Dictionary<int, GunArtRig> guns = new Dictionary<int, GunArtRig>();
        readonly Dictionary<TrialVortex, Transform> vortexVisuals = new Dictionary<TrialVortex, Transform>();
        readonly List<Pickup> pickups = new List<Pickup>();
        readonly List<Flash> flashes = new List<Flash>();
        readonly List<AudioClip> clips = new List<AudioClip>();
        ArtPalette palette;
        FantasyWorld world;
        PlayerArmsArt arms;
        CharacterController player;
        Transform effectRoot;
        Material lineMaterial, glowMaterial;
        Texture2D glowTexture;
        AudioSource audio;
        NavMeshData navData;
        NavMeshDataInstance navInstance;
        System.Random random;
        FantasyPhase observedPhase;
        float pitch, verticalSpeed, hitUntil, feedbackUntil;
        double nextSpawn, nextHealth;
        bool gameplayReleased, discardMouse = true;
        readonly bool[] choiceReleased = new bool[3];
        string feedback = "";
        GUIStyle title, text, small, number, button, cardTitle;
        static readonly Color Ink = new Color(0.04f, 0.065f, 0.07f);
        static readonly Color Gold = new Color(0.91f, 0.72f, 0.4f);

        sealed class EnemyView { public TrialEnemy Model; public GameObject Root; public NavMeshAgent Agent; public EnemyArtRig Rig; }
        sealed class Pickup { public GameObject Visual; public Vector3 Position; public bool Health; }
        sealed class Flash { public GameObject Visual; public float Remaining, Lifetime; public Color Color; }

        public static FantasyShowcaseController CreateWorld(Shader worldShader = null, Shader effectShader = null)
        {
            var root = new GameObject("Relic Clearing - Fantasy Showcase B");
            root.SetActive(false);
            var controller = root.AddComponent<FantasyShowcaseController>();
            controller.worldShader = worldShader;
            controller.effectShader = effectShader;
            root.SetActive(true);
            return controller;
        }

        void Awake()
        {
            palette = new ArtPalette(worldShader);
            lineMaterial = new Material(effectShader != null ? effectShader : Shader.Find("Sprites/Default"));
            glowMaterial = new Material(lineMaterial.shader);
            glowTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var glowPixels = new Color32[128 * 128];
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float radius = new Vector2((x - 63.5f) / 63.5f, (y - 63.5f) / 63.5f).magnitude;
                    byte alpha = (byte)(255 * Mathf.Pow(Mathf.Clamp01(1 - radius), 2));
                    glowPixels[y * 128 + x] = new Color32(255, 255, 255, alpha);
                }
            glowTexture.SetPixels32(glowPixels); glowTexture.Apply();
            glowMaterial.mainTexture = glowTexture;
            BuildPlayer();
            effectRoot = new GameObject("Owned-gun feedback").transform;
            effectRoot.SetParent(transform, false);
            MakeAudio();
            State.ClearPath = ClearPath;
            State.VortexTravelClear = ClearTravel;
            ResetRun();
            Debug.Log("SHOWCASE_READY revision=B ownMuzzles=true scaling=survival-time ranks=additive seed=" + Seed);
        }

        void BuildPlayer()
        {
            var body = new GameObject("Outsider");
            body.transform.SetParent(transform, false);
            body.layer = 2;
            player = body.AddComponent<CharacterController>();
            player.height = 1.8f; player.radius = 0.35f; player.center = Vector3.up * 0.9f;
            player.stepOffset = 0.25f;
            var view = new GameObject("Player view");
            view.transform.SetParent(body.transform, false);
            view.transform.localPosition = Vector3.up * 1.6f;
            ViewCamera = view.AddComponent<Camera>();
            ViewCamera.tag = "MainCamera";
            ViewCamera.nearClipPlane = 0.04f;
            ViewCamera.farClipPlane = 180;
            ViewCamera.fieldOfView = 72;
            ViewCamera.allowHDR = true;
            ViewCamera.allowMSAA = true;
            ViewCamera.clearFlags = CameraClearFlags.SolidColor;
            ViewCamera.backgroundColor = new Color(0.38f, 0.47f, 0.55f);
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = 65;
            QualitySettings.shadowCascades = 2;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            view.AddComponent<AudioListener>();
            audio = view.AddComponent<AudioSource>();
            audio.spatialBlend = 0; audio.volume = 0.24f;
            arms = PlayerArmsArt.Create(view.transform, palette);
        }

        void MakeAudio()
        {
            for (int kind = 0; kind < 4; kind++)
            {
                int size = kind == 2 ? 9500 : 5500;
                var data = new float[size];
                var noise = new System.Random(170 + kind);
                for (int i = 0; i < size; i++)
                {
                    float t = i / 22050f, envelope = Mathf.Exp(-t * (kind == 2 ? 9 : 30));
                    float n = (float)noise.NextDouble() * 2 - 1;
                    float tone = Mathf.Sin(t * Mathf.PI * 2 * (kind == 3 ? 850 - t * 1200 : 120 + kind * 90));
                    data[i] = (kind == 0 ? n * 0.65f + tone * 0.35f : kind == 2 ? n * 0.55f : tone * 0.6f + n * 0.25f) * envelope;
                }
                var clip = AudioClip.Create("Showcase " + (WeaponKind)kind, size, 1, 22050, false);
                clip.SetData(data, 0); clips.Add(clip);
            }
        }

        public void ResetRun(int? seed = null)
        {
            Seed = seed ?? UnityEngine.Random.Range(1, int.MaxValue);
            random = new System.Random(Seed);
            ClearEncounter();
            foreach (var gun in guns.Values) Destroy(gun.Root.gameObject);
            guns.Clear();
            foreach (var visual in vortexVisuals.Values) Destroy(visual.gameObject);
            vortexVisuals.Clear();
            foreach (var flash in flashes) Destroy(flash.Visual);
            flashes.Clear();
            if (navInstance.valid) navInstance.Remove();
            if (navData != null) Destroy(navData);
            if (world != null) world.Dispose();
            State.Reset(Seed);
            world = FantasyWorldArt.Create(transform, palette, Seed);
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = 0.48f; settings.agentHeight = 1.9f; settings.agentClimb = 0.3f;
            navData = NavMeshBuilder.BuildNavMeshData(settings, world.Sources, world.Bounds, Vector3.zero, Quaternion.identity);
            if (navData == null) throw new InvalidOperationException("Showcase navigation did not build.");
            navInstance = NavMesh.AddNavMeshData(navData);
            Teleport(new Vector3(0, 0.08f, 0));
            player.transform.rotation = Quaternion.identity;
            pitch = verticalSpeed = 0;
            ViewCamera.transform.localRotation = Quaternion.Euler(0, 0, 0);
            nextSpawn = 2; nextHealth = 45;
            SpawnEnemy(new Vector3(-1.7f, 0, 13));
            SpawnEnemy(new Vector3(2.4f, 0, 16));
            SpawnEnemy(new Vector3(7, 0, 17));
            observedPhase = FantasyPhase.Ready;
            feedback = "";
            SyncGuns();
            AnimateGuns(0);
            MenuInput();
            Debug.Log("SHOWCASE_RESET seed=" + Seed);
        }

        public void ClearEncounter()
        {
            foreach (var enemy in enemies.Values) { enemy.Root.SetActive(false); Destroy(enemy.Root); }
            enemies.Clear();
            State.Enemies.Clear(); State.Deaths.Clear();
            foreach (var pickup in pickups) Destroy(pickup.Visual);
            pickups.Clear();
        }

        public void Teleport(Vector3 position)
        {
            player.enabled = false; player.transform.position = position; player.enabled = true;
            Physics.SyncTransforms();
        }

        public void StartRun() { State.Start(); CaptureInput(); ObservePhase(); }
        public void PauseRun() { State.Pause(); ObservePhase(); }
        public void ResumeRun() { State.Resume(); CaptureInput(); ObservePhase(); }
        void CaptureInput() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; discardMouse = true; gameplayReleased = false; }
        void MenuInput() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; gameplayReleased = false; }

        void ObservePhase()
        {
            if (observedPhase == State.Phase) return;
            observedPhase = State.Phase;
            foreach (var enemy in enemies.Values)
                if (enemy.Agent.isOnNavMesh) enemy.Agent.isStopped = State.Phase != FantasyPhase.Playing || ScriptedControl;
            if (State.Phase != FantasyPhase.Playing)
            {
                MenuInput();
                for (int i = 0; i < choiceReleased.Length; i++) choiceReleased[i] = false;
            }
            else CaptureInput();
            Debug.Log($"SHOWCASE_PHASE phase={State.Phase} time={State.Elapsed:F3} hp={State.Health} level={State.Level}");
        }

        void Update()
        {
            if (ScriptedControl) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State.Phase == FantasyPhase.Playing) PauseRun();
                else if (State.Phase == FantasyPhase.Paused) ResumeRun();
                return;
            }
            if (State.Phase == FantasyPhase.Upgrade)
            {
                for (int i = 0; i < State.Choices.Count; i++)
                {
                    var key = KeyCode.Alpha1 + i;
                    if (!Input.GetKey(key)) choiceReleased[i] = true;
                    if (choiceReleased[i] && Input.GetKeyDown(key)) { SelectChoice(i); return; }
                }
                return;
            }
            if (State.Phase != FantasyPhase.Playing) return;
            if (!Input.GetMouseButton(0) && !Input.GetKey(KeyCode.Space)) gameplayReleased = true;
            if (Input.GetKeyDown(KeyCode.F))
            {
                State.Automatic = !State.Automatic;
                Feedback(State.Automatic ? "Automatic fire" : "Manual fire - hold left mouse");
                Debug.Log("SHOWCASE_MODE automatic=" + State.Automatic);
            }
            MovePlayer(Time.deltaTime);
            Step(Time.deltaTime, !State.Automatic && gameplayReleased && Input.GetMouseButton(0));
        }

        void MovePlayer(float dt)
        {
            float x = Input.GetAxisRaw("Mouse X"), y = Input.GetAxisRaw("Mouse Y");
            if (discardMouse) { x = y = 0; discardMouse = false; }
            if (Mathf.Abs(x) + Mathf.Abs(y) > 0.001f && ObservedLooks++ == 0) Debug.Log("SHOWCASE_NATIVE_LOOK");
            player.transform.Rotate(0, x * 1.8f, 0);
            pitch = Mathf.Clamp(pitch - y * 1.8f, -75, 75);
            ViewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector3 move = player.transform.right * Input.GetAxisRaw("Horizontal") +
                player.transform.forward * Input.GetAxisRaw("Vertical");
            if (move.sqrMagnitude > 1) move.Normalize();
            MovementAmount = move.magnitude;
            if (MovementAmount > 0.01f && ObservedMoves++ == 0) Debug.Log("SHOWCASE_NATIVE_MOVE");
            if (player.isGrounded)
            {
                verticalSpeed = -2;
                if (gameplayReleased && Input.GetKeyDown(KeyCode.Space))
                {
                    verticalSpeed = Mathf.Sqrt(52);
                    ObservedJumps++; Debug.Log("SHOWCASE_NATIVE_JUMP");
                }
            }
            verticalSpeed -= 20 * dt;
            player.Move((move * 6 + Vector3.up * verticalSpeed) * dt);
        }

        public void SyncGuns()
        {
            for (int i = 0; i < State.Weapons.Count; i++)
            {
                var weapon = State.Weapons[i];
                if (!guns.ContainsKey(weapon.CopyId))
                    guns.Add(weapon.CopyId, GunArtRig.Create(ViewCamera.transform, palette, weapon.Kind, weapon.CopyId, i));
            }
        }

        Vector3 AimPoint()
        {
            var ray = new Ray(ViewCamera.transform.position, ViewCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit, 30, (1 << 8) | (1 << 9) | (1 << 10))) return hit.point;
            return ray.GetPoint(30);
        }

        public void AnimateGuns(float dt)
        {
            SyncGuns();
            Vector3 point = AimPoint();
            foreach (var gun in guns.Values) gun.Animate(dt, (float)State.Elapsed, MovementAmount, point);
            arms.Animate(dt, (float)State.Elapsed, MovementAmount);
        }

        public void AimAt(Vector3 point)
        {
            ViewCamera.transform.rotation = Quaternion.LookRotation(point - ViewCamera.transform.position);
            AnimateGuns(0);
            Physics.SyncTransforms();
        }

        public bool RequestCopy(int index, bool manual, Vector3? ground = null, int? aimed = null)
        {
            SyncGuns();
            var weapon = State.Weapons[index];
            var gun = guns[weapon.CopyId];
            var camera = ViewCamera.transform;
            var ray = new Ray(camera.position, camera.forward);
            int target = aimed ?? AimedEnemy(ray);
            Vector3 point = AimPoint();
            Vector3 forward = (point - gun.Muzzle.position).normalized;
            return State.RequestAttack(index, gun.Muzzle.position, forward, target,
                ground ?? ProjectGround(ray), manual, camera.position, camera.forward);
        }

        public void Step(float dt, bool manualHeld)
        {
            if (State.Phase != FantasyPhase.Playing) return;
            foreach (var enemy in enemies.Values)
            {
                if (!enemy.Model.Alive) continue;
                enemy.Model.Position = enemy.Root.transform.position + Vector3.up * 0.85f;
                if (Vector3.Distance(enemy.Root.transform.position, PlayerPosition) < 1.08f && State.Contact())
                { Feedback("-10 health"); hitUntil = Time.unscaledTime + 0.2f; }
            }
            State.Advance(dt);
            AnimateGuns(dt);
            Physics.SyncTransforms();
            if (State.Phase == FantasyPhase.Playing && (gameplayReleased || ScriptedControl))
                for (int i = 0; i < State.Weapons.Count; i++)
                    if (State.Automatic || manualHeld) RequestCopy(i, !State.Automatic);
            ConsumeFeedback();
            foreach (var enemy in enemies.Values)
            {
                if (!enemy.Model.Alive || !enemy.Agent.isOnNavMesh) continue;
                Vector3 desired = enemy.Model.Position - Vector3.up * 0.85f;
                if (Vector3.Distance(desired, enemy.Root.transform.position) > 0.005f) enemy.Agent.Warp(desired);
                enemy.Agent.isStopped = State.Phase != FantasyPhase.Playing || ScriptedControl;
                if (!enemy.Agent.isStopped) enemy.Agent.SetDestination(PlayerPosition);
                enemy.Rig.Animate((float)State.Elapsed, enemy.Agent.velocity.magnitude);
            }
            CollectPickups();
            if (State.Phase == FantasyPhase.Playing)
            {
                if (State.Elapsed >= nextSpawn) { nextSpawn = State.Elapsed + FantasyRun.SpawnIntervalAt(State.Elapsed); TrySpawn(); }
                if (State.Elapsed >= nextHealth)
                {
                    nextHealth = State.Elapsed + 45;
                    AddPickup(new Vector3((float)random.NextDouble() * 12 - 6, 0.24f,
                        (float)random.NextDouble() * 12 - 6), true);
                }
                AdvanceVisuals(dt);
            }
            ObservePhase();
        }

        bool ClearPath(Vector3 from, Vector3 to) { return !Physics.Linecast(from, to, 1 << 8, QueryTriggerInteraction.Ignore); }
        bool ClearTravel(Vector3 from, Vector3 to)
        {
            var delta = to - from;
            if (Physics.CheckSphere(from + Vector3.up * 0.4f, 0.55f, 1 << 8)) return false;
            return delta.sqrMagnitude < 0.000001f || !Physics.SphereCast(from + Vector3.up * 0.4f,
                0.55f, delta.normalized, out _, delta.magnitude, 1 << 8, QueryTriggerInteraction.Ignore);
        }

        public int AimedEnemy(Ray ray)
        {
            if (!Physics.Raycast(ray, out var hit, 30, (1 << 8) | (1 << 10))) return -1;
            foreach (var enemy in enemies.Values)
                if (enemy.Model.Alive && hit.collider.transform.IsChildOf(enemy.Root.transform)) return enemy.Model.Id;
            return -1;
        }

        public Vector3? ProjectGround(Ray ray)
        {
            Vector3 point = Physics.Raycast(ray, out var hit, 18, (1 << 8) | (1 << 9) | (1 << 10)) ? hit.point : ray.GetPoint(17.8f);
            if (!Physics.Raycast(point + Vector3.up * 8, Vector3.down, out var ground, 35, 1 << 9)) return null;
            if (Vector3.Distance(ray.origin, ground.point) > 18 ||
                Physics.CheckSphere(ground.point + Vector3.up * 0.5f, 0.55f, 1 << 8)) return null;
            return ground.point + Vector3.up * 0.08f;
        }

        public TrialEnemy SpawnEnemy(Vector3 position, float diagnosticHealth = -1)
        {
            if (!NavMesh.SamplePosition(position, out var hit, 2, NavMesh.AllAreas)) return null;
            var model = State.AddEnemy(hit.position + Vector3.up * 0.85f, diagnosticHealth);
            var root = new GameObject("Relic sentinel " + model.Id);
            root.transform.SetParent(transform, false);
            root.transform.position = hit.position;
            Vector3 facing = PlayerPosition - root.transform.position;
            if (facing.sqrMagnitude > 0.01f) root.transform.rotation = Quaternion.LookRotation(facing);
            root.layer = 10;
            var collider = root.AddComponent<CapsuleCollider>();
            collider.height = 1.9f; collider.radius = 0.5f; collider.center = Vector3.up * 0.95f;
            var rig = EnemyArtRig.Create(root.transform, palette, model.Id % 2);
            var agent = root.AddComponent<NavMeshAgent>();
            agent.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
            agent.height = 1.9f; agent.radius = 0.48f;
            agent.speed = model.Speed; agent.acceleration = 14; agent.angularSpeed = 260;
            agent.stoppingDistance = 0.75f;
            agent.isStopped = State.Phase != FantasyPhase.Playing || ScriptedControl;
            enemies.Add(model.Id, new EnemyView { Model = model, Root = root, Agent = agent, Rig = rig });
            rig.Animate((float)State.Elapsed, 0);
            return model;
        }

        void TrySpawn()
        {
            if (enemies.Count >= 48) return;
            int start = random.Next(world.SpawnPoints.Length);
            for (int i = 0; i < world.SpawnPoints.Length; i++)
            {
                var point = world.SpawnPoints[(start + i) % world.SpawnPoints.Length];
                if (Vector3.Distance(point, PlayerPosition) < 9 ||
                    Physics.CheckSphere(point + Vector3.up, 0.9f, 1 << 10)) continue;
                if (SpawnEnemy(point) != null) return;
            }
        }

        public void AddPickup(Vector3 position, bool health)
        {
            var root = new GameObject(health ? "Recovery cell" : "Attunement core");
            root.transform.SetParent(transform, false);
            root.transform.position = position;
            ArtParts.Box("Relic housing", root.transform, Vector3.zero,
                new Vector3(0.18f, 0.28f, 0.18f), palette.Dark, 0.025f);
            ArtParts.Ring("Power halo", root.transform, Vector3.zero, 0.17f, 0.016f, health ? palette.Amber : palette.Teal);
            ArtParts.Box("Power core", root.transform, new Vector3(0, 0, -0.1f),
                new Vector3(0.08f, 0.15f, 0.025f), health ? palette.Amber : palette.Teal, 0.006f);
            pickups.Add(new Pickup { Visual = root, Position = position, Health = health });
        }

        void CollectPickups()
        {
            for (int i = pickups.Count - 1; i >= 0 && State.Phase == FantasyPhase.Playing; i--)
            {
                var pickup = pickups[i];
                if (Vector3.Distance(PlayerPosition + Vector3.up * 0.3f, pickup.Position) > 2) continue;
                if (pickup.Health) { if (!State.Heal(15)) continue; Feedback("+15 health"); }
                else { State.CollectXp(4); Feedback("+4 attunement"); Debug.Log($"SHOWCASE_XP value={State.Xp} phase={State.Phase}"); }
                Destroy(pickup.Visual); pickups.RemoveAt(i);
            }
        }

        public void ConsumeFeedback()
        {
            SyncGuns();
            foreach (var attack in State.Attacks)
            {
                var gun = guns[attack.CopyId];
                gun.Fire();
                Color color = palette.Accent(attack.Kind).color;
                Vector3 start = attack.Origin;
                if (attack.Kind == WeaponKind.Pistol)
                {
                    FlashPoint(start, color, 0.055f, 0.065f);
                    var end = attack.Points[0];
                    if (Physics.Linecast(start, end, out var hit, 1 << 8)) end = hit.point;
                    Line(new[] { start, end }, new Color(1, 0.86f, 0.56f, 0.8f), 0.008f, 0.045f);
                }
                else if (attack.Kind == WeaponKind.Ember)
                {
                    Vector3 forward = (attack.Points[0] - start).normalized;
                    for (int i = 0; i < 9; i++)
                    {
                        float angle = i * Mathf.PI * 2 / 9;
                        Vector3 edge = ViewCamera.transform.right * Mathf.Cos(angle) + ViewCamera.transform.up * Mathf.Sin(angle);
                        Vector3 end = start + (forward * 0.94f + edge * 0.34f).normalized * 6;
                        if (Physics.Linecast(start, end, out var hit, 1 << 8)) end = hit.point;
                        for (int plume = 0; plume < 3; plume++)
                        {
                            float distance = 0.35f + plume * 0.65f;
                            float fraction = Mathf.Min(1, distance / Mathf.Max(0.01f, Vector3.Distance(start, end)));
                            FlashPoint(Vector3.Lerp(start, end, fraction),
                                new Color(1, 0.4f + plume * 0.05f, 0.08f, 0.7f - plume * 0.12f),
                                0.075f + plume * 0.08f, 0.14f + plume * 0.025f);
                        }
                    }
                    FlashPoint(start, color, 0.11f, 0.1f);
                }
                else if (attack.Kind == WeaponKind.Spark)
                {
                    Vector3 last = start;
                    foreach (var point in attack.Points)
                    {
                        Vector3 middle = Vector3.Lerp(last, point, 0.5f) + Vector3.up * 0.12f;
                        Line(new[] { last, middle, point }, new Color(0.5f, 0.7f, 1, 0.95f), 0.016f, 0.17f);
                        FlashPoint(point, color, 0.08f, 0.12f);
                        last = point;
                    }
                }
                else
                {
                    FlashPoint(start, color, 0.09f, 0.13f);
                    if (attack.Points.Count > 0)
                        Line(new[] { start, attack.Points[0] }, new Color(0.3f, 0.84f, 0.62f, 0.22f), 0.008f, 0.12f);
                }
                audio.PlayOneShot(clips[(int)attack.Kind]);
                if (!ScriptedControl) Debug.Log($"SHOWCASE_GUN_FIRE copy={attack.CopyId} kind={attack.Kind} origin={attack.Origin}");
            }
            State.Attacks.Clear();
            foreach (var dead in State.Deaths)
            {
                if (enemies.TryGetValue(dead.Id, out var view))
                { view.Root.SetActive(false); Destroy(view.Root); enemies.Remove(dead.Id); }
                State.Enemies.Remove(dead);
                AddPickup(new Vector3(dead.Position.x, dead.Position.y - 0.55f, dead.Position.z), false);
                if (random.NextDouble() < 0.08)
                    AddPickup(new Vector3(dead.Position.x + 0.4f, dead.Position.y - 0.55f, dead.Position.z), true);
                for (int i = 0; i < 5; i++)
                    FlashPoint(dead.Position + new Vector3((i - 2) * 0.1f, 0.05f * i, 0), palette.Teal.color, 0.055f, 0.2f);
            }
            State.Deaths.Clear();
            var expired = new List<TrialVortex>();
            foreach (var entry in vortexVisuals) if (!State.Vortices.Contains(entry.Key)) expired.Add(entry.Key);
            foreach (var vortex in expired) { Destroy(vortexVisuals[vortex].gameObject); vortexVisuals.Remove(vortex); }
            foreach (var vortex in State.Vortices)
            {
                if (!vortexVisuals.TryGetValue(vortex, out var visual))
                {
                    visual = new GameObject("Wind field - copy " + vortex.Owner.CopyId).transform;
                    visual.SetParent(effectRoot, false);
                    for (int ring = 0; ring < 4; ring++)
                    {
                        var loop = new GameObject("Air current");
                        loop.transform.SetParent(visual, false);
                        var line = loop.AddComponent<LineRenderer>();
                        line.useWorldSpace = false; line.loop = true; line.positionCount = 40;
                        line.sharedMaterial = lineMaterial;
                        line.widthMultiplier = 0.016f;
                        line.startColor = line.endColor = new Color(0.45f, 0.85f, 0.64f, 0.45f);
                        for (int j = 0; j < 40; j++)
                        {
                            float a = j * Mathf.PI * 2 / 40;
                            line.SetPosition(j, new Vector3(Mathf.Cos(a) * (0.32f + ring * 0.32f),
                                ring * 0.31f + Mathf.Sin(a) * 0.14f, Mathf.Sin(a) * (0.32f + ring * 0.32f)));
                        }
                    }
                    vortexVisuals.Add(vortex, visual);
                }
                visual.position = vortex.Position;
                visual.rotation = Quaternion.Euler(0, (float)State.Elapsed * 150, 0);
            }
        }

        void FlashPoint(Vector3 position, Color color, float radius, float lifetime)
        {
            var go = new GameObject("Energy motes");
            go.transform.SetParent(effectRoot, false); go.transform.position = position;
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = glowMaterial;
            line.positionCount = 2; line.SetPositions(new[] { position - ViewCamera.transform.up * radius, position + ViewCamera.transform.up * radius });
            line.widthMultiplier = radius;
            line.startColor = line.endColor = color;
            flashes.Add(new Flash { Visual = go, Remaining = lifetime, Lifetime = lifetime, Color = color });
        }

        void Line(Vector3[] positions, Color color, float width, float lifetime)
        {
            var go = new GameObject("Own-muzzle attack path");
            go.transform.SetParent(effectRoot, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.positionCount = positions.Length; line.SetPositions(positions);
            line.startColor = line.endColor = color; line.widthMultiplier = width;
            line.numCapVertices = 4;
            flashes.Add(new Flash { Visual = go, Remaining = lifetime, Lifetime = lifetime, Color = color });
        }

        void AdvanceVisuals(float dt)
        {
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                flashes[i].Remaining -= dt;
                if (flashes[i].Remaining <= 0) { Destroy(flashes[i].Visual); flashes.RemoveAt(i); }
                else
                {
                    var line = flashes[i].Visual.GetComponent<LineRenderer>();
                    var color = flashes[i].Color;
                    color.a *= Mathf.Clamp01(flashes[i].Remaining / flashes[i].Lifetime);
                    line.startColor = line.endColor = color;
                }
            }
            foreach (var pickup in pickups)
            {
                pickup.Visual.transform.position = pickup.Position + Vector3.up * Mathf.Sin((float)State.Elapsed * 2.3f) * 0.06f;
                pickup.Visual.transform.Rotate(Vector3.up, dt * 30);
            }
        }

        public void SelectChoice(int index)
        {
            if (!State.Choose(index)) return;
            SyncGuns(); AnimateGuns(0);
            Feedback("Arsenal updated");
            Debug.Log($"SHOWCASE_CHOICE guns={VisibleGunCount} level={State.Level}");
            ObservePhase();
        }

        void Feedback(string value) { feedback = value; feedbackUntil = Time.unscaledTime + 1.3f; }
        public static string GunName(WeaponKind kind)
        {
            return kind == WeaponKind.Pistol ? "Earth pistol" : kind == WeaponKind.Ember ? "Fire projector" :
                kind == WeaponKind.Whirlwind ? "Wind projector" : "Lightning emitter";
        }

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 49, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            number = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true };
            cardTitle = new GUIStyle(text) { fontSize = 20, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.95f, 0.93f, 0.85f);
            text.normal.textColor = new Color(0.86f, 0.88f, 0.83f);
            small.normal.textColor = new Color(0.7f, 0.76f, 0.72f);
            number.normal.textColor = Gold;
        }

        void Panel(Rect r, float opacity = 0.9f)
        {
            GUI.color = new Color(Ink.r, Ink.g, Ink.b, opacity);
            GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
        }

        void Bar(Rect r, float value, Color color)
        {
            GUI.color = new Color(0.17f, 0.22f, 0.2f); GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color; GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(value), r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void OnGUI()
        {
            Styles();
            float scale = Mathf.Min(1.25f, Mathf.Min(Screen.width / 960f, Screen.height / 600f));
            float w = Screen.width / scale, h = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            if (State.Phase == FantasyPhase.Playing)
            {
                Panel(new Rect(24, 22, 230, 67), 0.7f);
                GUI.Label(new Rect(38, 28, 210, 20), "OUTSIDER / RELIC CLEARING", small);
                GUI.Label(new Rect(38, 49, 210, 26), State.Health + "   HEALTH", number);
                Bar(new Rect(38, 79, 200, 3), State.Health / 100f, Gold);
                double remaining = Math.Max(0, 600 - State.Elapsed);
                Panel(new Rect(w - 171, 22, 147, 67), 0.7f);
                GUI.Label(new Rect(w - 157, 28, 120, 20), "SURVIVE", small);
                GUI.Label(new Rect(w - 157, 49, 120, 26), ((int)remaining / 60).ToString("00") + ":" + ((int)remaining % 60).ToString("00"), number);
                GUI.Label(new Rect(w / 2 - 45, 25, 140, 20), State.Automatic ? "AUTO   [F]" : "MANUAL   [F]", small);
                GUI.color = Time.unscaledTime < hitUntil ? new Color(1, 0.35f, 0.2f) : new Color(1, 0.94f, 0.76f, 0.9f);
                GUI.DrawTexture(new Rect(w / 2 - 6, h / 2, 3, 1), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(w / 2 + 3, h / 2, 3, 1), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(w / 2, h / 2 - 6, 1, 3), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(w / 2, h / 2 + 3, 1, 3), Texture2D.whiteTexture);
                GUI.color = Color.white;
                float slot = (w - 60) / 6;
                for (int i = 0; i < State.Weapons.Count; i++)
                {
                    var gun = State.Weapons[i];
                    float x = 30 + i * slot;
                    Panel(new Rect(x, h - 63, slot - 7, 43), 0.7f);
                    GUI.Label(new Rect(x + 8, h - 60, slot - 22, 20), GunName(gun.Kind), small);
                    GUI.Label(new Rect(x + 8, h - 41, slot - 22, 20), "COPY " + gun.CopyId + "  /  RANK " + gun.DamageRanks, small);
                }
                Bar(new Rect(30, h - 14, w - 60, 2), State.Xp / (float)State.Threshold, Gold);
                GUI.Label(new Rect(30, h - 91, 360, 25), "LEVEL " + State.Level + "   /   " + State.Xp + " OF " + State.Threshold + " ATTUNEMENT", small);
                if (Time.unscaledTime < feedbackUntil) GUI.Label(new Rect(w / 2 - 125, h / 2 + 30, 280, 25), feedback, text);
                return;
            }
            if (State.Phase == FantasyPhase.Upgrade)
            {
                float left = (w - 840) / 2, top = (h - 450) / 2;
                Panel(new Rect(left, top, 840, 450), 0.94f);
                GUI.Label(new Rect(left + 28, top + 23, 780, 27), "ATTUNEMENT / INVENT YOUR ADVANTAGE", small);
                GUI.Label(new Rect(left + 28, top + 63, 780, 48), "A new edge. Not an easy victory.", cardTitle);
                GUI.Label(new Rect(left + 28, top + 108, 780, 30), "Time is frozen. Choose one gun or improve one identified copy.", text);
                for (int i = 0; i < State.Choices.Count; i++)
                {
                    var choice = State.Choices[i]; float x = left + 28 + i * 266;
                    Panel(new Rect(x, top + 165, 250, 172));
                    GUI.Label(new Rect(x + 15, top + 182, 220, 68),
                        (choice.IsGrant ? "ACQUIRE\n" : "COPY " + choice.CopyId + "\n") + GunName(choice.Kind), cardTitle);
                    GUI.Label(new Rect(x + 15, top + 275, 220, 48),
                        choice.IsGrant ? "Distinct gun. Independent muzzle." : "+10% of base power\nAdditive, not compounding", small);
                    if (GUI.Button(new Rect(x, top + 352, 250, 52), "Choose  " + (i + 1), button)) SelectChoice(i);
                }
                return;
            }
            float panelX = 30, panelY = (h - 520) / 2;
            Panel(new Rect(panelX, panelY, 440, 520), 0.83f);
            GUI.color = Gold; GUI.DrawTexture(new Rect(panelX, panelY, 440, 2), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(panelX + 26, panelY + 24, 390, 24), "FANTASY TECHNOLOGY / FIELD TRIAL B", small);
            string heading = State.Phase == FantasyPhase.Ready ? "THE RELIC\nCLEARING" :
                State.Phase == FantasyPhase.Paused ? "TAKE A\nBREATH" :
                State.Phase == FantasyPhase.Victory ? "YOU HELD\nTHE LINE" : "ANOTHER\nARRIVAL";
            GUI.Label(new Rect(panelX + 26, panelY + 68, 400, 128), heading, title);
            GUI.Label(new Rect(panelX + 26, panelY + 212, 385, 72),
                "A familiar gun. An unfamiliar world.\nRecover relic power. Build your arsenal.\nSurvive the rising pressure.", text);
            GUI.Label(new Rect(panelX + 26, panelY + 301, 385, 62),
                "WASD move  /  Mouse aim  /  Space jump\nF auto/manual  /  Hold left mouse to fire\nEscape pause  /  1-3 choose inventions", small);
            string action = State.Phase == FantasyPhase.Paused ? "Resume survival" :
                State.Phase == FantasyPhase.Ready ? "Enter the clearing" : "Begin a fresh arrival";
            GUI.backgroundColor = Gold;
            if (GUI.Button(new Rect(panelX + 26, panelY + 386, 385, 51), action, button))
            {
                if (State.Phase == FantasyPhase.Paused) ResumeRun();
                else { if (State.Phase != FantasyPhase.Ready) ResetRun(); StartRun(); }
            }
            GUI.backgroundColor = Color.white;
            if (GUI.Button(new Rect(panelX + 26, panelY + 450, 180, 42), "Leave / reset", button)) ResetRun();
            if (GUI.Button(new Rect(panelX + 220, panelY + 450, 191, 42), "Quit", button)) Application.Quit();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && !ScriptedControl && State.Phase == FantasyPhase.Playing) PauseRun();
        }

        void OnDestroy()
        {
            if (navInstance.valid) navInstance.Remove();
            if (navData != null) Destroy(navData);
            if (world != null) world.Dispose();
            palette?.Dispose();
            ArtParts.DisposeMeshes();
            if (lineMaterial != null) Destroy(lineMaterial);
            if (glowMaterial != null) Destroy(glowMaterial);
            if (glowTexture != null) Destroy(glowTexture);
            foreach (var clip in clips) Destroy(clip);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
