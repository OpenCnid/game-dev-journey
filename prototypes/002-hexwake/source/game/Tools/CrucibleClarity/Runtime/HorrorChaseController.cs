using System;
using System.Collections.Generic;
using GameDevHarness.FantasyTrialB;
using UnityEngine;
using UnityEngine.AI;

namespace GameDevHarness.CrucibleReborn
{
    public sealed class ChaseSpawnRecord
    {
        public int EnemyId, Portal;
        public Vector3 Position;
        public double Time, WarnedAt;
        public float PlayerDistance;
    }

    public sealed class HorrorChaseController : MonoBehaviour
    {
        public Shader worldShader, effectShader;
        public ChaseRun State { get; } = new ChaseRun();
        public ChaseHud Hud { get; } = new ChaseHud();
        public Camera ViewCamera { get; private set; }
        public ChaseArena Arena { get; private set; }
        public ChaseSwarm Swarm { get; private set; }
        public ChaseEffects HitFeedback { get; private set; }
        public ChasePlayerFeedback PlayerFeedback { get; private set; }
        public float MasterVolume { get; private set; }
        public float DamageFlash => PlayerFeedback?.DamageFlash ?? 0;
        public double MobilityRemaining => Math.Max(0, mobilityReadyAt - State.Elapsed);
        public string MobilityLabel => (int)starter / 2 == 2 ? "Barbarian leap" :
            (int)starter / 2 == 5 ? "Shield charge" : (int)starter / 2 == 6 ? "Mage blink" : "Combat surge";
        public int PickupCount => pickups.Count;
        public Vector3[] PickupPositions(bool health)
        {
            var result = new List<Vector3>();
            foreach (var pickup in pickups) if (pickup.Health == health) result.Add(pickup.Visual.transform.position);
            return result.ToArray();
        }
        public bool ScriptedControl, SpawningEnabled = true, MapExpanded;
        public int Seed, SpawnedTotal, ContactsObserved, ObservedMoves, ObservedJumps;
        public int EnemyCount => enemies.Count;
        public int VisibleGunCount => guns.Count;
        public Vector3 PlayerPosition => player.transform.position;
        public ChaseLoot NearestLoot
        {
            get
            {
                ChaseLoot nearest = null; float distance = 2.2f;
                foreach (var loot in State.Loot)
                {
                    float candidate = Vector3.Distance(PlayerPosition + Vector3.up * .3f, loot.Position);
                    if (candidate < distance) { nearest = loot; distance = candidate; }
                }
                return nearest;
            }
        }
        public float MovementAmount { get; private set; }
        public string FeedbackText => State.Elapsed < feedbackUntil ? feedback : "";
        public IReadOnlyDictionary<int, ChaseGunRig> Guns => guns;
        public readonly List<ChaseSpawnRecord> SpawnRecords = new List<ChaseSpawnRecord>();
        public event Action<ChaseSpawnRecord> SpawnObserved;
        public event Action<int> ContactObserved;
        public readonly double[] PortalWarnedAt = new double[6];
        public double SimulationMilliseconds, NavigationMilliseconds, SwarmMilliseconds, EffectsMilliseconds, SpawnMilliseconds;
        public int ProfileFrames;
        public double ContactMilliseconds, ClearPathMilliseconds;
        public long ClearPathQueries, AdvanceSubsteps;
        ChaseVisibility visibility;
        public int ActivePortals => State.Phase == ChasePhase.Ready ? 0 : ChaseCalibration.ActivePortalsAt(State.Elapsed);
        public int SelectedEnemyId => AimedEnemy(new Ray(ViewCamera.transform.position, ViewCamera.transform.forward));

        sealed class EnemyView
        {
            public ChaseEnemy Model;
            public GameObject Root;
            public EnemyMotor Motor;
            public int BoundTargetId;
            public double BoundTargetReadyAt;
        }
        sealed class Pickup { public GameObject Visual; public Vector3 Position; public bool Health; }
        sealed class LootView { public GameObject Root; }
        readonly Dictionary<int, EnemyView> enemies = new Dictionary<int, EnemyView>();
        readonly Dictionary<int, ChaseGunRig> guns = new Dictionary<int, ChaseGunRig>();
        readonly List<ChasePose> poses = new List<ChasePose>();
        public event System.Action<int> EnemyPresentationComplete;
        readonly List<Pickup> pickups = new List<Pickup>();
        readonly Dictionary<int, LootView> lootViews = new Dictionary<int, LootView>();
        ChaseLootArt lootArt;
        readonly Stack<GameObject> enemyRoots = new Stack<GameObject>();
        readonly Stack<GameObject>[] pickupRoots = { new Stack<GameObject>(), new Stack<GameObject>() };
        readonly GameObject[] pickupTemplates = new GameObject[2];
        ArtPalette palette;
        ChaseArmsArt arms;
        CharacterController player;
        Material glowMaterial;
        Texture2D glowTexture;
        NavMeshData navData;
        NavMeshDataInstance navInstance;
        NavMeshPath spawnPath;
        System.Random random;
        ChasePhase observedPhase;
        ChaseWeaponKind starter = ChaseWeaponKind.Stormsplit;
        float pitch;
        float landingKick;
        readonly ChaseMovement movement = new ChaseMovement();
        public Vector3 HorizontalVelocity => movement.HorizontalVelocity;
        public bool PlayerGrounded => player.isGrounded;
        double spawnBudget, nextHealth, feedbackUntil;
        string feedback = "";
        int nextPortal;
        bool discardMouse = true, gameplayReleased;
        bool wasAirborne, leapLanding;
        double mobilityReadyAt;
        double nextContactCheck;
        GameObject chargeShield;
        const int Solids = (1 << 8) | (1 << 9);

