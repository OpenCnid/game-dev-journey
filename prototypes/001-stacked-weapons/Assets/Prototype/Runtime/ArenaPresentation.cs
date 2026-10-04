using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Profiling;

namespace Journey
{
    public sealed class ArenaPresentation
    {
        readonly Matrix4x4[] bodies = new Matrix4x4[CombatSimulation.Capacity];
        readonly Matrix4x4[] heads = new Matrix4x4[CombatSimulation.Capacity];
        readonly Matrix4x4[] projectiles = new Matrix4x4[BossAbilities.ProjectileCapacity];
        readonly Matrix4x4[] telegraphs = new Matrix4x4[CombatSimulation.Capacity];
        readonly Matrix4x4[] hitSparks = new Matrix4x4[CombatFeedback.ImpactCapacity * 8];
        readonly Matrix4x4[] killSparks = new Matrix4x4[CombatFeedback.ImpactCapacity * 8];
        readonly byte[] woundMask = new byte[CombatSimulation.Capacity];
        Material hitMaterial, killMaterial;
        public readonly CombatFeedback Feedback = new CombatFeedback();
        Mesh cube, ring, octahedron, character;
        Texture2D characterAtlas;
        Material characterMaterial;
        CompactProjectiles compactProjectiles;
        public string ProjectileRenderer { get; set; } = "cube";
        public bool CombinedCharacters { get; set; }
        public bool CacheWarnings { get; set; }
        bool IndirectProjectiles => ProjectileRenderer.StartsWith("indirect-");
        Mesh ProjectileMesh => ProjectileRenderer.EndsWith("octa") ? octahedron : cube;
        Material enemyMaterial, headMaterial, projectileMaterial, telegraphMaterial;
        LineRenderer tracer;
        Transform weapon, muzzlePulse;
        Material muzzleMaterial;
        AudioSource audio;
        AudioClip shot;
        float flash, recoil;
        readonly Plane[] frustum = new Plane[6];
        readonly WarningCollector warnings = new WarningCollector();
        static readonly ProfilerMarker PrepareMarker = new ProfilerMarker("Journey.RenderPrepare");
        static readonly ProfilerMarker SubmitMarker = new ProfilerMarker("Journey.RenderSubmit");
        BurstInstances instances, projectileInstances, warningInstances;
        public bool HideWarnings { get; set; }
        public bool HideProjectiles { get; set; }
        public bool DeduplicateWarnings { get; set; }
        public bool OverlapPreparation { get; set; }
        public bool DiagnosticOnly => HideWarnings || HideProjectiles;
        public bool CullInstances { get; set; } = true;
        public int BatchSize { get; set; } = 128;
        public Camera Camera { get; private set; }

        public void Create(Transform root)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.36f, .43f, .53f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.025f, .04f, .065f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 25; RenderSettings.fogEndDistance = 72;
            Material floor = Material("Floor", new Color(.065f, .085f, .12f));
            Material wall = Material("Boundary", new Color(.12f, .18f, .23f));
            Material grid = Material("Grid", new Color(.12f, .28f, .33f));
            Material accent = Material("Accent", new Color(.1f, .8f, .85f));
            Cube("Floor", new Vector3(0, -.3f, 0), new Vector3(62, .5f, 62), floor, root);
            for (int i = -30; i <= 30; i += 5)
            {
                Cube("Grid X", new Vector3(i, -.035f, 0), new Vector3(.035f, .02f, 60), grid, root);
                Cube("Grid Z", new Vector3(0, -.035f, i), new Vector3(60, .02f, .035f), grid, root);
            }
            for (int i = 0; i < 4; i++)
            {
                bool alongX = i < 2;
                float side = (i % 2 == 0 ? -1 : 1) * 30.5f;
                var p = alongX ? new Vector3(side, 1, 0) : new Vector3(0, 1, side);
                var scale = alongX ? new Vector3(1, 2, 62) : new Vector3(62, 2, 1);
                Cube("Boundary", p, scale, wall, root);
                Cube("Boundary stripe", p + Vector3.up, new Vector3(scale.x, .08f, scale.z), accent, root);
            }
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                Cube("Beacon", new Vector3(x * 30, 4, z * 30), new Vector3(.3f, 8, .3f), accent, root);

