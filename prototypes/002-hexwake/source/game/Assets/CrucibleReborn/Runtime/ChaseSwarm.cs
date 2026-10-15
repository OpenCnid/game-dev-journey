using System;
using System.Collections.Generic;
using GameDevHarness.FantasyTrialB;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.CrucibleReborn
{
    public struct ChasePose
    {
        public int Id;
        public ChaseEnemyKind Kind;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Speed;
        // Frozen spawn scale supplied by the controller; Position is feet.
        public float SizeScale;
        public double FlashUntil;
        public bool Bound;
    }

    // Presentation only: the controller retains every independent EnemyMotor,
    // NavMeshAgent, collider and simulation entity, including far instances.
    public sealed class ChaseSwarm : IDisposable
    {
        const int MaximumDetail = 48;
        const int BatchSize = 1023;
        const float CellSize = 64;
        const float AnimatedDistanceSquared = 32 * 32;

        sealed class Slot
        {
            internal ChaseEnemyRig Rig;
            internal int Id;
            internal bool Assigned, Used;
        }

        sealed class Snapshot
        {
            internal Mesh Mesh;
            internal Material Material;
            internal int Group, Phase;
            internal bool FlashOnly, Rigid;
            internal Matrix4x4 Local;
        }

        struct CellKey : IEquatable<CellKey>
        {
            internal int X, Z, Kind, State, Phase;
            internal bool Rigid;
            public bool Equals(CellKey other)
                => X == other.X && Z == other.Z && Kind == other.Kind && State == other.State &&
                    Rigid == other.Rigid && Phase == other.Phase;
            public override bool Equals(object obj) => obj is CellKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked { return (((((X * 397) ^ Z) * 397 ^ Kind) * 397 ^ State) * 397 ^ Phase) * 397 ^ Rigid.GetHashCode(); }
            }
        }

        readonly Transform root;
        readonly ChaseArt art = new ChaseArt();
        readonly ChaseEnemyRig[] templates = new ChaseEnemyRig[3];
        readonly List<Snapshot>[] snapshots = new List<Snapshot>[3];
        readonly List<Mesh> snapshotMeshes = new List<Mesh>();
        readonly Slot[] slots = new Slot[MaximumDetail * 3];
        readonly int[] nearest = new int[MaximumDetail];
        readonly float[] distances = new float[MaximumDetail];
        readonly int[] assignments = new int[MaximumDetail];
        readonly Dictionary<CellKey, List<int>> cells = new Dictionary<CellKey, List<int>>();
        readonly Matrix4x4[] matrices = new Matrix4x4[BatchSize];
        Matrix4x4[] poseMatrices = new Matrix4x4[1200];
        bool[] detailedPoses = new bool[1200];
        int quality = 2;
        bool disposed;

        public int DetailedCount { get; private set; }
        public int InstancedCount { get; private set; }
        public int InstancedDrawCount { get; private set; }
        public int DetailedLimit => quality == 0 ? 16 : quality == 1 ? 32 : 48;
        public int DetailedCapacity { get; private set; }
        // Three normal plus flash and bound-source materials per enemy kind.
        public int SnapshotMaterialCount => 15;
        public int Quality => quality;
        public int FlashingCount { get; private set; }
        public int BoundCount { get; private set; }
        public int RigidInstancedCount { get; private set; }

        ChaseSwarm(Transform parent, ArtPalette palette)
        {
            root = ChaseArt.Group(parent, "I connected anatomy swarm", Vector3.zero);
            var ivory = palette.Make(new Color(.55f, .49f, .36f), 0, .16f);
            var basalt = palette.Make(new Color(.28f, .055f, .035f), 0, .22f);
            var royal = palette.Make(new Color(.20f, .13f, .09f), 0, .16f);
            var boneArmor = palette.Make(new Color(.24f, .19f, .12f), 0, .16f);
            var obsidian = palette.Make(new Color(.075f, .045f, .025f), .15f, .28f);
            var plate = palette.Make(new Color(.25f, .28f, .31f), .8f, .42f);
            Material[] bodies = { ivory, basalt, royal }, armor = { boneArmor, obsidian, plate };
            ChaseMaterials.Weather(ivory, 2);
            ChaseMaterials.Weather(basalt, 3);
            ChaseMaterials.Weather(royal, 1);
            foreach (var worn in armor) ChaseMaterials.Weather(worn, 1);
            Color[] colors = { new Color(.35f, .86f, .68f), new Color(1, .27f, .095f),
                new Color(.75f, .45f, 1) };
            for (int i = 0; i < 3; i++)
            {
                var source = palette.Make(colors[i], 0, .2f, .7f);
                var flash = palette.Make(Color.Lerp(colors[i], Color.white, .88f), 0, .1f, .65f);
                var bound = palette.Make(new Color(.15f, 1, .8f), 0, .2f, .85f);
                templates[i] = ChaseEnemyRig.Create(root, art, (ChaseEnemyKind)i,
                    bodies[i], armor[i], source, flash, bound);
                BuildSnapshots(i);
                templates[i].Root.gameObject.SetActive(false);
            }
            for (int kind = 0; kind < 3; kind++)
                for (int i = 0; i < MaximumDetail; i++)
                {
                    var slot = CreateSlot((ChaseEnemyKind)kind);
                    slot.Rig.Root.gameObject.SetActive(false);
                    slots[kind * MaximumDetail + i] = slot;
                    DetailedCapacity++;
                }
        }

        public static ChaseSwarm Create(Transform parent, ArtPalette palette) => new ChaseSwarm(parent, palette);

        void BuildSnapshots(int kind)
        {
            var rig = templates[kind];
            snapshots[kind] = new List<Snapshot>();
            var low = rig.Anatomy.Low;
            var vertices = low.vertices;
            var weights = low.boneWeights;
            var normals = low.normals;
            var uv = low.uv;
            var materials = new[] { rig.Body, rig.Armor, rig.Source };
            foreach (var material in materials) material.enableInstancing = true;
            rig.Flash.enableInstancing = rig.BoundSource.enableInstancing = true;
            // Eight cached skinned poses plus a rest silhouette. CPU skinning is
            // startup-only, never per actor/frame. All LODs retain head/hands/feet.
            for (int phase = -1; phase < 8; phase++)
            {
                var bones = new Matrix4x4[6];
                for (int bone = 0; bone < 6; bone++)
                    bones[bone] = phase < 0 ? Matrix4x4.identity :
                        ChaseEnemyRig.PhasePose(rig.Kind, bone, rig.Rest[bone],
                            phase * Mathf.PI * .25f, 1, false) * low.bindposes[bone];
                for (int material = 0; material < 3; material++)
                {
                    var triangles = low.GetTriangles(material);
                    if (triangles.Length == 0) continue;
                    var map = new Dictionary<int, int>();
                    var v = new List<Vector3>();
                    var n = new List<Vector3>();
                    var tex = new List<Vector2>();
                    for (int i = 0; i < triangles.Length; i++)
                    {
                        int source = triangles[i];
                        if (!map.TryGetValue(source, out int target))
                        {
                            target = v.Count;
                            map.Add(source, target);
                            var w = weights[source];
                            v.Add(bones[w.boneIndex0].MultiplyPoint3x4(vertices[source]) * w.weight0 +
                                bones[w.boneIndex1].MultiplyPoint3x4(vertices[source]) * w.weight1);
                            n.Add((bones[w.boneIndex0].MultiplyVector(normals[source]) * w.weight0 +
                                bones[w.boneIndex1].MultiplyVector(normals[source]) * w.weight1).normalized);
                            tex.Add(uv[source]);
                        }
                        triangles[i] = target;
                    }
                    var mesh = new Mesh { name = "I cached silhouette " + rig.Kind + " phase " + phase +
                        " material " + material, indexFormat = IndexFormat.UInt32 };
                    mesh.SetVertices(v);
                    mesh.SetNormals(n);
                    mesh.SetUVs(0, tex);
                    mesh.SetTriangles(triangles, 0);
                    mesh.RecalculateBounds();
                    mesh.RecalculateTangents();
                    snapshotMeshes.Add(mesh);
                    snapshots[kind].Add(new Snapshot { Mesh = mesh, Material = materials[material],
                        Group = 0, Phase = phase, Rigid = phase < 0, Local = Matrix4x4.identity });
                    snapshots[kind].Add(new Snapshot { Mesh = mesh, Material = rig.Flash,
                        Group = 0, Phase = phase, Rigid = phase < 0, FlashOnly = true, Local = Matrix4x4.identity });
                }
            }
        }

        public void SetQuality(int preset)
        {
            quality = Mathf.Clamp(preset, 0, 2);
            foreach (var slot in slots)
                if (slot != null)
                    foreach (var renderer in slot.Rig.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        renderer.shadowCastingMode = quality == 2 ? ShadowCastingMode.On : ShadowCastingMode.Off;
                        renderer.receiveShadows = quality > 0;
                    }
        }

        Slot CreateSlot(ChaseEnemyKind kind)
        {
            var rig = templates[(int)kind].Clone(root);
            foreach (var renderer in rig.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.shadowCastingMode = quality == 2 ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = quality > 0;
            }
            return new Slot { Rig = rig };
        }

        public void Render(List<ChasePose> poses, Vector3 playerPosition, float elapsed)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ChaseSwarm));
            if (poses.Count > detailedPoses.Length)
            {
                int capacity = Mathf.NextPowerOfTwo(poses.Count);
                detailedPoses = new bool[capacity];
                poseMatrices = new Matrix4x4[capacity];
            }
            Array.Clear(detailedPoses, 0, poses.Count);
            int count = 0, limit = DetailedLimit;
            for (int i = 0; i < poses.Count; i++)
            {
                float distance = (poses[i].Position - playerPosition).sqrMagnitude;
                int at = count;
                while (at > 0 && distance < distances[at - 1]) at--;
                if (at >= limit) continue;
                for (int j = Mathf.Min(count, limit - 1); j > at; j--)
                {
                    nearest[j] = nearest[j - 1];
                    distances[j] = distances[j - 1];
                }
                nearest[at] = i;
                distances[at] = distance;
                count = Mathf.Min(count + 1, limit);
            }
            foreach (var slot in slots) if (slot != null) slot.Used = false;
            // Preserve existing ID assignments before choosing recyclable slots.
            for (int i = 0; i < count; i++)
            {
                assignments[i] = -1;
                var pose = poses[nearest[i]];
                detailedPoses[nearest[i]] = true;
                for (int j = 0; j < slots.Length; j++)
                    if (slots[j] != null && slots[j].Assigned && slots[j].Id == pose.Id &&
                        slots[j].Rig.Kind == pose.Kind)
                    {
                        assignments[i] = j;
                        slots[j].Used = true;
                        break;
                    }
            }
            for (int i = 0; i < count; i++)
            {
                var pose = poses[nearest[i]];
                int index = assignments[i];
                if (index < 0)
                {
                    // Favor same-kind idle slots to avoid needless clone churn.
                    for (int j = 0; j < slots.Length; j++)
                        if (slots[j] != null && !slots[j].Used && slots[j].Rig.Kind == pose.Kind)
                        { index = j; break; }
                }
                var slot = slots[index];
                slot.Id = pose.Id;
                slot.Used = slot.Assigned = true;
                if (!slot.Rig.Root.gameObject.activeSelf) slot.Rig.Root.gameObject.SetActive(true);
                slot.Rig.Root.SetPositionAndRotation(pose.Position, pose.Rotation);
                slot.Rig.Root.localScale = Vector3.one * pose.SizeScale;
                slot.Rig.Animate(elapsed, pose.Speed, pose.Id, pose.FlashUntil > elapsed, pose.Bound, quality);
            }
            foreach (var slot in slots)
                if (slot != null && !slot.Used)
                {
                    slot.Assigned = false;
                    if (slot.Rig.Root.gameObject.activeSelf) slot.Rig.Root.gameObject.SetActive(false);
                }
            foreach (var cell in cells.Values) cell.Clear();
            DetailedCount = count;
            InstancedCount = InstancedDrawCount = FlashingCount = BoundCount = RigidInstancedCount = 0;
            for (int i = 0; i < poses.Count; i++)
            {
                var pose = poses[i];
                bool flash = pose.FlashUntil > elapsed;
                if (flash) FlashingCount++;
                if (pose.Bound) BoundCount++;
                if (detailedPoses[i]) continue;
                var rootPose = Matrix4x4.TRS(pose.Position, pose.Rotation, Vector3.one * pose.SizeScale);
                bool rigid = (pose.Position - playerPosition).sqrMagnitude > AnimatedDistanceSquared;
                int phase = rigid || pose.Speed < .05f ? -1 :
                    Mathf.FloorToInt(Mathf.Repeat(elapsed * 6.6f + pose.Id * .83f, Mathf.PI * 2) / (Mathf.PI * .25f));
                rigid = phase < 0;
                poseMatrices[i] = rootPose;
                if (rigid) RigidInstancedCount++;
                var key = new CellKey { X = Mathf.FloorToInt(pose.Position.x / CellSize),
                    Z = Mathf.FloorToInt(pose.Position.z / CellSize), Kind = (int)pose.Kind,
                    State = flash ? 2 : pose.Bound ? 1 : 0, Rigid = rigid, Phase = phase };
                if (!cells.TryGetValue(key, out var list))
                    cells.Add(key, list = new List<int>(64));
                list.Add(i);
                InstancedCount++;
            }
            foreach (var cell in cells)
            {
                var list = cell.Value;
                if (list.Count == 0) continue;
                var template = templates[cell.Key.Kind];
                foreach (var part in snapshots[cell.Key.Kind])
                {
                    if (part.Phase != cell.Key.Phase || part.Rigid != cell.Key.Rigid ||
                        part.FlashOnly != (cell.Key.State == 2)) continue;
                    var material = cell.Key.State == 1 && part.Material == template.Source
                        ? template.BoundSource : part.Material;
                    bool transformed = part.Local != Matrix4x4.identity;
                    for (int start = 0; start < list.Count; start += BatchSize)
                    {
                        int batchCount = Mathf.Min(BatchSize, list.Count - start);
                        for (int i = 0; i < batchCount; i++)
                        {
                            var poseMatrix = poseMatrices[list[start + i]];
                            matrices[i] = transformed ? poseMatrix * part.Local : poseMatrix;
                        }
                        Graphics.DrawMeshInstanced(part.Mesh, 0, material, matrices, batchCount,
                            null, ShadowCastingMode.Off, quality > 0, 2, null, LightProbeUsage.Off);
                        InstancedDrawCount++;
                    }
                }
            }
        }

        // Owner also removes this ID from its next pose list. Submitted far draws
        // last only the current frame; no persistent far object or combat state exists.
        public void Remove(int id)
        {
            foreach (var slot in slots)
                if (slot != null && slot.Assigned && slot.Id == id)
                {
                    slot.Assigned = slot.Used = false;
                    slot.Rig.Root.gameObject.SetActive(false);
                    DetailedCount = Mathf.Max(0, DetailedCount - 1);
                }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            root.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(root.gameObject);
            foreach (var mesh in snapshotMeshes) UnityEngine.Object.Destroy(mesh);
            snapshotMeshes.Clear();
            art.Dispose();
            cells.Clear();
            DetailedCount = InstancedCount = InstancedDrawCount = DetailedCapacity = FlashingCount = BoundCount = RigidInstancedCount = 0;
        }
    }
}
