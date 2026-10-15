using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    /// <summary>
    /// Static layer-8/9 arena visibility. Rebuild after changing, enabling, moving
    /// or destroying its colliders, or changing Physics.queriesHitBackfaces.
    /// All solid colliders relevant to these queries must belong to arenaRoot.
    /// </summary>
    public sealed class ChaseVisibility
    {
        // Keep PhysX's cooked triangle BVHs as the exact narrow phase. In
        // particular, reconstructing triangles from sharedMesh would duplicate
        // cooking, backface, initial-overlap and endpoint rounding rules.
        // This immutable top-level BVH removes the global scene query (and its
        // dynamic actor population) without approximating mesh or box cover.
        readonly Node[] nodes;
        readonly Solid[] solids;
        readonly bool hitBackfaces;

        struct Solid
        {
            public Collider Collider;
            public Vector3 Min, Max, Center;
            public int Order;
        }

        struct Node
        {
            public Vector3 Min, Max;
            public int Solid, Escape;
        }

        sealed class CenterComparer : IComparer<Solid>
        {
            readonly int axis;
            public CenterComparer(int axis) { this.axis = axis; }
            public int Compare(Solid a, Solid b)
            {
                int comparison = a.Center[axis].CompareTo(b.Center[axis]);
                return comparison != 0 ? comparison : a.Order.CompareTo(b.Order);
            }
        }

        static readonly IComparer<Solid>[] Comparers =
        {
            new CenterComparer(0), new CenterComparer(1), new CenterComparer(2)
        };

        public ChaseVisibility(Transform arenaRoot)
        {
            if (arenaRoot == null) throw new ArgumentNullException(nameof(arenaRoot));
            hitBackfaces = Physics.queriesHitBackfaces;
            // Arena construction sets transforms immediately before the index
            // is created. Capture the physics bounds, not stale pre-sync bounds.
            Physics.SyncTransforms();
            var collected = new List<Solid>();
            foreach (var collider in arenaRoot.GetComponentsInChildren<Collider>())
            {
                int layer = collider.gameObject.layer;
                if ((layer != 8 && layer != 9) || !collider.enabled ||
                    !collider.gameObject.activeInHierarchy || collider.isTrigger) continue;
                var mesh = collider as MeshCollider;
                if (!(collider is BoxCollider) && (mesh == null || mesh.convex))
                    throw new NotSupportedException("ChaseVisibility requires a static BoxCollider or " +
                        "non-convex MeshCollider; found " + collider.GetType().Name + " on " + collider.name);
                // A mesh-less MeshCollider has no shape in the physics scene.
                if (mesh != null && mesh.sharedMesh == null) continue;
                Bounds bounds = collider.bounds;
                Vector3 min = bounds.min, max = bounds.max;
                // Conservative broad-phase padding covers float ray/bounds
                // rounding only. The native narrow phase decides every hit:
                // padding cannot turn an open path into blocked cover.
                float magnitude = Mathf.Max(Mathf.Max(Mathf.Abs(min.x), Mathf.Abs(max.x)),
                    Mathf.Max(Mathf.Max(Mathf.Abs(min.y), Mathf.Abs(max.y)),
                        Mathf.Max(Mathf.Abs(min.z), Mathf.Abs(max.z))));
                Vector3 padding = Vector3.one * (0.00001f + magnitude * 0.000001f);
                collected.Add(new Solid { Collider = collider, Min = min - padding,
                    Max = max + padding, Center = bounds.center, Order = collected.Count });
            }
            solids = collected.ToArray();
            nodes = new Node[solids.Length == 0 ? 0 : solids.Length * 2 - 1];
            int next = 0;
            if (solids.Length != 0) Build(0, solids.Length, ref next);
        }

        int Build(int first, int count, ref int next)
        {
            int index = next++;
            Vector3 min = solids[first].Min, max = solids[first].Max;
            Vector3 centerMin = solids[first].Center, centerMax = centerMin;
            for (int i = first + 1; i < first + count; i++)
            {
                min = Vector3.Min(min, solids[i].Min);
                max = Vector3.Max(max, solids[i].Max);
                centerMin = Vector3.Min(centerMin, solids[i].Center);
                centerMax = Vector3.Max(centerMax, solids[i].Center);
            }
            var node = new Node { Min = min, Max = max, Solid = -1 };
            if (count == 1) node.Solid = first;
            else
            {
                Vector3 extent = centerMax - centerMin;
                int axis = extent.y > extent.x ? 1 : 0;
                if (extent.z > extent[axis]) axis = 2;
                Array.Sort(solids, first, count, Comparers[axis]);
                int left = count / 2;
                Build(first, left, ref next);
                Build(first + left, count - left, ref next);
            }
            node.Escape = next;
            nodes[index] = node;
            return index;
        }

        public bool Clear(Vector3 from, Vector3 to)
        {
            if (Physics.queriesHitBackfaces != hitBackfaces)
                throw new InvalidOperationException(
                    "Physics.queriesHitBackfaces changed; rebuild ChaseVisibility.");
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            // Match PhysicsScene.Raycast's zero-length guard, not Vector3's
            // much larger normalization epsilon.
            if (distance <= float.Epsilon || nodes.Length == 0) return true;
            var ray = new Ray(from, delta / distance);
            int index = 0;
            while (index < nodes.Length)
            {
                Node node = nodes[index];
                if (!Overlaps(from, delta, node.Min, node.Max))
                {
                    index = node.Escape;
                    continue;
                }
                if (node.Solid >= 0 &&
                    solids[node.Solid].Collider.Raycast(ray, out _, distance)) return false;
                index++;
            }
            return true;
        }

        static bool Overlaps(Vector3 from, Vector3 delta, Vector3 min, Vector3 max)
        {
            double enter = 0, exit = 1;
            return Slab(from.x, delta.x, min.x, max.x, ref enter, ref exit) &&
                Slab(from.y, delta.y, min.y, max.y, ref enter, ref exit) &&
                Slab(from.z, delta.z, min.z, max.z, ref enter, ref exit);
        }

        static bool Slab(float origin, float delta, float min, float max,
            ref double enter, ref double exit)
        {
            if (delta == 0) return origin >= min && origin <= max;
            double a = ((double)min - origin) / delta;
            double b = ((double)max - origin) / delta;
            if (a > b) { double swap = a; a = b; b = swap; }
            if (a > enter) enter = a;
            if (b < exit) exit = b;
            return enter <= exit;
        }
    }
}
