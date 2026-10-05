using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Journey
{
    // Flat camera-facing XP carriers, with fixed object ownership rather than one object per pickup.
    public sealed class XpPresentation : IDisposable
    {
        const int BatchSize = 250;
        readonly Mesh disc;
        readonly Material[] materials = new Material[3];
        readonly Matrix4x4[][] instances = new Matrix4x4[3][];
        readonly int[] counts = new int[3];
        readonly Vector3[] circle = new Vector3[64];
        readonly LineRenderer range;
        public int SubmittedCarriers { get; private set; }
        public XpPresentation(Transform parent)
        {
            const int sides = 20;
            var vertices = new Vector3[sides + 1]; var triangles = new int[sides * 3];
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                triangles[i * 3] = 0; triangles[i * 3 + 1] = (i + 1) % sides + 1; triangles[i * 3 + 2] = i + 1;
            }
            disc = new Mesh { name = "Flat XP disc", vertices = vertices, triangles = triangles }; disc.RecalculateBounds(); disc.RecalculateNormals();
            var colors = new[] { new Color(.15f, .65f, 1), new Color(.1f, 1, .85f), new Color(1, .8f, .2f) };
            for (int i = 0; i < 3; i++)
            {
                materials[i] = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "XP value " + i, enableInstancing = true };
                materials[i].SetColor("_BaseColor", colors[i]); instances[i] = new Matrix4x4[128];
            }
            range = new GameObject("XP pickup radius").AddComponent<LineRenderer>(); range.transform.SetParent(parent);
            range.sharedMaterial = materials[0]; range.loop = true; range.positionCount = circle.Length;
            range.startWidth = range.endWidth = .035f; range.useWorldSpace = true; range.enabled = false;
        }
        public void Draw(OrbReplica replica, Camera camera, Vector3 collector, bool showRange)
        {
            SubmittedCarriers = 0; Array.Clear(counts, 0, counts.Length);
            Quaternion facing = camera.transform.rotation;
            foreach (XpOrb orb in replica.Orbs.Values)
            {
                int group = orb.Value == 1 ? 0 : orb.Value < 5 ? 1 : 2;
                if (counts[group] == instances[group].Length) Array.Resize(ref instances[group], instances[group].Length * 2);
                float size = group == 0 ? .18f : group == 1 ? .25f : .34f;
                instances[group][counts[group]++] = Matrix4x4.TRS(orb.Position + Vector3.up * .22f, facing, Vector3.one * size);
                SubmittedCarriers++;
            }
            for (int group = 0; group < 3; group++)
            {
                var parameters = new RenderParams(materials[group]) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false,
                    worldBounds = new Bounds(Vector3.zero, Vector3.one * 160) };
                for (int offset = 0; offset < counts[group]; offset += BatchSize)
                    Graphics.RenderMeshInstanced(parameters, disc, 0, instances[group], Mathf.Min(BatchSize, counts[group] - offset), offset);
            }
            range.enabled = showRange;
            if (showRange)
            {
                for (int i = 0; i < circle.Length; i++)
                {
                    float angle = i * Mathf.PI * 2 / circle.Length;
                    circle[i] = collector + new Vector3(Mathf.Cos(angle) * EarnedGrowth.PickupRadius, .045f, Mathf.Sin(angle) * EarnedGrowth.PickupRadius);
                }
                range.SetPositions(circle);
            }
        }
        public void Dispose()
        {
            if (range != null) UnityEngine.Object.Destroy(range.gameObject);
            foreach (var material in materials) if (material != null) UnityEngine.Object.Destroy(material);
            if (disc != null) UnityEngine.Object.Destroy(disc);
        }
    }
}
