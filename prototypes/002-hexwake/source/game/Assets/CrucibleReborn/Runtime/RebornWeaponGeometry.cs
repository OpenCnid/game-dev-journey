using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.CrucibleReborn
{
    // I's drawing compiler. Profiles are measured in the (forward,height) plane;
    // no prior trial's chassis, primitive assembler or mesh is an input.
    internal sealed class RebornWeaponGeometry
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> indices = new List<int>();

        void Triangle(Vector3 a, Vector3 b, Vector3 c,
            Vector3 na = default(Vector3), Vector3 nb = default(Vector3), Vector3 nc = default(Vector3))
        {
            int n = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            Vector3 normal = Vector3.Cross(b - a, c - a);
            normals.Add(na == Vector3.zero ? normal.normalized : na);
            normals.Add(nb == Vector3.zero ? normal.normalized : nb);
            normals.Add(nc == Vector3.zero ? normal.normalized : nc);
            int axis = Mathf.Abs(normal.x) > Mathf.Abs(normal.y) ?
                (Mathf.Abs(normal.x) > Mathf.Abs(normal.z) ? 0 : 2) :
                (Mathf.Abs(normal.y) > Mathf.Abs(normal.z) ? 1 : 2);
            foreach (Vector3 p in new[] { a, b, c })
                uv.Add((axis == 0 ? new Vector2(p.z, p.y) :
                    axis == 1 ? new Vector2(p.x, p.z) : new Vector2(p.x, p.y)) * 8);
            indices.Add(n); indices.Add(n + 1); indices.Add(n + 2);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Triangle(a, b, c); Triangle(a, c, d);
        }

        static float Cross(Vector2 a, Vector2 b, Vector2 c) =>
            (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

        // Four perimeter rings produce a real bevel, with triangulated concave caps.
        internal void Profile(Matrix4x4 pose, float thickness, params float[] drawing)
        {
            int count = drawing.Length / 2;
            var polygon = new List<Vector2>();
            Vector2 center = Vector2.zero;
            float area = 0;
            for (int i = 0; i < count; i++)
            {
                var p = new Vector2(drawing[i * 2], drawing[i * 2 + 1]);
                polygon.Add(p); center += p;
                int j = (i + 1) % count;
                area += p.x * drawing[j * 2 + 1] - drawing[j * 2] * p.y;
            }
            if (area < 0) polygon.Reverse();
            center /= count;
            var rings = new Vector3[4, count];
            for (int ring = 0; ring < 4; ring++)
            {
                float x = thickness * (ring == 0 ? -.5f : ring == 1 ? -.36f : ring == 2 ? .36f : .5f);
                float inset = ring == 0 || ring == 3 ? .93f : 1;
                for (int i = 0; i < count; i++)
                {
                    Vector2 p = center + (polygon[i] - center) * inset;
                    rings[ring, i] = pose.MultiplyPoint3x4(new Vector3(x, p.y, p.x));
                }
            }
            for (int ring = 0; ring < 3; ring++)
                for (int i = 0; i < count; i++)
                {
                    int j = (i + 1) % count;
                    Quad(rings[ring, i], rings[ring + 1, i], rings[ring + 1, j], rings[ring, j]);
                }
            var remaining = new List<int>();
            for (int i = 0; i < count; i++) remaining.Add(i);
            while (remaining.Count > 2)
            {
                bool found = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a = remaining[(i + remaining.Count - 1) % remaining.Count];
                    int b = remaining[i], c = remaining[(i + 1) % remaining.Count];
                    if (Cross(polygon[a], polygon[b], polygon[c]) <= .0000001f) continue;
                    bool inside = false;
                    foreach (int p in remaining)
                    {
                        if (p == a || p == b || p == c) continue;
                        if (Cross(polygon[a], polygon[b], polygon[p]) >= 0 &&
                            Cross(polygon[b], polygon[c], polygon[p]) >= 0 &&
                            Cross(polygon[c], polygon[a], polygon[p]) >= 0) { inside = true; break; }
                    }
                    if (inside) continue;
                    Triangle(rings[0, a], rings[0, b], rings[0, c]);
                    Triangle(rings[3, c], rings[3, b], rings[3, a]);
                    remaining.RemoveAt(i); found = true; break;
                }
                if (!found) throw new ArgumentException("I profile is not a simple triangulable drawing.");
            }
        }

        // Cross-section travels along z and returns down the inside wall. The
        // aperture is hollow through its entire length, including both annular lips.
        internal void Bore(Matrix4x4 pose, float length, float radius, float bore, float flare = 1)
        {
            const int sides = 32;
            var section = new[] {
                new Vector2(0, radius * .94f), new Vector2(.006f, radius),
                new Vector2(length - .006f, radius * flare), new Vector2(length, radius * flare * .96f),
                new Vector2(length, bore * flare), new Vector2(length - .006f, bore * flare),
                new Vector2(.006f, bore), new Vector2(0, bore)
            };
            for (int r = 0; r < section.Length; r++)
                for (int s = 0; s < sides; s++)
                {
                    int next = (r + 1) % section.Length;
                    float a = s * Mathf.PI * 2 / sides, b = (s + 1) * Mathf.PI * 2 / sides;
                    Vector3 p = new Vector3(Mathf.Cos(a) * section[r].y, Mathf.Sin(a) * section[r].y, section[r].x);
                    Vector3 q = new Vector3(Mathf.Cos(b) * section[r].y, Mathf.Sin(b) * section[r].y, section[r].x);
                    Vector3 u = new Vector3(Mathf.Cos(b) * section[next].y, Mathf.Sin(b) * section[next].y, section[next].x);
                    Vector3 v = new Vector3(Mathf.Cos(a) * section[next].y, Mathf.Sin(a) * section[next].y, section[next].x);
                    Quad(pose.MultiplyPoint3x4(p), pose.MultiplyPoint3x4(q),
                        pose.MultiplyPoint3x4(u), pose.MultiplyPoint3x4(v));
                }
        }

        // Explicit elliptical anatomical sections, rather than scaled cylinders.
        // The frame follows section tangents; radii include knuckles/cloth folds.
        internal void Sections(Matrix4x4 pose, Vector3[] centers, Vector2[] radii, int sides = 16)
        {
            var rings = new Vector3[centers.Length, sides];
            var surfaceNormals = new Vector3[centers.Length, sides];
            for (int r = 0; r < centers.Length; r++)
            {
                Vector3 tangent = centers[Mathf.Min(r + 1, centers.Length - 1)] -
                    centers[Mathf.Max(0, r - 1)];
                Vector3 x = Vector3.Cross(tangent.normalized, Vector3.forward).normalized;
                if (x.sqrMagnitude < .1f) x = Vector3.right;
                Vector3 y = Vector3.Cross(tangent.normalized, x).normalized;
                for (int s = 0; s < sides; s++)
                {
                    float angle = s * Mathf.PI * 2 / sides;
                    rings[r, s] = pose.MultiplyPoint3x4(centers[r] +
                        x * Mathf.Cos(angle) * radii[r].x + y * Mathf.Sin(angle) * radii[r].y);
                    surfaceNormals[r, s] = pose.MultiplyVector(
                        x * Mathf.Cos(angle) / radii[r].x + y * Mathf.Sin(angle) / radii[r].y).normalized;
                }
            }
            for (int r = 0; r < centers.Length - 1; r++)
                for (int s = 0; s < sides; s++)
                {
                    int next = (s + 1) % sides;
                    Triangle(rings[r, s], rings[r, next], rings[r + 1, next],
                        surfaceNormals[r, s], surfaceNormals[r, next], surfaceNormals[r + 1, next]);
                    Triangle(rings[r, s], rings[r + 1, next], rings[r + 1, s],
                        surfaceNormals[r, s], surfaceNormals[r + 1, next], surfaceNormals[r + 1, s]);
                }
            for (int s = 0; s < sides; s++)
            {
                Triangle(pose.MultiplyPoint3x4(centers[0]), rings[0, (s + 1) % sides], rings[0, s]);
                int end = centers.Length - 1;
                Triangle(pose.MultiplyPoint3x4(centers[end]), rings[end, s], rings[end, (s + 1) % sides]);
            }
        }

        internal Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0);
            mesh.SetNormals(normals); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        internal static Transform Group(Transform parent, string name, Vector3 position)
        {
            var result = new GameObject(name).transform;
            result.SetParent(parent, false); result.localPosition = position;
            result.gameObject.layer = 13;
            return result;
        }

        internal static void Render(Transform parent, Mesh mesh, Material material)
        {
            var part = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false); part.layer = 13;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material; renderer.receiveShadows = false;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
