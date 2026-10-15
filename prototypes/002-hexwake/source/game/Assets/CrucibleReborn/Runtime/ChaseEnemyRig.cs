using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    public sealed class ChaseEnemyRig
    {
        public Transform Root { get; private set; }
        public ChaseEnemyKind Kind { get; private set; }
        internal Transform[] Groups;
        internal Vector3[] Rest;
        internal Material Body, Armor, Source, Flash, BoundSource;
        internal RebornAnatomy Anatomy;
        SkinnedMeshRenderer skin;
        Material[] normal, flashing, boundMaterials;
        int visibleState = -1;

        public static Vector3 LocalHeadCenter(ChaseEnemyKind kind) => RebornAnatomy.Head;
        public static float LocalHeadRadius(ChaseEnemyKind kind) => RebornAnatomy.HeadRadius;
        public Vector3 HeadCenter => RebornAnatomy.Head;
        public float HeadRadius => RebornAnatomy.HeadRadius;
        public Vector3 WorldHeadCenter => Groups[1].TransformPoint(HeadCenter - Rest[1]);
        public float WorldHeadRadius => HeadRadius * Root.lossyScale.x;

        internal static ChaseEnemyRig Create(Transform parent, ChaseArt art,
            ChaseEnemyKind kind, Material body, Material armor, Material source,
            Material flash, Material bound)
        {
            if (art.Anatomy[(int)kind] == null) art.Anatomy[(int)kind] = new RebornAnatomy(art, kind);
            var rig = new ChaseEnemyRig { Kind = kind, Body = body, Armor = armor,
                Source = source, Flash = flash, BoundSource = bound, Anatomy = art.Anatomy[(int)kind] };
            rig.Build(parent);
            return rig;
        }

        void Build(Transform parent)
        {
            Root = ChaseArt.Group(parent, "I connected " + Kind, Vector3.zero);
            Root.gameObject.layer = 2;
            Groups = new Transform[6];
            Rest = RebornAnatomy.Pivots;
            for (int i = 0; i < Groups.Length; i++)
                Groups[i] = ChaseArt.Group(Root, "I bone " + i, Rest[i]);
            skin = Root.gameObject.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = Anatomy.Detail;
            skin.bones = Groups;
            skin.rootBone = Root;
            skin.localBounds = new Bounds(new Vector3(0, 1.25f, 0), new Vector3(2, 3, 1.5f));
            normal = new[] { Body, Armor, Source };
            flashing = new[] { Flash, Flash, Flash };
            boundMaterials = new[] { Body, Armor, BoundSource };
            skin.sharedMaterials = normal;
            skin.quality = SkinQuality.Bone2;
        }

        internal ChaseEnemyRig Clone(Transform parent)
        {
            var clone = new ChaseEnemyRig { Kind = Kind, Body = Body, Armor = Armor,
                Source = Source, Flash = Flash, BoundSource = BoundSource, Anatomy = Anatomy };
            clone.Build(parent);
            return clone;
        }

        internal static Matrix4x4 GroupPose(ChaseEnemyKind kind, int group, Vector3 rest,
            float elapsed, float speed, int id, bool bound, bool low)
        {
            return PhasePose(kind, group, rest, elapsed * 6.6f + id * .83f,
                Mathf.Clamp01(speed / 3), bound);
        }

        internal static Matrix4x4 PhasePose(ChaseEnemyKind kind, int group, Vector3 rest,
            float phase, float stride, bool bound)
        {
            float gait = Mathf.Sin(phase) * stride * (bound ? .6f : 1);
            float bob = Mathf.Abs(Mathf.Sin(phase)) * stride * .025f;
            var angle = Vector3.zero;
            if (group >= 2)
                angle.x = gait * ((group & 1) == 0 ? 1 : -1) * (group >= 4 ? 24 : -18);
            if (group == 1) angle.y = Mathf.Sin(phase * .5f) * 3 * stride;
            if (kind == ChaseEnemyKind.Undead && group >= 2 && group < 4)
                angle.z = (group == 2 ? -1 : 1) * 6;
            return Matrix4x4.TRS(rest + Vector3.up * bob, Quaternion.Euler(angle), Vector3.one);
        }

        public void Animate(float elapsed, float speed, int id, bool flashingNow, bool bound, int quality = 2)
        {
            for (int i = 0; i < Groups.Length; i++)
            {
                var pose = GroupPose(Kind, i, Rest[i], elapsed, speed, id, bound, quality == 0);
                Groups[i].localPosition = pose.GetColumn(3);
                Groups[i].localRotation = pose.rotation;
            }
            int state = flashingNow ? 2 : bound ? 1 : 0;
            if (state == visibleState) return;
            visibleState = state;
            skin.sharedMaterials = state == 2 ? flashing : state == 1 ? boundMaterials : normal;
        }
    }
}