        public static HorrorChaseController CreateWorld(Shader worldShader = null, Shader effectShader = null)
        {
            var root = new GameObject("Crucible Reborn I");
            root.SetActive(false);
            var trial = root.AddComponent<HorrorChaseController>();
            trial.worldShader = worldShader; trial.effectShader = effectShader;
            root.SetActive(true);
            return trial;
        }

        void Awake()
        {
            spawnPath = new NavMeshPath();
            palette = new ArtPalette(worldShader);
            glowMaterial = new Material(effectShader != null ? effectShader : Shader.Find("Sprites/Default"));
            glowTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[4096];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float r = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
                    pixels[y * 64 + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Pow(Mathf.Clamp01(1 - r), 2)));
                }
            glowTexture.SetPixels32(pixels); glowTexture.Apply();
            glowMaterial.mainTexture = glowTexture;
            var body = new GameObject("Runner"); body.transform.SetParent(transform, false); body.layer = 2;
            player = body.AddComponent<CharacterController>();
            player.height = 1.8f; player.radius = 0.35f; player.center = Vector3.up * 0.9f;
            player.stepOffset = 0.3f;
            var view = new GameObject("Runner view"); view.transform.SetParent(body.transform, false);
            view.transform.localPosition = Vector3.up * 1.6f;
            ViewCamera = view.AddComponent<Camera>();
            ViewCamera.tag = "MainCamera"; ViewCamera.nearClipPlane = 0.04f; ViewCamera.farClipPlane = 180;
            ViewCamera.fieldOfView = 76; ViewCamera.allowHDR = true; ViewCamera.allowMSAA = true;
            ViewCamera.clearFlags = CameraClearFlags.SolidColor;
            ViewCamera.backgroundColor = new Color(.075f, .062f, .052f);
            view.AddComponent<AudioListener>();
            var heldFill = new GameObject("Warm held relic inspection light");
            heldFill.transform.SetParent(view.transform, false);
            heldFill.transform.localRotation = Quaternion.Euler(22, -32, 0);
            var heldLight = heldFill.AddComponent<Light>();
            heldLight.type = LightType.Directional;
            heldLight.color = new Color(.86f, .77f, .61f); heldLight.intensity = .85f;
            heldLight.cullingMask = 1 << 13; heldLight.shadows = LightShadows.None;
            arms = ChaseArmsArt.Create(view.transform, palette);
            Swarm = ChaseSwarm.Create(transform, palette);
            HitFeedback = ChaseEffects.Create(transform, ViewCamera, glowMaterial);
            PlayerFeedback = ChasePlayerFeedback.Create(transform, ViewCamera);
            SetMasterVolume(PlayerPrefs.GetFloat("CrucibleRebornI.MasterVolume",
                PlayerPrefs.GetFloat("InfernalRealismH.MasterVolume", .28f)));
            chargeShield = new GameObject("Paladin forward charge shield");
            chargeShield.transform.SetParent(view.transform, false);
            chargeShield.transform.localPosition = new Vector3(-.28f, -.2f, .62f);
            ArtParts.Box("Aged shield face", chargeShield.transform, Vector3.zero,
                new Vector3(.58f, .72f, .09f), palette.Steel);
            ArtParts.Box("Shield holy spine", chargeShield.transform, new Vector3(0, 0, -.06f),
                new Vector3(.06f, .62f, .035f), palette.Amber);
            chargeShield.SetActive(false);
            State.ClearPath = ClearPath; State.VortexTravelClear = ClearTravel;
            State.SurfaceHit = (from, to) => Physics.Linecast(from, to, out var hit, Solids, QueryTriggerInteraction.Ignore)
                ? (ChaseSurface?)new ChaseSurface { Point = hit.point, Normal = hit.normal } : null;
            ResetRun();
            SetGraphics(2);
            Debug.Log("CHASE_READY revision=I classes=8 guns=16 fixedPortals=6");
        }

        public void ResetRun(int? seed = null, ChaseWeaponKind? starter = null)
        {
            Seed = seed ?? UnityEngine.Random.Range(1, int.MaxValue);
            if (starter.HasValue) this.starter = starter.Value;
            random = new System.Random(Seed);
            ClearEncounter(); ClearGuns();
            if (navInstance.valid) navInstance.Remove();
            if (navData != null) Destroy(navData);
            Arena?.Dispose();
            State.Reset(Seed, this.starter);
            SyncLootViews();
            Arena = ChaseArena.Create(transform, palette, Seed);
            visibility = new ChaseVisibility(Arena.Root);
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = .55f; settings.agentHeight = 2.4f; settings.agentClimb = .3f;
            settings.overrideVoxelSize = true; settings.voxelSize = .1f;
            navData = NavMeshBuilder.BuildNavMeshData(settings, Arena.Sources, Arena.Bounds, Vector3.zero, Quaternion.identity);
            if (navData == null) throw new InvalidOperationException("Chase navigation did not build.");
            navInstance = NavMesh.AddNavMeshData(navData);
            Teleport(Arena.PlayerStart);
            player.transform.rotation = Quaternion.identity; ViewCamera.transform.localRotation = Quaternion.identity;
            pitch = 0; landingKick = 0; movement.Reset(); spawnBudget = 0; nextPortal = 0; nextHealth = 90;
            mobilityReadyAt = 0; leapLanding = wasAirborne = false;
            nextContactCheck = 0;
            chargeShield?.SetActive(false);
            SpawnedTotal = ContactsObserved = 0; SpawnRecords.Clear();
            for (int i = 0; i < PortalWarnedAt.Length; i++) PortalWarnedAt[i] = -1;
            observedPhase = ChasePhase.Ready; feedback = ""; MapExpanded = false;
            SyncGuns(); AnimateGuns(0); MenuInput(); SetGraphics(Hud.GraphicsPreset);
            Debug.Log("CHASE_RESET seed=" + Seed);
        }

        void ClearGuns()
        {
            foreach (var gun in guns.Values) gun.Dispose();
            guns.Clear();
        }

        public void SetStarter(ChaseWeaponKind kind)
        {
            if (State.Phase != ChasePhase.Ready) return;
            starter = kind; State.Reset(Seed, kind); ClearGuns(); SyncGuns(); AnimateGuns(0);
            PlayerFeedback?.Play(ChasePlayerCue.ClassSelect);
        }

        public void ClearEncounter()
        {
            foreach (var view in enemies.Values)
            {
                Swarm?.Remove(view.Model.Id); view.Motor.Retire();
                view.Root.SetActive(false); enemyRoots.Push(view.Root);
            }
            enemies.Clear(); State.Enemies.Clear(); State.Deaths.Clear();
            State.Fields.Clear(); State.Corpses.Clear(); State.Hits.Clear(); State.Attacks.Clear();
            foreach (var pickup in pickups) ReturnPickup(pickup);
            pickups.Clear(); HitFeedback?.Clear();
            poses.Clear();
        }

        public void Teleport(Vector3 position)
        {
            player.enabled = false; player.transform.position = position; player.enabled = true;
            movement.Reset();
            PlayerFeedback?.ResetFootsteps();
            Physics.SyncTransforms();
        }


        public void StartRun() { State.Start(); CaptureInput(); ObservePhase(); }
        public void PauseRun() { State.Pause(); ObservePhase(); }
        public void ResumeRun() { MapExpanded = false; State.Resume(); CaptureInput(); ObservePhase(); }
        public void ToggleMap()
        {
            if (State.Phase == ChasePhase.Playing) { MapExpanded = true; PauseRun(); }
            else if (State.Phase == ChasePhase.Paused && MapExpanded) ResumeRun();
        }
        public void ReloadAll()
        {
            if (State.Phase != ChasePhase.Playing ||
                !State.Weapons.Exists(w => w.Ammo < w.Magazine && w.ReloadUntil <= State.Elapsed)) return;
            State.ReloadAll(); Feedback("Reloading arcane cartridges");
        }
        public void SetGraphics(int preset)
        { ChaseGraphics.Apply(preset, ViewCamera, Swarm, Arena); HitFeedback?.SetQuality(preset); }
        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat("CrucibleRebornI.MasterVolume", MasterVolume);
            PlayerPrefs.Save();
        }
        public bool RequestMobility()
        {
            if (State.Phase != ChasePhase.Playing || MobilityRemaining > 0) return false;
            Vector3 forward = player.transform.forward; forward.y = 0; forward.Normalize();
            int classId = (int)starter / 2;
            if (classId == 6)
            {
                float distance = 8;
                Vector3 low = PlayerPosition + Vector3.up * player.radius;
                Vector3 high = PlayerPosition + Vector3.up * (player.height - player.radius);
                if (Physics.CapsuleCast(low, high, player.radius, forward, out var obstruction,
                    distance, Solids, QueryTriggerInteraction.Ignore))
                    distance = Mathf.Max(0, obstruction.distance - .12f);
                if (distance < .5f) return false;
                Vector3 destination = PlayerPosition + forward * distance;
                if (!Physics.Raycast(destination + Vector3.up * .4f, Vector3.down,
                    out var floor, 2, 1 << 9, QueryTriggerInteraction.Ignore)) return false;
                destination.y = floor.point.y + .04f;
                if (Physics.CheckCapsule(destination + Vector3.up * player.radius,
                    destination + Vector3.up * (player.height - player.radius), player.radius,
                    Solids, QueryTriggerInteraction.Ignore)) return false;
                Teleport(destination); PlayerFeedback.Play(ChasePlayerCue.Teleport);
                mobilityReadyAt = State.Elapsed + 5;
            }
            else if (classId == 2)
            {
                if (!player.isGrounded) return false;
                movement.Launch(forward * 11, 10); leapLanding = true;
                PlayerFeedback.Play(ChasePlayerCue.Leap); mobilityReadyAt = State.Elapsed + 5;
            }
            else
            {
                movement.Launch(forward * (classId == 5 ? 14 : 10), player.isGrounded ? 1 : movement.VerticalVelocity);
                if (classId == 5) chargeShield.SetActive(true);
                PlayerFeedback.Play(ChasePlayerCue.ShieldDash);
                mobilityReadyAt = State.Elapsed + (classId == 5 ? 4 : 6);
            }
            Feedback(MobilityLabel); return true;
        }
        public void SelectChoice(int index)
        {
            if (!State.Choose(index)) return;
            PlayerFeedback?.Play(ChasePlayerCue.UpgradeSelect);
            SyncGuns(); AnimateGuns(0); Feedback("Arsenal improved"); ObservePhase();
        }
        void CaptureInput()
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; discardMouse = true; gameplayReleased = false; }
        void MenuInput() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; gameplayReleased = false; }
        void ObservePhase()
        {
            if (observedPhase == State.Phase) return;
            observedPhase = State.Phase;
            foreach (var view in enemies.Values) view.Motor.SetPaused(State.Phase != ChasePhase.Playing);
            if (State.Phase != ChasePhase.Playing) PlayerFeedback?.UpdateFootsteps(0, false, false);
            if (State.Phase == ChasePhase.Playing) CaptureInput(); else MenuInput();
            Debug.Log($"CHASE_PHASE phase={State.Phase} time={State.Elapsed:F2} hp={State.Health}");
        }

        void Update()
        {
            PlayerFeedback?.Advance(Time.unscaledDeltaTime);
            landingKick = Mathf.MoveTowards(landingKick, 0, Time.unscaledDeltaTime * .8f);
            if (ViewCamera != null) ViewCamera.transform.localPosition = Vector3.up * (1.6f - landingKick);
            if (chargeShield != null && chargeShield.activeSelf &&
                (State.Phase != ChasePhase.Playing || MobilityRemaining < 3.65)) chargeShield.SetActive(false);
            if (ScriptedControl) { Hud.ObserveState(State, true); return; }
            Hud.HandleInput(this);
            if (State.Phase != ChasePhase.Playing)
            { PlayerFeedback?.UpdateFootsteps(0, false, false); return; }
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetKey(KeyCode.Space)) gameplayReleased = true;
            MovePlayer(Time.deltaTime);
            Step(Time.deltaTime, gameplayReleased && Input.GetMouseButton(0),
                gameplayReleased && Input.GetMouseButton(1));
        }
        void LateUpdate()
        {
            if (Swarm == null) return;
            SyncLootViews();
            long measured = System.Diagnostics.Stopwatch.GetTimestamp();
            Swarm.Render(poses, PlayerPosition, (float)State.Elapsed);
            SwarmMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp() - measured) *
                1000.0 / System.Diagnostics.Stopwatch.Frequency;
            EnemyPresentationComplete?.Invoke(poses.Count);
        }

        void MovePlayer(float dt)
        {
            float x = Input.GetAxisRaw("Mouse X"), y = Input.GetAxisRaw("Mouse Y");
            if (discardMouse) { x = y = 0; discardMouse = false; }
            player.transform.Rotate(0, x * 1.8f, 0);
            pitch = Mathf.Clamp(pitch - y * 1.8f, -75, 75);
            ViewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector3 move = player.transform.right * Input.GetAxisRaw("Horizontal") +
                player.transform.forward * Input.GetAxisRaw("Vertical");
            MoveRunner(move, gameplayReleased && Input.GetKey(KeyCode.Space), dt);
        }

        public void MoveRunner(Vector3 direction, bool jump, float dt)
        {
            int beforeJumps = movement.JumpCount;
            Vector3 before = PlayerPosition;
            Vector3 displacement = movement.Step(direction, player.isGrounded, jump, dt,
                State.PlayerSpeedMultiplier, 1 + .3f * State.RelicPower(ChaseRelicKind.Windstep));
            CollisionFlags flags = player.Move(displacement);
            if (wasAirborne && player.isGrounded)
            {
                landingKick = leapLanding ? .12f : .025f;
                PlayerFeedback?.Play(leapLanding ? ChasePlayerCue.StrongLand : ChasePlayerCue.Land);
                leapLanding = false;
            }
            if ((flags & CollisionFlags.Sides) != 0) movement.ResolveSides(PlayerPosition - before);
            MovementAmount = Mathf.Clamp01(movement.HorizontalVelocity.magnitude / 6.8f);
            if (MovementAmount > .01f) ObservedMoves++;
            ObservedJumps += movement.JumpCount - beforeJumps;
            if (movement.JumpCount > beforeJumps) PlayerFeedback?.Play(ChasePlayerCue.Jump);
            Vector3 traveled = PlayerPosition - before;
            traveled.y = 0;
            PlayerFeedback?.UpdateFootsteps(traveled.magnitude,
                player.isGrounded && !wasAirborne && movement.JumpCount == beforeJumps,
                State.Phase == ChasePhase.Playing);
            wasAirborne = !player.isGrounded;
        }

        public void SyncGuns()
        {
            var retired = new List<int>();
            foreach (var entry in guns)
                if (!State.Weapons.Exists(w => w.CopyId == entry.Key)) retired.Add(entry.Key);
            foreach (int id in retired) { guns[id].Dispose(); guns.Remove(id); }
            for (int i = 0; i < State.Weapons.Count; i++)
            {
                var w = State.Weapons[i];
                if (!guns.ContainsKey(w.CopyId))
                    guns.Add(w.CopyId, ChaseGunRig.Create(ViewCamera.transform, palette, w.Kind, w.CopyId, i));
            }
        }
        public bool CollectNearestLoot()
        {
            var loot = NearestLoot;
            if (loot == null) return false;
            int selected = Mathf.Clamp(Hud.SelectedCopyIndex, 0, State.Weapons.Count - 1);
            int replace = State.Weapons.Count == 6 ? State.Weapons[selected].CopyId : -1;
            if (!State.CollectLoot(loot.Id, replace)) return false;
            Feedback(loot.Kind == ChaseLootKind.Weapon ? "Acquired " + ChaseCatalog.Name(loot.Weapon) :
                "Acquired " + ChaseRelics.Name(loot.Relic));
            PlayerFeedback?.Play(ChasePlayerCue.PickupXp);
            SyncGuns(); SyncLootViews();
            return true;
        }

        void SyncLootViews()
        {
            var retired = new List<int>();
            foreach (var entry in lootViews)
                if (!State.Loot.Exists(l => l.Id == entry.Key)) retired.Add(entry.Key);
            foreach (int id in retired)
            {
                lootViews[id].Root.SetActive(false);
                Destroy(lootViews[id].Root); lootViews.Remove(id);
            }
            foreach (var loot in State.Loot)
            {
                if (!lootViews.TryGetValue(loot.Id, out var view))
                {
                    if (lootArt == null) lootArt = new ChaseLootArt(transform, palette, glowMaterial);
                    view = new LootView { Root = lootArt.Create(loot, transform) };
                    lootViews.Add(loot.Id, view);
                }
                view.Root.transform.position = loot.Position;
            }
        }
        public void AnimateGuns(float dt)
        {
            SyncGuns();
            Vector3 point = AimPoint();
            foreach (var weapon in State.Weapons)
            {
                var gun = guns[weapon.CopyId];
                gun.Reload(weapon.ReloadUntil > State.Elapsed ?
                    Mathf.Clamp01(1 - (float)(weapon.ReloadUntil - State.Elapsed) / 1.6f) : -1);
                gun.Animate(dt, (float)State.Elapsed, MovementAmount, point);
            }
            arms.Animate((float)State.Elapsed, MovementAmount);
        }
        Vector3 AimPoint()
        {
            var ray = new Ray(ViewCamera.transform.position, ViewCamera.transform.forward);
            var shot = State.FindShot(ray.origin, ray.direction, 60);
            bool solid = Physics.Raycast(ray, out var hit, 60, Solids);
            return shot.HasValue && (!solid || shot.Value.Distance < hit.distance) ? shot.Value.Point :
                solid ? hit.point : ray.GetPoint(45);
        }
        public void AimAt(Vector3 point)
        {
            ViewCamera.transform.rotation = Quaternion.LookRotation(point - ViewCamera.transform.position);
            AnimateGuns(0); Physics.SyncTransforms();

        }
        public int AimedEnemy(Ray ray)
        {
            var shot = State.FindShot(ray.origin, ray.direction, 60);
            return shot.HasValue ? shot.Value.Enemy.Id : -1;
        }
        public Vector3? ProjectGround(Ray ray)
        {
            Vector3 point = Physics.Raycast(ray, out var hit, 40, Solids | (1 << 10)) ? hit.point : ray.GetPoint(20);
            if (hit.collider != null && hit.collider.gameObject.layer == 9 && hit.normal.y > .5f)
                return hit.point + Vector3.up * .08f;
            if (!Physics.Raycast(point + Vector3.up * .25f, Vector3.down, out var ground, 8, 1 << 9)) return null;
            if (Physics.CheckSphere(ground.point + Vector3.up * 0.5f, 0.45f, 1 << 8)) return null;
            return ground.point + Vector3.up * 0.08f;
        }
        public bool RequestCopy(int index, bool manual, Vector3? ground = null, int? aimed = null, bool alternate = false)
        {
            if (index < 0 || index >= State.Weapons.Count) return false;
            SyncGuns(); var gun = guns[State.Weapons[index].CopyId];
            var cam = ViewCamera.transform;
            return State.RequestAttack(index, gun.Muzzle.position, (AimPoint() - gun.Muzzle.position).normalized,
                aimed ?? AimedEnemy(new Ray(cam.position, cam.forward)), ground ?? ProjectGround(new Ray(cam.position, cam.forward)),
                manual, cam.position, cam.forward, alternate);
        }

        public void Step(float dt, bool manualHeld, bool alternateHeld = false)
        {
            if (State.Phase != ChasePhase.Playing) return;
            long measured = System.Diagnostics.Stopwatch.GetTimestamp();
            foreach (var view in enemies.Values)
            {
                if (!view.Model.Alive) continue;
                view.Model.Position = view.Root.transform.position + Vector3.up * view.Model.BodyOffset;
                view.Model.Facing = view.Root.transform.rotation;
                if (view.Model.BoundUntil <= State.Elapsed &&
                    State.Elapsed >= nextContactCheck &&
                    new Vector2(view.Root.transform.position.x - PlayerPosition.x, view.Root.transform.position.z - PlayerPosition.z).sqrMagnitude <
                        (view.Model.BodyRadius + .6f) * (view.Model.BodyRadius + .6f) &&
                    PlayerPosition.y < view.Root.transform.position.y + view.Model.BodyHeight &&
                    PlayerPosition.y + player.height > view.Root.transform.position.y + .1f &&
                    ClearPath(view.Model.Position, PlayerPosition + Vector3.up * 0.9f) && State.Contact())
                {
                    nextContactCheck = State.Elapsed + ChaseCalibration.DamageCooldownSeconds;
                    ContactsObserved++; ContactObserved?.Invoke(view.Model.Id);
                    PlayerFeedback?.Play(ChasePlayerCue.Damage);
                    Feedback("Keep moving - contact damage");
                }
            }
            ContactMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp() - measured) *
                1000.0 / System.Diagnostics.Stopwatch.Frequency;
            State.Advance(dt);
            AdvanceSubsteps += State.LastAdvanceSubsteps;
            int hazard = Arena.HazardDamageAt(PlayerPosition, State.Elapsed);
            int scaledHazard = Mathf.RoundToInt(hazard *
                (ChaseCalibration.ContactDamageAt(State.Elapsed, State.DamageTakenCount) / 12f));
            if (hazard > 0 && State.TakeHazardDamage(scaledHazard))
            { PlayerFeedback?.Play(ChasePlayerCue.Damage); Feedback("Flame jet - leave the burning grate"); }
            SimulationMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp() - measured) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            measured = System.Diagnostics.Stopwatch.GetTimestamp();
            AnimateGuns(dt); Physics.SyncTransforms();
            if (State.Phase == ChasePhase.Playing && (gameplayReleased || ScriptedControl))
                for (int i = 0; i < State.Weapons.Count; i++)
                {
                    bool alternate = alternateHeld && i == Hud.SelectedCopyIndex;
                    if (alternate || State.Automatic || manualHeld) RequestCopy(i, alternate || !State.Automatic, alternate: alternate);
                }
            ConsumeFeedback();
            EffectsMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp() - measured) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            measured = System.Diagnostics.Stopwatch.GetTimestamp();
            if (State.Phase == ChasePhase.Playing) AnnouncePortals();
            if (State.Phase == ChasePhase.Playing && SpawningEnabled)
            {
                spawnBudget = Math.Min(8, spawnBudget + dt * ChaseCalibration.SpawnRateAt(State.Elapsed));
                int due = Math.Min(8, (int)spawnBudget);
                for (int i = 0; i < due; i++) { TrySpawn(); spawnBudget -= 1; }
            }
            SpawnMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp() - measured) *
                1000.0 / System.Diagnostics.Stopwatch.Frequency;
            measured = System.Diagnostics.Stopwatch.GetTimestamp();
            poses.Clear();
            foreach (var view in enemies.Values)
            {
                if (!view.Model.Alive) continue;
                Vector3 desired = view.Model.Feet;
                if ((desired - view.Root.transform.position).sqrMagnitude > 0.000025f) view.Motor.AdjustPosition(desired);
                view.Motor.Speed = State.EffectiveSpeed(view.Model);
                view.Motor.SetPaused(State.Phase != ChasePhase.Playing || view.Model.StunUntil > State.Elapsed);
                Vector3 target = PlayerPosition;
                if (view.Model.BoundUntil > State.Elapsed)
                {
                    if (State.Elapsed >= view.BoundTargetReadyAt ||
                        !enemies.TryGetValue(view.BoundTargetId, out var cached) ||
                        !cached.Model.Alive || cached.Model.BoundUntil > State.Elapsed)
                    {
                        view.BoundTargetReadyAt = State.Elapsed + .35;
                        view.BoundTargetId = 0;
                        float nearest = float.MaxValue;
                        foreach (var foe in State.Enemies)
                        {
                            if (!foe.Alive || foe.BoundUntil > State.Elapsed || foe == view.Model) continue;
                            float distance = (foe.Position - view.Model.Position).sqrMagnitude;
                            if (distance < nearest) { nearest = distance; view.BoundTargetId = foe.Id; }
                        }
                    }
                    if (enemies.TryGetValue(view.BoundTargetId, out var selected)) target = selected.Model.Feet;
                }
                view.Motor.Tick(dt, target, State.Elapsed);
                poses.Add(new ChasePose { Id = view.Model.Id, Kind = view.Model.Kind, Position = view.Root.transform.position,
                    Rotation = view.Root.transform.rotation, Speed = view.Motor.Jumping ? view.Motor.Speed :
                    view.Motor.Agent.enabled ? view.Motor.Agent.velocity.magnitude : 0,
                    SizeScale = view.Model.SizeScale, FlashUntil = view.Model.FlashUntil, Bound = view.Model.BoundUntil > State.Elapsed });
            }
            NavigationMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp() - measured) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            measured = System.Diagnostics.Stopwatch.GetTimestamp();
            measured = System.Diagnostics.Stopwatch.GetTimestamp();
            CollectPickups();
            if (State.Phase == ChasePhase.Playing)
            {
                if (State.Elapsed >= nextHealth)
                { nextHealth = State.Elapsed + 90; AddPickup(PlayerPosition + player.transform.right * 7, true); }
                Arena.Animate(State.Elapsed, ActivePortals);
                HitFeedback.Fields(State.Fields, State.Elapsed); HitFeedback.Advance(dt);
            }
            SpawnMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp() - measured) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            ProfileFrames++;
            ObservePhase();
        }

        void AnnouncePortals()
        {
            for (int p = 0; p < ActivePortals; p++)
                if (PortalWarnedAt[p] < 0)
                { PortalWarnedAt[p] = State.Elapsed; Arena.Telegraph(p, State.Elapsed); Debug.Log($"CHASE_CUBE_WARNING portal={p} time={State.Elapsed:F2}"); }
        }
        bool TrySpawn()
        {
            if (EnemyCount >= ChaseCalibration.ActiveCapAt(State.Elapsed)) return false;
            if (!NavMesh.SamplePosition(PlayerPosition, out var destination, 4, NavMesh.AllAreas)) return false;
            int active = ActivePortals;
            for (int attempt = 0; attempt < active; attempt++)
            {
                int portal = nextPortal++ % active;
                if (PortalWarnedAt[portal] < 0 || State.Elapsed - PortalWarnedAt[portal] < 2) continue;
                var slots = Arena.SpawnSlots[portal];
                int first = random.Next(slots.Length);
                for (int j = 0; j < slots.Length; j++)
                {
                    Vector3 point = slots[(first + j) % slots.Length];
                    if (Vector3.Distance(point, PlayerPosition) < 22 ||
                        Physics.CheckSphere(point + Vector3.up, 0.5f, (1 << 8) | (1 << 10))) continue;
                    if (!NavMesh.SamplePosition(point, out var sample, 0.35f, NavMesh.AllAreas)) continue;
                    if (!NavMesh.CalculatePath(sample.position, destination.position, NavMesh.AllAreas, spawnPath) ||
                        spawnPath.status != NavMeshPathStatus.PathComplete) continue;
                    ChaseEnemyKind kind = State.Elapsed >= 240 && SpawnedTotal % 11 == 0 ? ChaseEnemyKind.Elite :
                        State.Elapsed >= 90 && SpawnedTotal % 3 == 0 ? ChaseEnemyKind.Demon : ChaseEnemyKind.Undead;
                    if (SpawnEnemy(sample.position, kind: kind, portal: portal) != null) return true;
                }
            }
            return false;
        }
        public ChaseEnemy SpawnEnemy(Vector3 feet, float diagnosticHealth = -1,
            ChaseEnemyKind kind = ChaseEnemyKind.Undead, int portal = -1, bool bypassCap = false)
        {
            if ((!bypassCap && enemies.Count >= ChaseCalibration.ActiveCapAt(State.Elapsed)) ||
                !NavMesh.SamplePosition(feet, out var sample, 2, NavMesh.AllAreas)) return null;
            var model = State.AddEnemy(sample.position, diagnosticHealth, kind);
            model.HeadOffset = ChaseEnemyRig.LocalHeadCenter(kind);
            model.LocalHeadRadius = ChaseEnemyRig.LocalHeadRadius(kind);
            model.Position = sample.position + Vector3.up * model.BodyOffset;
            var root = enemyRoots.Count > 0 ? enemyRoots.Pop() :
                new GameObject("G pooled pursuit body", typeof(CapsuleCollider));
            root.name = kind + " pursuer " + model.Id;
            root.transform.SetParent(transform, false); root.transform.position = sample.position;
            root.layer = 10; root.SetActive(true);
            var collider = root.GetComponent<CapsuleCollider>();
            collider.height = model.BodyHeight; collider.radius = model.BodyRadius; collider.center = Vector3.up * model.BodyOffset;
            var motor = new EnemyMotor(root.transform, model.Speed, model.Id, model.SizeScale);
            motor.SetPaused(State.Phase != ChasePhase.Playing);
            enemies.Add(model.Id, new EnemyView { Model = model, Root = root, Motor = motor });
            SpawnedTotal++;
            if (portal >= 0)
            {
                var record = new ChaseSpawnRecord { EnemyId = model.Id, Portal = portal, Position = sample.position,
                    Time = State.Elapsed, WarnedAt = PortalWarnedAt[portal], PlayerDistance = Vector3.Distance(sample.position, PlayerPosition) };
                if (SpawnRecords.Count >= 4096) SpawnRecords.RemoveRange(0, 1024);
                SpawnRecords.Add(record); SpawnObserved?.Invoke(record);
            }
            return model;
        }
        public Vector3 EnemyFeet(int id) => enemies[id].Root.transform.position;
        public bool EnemyIsJumping(int id) => enemies[id].Motor.Jumping;
        public string JumpDiagnostic(int id)
        {
            var view = enemies[id]; var agent = view.Motor.Agent;
            string navigation = agent.enabled && agent.isOnNavMesh ?
                $"path={agent.pathStatus} remaining={agent.remainingDistance:F2} velocity={agent.velocity} destination={agent.destination}" : "off-navmesh";
            return $"started={view.Motor.JumpsStarted} completed={view.Motor.JumpsCompleted} rejected={view.Motor.LastJumpRejection} " +
                $"feet={view.Root.transform.position} player={PlayerPosition} elapsed={State.Elapsed:F2} speed={view.Model.Speed:F2} {navigation}";
        }
        public int CompletedJumps { get { int n = 0; foreach (var view in enemies.Values) n += view.Motor.JumpsCompleted; return n; } }
        bool ClearPath(Vector3 from, Vector3 to)
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            bool clear = visibility.Clear(from, to);
            ClearPathQueries++;
            ClearPathMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp() - start) *
                1000.0 / System.Diagnostics.Stopwatch.Frequency;
            return clear;
        }
        bool ClearTravel(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            return !Physics.CheckSphere(from + Vector3.up * 0.4f, 0.4f, 1 << 8) &&
                (delta.sqrMagnitude < 0.000001f || !Physics.SphereCast(from + Vector3.up * 0.4f, 0.4f,
                delta.normalized, out _, delta.magnitude, 1 << 8, QueryTriggerInteraction.Ignore));
        }
        public void ConsumeFeedback()
        {
            SyncGuns();
            foreach (var attack in State.Attacks)
            { guns[attack.CopyId].Fire(); HitFeedback.Attack(attack); }
            State.Attacks.Clear();
            foreach (var hit in State.Hits)
            {
                // Feedback must survive to presentation even when a long simulation
                // step produced the hit earlier inside that step.
                if (hit.Enemy.Alive)
                    hit.Enemy.FlashUntil = Math.Max(hit.Enemy.FlashUntil, State.Elapsed + .14);
                HitFeedback.Hit(hit);
                if (hit.Critical) Feedback("HEADSHOT  x2  /  " + hit.Damage.ToString("0.#") + " damage");
            }
            State.Hits.Clear();
            foreach (var dead in State.Deaths)
            {
                if (enemies.TryGetValue(dead.Id, out var view))
                {
                    Swarm.Remove(dead.Id); view.Motor.Retire();
                    view.Root.SetActive(false); enemyRoots.Push(view.Root); enemies.Remove(dead.Id);
                }
                State.Enemies.Remove(dead);
                AddPickup(dead.Feet + Vector3.up * .3f, false, false);
                if (random.NextDouble() < 0.025) AddPickup(dead.Feet + new Vector3(.4f, .3f, 0), true, false);
            }
            State.Deaths.Clear();
            HitFeedback.FlushPendingHits();
        }
        public void AddPickup(Vector3 position, bool health, bool projectToFloor = true)
        {
            if (projectToFloor)
            {
                if (!Physics.Raycast(position + Vector3.up * .15f, Vector3.down, out var floor, 2, 1 << 9)) return;
                position.y = floor.point.y + 0.3f;
            }
            int type = health ? 1 : 0;
            if (pickupTemplates[type] == null)
            {
                var template = new GameObject(health ? "Crimson recovery relic" : "Soul residue");
                template.transform.SetParent(transform, false);
                ArtParts.Sphere("Relic core", template.transform, Vector3.zero, Vector3.one * .22f,
                    health ? palette.Amber : palette.Teal);
                ArtParts.Ring("Relic halo", template.transform, Vector3.zero, .2f, .02f,
                    health ? palette.Amber : palette.Teal);
                template.SetActive(false); pickupTemplates[type] = template;
            }
            var root = pickupRoots[type].Count > 0 ? pickupRoots[type].Pop() :
                Instantiate(pickupTemplates[type], transform);
            root.transform.position = position; root.SetActive(true);
            pickups.Add(new Pickup { Visual = root, Position = position, Health = health });
        }
        void ReturnPickup(Pickup pickup)
        {
            pickup.Visual.SetActive(false);
            pickupRoots[pickup.Health ? 1 : 0].Push(pickup.Visual);
        }
        void CollectPickups()
        {
            for (int i = pickups.Count - 1; i >= 0 && State.Phase == ChasePhase.Playing; i--)
            {
                var pickup = pickups[i];
                if (Vector3.Distance(PlayerPosition + Vector3.up * 0.3f, pickup.Position) > 2) continue;
                if (pickup.Health)
                { if (!State.Heal(8)) continue; Feedback("+8 health"); PlayerFeedback?.Play(ChasePlayerCue.PickupHealth); }
                else
                { State.CollectXp(4); Feedback("+4 soul residue"); PlayerFeedback?.Play(ChasePlayerCue.PickupXp); }
                ReturnPickup(pickup); pickups.RemoveAt(i);
            }
        }
        void Feedback(string value) { feedback = value; feedbackUntil = State.Elapsed + 1.4; }
        void OnGUI() { Hud.Draw(this); }
        void OnApplicationFocus(bool focused)
        { if (!focused && !ScriptedControl && State.Phase == ChasePhase.Playing) PauseRun(); }
        void OnDestroy()
        {
            if (navInstance.valid) navInstance.Remove();
            if (navData != null) Destroy(navData);
            ClearGuns(); Arena?.Dispose(); Swarm?.Dispose(); arms?.Dispose();
            HitFeedback?.Dispose(); PlayerFeedback?.Dispose(); palette?.Dispose();
            if (glowMaterial != null) Destroy(glowMaterial);
            if (glowTexture != null) Destroy(glowTexture);
            ChaseScenery.Dispose();
            foreach (var view in lootViews.Values) Destroy(view.Root);
            lootArt?.Dispose();
            ArtParts.DisposeMeshes(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            ChaseMaterials.Dispose();
        }
    }
}