            var light = new GameObject("Key light").AddComponent<Light>();
            light.transform.SetParent(root); light.transform.rotation = Quaternion.Euler(48, -28, 0);
            light.type = LightType.Directional; light.intensity = 1.4f; light.shadows = LightShadows.None;
            var cameraObject = new GameObject("Player camera"); cameraObject.transform.SetParent(root);
            cameraObject.tag = "MainCamera";
            Camera = cameraObject.AddComponent<Camera>(); Camera.fieldOfView = 85;
            Camera.nearClipPlane = .05f; Camera.farClipPlane = 100;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = RenderSettings.fogColor;
            cameraObject.AddComponent<AudioListener>();
            weapon = new GameObject("Pulse rifle").transform; weapon.SetParent(Camera.transform, false);
            weapon.localPosition = new Vector3(.28f, -.24f, .55f);
            Material gun = Material("Rifle", new Color(.13f, .19f, .24f));
            Cube("Rifle body", Vector3.zero, new Vector3(.19f, .17f, .52f), gun, weapon, true);
            Cube("Rifle barrel", new Vector3(0, .035f, .36f), new Vector3(.075f, .075f, .28f), accent, weapon, true);
            Cube("Rifle sight", new Vector3(0, .12f, .02f), new Vector3(.04f, .045f, .09f), accent, weapon, true);
            muzzleMaterial = Material("Muzzle pulse", Color.white, true);
            var muzzle = new GameObject("Muzzle pulse"); muzzlePulse = muzzle.transform;
            muzzlePulse.SetParent(weapon, false); muzzlePulse.localPosition = new Vector3(0, .035f, .55f);
            muzzlePulse.localScale = new Vector3(.15f, .15f, .08f);
            // Mesh assigned after the primitive mesh is acquired below; no collider or enemy cue.
            muzzle.AddComponent<MeshRenderer>().sharedMaterial = muzzleMaterial;
            muzzle.SetActive(false);

            var sample = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = sample.GetComponent<MeshFilter>().sharedMesh;
            muzzle.AddComponent<MeshFilter>().sharedMesh = cube;
            Object.Destroy(sample);
            enemyMaterial = Material("Drone body", new Color(.94f, .23f, .19f));
            headMaterial = Material("Drone core", new Color(1, .72f, .27f));
            enemyMaterial.enableInstancing = headMaterial.enableInstancing = true;
            hitMaterial = Material("Damage sparks", Color.white, true);
            killMaterial = Material("Death sparks", new Color(1, .72f, .12f), true);
            hitMaterial.enableInstancing = killMaterial.enableInstancing = true;
            projectileMaterial = Material("Boss projectiles", new Color(1, .15f, .75f), true);
            telegraphMaterial = Material("Area warning", new Color(1, .4f, .06f), true);
            projectileMaterial.enableInstancing = telegraphMaterial.enableInstancing = true;
            ring = CreateRing();
            octahedron = ExperimentMeshes.Octahedron(); character = ExperimentMeshes.Character(cube);
            characterAtlas = new Texture2D(2, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            characterAtlas.SetPixels(new[] { new Color(.94f, .23f, .19f), new Color(1, .72f, .27f) }); characterAtlas.Apply();
            characterMaterial = Material("Combined drone", Color.white); characterMaterial.enableInstancing = true;
            characterMaterial.SetTexture("_BaseMap", characterAtlas);
            tracer = new GameObject("Shot trail").AddComponent<LineRenderer>();
            tracer.transform.SetParent(root); tracer.positionCount = 2;
            tracer.startWidth = .018f; tracer.endWidth = .004f;
            tracer.sharedMaterial = Material("Tracer", new Color(.38f, 1, 1), true);
            tracer.enabled = false;
            audio = cameraObject.AddComponent<AudioSource>(); audio.spatialBlend = 0; audio.volume = .09f;
            shot = AudioClip.Create("Synth pulse", 2205, 1, 44100, false);
            var samples = new float[2205];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / 44100f;
                samples[i] = Mathf.Sin(2 * Mathf.PI * (700 * t - 1800 * t * t)) * Mathf.Exp(-t * 90);
            }
            shot.SetData(samples, 0);
        }

