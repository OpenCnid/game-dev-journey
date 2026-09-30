using UnityEngine;
using UnityEngine.Rendering;

namespace Journey
{
    public sealed class ArenaPresentation
    {
        readonly Matrix4x4[] bodies = new Matrix4x4[CombatSimulation.Capacity];
        readonly Matrix4x4[] heads = new Matrix4x4[CombatSimulation.Capacity];
        Mesh cube;
        Material enemyMaterial, headMaterial;
        LineRenderer tracer;
        Transform weapon;
        AudioSource audio;
        AudioClip shot;
        float flash, recoil;
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

            var sample = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = sample.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(sample);
            enemyMaterial = Material("Drone body", new Color(.94f, .23f, .19f));
            headMaterial = Material("Drone core", new Color(1, .72f, .27f));
            enemyMaterial.enableInstancing = headMaterial.enableInstancing = true;
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

        public void ShowShot(Vector3 end)
        {
            tracer.SetPosition(0, weapon.TransformPoint(new Vector3(0, .035f, .5f)));
            tracer.SetPosition(1, end); flash = .045f; recoil = .065f;
            audio.PlayOneShot(shot);
        }
        public void Draw(CombatSimulation sim, float alpha, float yaw, float pitch, float dt)
        {
            Camera.transform.SetPositionAndRotation(sim.RenderPlayer(alpha) + Vector3.up * 1.65f, Quaternion.Euler(pitch, yaw, 0));
            flash -= dt; tracer.enabled = flash > 0;
            recoil = Mathf.MoveTowards(recoil, 0, dt * .55f);
            weapon.localPosition = new Vector3(.28f, -.24f, .55f - recoil);
            for (int i = 0; i < sim.Count; i++)
            {
                Vector3 p = sim.RenderPosition(i, alpha);
                bodies[i] = Matrix4x4.TRS(p + Vector3.up * .8f, Quaternion.identity, new Vector3(.85f, 1.4f, .85f));
                heads[i] = Matrix4x4.TRS(p + Vector3.up * 1.6f, Quaternion.identity, Vector3.one * .45f);
            }
            if (sim.Count == 0) return;
            var bodyParams = new RenderParams(enemyMaterial) { shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false, worldBounds = new Bounds(Vector3.zero, new Vector3(65, 8, 65)) };
            var headParams = new RenderParams(headMaterial) { shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false, worldBounds = bodyParams.worldBounds };
            // Conservative batches also fit platforms with a smaller constant-buffer instance limit.
            for (int offset = 0; offset < sim.Count; offset += 250)
            {
                int count = Mathf.Min(250, sim.Count - offset);
                Graphics.RenderMeshInstanced(bodyParams, cube, 0, bodies, count, offset);
                Graphics.RenderMeshInstanced(headParams, cube, 0, heads, count, offset);
            }
        }
        public void ResetFeedback() { flash = recoil = 0; tracer.enabled = false; audio.Stop(); }
        public void Dispose() { if (shot != null) Object.Destroy(shot); }

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
