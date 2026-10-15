using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.CrucibleReborn
{
    // Presentation only. Attack geometry is cosmetic; only submitted simulation Hits
    // create damage flashes and impact reports, including fatal hits and field ticks.
    public sealed class ChaseEffects : IDisposable
    {
        public const int SampleRate = 48000;
        public const int AttackCapacity = 48;
        public const int HitCapacity = 64;
        public const int FieldCapacity = 48;
        public const int AttackVoiceCapacity = 4;
        public const int HitVoiceCapacity = 16;
        const int PointCapacity = 32;
        const int WeaponCount = 16;
        readonly GameObject root;
        readonly Camera camera;
        readonly Material material;
        readonly Mesh crystal, hitCube;
        readonly Visual[] attacks = new Visual[AttackCapacity];
        readonly Visual[] hits = new Visual[HitCapacity];
        readonly FieldVisual[] fields = new FieldVisual[FieldCapacity];
        readonly Voice[] attackVoices = new Voice[AttackVoiceCapacity];
        readonly Voice[] hitVoices = new Voice[HitVoiceCapacity];
        readonly PendingHit[] pending = new PendingHit[HitVoiceCapacity];
        readonly AudioClip[] reports = new AudioClip[16];
        readonly AudioClip[] impacts = new AudioClip[3 * WeaponCount * 2];
        readonly int[] reportCounts = new int[16];
        readonly int[] impactCounts = new int[3 * WeaponCount * 2];
        int pendingCount, nextAttack, quality = 1;
        bool disposed;
        float clock;

        sealed class Visual
        {
            // Three essential strokes plus two ribbons, a shockfront and three
            // ballistic fragments. All renderers are created once, never per shot.
            public readonly LineRenderer[] Lines = new LineRenderer[9];
            public Transform Core;
            public Renderer CoreRenderer;
            public MaterialPropertyBlock Tint = new MaterialPropertyBlock();
            public readonly Vector3[] Points = new Vector3[PointCapacity];
            public readonly Vector3[] Shape = new Vector3[PointCapacity];
            public Vector3 Origin, End;
            public ChaseWeaponKind Kind;
            public float Age, Life;
            public int Count;
            public bool Active, Fatal;
        }

        sealed class FieldVisual
        {
            public ChaseField Field;
            public Visual Visual;
            public bool Seen;
        }

        sealed class Voice
        {
            public AudioSource Source;
            public Vector3 Position;
            public float Remaining;
            public bool PlayerReport;
        }

        struct PendingHit
        {
            public Vector3 Position;
            public int Signature;
            public float Distance;
        }

        public int PlayedHits { get; private set; }
        public int PlayedAttacks { get; private set; }
        public int SubmittedHits { get; private set; }
        public int SuppressedHitAudio { get; private set; }
        public int RecycledAttackVisuals { get; private set; }
        public int SuppressedHitVisuals { get; private set; }
        public int SuppressedFields { get; private set; }
        public int LastHitSignature { get; private set; } = -1;
        public int LastAttackSignature { get; private set; } = -1;
        public int ActiveFieldCount { get; private set; }
        public bool IsDisposed => disposed;
        public int ActiveVoiceCount
        {
            get
            {
                int n = 0;
                foreach (var v in attackVoices) if (v.Remaining > 0) n++;
                foreach (var v in hitVoices) if (v.Remaining > 0) n++;
                return n;
            }
        }

        public static ChaseEffects Create(Transform parent, Camera camera, Material material)
        {
            return new ChaseEffects(parent, camera, material);
        }

        public void Clear()
        {
            foreach (var v in attacks) Hide(v);
            foreach (var v in hits) Hide(v);
            foreach (var field in fields)
            { Hide(field.Visual); field.Field = null; field.Seen = false; }
            foreach (var voice in attackVoices)
            { voice.Source.Stop(); voice.Remaining = 0; }
            foreach (var voice in hitVoices)
            { voice.Source.Stop(); voice.Remaining = 0; }
            pendingCount = nextAttack = ActiveFieldCount = 0;
            PlayedHits = PlayedAttacks = SubmittedHits = SuppressedHitAudio = 0;
            RecycledAttackVisuals = SuppressedHitVisuals = SuppressedFields = 0;
            LastHitSignature = LastAttackSignature = -1;
            Array.Clear(reportCounts, 0, reportCounts.Length);
            Array.Clear(impactCounts, 0, impactCounts.Length);
        }

        ChaseEffects(Transform parent, Camera camera, Material source)
        {
            this.camera = camera;
            root = new GameObject("G bounded horror combat presentation");
            root.transform.SetParent(parent, false);
            material = new Material(source);
            // Retain the supplied shader/texture but remove the inherited neon
            // emission. Thin silhouettes, not broad light, carry attack identity.
            material.SetColor("_EmissionColor", new Color(.06f, .045f, .035f));
            crystal = CrystalMesh();
            hitCube = CubeMesh();
            for (int i = 0; i < attacks.Length; i++) attacks[i] = CreateVisual("Attack " + i);
            for (int i = 0; i < hits.Length; i++) hits[i] = CreateVisual("Enemy impact " + i, hitCube);
            for (int i = 0; i < fields.Length; i++)
                fields[i] = new FieldVisual { Visual = CreateVisual("Persistent field " + i) };
            for (int i = 0; i < reports.Length; i++)
                reports[i] = Clip(AttackSignature((ChaseWeaponKind)i), Synthesize(i, -1, false));
            for (int enemy = 0; enemy < 3; enemy++)
                for (int weapon = 0; weapon < WeaponCount; weapon++)
                    for (int fatal = 0; fatal < 2; fatal++)
                    {
                        int id = (enemy * WeaponCount + weapon) * 2 + fatal;
                        impacts[id] = Clip(HitSignature(id), Synthesize(weapon, enemy, fatal == 1));
                    }
            CreateVoices(attackVoices, "Own-origin attack report", 32);
            CreateVoices(hitVoices, "Enemy damage report", 96);
        }

        static Mesh CrystalMesh()
        {
            var mesh = new Mesh { name = "Original eight-face projectile crystal" };
            mesh.vertices = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            mesh.triangles = new[] { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
            mesh.uv = new[] { Vector2.one * .5f, Vector2.one * .5f, Vector2.one * .5f,
                Vector2.one * .5f, Vector2.one * .5f, Vector2.one * .5f };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh CubeMesh()
        {
            var mesh = new Mesh { name = "Original essential enemy hit cube" };
            mesh.vertices = new[] { new Vector3(-1, -1, -1), new Vector3(1, -1, -1),
                new Vector3(1, 1, -1), new Vector3(-1, 1, -1), new Vector3(-1, -1, 1),
                new Vector3(1, -1, 1), new Vector3(1, 1, 1), new Vector3(-1, 1, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5, 3, 7, 6, 3, 6, 2, 0, 1, 5, 0, 5, 4 };
            var uv = new Vector2[8];
            for (int i = 0; i < uv.Length; i++) uv[i] = Vector2.one * .5f;
            mesh.uv = uv;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        Visual CreateVisual(string name, Mesh mesh = null)
        {
            var visual = new Visual();
            for (int i = 0; i < visual.Lines.Length; i++)
            {
                var go = new GameObject(name + (i < 3 ? " essential stroke " : " detail stroke ") + i);
                go.transform.SetParent(root.transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.numCapVertices = 1;
                line.numCornerVertices = 1;
                line.textureMode = LineTextureMode.Stretch;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.enabled = false;
                visual.Lines[i] = line;
            }
            var core = new GameObject(name + " projectile core");
            core.transform.SetParent(root.transform, false);
            core.AddComponent<MeshFilter>().sharedMesh = mesh != null ? mesh : crystal;
            visual.CoreRenderer = core.AddComponent<MeshRenderer>();
            visual.CoreRenderer.sharedMaterial = material;
            visual.CoreRenderer.shadowCastingMode = ShadowCastingMode.Off;
            visual.CoreRenderer.receiveShadows = false;
            visual.CoreRenderer.enabled = false;
            visual.Core = core.transform;
            return visual;
        }

        void CreateVoices(Voice[] pool, string name, int priority)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject(name + " " + i);
                go.transform.SetParent(root.transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                // Preserve the authored stereo image. Listener-relative pan and
                // attenuation below locate reports without Unity downmixing to mono.
                source.spatialBlend = 0;
                source.dopplerLevel = 0;
                source.priority = priority;
                pool[i] = new Voice { Source = source, PlayerReport = pool == attackVoices };
            }
        }

        public void SetQuality(int preset)
        {
            quality = Mathf.Clamp(preset, 0, 2);
            // Apply immediately, including paused fields. Essential outlines are
            // never part of the cosmetic quality budget.
            foreach (var v in attacks) if (v.Active) DrawAttack(v);
            foreach (var v in hits) if (v.Active) DrawHit(v);
            foreach (var slot in fields)
                for (int i = 3; i < slot.Visual.Lines.Length; i++) slot.Visual.Lines[i].enabled = false;
        }
        public int AttackSignatureCount(ChaseWeaponKind kind) { return reportCounts[(int)kind]; }
        public int HitSignatureCount(int id) { return impactCounts[id]; }
        public static string AttackSignature(ChaseWeaponKind kind) { return "attack/" + kind; }
        public static int SignatureId(ChaseEnemyKind enemyKind, ChaseWeaponKind weaponKind, bool fatal = false)
        {
            return ((int)enemyKind * WeaponCount + (int)weaponKind) * 2 + (fatal ? 1 : 0);
        }

        public static string HitSignature(int id)
        {
            var weapon = (ChaseWeaponKind)((id / 2) % WeaponCount);
            return "impact/" + (ChaseEnemyKind)(id / (WeaponCount * 2)) + "/" +
                weapon + "/" + FamilyName(Family(weapon)) + (id % 2 == 1 ? "/fatal" : "/hit");
        }

        static string FamilyName(int family)
        {
            switch (family)
            {
                case 1: return "fire";
                case 2: return "ice";
                case 3: return "wind";
                case 4: return "lightning";
                case 5: return "holy";
                case 6: return "void";
                default: return "physical";
            }
        }

        static int Family(ChaseWeaponKind kind)
        {
            switch (kind)
            {
                case ChaseWeaponKind.Magma:
                case ChaseWeaponKind.Inferno:
                case ChaseWeaponKind.Graveburst: return 1;
                case ChaseWeaponKind.Winterglass: return 2;
                case ChaseWeaponKind.Warcry:
                case ChaseWeaponKind.Cyclone: return 3;
                case ChaseWeaponKind.Stormsplit:
                case ChaseWeaponKind.Sentry: return 4;
                case ChaseWeaponKind.Sanctifier:
                case ChaseWeaponKind.OrbitHammer: return 5;
                case ChaseWeaponKind.Pactbinder:
                case ChaseWeaponKind.Abyss: return 6;
                default: return 0;
            }
        }

        static Color ColorOf(ChaseWeaponKind kind)
        {
            switch (Family(kind))
            {
                case 1: return new Color(.82f, .28f, .09f); // Smouldering ember.
                case 2: return new Color(.42f, .65f, .72f); // Clouded ice.
                case 3: return new Color(.49f, .62f, .51f); // Sickly ash wind.
                case 4: return new Color(.57f, .59f, .78f); // Bruised lightning.
                case 5: return new Color(.78f, .64f, .36f); // Tarnished gold.
                case 6: return new Color(.59f, .29f, .57f); // Dried violet.
                default: return new Color(.74f, .65f, .51f);
            }
        }

        public void Attack(ChaseAttack attack)
        {
            if (disposed) return;
            var v = attacks[nextAttack];
            nextAttack = (nextAttack + 1) % attacks.Length;
            if (v.Active) RecycledAttackVisuals++;
            Hide(v);
            v.Kind = attack.Kind;
            v.Origin = attack.Origin;
            v.Count = Mathf.Min(PointCapacity, attack.Points.Count);
            for (int i = 0; i < v.Count; i++) v.Points[i] = attack.Points[i];
            v.End = v.Count > 0 ? v.Points[v.Count - 1] : v.Origin;
            v.Age = 0;
            v.Life = attack.Kind == ChaseWeaponKind.Inferno ? .36f : .28f;
            v.Active = true;
            DrawAttack(v);
            int slot = 0;
            for (int i = 0; i < attackVoices.Length; i++)
            {
                if (attackVoices[i].Remaining <= 0) { slot = i; break; }
                if (attackVoices[i].Remaining < attackVoices[slot].Remaining) slot = i;
            }
            StartVoice(attackVoices[slot], reports[(int)attack.Kind], attack.Origin);
            reportCounts[(int)attack.Kind]++;
            PlayedAttacks++;
            LastAttackSignature = (int)attack.Kind;
        }

        float Distance(Vector3 point) { return (camera.transform.position - point).sqrMagnitude; }

        public void Hit(ChaseHit hit)
        {
            if (disposed) return;
            SubmittedHits++;
            int signature = SignatureId(hit.Enemy.Kind, hit.WeaponKind, hit.Fatal);
            float distance = Distance(hit.Position);
            int slot = pendingCount;
            if (pendingCount < pending.Length) pendingCount++;
            else
            {
                slot = 0;
                for (int i = 1; i < pending.Length; i++) if (pending[i].Distance > pending[slot].Distance) slot = i;
                SuppressedHitAudio++;
                if (distance >= pending[slot].Distance) slot = -1;
            }
            if (slot >= 0) pending[slot] = new PendingHit { Position = hit.Position, Signature = signature, Distance = distance };
            int visualSlot = -1;
            float farthest = -1;
            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].Active) { visualSlot = i; break; }
                float d = Distance(hits[i].End);
                if (d > farthest) { farthest = d; visualSlot = i; }
            }
            if (hits[visualSlot].Active && distance >= farthest) { SuppressedHitVisuals++; return; }
            var v = hits[visualSlot];
            if (v.Active) SuppressedHitVisuals++;
            v.Kind = hit.WeaponKind;
            v.End = hit.Position;
            v.Age = 0;
            v.Life = hit.Fatal ? .52f : .3f;
            v.Active = true;
            v.Fatal = hit.Fatal;
            DrawHit(v);
        }

        void FlushHits()
        {
            // Selection is batch-wide, so a distant first-enumerated agent cannot
            // consume the budget ahead of the closest damage cue.
            for (int n = 0; n < pendingCount; n++)
            {
                int closest = n;
                for (int j = n + 1; j < pendingCount; j++)
                    if (pending[j].Distance < pending[closest].Distance) closest = j;
                var item = pending[closest]; pending[closest] = pending[n]; pending[n] = item;
                int slot = -1;
                float farthest = -1;
                for (int i = 0; i < hitVoices.Length; i++)
                {
                    if (hitVoices[i].Remaining <= 0 || !hitVoices[i].Source.isPlaying) { slot = i; break; }
                    float d = Distance(hitVoices[i].Position);
                    if (d > farthest) { farthest = d; slot = i; }
                }
                if (hitVoices[slot].Remaining > 0 && hitVoices[slot].Source.isPlaying && item.Distance > farthest)
                { SuppressedHitAudio++; continue; }
                StartVoice(hitVoices[slot], impacts[item.Signature], item.Position);
                PlayedHits++;
                impactCounts[item.Signature]++;
                LastHitSignature = item.Signature;
            }
            pendingCount = 0;
        }

        void StartVoice(Voice v, AudioClip clip, Vector3 position)
        {
            v.Source.Stop();
            v.Position = position;
            v.Source.clip = clip;
            v.Remaining = clip.length;
            MixVoice(v);
            v.Source.Play();
        }

        void MixVoice(Voice v)
        {
            Vector3 relative = camera.transform.InverseTransformPoint(v.Position);
            v.Source.panStereo = Mathf.Clamp(relative.x / Mathf.Max(2, relative.magnitude), -.8f, .8f);
            // Reserve headroom for separate player event feedback (.18 budget).
            // Combat volumes sum to <= .48 even with correlated report clips.
            int reportsActive = 0, hitsActive = 0;
            foreach (var voice in attackVoices) if (voice.Remaining > 0) reportsActive++;
            foreach (var voice in hitVoices) if (voice.Remaining > 0) hitsActive++;
            float budget = v.PlayerReport ? (hitsActive > 0 ? .36f : .48f) : (reportsActive > 0 ? .12f : .48f);
            int count = v.PlayerReport ? reportsActive : hitsActive;
            v.Source.volume = (budget / Mathf.Max(1, count)) / (1 + relative.magnitude * .035f);
        }

        static void Hide(Visual v)
        {
            foreach (var line in v.Lines) line.enabled = false;
            v.CoreRenderer.enabled = false;
            v.Active = false;
        }

        static void Stroke(LineRenderer line, Vector3[] points, int count, Color color, float width)
        {
            line.enabled = count > 1;
            line.positionCount = count;
            // Desaturate even explicit white/yellow accents into warm ash; keep
            // opacity and every essential stroke at Low for threat readability.
            Color ash = new Color(.72f, .66f, .57f, color.a);
            line.startColor = line.endColor = Color.Lerp(color, ash, .18f);
            line.widthMultiplier = width * .72f;
            for (int i = 0; i < count; i++) line.SetPosition(i, points[i]);
        }

        static void Segment(Visual v, int index, Vector3 a, Vector3 b, Color color, float width)
        {
            v.Shape[0] = a; v.Shape[1] = b;
            Stroke(v.Lines[index], v.Shape, 2, color, width);
        }

        static void Ring(Visual v, int index, Vector3 center, Vector3 normal, float radius, Color color, float width, float phase = 0)
        {
            var rotation = Quaternion.FromToRotation(Vector3.up, normal.normalized);
            for (int i = 0; i < 25; i++)
            {
                float angle = i * Mathf.PI * 2 / 24 + phase;
                v.Shape[i] = center + rotation * new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            }
            Stroke(v.Lines[index], v.Shape, 25, color, width);
        }

        static void Core(Visual v, Vector3 position, Vector3 scale, Color color, Quaternion rotation)
        {
            v.CoreRenderer.enabled = true;
            v.Core.position = position; v.Core.localScale = scale; v.Core.rotation = rotation;
            color = Color.Lerp(color, new Color(.47f, .42f, .37f, color.a), .28f);
            v.Tint.SetColor("_Color", color);
            v.Tint.SetColor("_BaseColor", color);
            v.Tint.SetColor("_EmissionColor", color * .18f);
            v.CoreRenderer.SetPropertyBlock(v.Tint);
        }

        void DrawAttack(Visual v)
        {
            foreach (var line in v.Lines) line.enabled = false;
            v.CoreRenderer.enabled = false;
            Color c = ColorOf(v.Kind);
            float progress = Mathf.Clamp01(v.Age / v.Life);
            Vector3 direction = (v.End - v.Origin).normalized;
            if (direction.sqrMagnitude < .1f) direction = camera.transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            if (right.sqrMagnitude < .1f) right = camera.transform.right;
            Quaternion facing = Quaternion.LookRotation(direction);
            Vector3 head = Vector3.Lerp(v.Origin, v.End, Mathf.Clamp01(progress * 2.5f));
            if (v.Kind == ChaseWeaponKind.Razor)
            {
                float length = 0;
                Vector3 previous = v.Origin;
                for (int i = 0; i < v.Count; i++)
                { length += Vector3.Distance(previous, v.Points[i]); previous = v.Points[i]; }
                float remaining = length * Mathf.Clamp01(progress * 2.5f);
                previous = v.Origin;
                for (int i = 0; i < v.Count; i++)
                {
                    float segment = Vector3.Distance(previous, v.Points[i]);
                    if (remaining <= segment && segment > .0001f)
                    {
                        direction = (v.Points[i] - previous).normalized;
                        head = Vector3.Lerp(previous, v.Points[i], remaining / segment);
                        break;
                    }
                    remaining -= segment;
                    previous = v.Points[i];
                    head = previous;
                }
            }
            switch (v.Kind)
            {
                case ChaseWeaponKind.Stormsplit:
                    if (v.Count > 0)
                    {
                        // Simulation records first target then explicit parent/end
                        // pairs. Return via known parent edges, never connect siblings.
                        Segment(v, 0, v.Origin, v.Points[0], Color.white, .065f);
                        int count = 0;
                        v.Shape[count++] = v.Points[0];
                        for (int i = 1; i + 1 < v.Count && count + 4 <= PointCapacity; i += 2)
                        {
                            v.Shape[count++] = v.Points[i];
                            v.Shape[count++] = v.Points[i + 1];
                            v.Shape[count++] = v.Points[i];
                            v.Shape[count++] = v.Points[0];
                        }
                        Stroke(v.Lines[1], v.Shape, count, c, .05f);
                    }
                    break;
                case ChaseWeaponKind.Volley:
                    // Each ray returns to the actual muzzle; no false chain links.
                    for (int i = 0; i < Mathf.Min(v.Count, 15); i++)
                    { v.Shape[i * 2] = v.Origin; v.Shape[i * 2 + 1] = v.Points[i]; }
                    Stroke(v.Lines[0], v.Shape, Mathf.Min(v.Count, 15) * 2, c, .035f);
                    Core(v, head, new Vector3(.045f, .045f, .22f), c, facing);
                    break;
                case ChaseWeaponKind.Inferno:
                case ChaseWeaponKind.Warcry:
                    DrawCone(v, v.Origin, direction, Vector3.Distance(v.Origin, v.End),
                        v.Kind == ChaseWeaponKind.Inferno ? .5317f : 1.1918f, c, progress);
                    break;
                case ChaseWeaponKind.Cyclone:
                    Segment(v, 0, v.Origin, head, c, .045f);
                    DrawVortex(v, head, .8f, clock * 5, c);
                    break;
                case ChaseWeaponKind.Magma:
                    Segment(v, 0, v.Origin, head, new Color(1, .17f, .015f), .055f);
                    Core(v, head, new Vector3(.2f, .16f, .25f), c, Quaternion.Euler(clock * 150, clock * 90, 0));
                    Ring(v, 1, head, direction, .32f, Color.yellow, .045f);
                    break;
                case ChaseWeaponKind.BoneLance:
                    Segment(v, 0, v.Origin, v.End, c, .045f);
                    Core(v, head, new Vector3(.075f, .095f, .68f), Color.white, facing);
                    Segment(v, 1, head - right * .22f - direction * .18f,
                        head + right * .22f - direction * .18f, c, .055f);
                    break;
                case ChaseWeaponKind.Graveburst:
                    Segment(v, 0, v.Origin, head, c, .04f);
                    Core(v, head, new Vector3(.14f, .11f, .2f), c, Quaternion.Euler(clock * 80, 0, clock * 45));
                    Ring(v, 1, v.End, Vector3.up, .22f + progress * .45f, c, .05f);
                    Ring(v, 2, v.End, right, .16f + progress * .3f, c, .035f);
                    break;
                case ChaseWeaponKind.Sanctifier:
                    Segment(v, 0, v.Origin, head, c, .05f);
                    Core(v, head, new Vector3(.055f, .055f, .38f), c, facing);
                    Segment(v, 1, head - right * .2f, head + right * .2f, Color.white, .04f);
                    Segment(v, 2, head - Vector3.up * .3f, head + Vector3.up * .3f, Color.white, .04f);
                    break;
                case ChaseWeaponKind.OrbitHammer:
                    Stroke(v.Lines[0], v.Points, v.Count, c, .045f);
                    Segment(v, 1, head - Vector3.up * .34f, head + Vector3.up * .18f, c, .065f);
                    Segment(v, 2, head - right * .3f + Vector3.up * .18f,
                        head + right * .3f + Vector3.up * .18f, c, .1f);
                    break;
                case ChaseWeaponKind.Winterglass:
                    if (v.Count > 1)
                    {
                        for (int i = 0; i < Mathf.Min(v.Count, 15); i++)
                        { v.Shape[i * 2] = v.Origin; v.Shape[i * 2 + 1] = v.Points[i]; }
                        Stroke(v.Lines[0], v.Shape, Mathf.Min(v.Count, 15) * 2, c, .04f);
                        head = v.Origin;
                    }
                    else Segment(v, 0, v.Origin, head, c, .045f);
                    // The travelling field owns the orb; the discharge adds shards,
                    // not a second opaque core in the same first-person sightline.
                    Ring(v, 1, head, direction, .28f, Color.white, .035f);
                    break;
                case ChaseWeaponKind.Pactbinder:
                    Segment(v, 0, v.Origin, head, c, .045f);
                    Ring(v, 1, v.End, Vector3.up, .34f, c, .05f);
                    Ring(v, 2, v.End, right, .24f, c, .035f);
                    break;
                case ChaseWeaponKind.Abyss:
                    Segment(v, 0, v.Origin, v.End, c, .035f);
                    Ring(v, 1, v.End, Vector3.up, .65f, c, .04f);
                    Ring(v, 2, v.End, Vector3.up, .18f + progress * .12f, Color.black, .03f);
                    Core(v, v.End, Vector3.one * .12f, c, Quaternion.identity);
                    break;
                case ChaseWeaponKind.Sentry:
                    Segment(v, 0, v.Origin, head, c, .035f);
                    Ring(v, 1, v.End, Vector3.up, .22f, c, .04f);
                    Core(v, head, new Vector3(.06f, .06f, .16f), c, facing);
                    break;
                case ChaseWeaponKind.Razor:
                    // Points are the actual ricochet path.
                    v.Shape[0] = v.Origin;
                    for (int i = 0; i < Mathf.Min(v.Count, 31); i++) v.Shape[i + 1] = v.Points[i];
                    Stroke(v.Lines[0], v.Shape, Mathf.Min(v.Count, 31) + 1, c, .035f);
                    Ring(v, 1, head, direction, .2f, c, .065f, clock * 16);
                    break;
                default: // Frenzy's short solid slug and crossed muzzle vent.
                    Segment(v, 0, v.Origin, head, c, .03f);
                    Core(v, head, new Vector3(.04f, .04f, .16f), Color.white, facing);
                    Segment(v, 1, v.Origin - right * .09f, v.Origin + right * .09f, c, .045f);
                    break;
            }
            Vector3 detailHead = v.Kind == ChaseWeaponKind.Winterglass && v.Count > 1 ? v.Points[0] : head;
            if (quality > 0 && Vector3.Distance(camera.transform.position, detailHead) > 1.25f)
                DrawPayload(v, detailHead, direction, right, c, progress);
            // A large travelling core must leave the first-person near field
            // before becoming visible. Essential thin attack strokes stay present.
            if (v.CoreRenderer.enabled && Vector3.Distance(camera.transform.position, v.Core.position) < 1.25f)
                v.CoreRenderer.enabled = false;
        }

        static void Ribbon(Visual v, int index, Vector3 start, Vector3 end,
            Vector3 right, Color c, float radius, float turns, float phase, float width)
        {
            Vector3 forward = (end - start).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized;
            for (int i = 0; i < 24; i++)
            {
                float f = i / 23f;
                float a = f * turns * Mathf.PI * 2 + phase;
                // Closed endpoints: decorative braids never invent another target.
                float envelope = Mathf.Sin(f * Mathf.PI) * radius;
                v.Shape[i] = Vector3.Lerp(start, end, f)
                    + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * envelope;
            }
            Stroke(v.Lines[index], v.Shape, 24, c, width);
        }

        void DrawPayload(Visual v, Vector3 head, Vector3 direction, Vector3 right, Color c, float progress)
        {
            float fade = 1 - progress;
            float phase = v.Age * 12;
            Vector3 start = v.Origin;
            // Detail stays downrange, not around the reticle or inside the gun.
            float distance = Vector3.Distance(start, head);
            if (distance > .8f) start += (head - start).normalized * .65f;
            Color secondary = Color.Lerp(c, new Color(.52f, .47f, .42f), .4f);
            switch (v.Kind)
            {
                case ChaseWeaponKind.Stormsplit:
                    if (v.Count == 0) return;
                    head = v.Points[0];
                    Ribbon(v, 3, start, head, right, c, .085f, 5, phase, .025f * fade);
                    Ring(v, 4, head, direction, .2f + progress * .12f, secondary, .025f * fade);
                    if (quality == 2) Fragments(v, head, direction, right, c, progress, .28f);
                    return;
                case ChaseWeaponKind.Volley:
                    // Five tiny darts retain their submitted lanes without a broad
                    // muzzle sheet or a second false hit path.
                    for (int i = 0; i < Mathf.Min(v.Count, 15); i++)
                    { v.Shape[i * 2] = v.Origin; v.Shape[i * 2 + 1] = Vector3.Lerp(v.Origin, v.Points[i], Mathf.Clamp01(progress * 3)); }
                    Stroke(v.Lines[3], v.Shape, Mathf.Min(v.Count, 15) * 2, secondary, .018f * fade);
                    Ring(v, 5, head, direction, .12f + progress * .12f, c, .018f * fade);
                    if (quality == 2) Fragments(v, head, direction, right, c, progress, .24f);
                    return;
                case ChaseWeaponKind.Sentry:
                    Segment(v, 3, head - direction * .3f - right * .06f, head + right * .05f,
                        secondary, .018f * fade);
                    Segment(v, 4, head - direction * .2f + right * .07f, head - right * .04f,
                        c, .014f * fade);
                    return;
                case ChaseWeaponKind.Razor:
                    // Retain every bounce in the actual path; do not shortcut corners.
                    v.Shape[0] = v.Origin;
                    for (int i = 0; i < Mathf.Min(v.Count, 31); i++) v.Shape[i + 1] = v.Points[i];
                    Stroke(v.Lines[3], v.Shape, Mathf.Min(v.Count, 31) + 1, secondary, .018f * fade);
                    Ring(v, 4, head, direction, .16f, secondary, .025f * fade, phase);
                    Ring(v, 5, head, direction, .24f, c, .014f * fade, -phase);
                    if (quality == 2) Fragments(v, head, direction, right, c, progress, .25f);
                    return;
                case ChaseWeaponKind.Frenzy:
                    Segment(v, 3, head - direction * .22f, head - direction * .06f + right * .035f,
                        secondary, .015f * fade);
                    Segment(v, 4, head - direction * .15f, head - right * .025f,
                        c, .012f * fade);
                    return;
                case ChaseWeaponKind.Warcry:
                    // Separated expanding wavefronts, never a filled cone.
                    Ring(v, 3, Vector3.Lerp(v.Origin, v.End, progress), direction,
                        Vector3.Distance(v.Origin, v.End) * progress * 1.1918f, secondary, .03f * fade);
                    Ring(v, 4, Vector3.Lerp(v.Origin, v.End, progress * .7f), direction,
                        Vector3.Distance(v.Origin, v.End) * progress * .7f * 1.1918f, c, .018f * fade);
                    if (quality == 2) Fragments(v, head, direction, right, c, progress, .4f);
                    return;
                case ChaseWeaponKind.Cyclone:
                    Ribbon(v, 3, head, head + Vector3.up * 1.1f, Vector3.right,
                        c, .26f, 2.5f, phase, .025f * fade);
                    Ring(v, 4, head + Vector3.up * .08f, Vector3.up, .42f, secondary, .018f * fade);
                    return;
                case ChaseWeaponKind.Magma:
                    Ribbon(v, 3, start, head, right, c, .09f, 2, phase, .025f * fade);
                    Ring(v, 4, head, direction, .2f + progress * .1f, secondary, .022f * fade);
                    if (quality == 2) Fragments(v, head, direction, right, c, progress, .32f);
                    return;
                case ChaseWeaponKind.BoneLance:
                    Segment(v, 3, head - direction * .55f - right * .13f,
                        head - direction * .38f, secondary, .024f * fade);
                    Segment(v, 4, head - direction * .34f,
                        head - direction * .48f + right * .13f, c, .018f * fade);
                    return;
                case ChaseWeaponKind.Graveburst:
                    Ribbon(v, 3, start, head, right, secondary, .11f, 3.5f, phase, .022f * fade);
                    Ring(v, 4, v.End, Vector3.up, .24f + progress * .25f, c, .02f * fade);
                    if (quality == 2) Fragments(v, v.End, Vector3.up, right, c, progress, .35f);
                    return;
                case ChaseWeaponKind.Sanctifier:
                    Segment(v, 3, start + right * .045f, head + right * .045f, secondary, .015f * fade);
                    Segment(v, 4, start - right * .045f, head - right * .045f, c, .015f * fade);
                    Ring(v, 5, head, direction, .14f, c, .018f * fade);
                    return;
                case ChaseWeaponKind.OrbitHammer:
                    Ribbon(v, 3, head - direction * .55f, head, right, c, .18f, .75f, phase, .028f * fade);
                    Segment(v, 4, head - right * .24f, head + right * .24f, secondary, .035f * fade);
                    return;
                case ChaseWeaponKind.Inferno:
                    Ribbon(v, 3, start, head, right, c, .12f, 3, phase, .026f * fade);
                    Ribbon(v, 4, start, head, right, secondary, .07f, 2, -phase, .016f * fade);
                    return;
                case ChaseWeaponKind.Winterglass:
                    if (v.Count > 1)
                    {
                        for (int i = 0; i < Mathf.Min(v.Count, 15); i++)
                        { v.Shape[i * 2] = v.Origin; v.Shape[i * 2 + 1] = v.Points[i]; }
                        Stroke(v.Lines[3], v.Shape, Mathf.Min(v.Count, 15) * 2, secondary, .016f * fade);
                        Ring(v, 5, head, direction, .2f + progress * .12f, c, .018f * fade);
                        if (quality == 2) Fragments(v, head, direction, right, c, progress, .28f);
                        return;
                    }
                    Segment(v, 3, head - direction * .3f - right * .09f, head, secondary, .018f * fade);
                    Segment(v, 4, head - direction * .22f + right * .09f, head, c, .014f * fade);
                    Ring(v, 5, head, direction, .2f, c, .018f * fade);
                    return;
                case ChaseWeaponKind.Pactbinder:
                    Ribbon(v, 3, start, head, right, c, .1f, 2.5f, phase, .021f * fade);
                    Ribbon(v, 4, start, head, right, secondary, .06f, 2.5f, -phase, .014f * fade);
                    Ring(v, 5, v.End, Vector3.up, .22f, c, .018f * fade);
                    return;
                case ChaseWeaponKind.Abyss:
                    // No long travelling suction ropes. Placement is a quiet seam;
                    // persistent rings own the calibrated local influence volume.
                    Ring(v, 3, v.End + Vector3.up * .06f, Vector3.up, .65f, c, .02f * fade);
                    Ring(v, 4, v.End + Vector3.up * .15f, Vector3.up,
                        Mathf.Max(.1f, .55f - v.Age * .5f), secondary, .016f * fade);
                    Ring(v, 5, v.End + Vector3.up * .1f, Vector3.up, .13f + progress * .08f, c, .014f * fade);
                    if (quality == 2) Fragments(v, v.End + Vector3.up * .1f, Vector3.up, Vector3.right, c, progress, .18f);
                    return;
            }
        }

        static void Fragments(Visual v, Vector3 center, Vector3 direction, Vector3 right,
            Color c, float progress, float extent)
        {
            Vector3 up = Vector3.Cross(direction, right).normalized;
            float t = v.Age;
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.39996f + (int)v.Kind * .37f;
                Vector3 radial = right * Mathf.Cos(a) + up * Mathf.Sin(a);
                Vector3 position = center + radial * (extent * progress)
                    + Vector3.up * (.55f * progress - t * t * 1.4f);
                Vector3 tail = radial * (.1f + extent * .15f) * (1 - progress);
                // A closed faceted shard, then a narrow spark wake. No persistent
                // decal, collider, debris GameObject or screen overlay is spawned.
                v.Shape[0] = position - tail;
                v.Shape[1] = position + up * .055f * (1 - progress);
                v.Shape[2] = position + tail * .4f;
                v.Shape[3] = v.Shape[0];
                Stroke(v.Lines[6 + i], v.Shape, 4, Color.Lerp(c, Color.white, i * .2f),
                    .035f * (1 - progress));
            }
        }

        static void DrawCone(Visual v, Vector3 origin, Vector3 direction, float reach, float tangent, Color c, float progress)
        {
            Vector3 end = origin + direction * reach;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            if (right.sqrMagnitude < .1f) right = Vector3.right;
            Vector3 up = Vector3.Cross(direction, right).normalized;
            float radius = reach * tangent;
            // Open volume edges and a restrained ember spine keep the cone
            // readable without a bright opaque sheet across the first-person view.
            v.Shape[0] = origin; v.Shape[1] = end - right * radius;
            v.Shape[2] = end + up * radius; v.Shape[3] = origin;
            v.Shape[4] = end + right * radius; v.Shape[5] = end - up * radius;
            v.Shape[6] = origin;
            Stroke(v.Lines[0], v.Shape, 7, c, .045f);
            Ring(v, 1, end, direction, radius, c, .055f);
            Segment(v, 2, origin, end, Color.Lerp(c, new Color(.68f, .49f, .28f), .35f),
                .04f * (1 - progress * .4f));
            Core(v, origin + direction * reach * .65f,
                new Vector3(.045f, .045f, .14f), c, Quaternion.LookRotation(direction));
        }

        static void DrawVortex(Visual v, Vector3 center, float radius, float phase, Color c)
        {
            for (int i = 0; i < PointCapacity; i++)
            {
                float fraction = (float)i / (PointCapacity - 1);
                float angle = phase + fraction * Mathf.PI * 6;
                float r = radius * (1 - fraction * .65f);
                v.Shape[i] = center + new Vector3(Mathf.Cos(angle) * r, fraction * 1.1f, Mathf.Sin(angle) * r);
            }
            Stroke(v.Lines[1], v.Shape, PointCapacity, c, .045f);
            Ring(v, 2, center + Vector3.up * .1f, Vector3.up, radius, c, .05f);
            Core(v, center + Vector3.up * .38f, new Vector3(.1f, .38f, .1f), Color.white, Quaternion.identity);
        }

        void DrawHit(Visual v)
        {
            foreach (var line in v.Lines) line.enabled = false;
            Color c = ColorOf(v.Kind);
            float progress = Mathf.Clamp01(v.Age / v.Life);
            float fade = 1 - progress;
            float r = (v.Fatal ? .48f : .26f) * (1 + progress);
            Segment(v, 0, v.End - camera.transform.right * r, v.End + camera.transform.right * r, c, .045f);
            Segment(v, 1, v.End - camera.transform.up * r, v.End + camera.transform.up * r, Color.white, .035f);
            // Compact faceted cube-like flash survives Low, independently of the
            // living swarm's damage flash (the swarm may already remove a corpse).
            Core(v, v.End, Vector3.one * r * .24f * fade, c, Quaternion.Euler(progress * 120, 45, 45));
            // Payload-specific fracture silhouettes replace an undifferentiated
            // explosion. These are damage-only, including fatal field ticks.
            Vector3 right = camera.transform.right, up = camera.transform.up;
            Vector3 normal = camera.transform.forward;
            float spread = v.Fatal ? 1.3f : .7f;
            switch (v.Kind)
            {
                case ChaseWeaponKind.Stormsplit:
                    for (int i = 0; i < 9; i++)
                        v.Shape[i] = v.End + right * ((i / 8f - .5f) * r * 3)
                            + up * (i % 2 == 0 ? r * .35f : -r * .35f) * fade;
                    Stroke(v.Lines[2], v.Shape, 9, c, .035f * fade);
                    break;
                case ChaseWeaponKind.Volley:
                    for (int i = 0; i < 6; i++)
                        v.Shape[i] = v.End + right * ((i - 2.5f) * r * .34f)
                            + up * (Mathf.Abs(i - 2.5f) * r * .18f);
                    Stroke(v.Lines[2], v.Shape, 6, c, .027f * fade);
                    spread *= 1.1f;
                    break;
                case ChaseWeaponKind.Sentry:
                    for (int i = 0; i < 5; i++)
                        v.Shape[i] = v.End + right * ((i - 2) * r * .28f)
                            + up * (i % 2 == 0 ? r * .18f : -r * .12f);
                    Stroke(v.Lines[2], v.Shape, 5, c, .028f * fade);
                    spread *= .6f;
                    break;
                case ChaseWeaponKind.Razor:
                    Segment(v, 2, v.End - right * r - up * r,
                        v.End + right * r + up * r, c, .045f * fade);
                    break;
                case ChaseWeaponKind.Frenzy:
                    Segment(v, 2, v.End - normal * .12f,
                        v.End + up * r * 1.2f, Color.white, .03f * fade);
                    spread *= .5f;
                    break;
                case ChaseWeaponKind.Warcry:
                    Ring(v, 2, v.End, normal, r * 1.65f, c, .032f * fade);
                    break;
                case ChaseWeaponKind.Cyclone:
                    Ring(v, 2, v.End, Vector3.up, r * 1.3f, c, .03f * fade, progress * 5);
                    break;
                case ChaseWeaponKind.Magma:
                    v.Shape[0] = v.End - right * r * .55f;
                    v.Shape[1] = v.End + up * r * 1.2f;
                    v.Shape[2] = v.End;
                    v.Shape[3] = v.End + up * r * 1.65f;
                    v.Shape[4] = v.End + right * r * .55f;
                    Stroke(v.Lines[2], v.Shape, 5, c, .04f * fade);
                    spread *= 1.35f;
                    break;
                case ChaseWeaponKind.BoneLance:
                    v.Shape[0] = v.End - normal * .16f;
                    v.Shape[1] = v.End + up * r * 1.35f;
                    v.Shape[2] = v.End - right * r * .35f + up * r * .75f;
                    v.Shape[3] = v.End + right * r * .35f + up * r * .75f;
                    Stroke(v.Lines[2], v.Shape, 4, Color.white, .032f * fade);
                    spread *= .75f;
                    break;
                case ChaseWeaponKind.Graveburst:
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 2.2f;
                        v.Shape[i] = v.End + right * Mathf.Cos(a) * r * (i + 1) / 6
                            + up * Mathf.Sin(a) * r * (i + 1) / 6;
                    }
                    Stroke(v.Lines[2], v.Shape, 6, c, .034f * fade);
                    spread *= 1.2f;
                    break;
                case ChaseWeaponKind.Sanctifier:
                    v.Shape[0] = v.End - up * r;
                    v.Shape[1] = v.End + up * r;
                    v.Shape[2] = v.End - right * r * .55f + up * r * .2f;
                    v.Shape[3] = v.End + right * r * .55f + up * r * .2f;
                    Stroke(v.Lines[2], v.Shape, 4, Color.white, .026f * fade);
                    break;
                case ChaseWeaponKind.OrbitHammer:
                    Ring(v, 2, v.End, Vector3.up, r * 1.45f, c, .04f * fade);
                    spread *= 1.15f;
                    break;
                case ChaseWeaponKind.Inferno:
                    for (int i = 0; i < 7; i++)
                        v.Shape[i] = v.End + right * ((i / 6f - .5f) * r * 1.4f)
                            + up * (i % 2 == 0 ? r * (1 + i * .08f) : r * .35f);
                    Stroke(v.Lines[2], v.Shape, 7, c, .035f * fade);
                    spread *= .85f;
                    break;
                case ChaseWeaponKind.Winterglass:
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * Mathf.PI / 3;
                        v.Shape[i] = v.End + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
                    }
                    Stroke(v.Lines[2], v.Shape, 7, c, .032f * fade);
                    break;
                case ChaseWeaponKind.Pactbinder:
                    Ring(v, 2, v.End, normal, r * .8f * (1 - progress * .5f), c, .03f * fade);
                    break;
                case ChaseWeaponKind.Abyss:
                    Ring(v, 2, v.End, Vector3.up, r * .5f * (1 - progress * .7f), c, .025f * fade);
                    spread *= .3f;
                    break;
            }
            if (quality == 0) return;
            Ring(v, 5, v.End, normal, spread * progress, c, .024f * fade);
            // Broken outward spokes imply material fracture but stay local to the
            // actual hit; they do not pretend to damage surrounding architecture.
            for (int i = 0; i < 2; i++)
            {
                float a = (int)v.Kind * .41f + i * 2.1f;
                Vector3 ray = right * Mathf.Cos(a) + up * Mathf.Sin(a);
                Segment(v, 3 + i, v.End + ray * r * .5f,
                    v.End + ray * spread * (.3f + progress), c, .028f * fade);
            }
            if (quality == 2) Fragments(v, v.End, normal, right, c, progress, spread);
        }

        public void Fields(List<ChaseField> state, double elapsed)
        {
            if (disposed) return;
            foreach (var slot in fields)
            {
                slot.Seen = false;
                if (slot.Field != null && (slot.Field.ExpiresAt <= elapsed || !state.Contains(slot.Field)))
                { slot.Field = null; Hide(slot.Visual); }
            }
            foreach (var field in state)
            {
                if (field.ExpiresAt <= elapsed) continue;
                FieldVisual slot = null;
                foreach (var candidate in fields)
                    if (ReferenceEquals(candidate.Field, field)) { slot = candidate; break; }
                if (slot == null)
                {
                    foreach (var candidate in fields)
                        if (candidate.Field == null) { slot = candidate; break; }
                }
                if (slot == null)
                {
                    float farthest = -1;
                    foreach (var candidate in fields)
                    {
                        float d = Distance(candidate.Field.Position);
                        if (d > farthest) { farthest = d; slot = candidate; }
                    }
                    SuppressedFields++;
                    if (Distance(field.Position) >= farthest) continue;
                }
                slot.Field = field;
                slot.Seen = true;
                DrawField(slot.Visual, field, (float)elapsed);
            }
            ActiveFieldCount = 0;
            foreach (var slot in fields)
            {
                if (!slot.Seen) { slot.Field = null; Hide(slot.Visual); }
                else ActiveFieldCount++;
            }
        }

        void DrawField(Visual v, ChaseField field, float elapsed)
        {
            foreach (var line in v.Lines) line.enabled = false;
            v.CoreRenderer.enabled = false;
            v.Kind = field.Owner.Kind;
            Color c = ColorOf(v.Kind);
            Vector3 p = field.Position;
            Vector3 direction = field.Direction.sqrMagnitude > .01f ? field.Direction.normalized : Vector3.forward;
            float r = Mathf.Max(.25f, field.Radius);
            float age = elapsed - (float)field.CreatedAt;
            switch (v.Kind)
            {
                case ChaseWeaponKind.Inferno:
                    DrawCone(v, p, direction, 11, .5317f, c, 0);
                    break;
                case ChaseWeaponKind.Cyclone:
                    Ring(v, 0, p + Vector3.up * .08f, Vector3.up, r * .72f, Color.white, .045f);
                    DrawVortex(v, p, r, age * 6, c);
                    break;
                case ChaseWeaponKind.Abyss:
                    // The simulation supplies the 2.4 m horizontal radius and
                    // excludes other floors. Rings contract at 1.2 m/s, not the
                    // old ~3.6 m/s. No towering funnel or cross-floor suction.
                    Ring(v, 0, p + Vector3.up * .08f, Vector3.up, r, c, .075f);
                    Ring(v, 1, p + Vector3.up * .1f, Vector3.up,
                        Mathf.Max(.1f, r - Mathf.Repeat(age * 1.2f, r)), c, .055f);
                    Ring(v, 2, p + Vector3.up * .13f, Vector3.up,
                        Mathf.Max(.1f, r - Mathf.Repeat(age * 1.2f + r * .5f, r)), c, .045f);
                    Core(v, p + Vector3.up * .3f, Vector3.one * .2f, c, Quaternion.Euler(0, age * 35, 0));
                    if (quality > 0)
                    {
                        Ring(v, 3, p + Vector3.up * 1.5f, Vector3.up, r, c, .024f);
                        for (int i = 0; i < 4; i++)
                        {
                            float a = i * Mathf.PI * .5f;
                            Vector3 edge = p + new Vector3(Mathf.Cos(a) * r, .1f, Mathf.Sin(a) * r);
                            v.Shape[i * 3] = edge;
                            v.Shape[i * 3 + 1] = edge + Vector3.up * 1.4f;
                            v.Shape[i * 3 + 2] = edge;
                        }
                        Stroke(v.Lines[4], v.Shape, 12, c, .018f);
                    }
                    break;
                case ChaseWeaponKind.Sentry:
                    Ring(v, 0, p + Vector3.up * .05f, Vector3.up, r, c, .045f);
                    Segment(v, 1, p, p + Vector3.up * 1.05f, c, .07f);
                    Segment(v, 2, p + Vector3.up * .8f, p + Vector3.up * .8f + direction * .65f, Color.white, .05f);
                    Core(v, p + Vector3.up * .6f, new Vector3(.2f, .32f, .2f), c, Quaternion.identity);
                    break;
                case ChaseWeaponKind.Magma:
                case ChaseWeaponKind.Graveburst:
                    if (v.Kind == ChaseWeaponKind.Magma && field.Direction.sqrMagnitude > .01f)
                    {
                        Core(v, p, Vector3.one * .24f, c, Quaternion.Euler(age * 130, age * 70, 0));
                        Ring(v, 0, p, direction, .36f, Color.yellow, .045f);
                        Segment(v, 1, p - direction * .8f, p, c, .055f);
                        break;
                    }
                    Ring(v, 0, p + Vector3.up * .08f, Vector3.up, r, c, .055f);
                    Ring(v, 1, p + Vector3.up * .1f, Vector3.up, r * .62f, Color.yellow, .04f);
                    for (int i = 0; i < 9; i++)
                    {
                        float angle = i * Mathf.PI / 4;
                        v.Shape[i * 3] = p + new Vector3(Mathf.Cos(angle) * r * .72f, .08f, Mathf.Sin(angle) * r * .72f);
                        v.Shape[i * 3 + 1] = v.Shape[i * 3] + Vector3.up * (.7f + .2f * Mathf.Sin(age * 8 + i));
                        v.Shape[i * 3 + 2] = v.Shape[i * 3];
                    }
                    Stroke(v.Lines[2], v.Shape, 27, c, .045f);
                    Core(v, p + Vector3.up * .2f, new Vector3(.28f, .12f, .28f), c, Quaternion.identity);
                    break;
                case ChaseWeaponKind.Winterglass:
                    Ring(v, 0, p, Vector3.up, r, c, .045f);
                    Ring(v, 1, p + Vector3.up * .45f, direction, .35f, Color.white, .035f);
                    c.a = .45f;
                    Core(v, p + Vector3.up * .45f, Vector3.one * .22f, c, Quaternion.Euler(age * 80, age * 100, 0));
                    break;
                case ChaseWeaponKind.OrbitHammer:
                    Ring(v, 0, p, Vector3.up, r, c, .045f, age * 3);
                    Vector3 hammerRight = Quaternion.AngleAxis(age * 360, Vector3.up) * Vector3.right;
                    Segment(v, 1, p - Vector3.up * .5f, p + Vector3.up * .3f, c, .06f);
                    Segment(v, 2, p + Vector3.up * .3f - hammerRight * .45f,
                        p + Vector3.up * .3f + hammerRight * .45f, Color.white, .09f);
                    break;
                case ChaseWeaponKind.Pactbinder:
                    Ring(v, 0, p, Vector3.up, r, c, .045f);
                    Ring(v, 1, p + Vector3.up * .7f, direction, r * .65f, c, .035f);
                    Segment(v, 2, p, p + Vector3.up * 1.7f, Color.white, .03f);
                    break;
                default:
                    // Other field owners retain their class attack silhouette.
                    v.Origin = p; v.End = p + direction * r;
                    v.Points[0] = v.End; v.Count = 1; v.Age = 0; v.Life = 1;
                    DrawAttack(v);
                    break;
            }
        }

        public void FlushPendingHits()
        {
            FlushHits();
        }

        public void Advance(float dt)
        {
            if (disposed) return;
            dt = Mathf.Max(0, dt);
            clock += dt;
            foreach (var v in attacks)
            {
                if (!v.Active) continue;
                v.Age += dt;
                if (v.Age >= v.Life) Hide(v); else DrawAttack(v);
            }
            foreach (var v in hits)
            {
                if (!v.Active) continue;
                v.Age += dt;
                if (v.Age >= v.Life) Hide(v); else DrawHit(v);
            }
            AdvanceVoices(attackVoices, dt);
            AdvanceVoices(hitVoices, dt);
            FlushHits();
            foreach (var v in attackVoices) if (v.Remaining > 0) MixVoice(v);
            foreach (var v in hitVoices) if (v.Remaining > 0) MixVoice(v);
        }

        void AdvanceVoices(Voice[] pool, float dt)
        {
            foreach (var v in pool)
            {
                v.Remaining = Mathf.Max(0, v.Remaining - dt);
                if (v.Remaining <= 0 || !v.Source.isPlaying) { v.Remaining = 0; v.Source.Stop(); }
            }
        }

        static AudioClip Clip(string name, float[] samples)
        {
            var clip = AudioClip.Create("F original " + name, samples.Length / 2, 2, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // Original deterministic synthesis. Impact material and elemental layer
        // are independent signals; this is not a beep with randomized pitch.
        static float[] Synthesize(int kind, int enemy, bool fatal)
        {
            bool impact = enemy >= 0;
            int family = Family((ChaseWeaponKind)kind);
            // A complete receiver/discharge/decay phrase rather than the inherited
            // sub-second report. Tails are baked into stereo clips, not extra voices.
            double duration = impact ? (fatal ? .86 : .52) : ReportDurations[kind];
            int frames = (int)(SampleRate * duration);
            var dry = new double[frames];
            uint seed = (uint)(0x71E3 + kind * 7919 + (enemy + 1) * 104729 + (fatal ? 65537 : 0));
            double low = 0, mid = 0, phase = 0, bassPhase = 0;
            for (int i = 0; i < frames; i++)
            {
                double t = (double)i / SampleRate;
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                double noise = seed / (double)uint.MaxValue * 2 - 1;
                low += .035 * (noise - low);
                mid += .2 * (noise - mid);
                double body = 0;
                if (impact)
                {
                    if (enemy == 0)
                    {
                        // Dry ivory clicks: three separated brittle transients.
                        foreach (double onset in BoneOnsets)
                        {
                            double x = t - onset;
                            if (x >= 0) body += ((noise - mid) * 1.6 + Math.Sin(x * 2 * Math.PI * 1800) * .22) * Math.Exp(-x * 180);
                        }
                    }
                    else if (enemy == 1)
                    {
                        // Broad fleshy thud, wet low-passed slap, short grit.
                        body = (Math.Sin(2 * Math.PI * (95 * t + 30 * t * Math.Exp(-t * 28))) * .75 + low * 3) * Math.Exp(-t * 31)
                            + mid * 1.7 * Math.Exp(-t * 85);
                    }
                    else
                    {
                        // Armored elite: inharmonic plate modes plus hard strike.
                        body = (Math.Sin(t * 2 * Math.PI * 620) * .45 + Math.Sin(t * 2 * Math.PI * 997) * .3
                            + Math.Sin(t * 2 * Math.PI * 1523) * .2) * Math.Exp(-t * 17)
                            + (noise - low) * .95 * Math.Exp(-t * 150);
                    }
                    if (fatal)
                        body += (low * 2.2 + Math.Sin(t * 2 * Math.PI * 58) * .4) * Math.Exp(-t * 7)
                            + (noise - mid) * .22 * Math.Exp(-Math.Abs(t - .12) * 40);
                }
                double layer;
                switch (family)
                {
                    case 1: // Combustion, not a tonal fire beep.
                        layer = (mid * 2.2 + low * 2.7) * Math.Exp(-t * (impact ? 14 : 5))
                            + (noise - mid) * .7 * Math.Exp(-t * 95)
                            + (noise - mid) * .24 * Math.Exp(-Math.Abs(t - .19) * 35);
                        break;
                    case 2: // Glass fracture and ringing crystalline partials.
                        layer = (Math.Sin(t * 2 * Math.PI * 2411) * .25 + Math.Sin(t * 2 * Math.PI * 3727) * .18)
                            * Math.Exp(-t * 7) + (noise - mid) * .65 * Math.Exp(-t * 120)
                            + Math.Sin(t * 2 * Math.PI * 1579) * .1 * Math.Exp(-t * 4);
                        break;
                    case 3: // Broad turbulent gust with a swelling resonant throat.
                        phase += 2 * Math.PI * (180 - 100 * Math.Min(1, t * 3)) / SampleRate;
                        layer = (mid * 3 + low * 2 + Math.Sin(phase) * .17) * Math.Min(1, t / .018) * Math.Exp(-t * 5);
                        break;
                    case 4: // Discontinuous electrical crack, buzz and discharge.
                        phase += 2 * Math.PI * (2500 * Math.Exp(-t * 9) + 120) / SampleRate;
                        layer = (noise - low) * Math.Exp(-t * 35) +
                            Math.Sin(phase) * Math.Sign(Math.Sin(t * 2 * Math.PI * 63)) * .3 * Math.Exp(-t * 9)
                            + mid * Math.Exp(-Math.Abs(t - .085) * 75);
                        break;
                    case 5: // Bell stack, airy contact, warm low resonant body.
                        layer = (Math.Sin(t * 2 * Math.PI * 523.25) * .45 + Math.Sin(t * 2 * Math.PI * 784.9) * .25
                            + Math.Sin(t * 2 * Math.PI * 1318.5) * .15) * Math.Exp(-t * 5)
                            + (noise - mid) * .3 * Math.Exp(-t * 90);
                        break;
                    case 6: // Falling subharmonics and fluttering inward suction.
                        phase += 2 * Math.PI * (130 * Math.Exp(-t * 5) + 35) / SampleRate;
                        layer = (Math.Sin(phase) * .8 + low * 2.5 + mid * Math.Sin(t * 2 * Math.PI * 23))
                            * Math.Exp(-t * 5);
                        break;
                    default: // Physical crack, report bass, mechanical afterclick.
                        phase += 2 * Math.PI * (65 + 120 * Math.Exp(-t * 45)) / SampleRate;
                        layer = Math.Sin(phase) * .9 * Math.Exp(-t * 22) + (noise - mid) * 1.2 * Math.Exp(-t * 190)
                            + low * 2 * Math.Exp(-t * 32);
                        if (t > .04) layer += (noise - mid) * .18 * Math.Exp(-(t - .04) * 130);
                        break;
                }
                if (kind >= 0)
                {
                    // Each weapon has a distinct authored temporal or spectral
                    // motif on top of its family, with fixed pitch (no randomness).
                    switch ((ChaseWeaponKind)kind)
                    {
                        case ChaseWeaponKind.Stormsplit:
                            layer += (noise - mid) * .55 * Math.Exp(-Math.Abs(t - .065) * 130)
                                + low * 1.2 * Math.Exp(-Math.Abs(t - .16) * 22);
                            break;
                        case ChaseWeaponKind.Volley: layer *= .5 + .5 * Math.Abs(Math.Cos(t * 2 * Math.PI * 36)); break;
                        case ChaseWeaponKind.Sentry: layer += Math.Sin(t * 2 * Math.PI * 410) * .28 * Math.Exp(-t * 13); break;
                        case ChaseWeaponKind.Razor: layer += (noise - mid) * Math.Sin(t * 2 * Math.PI * 95) * Math.Exp(-t * 10); break;
                        case ChaseWeaponKind.Frenzy: layer += mid * 2 * Math.Exp(-Math.Abs(t - .055) * 100); break;
                        case ChaseWeaponKind.Warcry: layer += Math.Sin(t * 2 * Math.PI * 85) * .8 * Math.Exp(-t * 9); break;
                        case ChaseWeaponKind.Cyclone:
                            layer += mid * .8 * Math.Sin(t * 2 * Math.PI * 7) * Math.Exp(-t * 4)
                                + low * 1.4 * Math.Exp(-Math.Abs(t - .24) * 8);
                            break;
                        case ChaseWeaponKind.Magma: layer += Math.Sin(t * 2 * Math.PI * 47) * .85 * Math.Exp(-t * 7); break;
                        case ChaseWeaponKind.BoneLance: layer += (noise - mid) * .7 * Math.Exp(-Math.Abs(t - .035) * 150); break;
                        case ChaseWeaponKind.Graveburst: layer += low * 4 * Math.Exp(-Math.Abs(t - .08) * 28); break;
                        case ChaseWeaponKind.OrbitHammer: layer += Math.Sin(t * 2 * Math.PI * 261.6) * .45 * Math.Exp(-t * 8); break;
                        case ChaseWeaponKind.Inferno: layer *= 1 + .4 * Math.Sin(t * 2 * Math.PI * 17); break;
                        case ChaseWeaponKind.Pactbinder: layer += Math.Sin(t * 2 * Math.PI * 197) * .5 * Math.Exp(-t * 8); break;
                        case ChaseWeaponKind.Abyss: layer += Math.Sin(t * 2 * Math.PI * 39) * .9 * Math.Exp(-t * 7); break;
                        case ChaseWeaponKind.Sanctifier:
                            layer += Math.Sin(t * 2 * Math.PI * 349.23) * .26 * Math.Exp(-t * 4)
                                + (noise - mid) * .4 * Math.Exp(-Math.Abs(t - .09) * 70);
                            break;
                        case ChaseWeaponKind.Winterglass:
                            layer += (Math.Sin(t * 2 * Math.PI * 1889) + Math.Sin(t * 2 * Math.PI * 2801)) * .18
                                * Math.Exp(-Math.Abs(t - .12) * 9) + (noise - low) * .45 * Math.Exp(-Math.Abs(t - .035) * 110);
                            break;
                    }
                }
                // A descending pressure body, short metal receiver modes and a
                // delayed bolt/casing phrase separate power from mere loudness.
                bassPhase += 2 * Math.PI * (43 + kind * 1.3 + 92 * Math.Exp(-t * 32)) / SampleRate;
                double pressure = Math.Sin(bassPhase) * (impact ? .24 : .62) * Math.Exp(-t * (impact ? 13 : 7));
                double mechanism = 0;
                if (!impact)
                {
                    double release = t - (.045 + (kind % 4) * .012);
                    if (release >= 0)
                        mechanism = ((noise - mid) * .5 + Math.Sin(release * 2 * Math.PI * (1387 + kind * 23)) * .18

                            + Math.Sin(release * 2 * Math.PI * 2243) * .12) * Math.Exp(-release * 72);
                    double settle = t - (.16 + (kind % 3) * .025);
                    if (settle >= 0)
                        mechanism += (mid * .65 + Math.Sin(settle * 2 * Math.PI * (311 + kind * 9)) * .16)
                            * Math.Exp(-settle * 43);
                    mechanism += low * .6 * Math.Exp(-t * 4);
                }
                dry[i] = Math.Tanh(body * .95 + layer * (impact ? .5 : 1.1) + pressure + mechanism)
                    * Math.Min(1, t / .0007);
            }
            var samples = new float[frames * 2];
            double peak = 0;
            for (int i = 0; i < frames; i++)
                for (int channel = 0; channel < 2; channel++)
                {
                    // Original asymmetric fortress early reflections, 17-113 ms.
                    // Low-passed later taps suggest stone mass without a global
                    // reverb bus smearing attack timing or consuming more sources.
                    int back = i - (channel == 0 ? 821 : 1193);
                    int second = i - (channel == 0 ? 2351 : 3301);
                    int third = i - (channel == 0 ? 4363 : 5413);
                    double reflected = second >= 2 ? (dry[second] + dry[second - 1] + dry[second - 2]) / 3 : 0;
                    double distant = third >= 3 ? (dry[third] + dry[third - 1] + dry[third - 2] + dry[third - 3]) / 4 : 0;
                    double value = dry[i] + (back >= 0 ? dry[back] * .22 : 0) + reflected * .12 + distant * .055;
                    value *= Math.Min(1, (frames - 1 - i) / (SampleRate * .09));
                    samples[i * 2 + channel] = (float)value;
                    peak = Math.Max(peak, Math.Abs(value));
                }
            float scale = (float)(.82 / Math.Max(.001, peak));
            for (int i = 0; i < samples.Length; i++) samples[i] *= scale;
            return samples;
        }

        static readonly double[] BoneOnsets = { 0, .018, .042 };
        static readonly double[] ReportDurations =
        {
            1.12, .94, .98, 1.08, .92, 1.3, 1.38, 1.46,
            1.02, 1.42, 1.36, 1.32, 1.52, 1.4, 1.34, 1.48
        };

        public void ExportWaveforms(string directory)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ChaseEffects));
            Directory.CreateDirectory(directory);
            using (var manifest = new StreamWriter(Path.Combine(directory, "signatures.csv")))
            {
                manifest.WriteLine("signature,file,played,sampleRate,channels,origin");
                for (int i = 0; i < reports.Length; i++)
                    ExportClip(directory, "attack-" + (ChaseWeaponKind)i, AttackSignature((ChaseWeaponKind)i), reports[i], reportCounts[i], manifest);
                for (int i = 0; i < impacts.Length; i++)
                    ExportClip(directory, "impact-" + i, HitSignature(i), impacts[i], impactCounts[i], manifest);
            }
        }

        public void ExportAudio(string outputDirectory) { ExportWaveforms(outputDirectory); }

        static void ExportClip(string directory, string name, string signature, AudioClip clip, int played, StreamWriter manifest)
        {
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            using (var writer = new BinaryWriter(File.Create(Path.Combine(directory, name + ".wav"))))
            {
                int bytes = samples.Length * 2;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)2);
                writer.Write(SampleRate); writer.Write(SampleRate * 4); writer.Write((short)4); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
                foreach (float sample in samples) writer.Write((short)Math.Round(sample * 32767));
            }
            manifest.WriteLine(signature + "," + name + ".wav," + played + "," + SampleRate + ",2,original-deterministic-synthesis");
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var v in attackVoices) { v.Source.Stop(); v.Remaining = 0; }
            foreach (var v in hitVoices) { v.Source.Stop(); v.Remaining = 0; }
            foreach (var clip in reports) UnityEngine.Object.Destroy(clip);
            foreach (var clip in impacts) UnityEngine.Object.Destroy(clip);
            pendingCount = 0; ActiveFieldCount = 0;
            UnityEngine.Object.Destroy(crystal);
            UnityEngine.Object.Destroy(hitCube);
            UnityEngine.Object.Destroy(material);
            UnityEngine.Object.Destroy(root);
        }
    }
}