        // Combat calls this after BeginFrame, with the resolved camera-ray endpoint.
        // Benchmark calls it before BeginFrame to retain its original presentation workload.
        public void ShowShot(Vector3 end, bool benchmark = false)
        {
            flash = .045f; recoil = .065f;
            if (!benchmark) weapon.localPosition = new Vector3(.28f, -.24f, .55f - recoil);
            tracer.SetPosition(0, benchmark ? weapon.TransformPoint(new Vector3(0, .035f, .5f)) : muzzlePulse.position);
            tracer.SetPosition(1, end); tracer.enabled = true;
            if (!benchmark) muzzlePulse.gameObject.SetActive(true);
            audio.PlayOneShot(shot);
        }
        public void BeginFrame(CombatSimulation sim, float alpha, float yaw, float pitch, float dt)
        {
            Camera.transform.SetPositionAndRotation(sim.RenderPlayer(alpha) + Vector3.up * 1.65f, Quaternion.Euler(pitch, yaw, 0));
            if (sim.State != RunState.Paused) flash -= dt;
            // An immediate combat tracer exists for its emission frame only. Do not keep
            // projecting an old world segment through new camera poses during a swivel.
            tracer.enabled = sim.Benchmark && flash > 0;
            muzzlePulse.gameObject.SetActive(!sim.Benchmark && flash > 0);
            if (sim.State != RunState.Paused || sim.Benchmark) recoil = Mathf.MoveTowards(recoil, 0, dt * .55f);
            weapon.localPosition = new Vector3(.28f, -.24f, .55f - recoil);
            Feedback.Advance(sim, dt);
        }
        public void Draw(CombatSimulation sim, float alpha, float yaw, float pitch, float dt)
        {
            BeginFrame(sim, alpha, yaw, pitch, dt);
            Draw(sim, alpha);
        }
        public void Draw(CombatSimulation sim, float alpha)
        {
            Feedback.CollectBars(sim, Camera, alpha);
            if (!sim.Benchmark)
            {
                Feedback.BuildSparks(sim, alpha, hitSparks, killSparks, out int hits, out int kills);
                DrawInstances(hitMaterial, cube, hitSparks, hits);
                DrawInstances(killMaterial, cube, killSparks, kills);
            }
            if (CullInstances) { DrawCulled(sim, alpha); return; }
            long stageStart = StageProfile.Begin();
            using (PrepareMarker.Auto())
            {
                for (int i = 0; i < sim.Count; i++)
                {
                    Vector3 p = sim.RenderPosition(i, alpha);
                    bool wounded = !sim.Benchmark && sim.EnemyHealth(i) < CombatSimulation.EnemyMaxHealth;
                    bodies[i] = BurstInstances.CharacterMatrix(p, Vector3.up * .8f, new Vector3(.85f, 1.4f, .85f), wounded);
                    heads[i] = BurstInstances.CharacterMatrix(p, Vector3.up * 1.6f, Vector3.one * .45f, wounded, core: true);
                }
            }
            StageProfile.RenderPrepareMs += StageProfile.End(stageStart);
            if (sim.Count == 0) return;
            var bodyParams = new RenderParams(enemyMaterial) { shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false, worldBounds = new Bounds(Vector3.zero, new Vector3(65, 8, 65)) };
            var headParams = new RenderParams(headMaterial) { shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false, worldBounds = bodyParams.worldBounds };
            // Conservative batches also fit platforms with a smaller constant-buffer instance limit.
            stageStart = StageProfile.Begin();
            using (SubmitMarker.Auto())
            {
                for (int offset = 0; offset < sim.Count; offset += 250)
                {
                    int count = Mathf.Min(250, sim.Count - offset);
                    Graphics.RenderMeshInstanced(bodyParams, cube, 0, bodies, count, offset);
                    Graphics.RenderMeshInstanced(headParams, cube, 0, heads, count, offset);
                    StageProfile.InstanceBatches += 2; StageProfile.SubmittedInstances += count * 2;
                }
            }
            StageProfile.RenderSubmitMs += StageProfile.End(stageStart);
            int warnings = 0;
            if (sim.Abilities.Enabled)
            {
                stageStart = StageProfile.Begin();
                using (PrepareMarker.Auto())
                {
                    for (int i = 0; i < sim.Count; i++)
                        if (sim.Abilities.IsCasting(i)) telegraphs[warnings++] = ScaledTranslation(
                            sim.Abilities.Target(i) + Vector3.up * .02f, Vector3.one * BossAbilities.AreaRadius);
                    for (int i = 0; i < sim.Abilities.Projectiles; i++)
                        projectiles[i] = ScaledTranslation(sim.Abilities.RenderProjectile(i, alpha) + Vector3.up,
                            Vector3.one * .18f);
                }
                StageProfile.RenderPrepareMs += StageProfile.End(stageStart);
                DrawInstances(projectileMaterial, cube, projectiles, sim.Abilities.Projectiles);
                DrawInstances(telegraphMaterial, ring, telegraphs, warnings);
            }
        }
        void DrawCulled(CombatSimulation sim, float alpha)
        {
            if (instances == null) instances = new BurstInstances();
            bool showProjectiles = sim.Abilities.Enabled && !(sim.Benchmark && HideProjectiles);
            bool showWarnings = sim.Abilities.Enabled && !(sim.Benchmark && HideWarnings);
            long begin = StageProfile.Begin();
            using (PrepareMarker.Auto())
            {
                GeometryUtility.CalculateFrustumPlanes(Camera, frustum);
                ScheduleEnemies(instances, sim, alpha);
            }
            StageProfile.RenderPrepareMs += StageProfile.End(begin);
            if (OverlapPreparation)
            {
                if (projectileInstances == null) projectileInstances = new BurstInstances();
                if (warningInstances == null) warningInstances = new BurstInstances();
                begin = StageProfile.Begin();
                using (PrepareMarker.Auto())
                {
                    if (showProjectiles && !IndirectProjectiles) ScheduleProjectiles(projectileInstances, sim, alpha);
                    if (showWarnings) ScheduleWarnings(warningInstances, sim);
                }
                StageProfile.RenderPrepareMs += StageProfile.End(begin);
            }
            int visible = FinishPreparation(instances);
            if (CombinedCharacters) DrawNative(characterMaterial, character, instances.A, visible);
            else { DrawNative(enemyMaterial, cube, instances.A, visible); DrawNative(headMaterial, cube, instances.B, visible); }
            if (showProjectiles && IndirectProjectiles)
            {
                if (!sim.NativeState) throw new System.InvalidOperationException("Indirect projectiles require native state.");
                if (compactProjectiles == null) compactProjectiles = new CompactProjectiles();
                begin = StageProfile.Begin();
                int total;
                using (PrepareMarker.Auto()) total = compactProjectiles.Prepare(sim.Abilities.NativePositions, sim.Abilities.NativePrevious,
                    sim.Abilities.Projectiles, alpha, frustum, BatchSize);
                StageProfile.RenderPrepareMs += StageProfile.End(begin);
                begin = StageProfile.Begin();
                using (SubmitMarker.Auto()) compactProjectiles.Draw(ProjectileMesh, total);
                StageProfile.RenderSubmitMs += StageProfile.End(begin);
            }
            if (showProjectiles && !IndirectProjectiles)
            {
                var buffer = OverlapPreparation ? projectileInstances : instances;
                if (!OverlapPreparation)
                {
                    begin = StageProfile.Begin();
                    using (PrepareMarker.Auto()) ScheduleProjectiles(buffer, sim, alpha);
                    StageProfile.RenderPrepareMs += StageProfile.End(begin);
                }
                DrawNative(projectileMaterial, ProjectileMesh, buffer.A, FinishPreparation(buffer));
            }
            if (showWarnings)
            {
                var buffer = OverlapPreparation ? warningInstances : instances;
                if (!OverlapPreparation)
                {
                    begin = StageProfile.Begin();
                    using (PrepareMarker.Auto()) ScheduleWarnings(buffer, sim);
                    StageProfile.RenderPrepareMs += StageProfile.End(begin);
                }
                DrawNative(telegraphMaterial, ring, buffer.A, FinishPreparation(buffer));
            }
        }
        void ScheduleEnemies(BurstInstances buffer, CombatSimulation sim, float alpha)
        {
            byte[] mask = null;
            if (!sim.Benchmark)
            {
                mask = woundMask;
                for (int i = 0; i < sim.Count; i++) mask[i] = (byte)(sim.EnemyHealth(i) < CombatSimulation.EnemyMaxHealth ? 1 : 0);
            }
            if (sim.NativeState)
                buffer.ScheduleNative(sim.NativePositions, sim.NativePrevious, sim.Count, alpha, frustum,
                    CombinedCharacters ? Vector3.zero : Vector3.up * .8f, CombinedCharacters ? Vector3.one : new Vector3(.85f, 1.4f, .85f), Vector3.up * 1.6f, Vector3.one * .45f,
                    Vector3.up, 1.2f, !CombinedCharacters, BatchSize, mask);
            else buffer.Schedule(sim.Positions, sim.PreviousPositions, sim.Count, alpha, frustum,
                    CombinedCharacters ? Vector3.zero : Vector3.up * .8f, CombinedCharacters ? Vector3.one : new Vector3(.85f, 1.4f, .85f), Vector3.up * 1.6f, Vector3.one * .45f,
                    Vector3.up, 1.2f, !CombinedCharacters, BatchSize, mask);
        }
        void ScheduleProjectiles(BurstInstances buffer, CombatSimulation sim, float alpha)
        {
            if (sim.NativeState)
                buffer.ScheduleNative(sim.Abilities.NativePositions, sim.Abilities.NativePrevious,
                    sim.Abilities.Projectiles, alpha, frustum, Vector3.up, Vector3.one * .18f,
                    Vector3.zero, Vector3.zero, Vector3.up, .17f, false, BatchSize);
            else buffer.Schedule(sim.Abilities.Positions, sim.Abilities.PreviousPositions,
                    sim.Abilities.Projectiles, alpha, frustum, Vector3.up, Vector3.one * .18f,
                    Vector3.zero, Vector3.zero, Vector3.up, .17f, false, BatchSize);
        }
        void ScheduleWarnings(BurstInstances buffer, CombatSimulation sim)
        {
            warnings.Collect(sim, DeduplicateWarnings, CacheWarnings);
            StageProfile.WarningCandidates = warnings.Candidates; StageProfile.UniqueWarnings = warnings.Count;
            buffer.Schedule(warnings.Positions, warnings.Positions, warnings.Count, 1, frustum,
                Vector3.up * .02f, Vector3.one * BossAbilities.AreaRadius, Vector3.zero, Vector3.zero,
                Vector3.up * .02f, BossAbilities.AreaRadius, false, BatchSize);
        }
        static int FinishPreparation(BurstInstances buffer)
        {
            long begin = StageProfile.Begin();
            int count;
            using (PrepareMarker.Auto()) count = buffer.Complete();
            StageProfile.RenderPrepareMs += StageProfile.End(begin);
            return count;
        }
        static void DrawNative(Material material, Mesh mesh, NativeArray<Matrix4x4> matrices, int total)
        {
            long begin = StageProfile.Begin();
            using (SubmitMarker.Auto())
            {
                var parameters = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false, worldBounds = new Bounds(Vector3.zero, new Vector3(120, 8, 120)) };
                for (int offset = 0; offset < total; offset += 500)
                {
                    int count = Mathf.Min(500, total - offset);
                    Graphics.RenderMeshInstanced(parameters, mesh, 0, matrices, count, offset);
                    StageProfile.InstanceBatches++; StageProfile.SubmittedInstances += count;
                }
            }
            StageProfile.RenderSubmitMs += StageProfile.End(begin);
        }
        static Matrix4x4 ScaledTranslation(Vector3 p, Vector3 scale)
        {
            var m = Matrix4x4.identity;
            m.m00 = scale.x; m.m11 = scale.y; m.m22 = scale.z;
            m.m03 = p.x; m.m13 = p.y; m.m23 = p.z; return m;
        }
        static void DrawInstances(Material material, Mesh mesh, Matrix4x4[] matrices, int total)
        {
            long begin = StageProfile.Begin();
            using (SubmitMarker.Auto())
            {
                var parameters = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false, worldBounds = new Bounds(Vector3.zero, new Vector3(120, 8, 120)) };
                for (int offset = 0; offset < total; offset += 250)
                {
                    Graphics.RenderMeshInstanced(parameters, mesh, 0, matrices, Mathf.Min(250, total - offset), offset);
                    StageProfile.InstanceBatches++; StageProfile.SubmittedInstances += Mathf.Min(250, total - offset);
                }
            }
            StageProfile.RenderSubmitMs += StageProfile.End(begin);
        }
        static Mesh CreateRing()
        {
            const int segments = 32;
            var vertices = new Vector3[segments * 2]; var indices = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                var v = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices[i * 2] = v; vertices[i * 2 + 1] = v * .94f;
                int a = i * 2, b = (i * 2 + 2) % vertices.Length, t = i * 6;
                indices[t] = a; indices[t + 1] = a + 1; indices[t + 2] = b;
                indices[t + 3] = a + 1; indices[t + 4] = b + 1; indices[t + 5] = b;
            }
            var mesh = new Mesh { name = "Area warning ring", vertices = vertices, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        public void ResetFeedback() { Feedback.Reset(); flash = recoil = 0; tracer.enabled = false; muzzlePulse.gameObject.SetActive(false); audio.Stop(); }
        public void Dispose()
        {
            Feedback.Reset();
            if (hitMaterial != null) Object.Destroy(hitMaterial);
            if (killMaterial != null) Object.Destroy(killMaterial);
            if (muzzleMaterial != null) Object.Destroy(muzzleMaterial);
            compactProjectiles?.Dispose(); compactProjectiles = null;
            if (octahedron != null) Object.Destroy(octahedron); if (character != null) Object.Destroy(character);
            if (characterMaterial != null) Object.Destroy(characterMaterial); if (characterAtlas != null) Object.Destroy(characterAtlas);
            instances?.Dispose(); instances = null; projectileInstances?.Dispose(); projectileInstances = null;
            warningInstances?.Dispose(); warningInstances = null;
            if (shot != null) Object.Destroy(shot); if (ring != null) Object.Destroy(ring);
        }

        static Material Material(string name, Color color, bool unlit = false)
        {
            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Simple Lit");
            if (shader == null) throw new System.InvalidOperationException("URP shaders missing. Run Journey > Prepare project before building.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color); return material;
        }
        static void Cube(string name, Vector3 p, Vector3 scale, Material material, Transform parent, bool local = false)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.SetParent(parent, false);
            if (local) obj.transform.localPosition = p; else obj.transform.position = p;
            obj.transform.localScale = scale;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.Destroy(obj.GetComponent<Collider>());
        }
    }
}
