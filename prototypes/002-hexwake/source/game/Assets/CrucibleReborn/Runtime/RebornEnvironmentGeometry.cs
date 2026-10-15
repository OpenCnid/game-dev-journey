using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.CrucibleReborn
{
    // Original revision-I mesh kit. No imported geometry or earlier trial mesh inputs.
    // Builders are construction-only; arena batches own the resulting meshes.
    public sealed class RebornEnvironmentGeometry
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> indices = new List<int>();
        readonly List<Vector2> uv = new List<Vector2>();
        public int TriangleCount => indices.Count / 3;

        public void Face(Vector3 a, Vector3 b, Vector3 c)
        {
            int n = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            foreach (Vector3 p in new[] { a, b, c })
                uv.Add(Mathf.Abs(normal.y) > .65f ? new Vector2(p.x, p.z) :
                    Mathf.Abs(normal.x) > .65f ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y));
            indices.Add(n); indices.Add(n + 1); indices.Add(n + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        { Face(a, b, c); Face(a, c, d); }

        // Convex planar profile, counterclockwise from +Z. Ends are fully closed.
        public void Extrude(Vector2[] profile, float depth, Matrix4x4 matrix)
        {
            Vector3[] front = new Vector3[profile.Length], back = new Vector3[profile.Length];
            for (int i = 0; i < profile.Length; i++)
            {
                front[i] = matrix.MultiplyPoint3x4(new Vector3(profile[i].x, profile[i].y, depth / 2));
                back[i] = matrix.MultiplyPoint3x4(new Vector3(profile[i].x, profile[i].y, -depth / 2));
            }
            for (int i = 1; i < profile.Length - 1; i++)
            { Face(front[0], front[i], front[i + 1]); Face(back[0], back[i + 1], back[i]); }
            for (int i = 0; i < profile.Length; i++)
            {
                int j = (i + 1) % profile.Length;
                Quad(front[i], back[i], back[j], front[j]);
            }
        }

        public void Block(Vector3 center, Vector3 size, Quaternion rotation, float chip = .08f)
        {
            float x = size.x / 2, y = size.y / 2, c = Mathf.Min(chip, Mathf.Min(x, y) * .4f);
            if (c == 0)
            {
                Extrude(new[] { new Vector2(-x, -y), new Vector2(x, -y),
                    new Vector2(x, y), new Vector2(-x, y) }, size.z,
                    Matrix4x4.TRS(center, rotation, Vector3.one));
                return;
            }
            Extrude(new[] { new Vector2(-x + c, -y), new Vector2(x - c, -y),
                new Vector2(x, -y + c), new Vector2(x, y - c), new Vector2(x - c, y),
                new Vector2(-x + c, y), new Vector2(-x, y - c), new Vector2(-x, -y + c) },
                size.z, Matrix4x4.TRS(center, rotation, Vector3.one));
        }

        public void BrokenPier(Vector3 origin, float width, float height, float depth, Quaternion rotation)
        {
            float w = width / 2;
            Extrude(new[] { new Vector2(-w, 0), new Vector2(w, 0),
                new Vector2(w * .84f, height * .72f), new Vector2(w * .32f, height),
                new Vector2(-w * .14f, height * .81f), new Vector2(-w * .76f, height * .91f) },
                depth, Matrix4x4.TRS(origin, rotation, Vector3.one));
        }

        // Equilateral Gothic arch: two distinct circular centers, not an ellipse
        // or a bent box. Radial voussoirs leave real air under the pointed crown.
        public void PointedArch(Vector3 origin, float halfSpan, float spring, float thickness,
            float depth, Quaternion rotation, int stones = 12, bool broken = false)
        {
            Matrix4x4 m = Matrix4x4.TRS(origin, rotation, Vector3.one);
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < stones; i++)
                {
                    if (broken && side == 1 && i >= stones - 3) continue;
                    float a = Mathf.Lerp(side == 0 ? Mathf.PI : 0,
                        side == 0 ? Mathf.PI * 2 / 3 : Mathf.PI / 3, (i + .025f) / stones);
                    float b = Mathf.Lerp(side == 0 ? Mathf.PI : 0,
                        side == 0 ? Mathf.PI * 2 / 3 : Mathf.PI / 3, (i + .975f) / stones);
                    Vector2 center = new Vector2(side == 0 ? halfSpan : -halfSpan, spring);
                    Vector2 pa = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Vector2 pb = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                    Vector2[] profile = side == 0 ?
                        new[] { center + pa * (2 * halfSpan), center + pb * (2 * halfSpan),
                            center + pb * (2 * halfSpan + thickness), center + pa * (2 * halfSpan + thickness) } :
                        new[] { center + pb * (2 * halfSpan), center + pa * (2 * halfSpan),
                            center + pa * (2 * halfSpan + thickness), center + pb * (2 * halfSpan + thickness) };
                    Extrude(profile, depth, m);
                }
        }

        // Closed tube along a polyline. Bone and iron curves have actual volume.
        public void Tube(IList<Vector3> path, IList<float> radii, int sides = 8)
        {
            var rings = new Vector3[path.Count][];
            for (int j = 0; j < path.Count; j++)
            {
                Vector3 tangent = (path[Mathf.Min(j + 1, path.Count - 1)] -
                    path[Mathf.Max(0, j - 1)]).normalized;
                Vector3 axis = Mathf.Abs(tangent.y) > .9f ? Vector3.right : Vector3.up;
                Vector3 u = Vector3.Cross(tangent, axis).normalized;
                Vector3 v = Vector3.Cross(tangent, u);
                rings[j] = new Vector3[sides];
                for (int k = 0; k < sides; k++)
                {
                    float angle = k * Mathf.PI * 2 / sides;
                    rings[j][k] = path[j] + (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * radii[j];
                }
            }
            for (int j = 0; j < path.Count - 1; j++)
                for (int k = 0; k < sides; k++)
                {
                    int n = (k + 1) % sides;
                    Quad(rings[j][k], rings[j][n], rings[j + 1][n], rings[j + 1][k]);
                }
            for (int k = 0; k < sides; k++)
            {
                int n = (k + 1) % sides;
                Face(path[0], rings[0][n], rings[0][k]);
                Face(path[path.Count - 1], rings[path.Count - 1][k], rings[path.Count - 1][n]);
            }
        }

        // Annular shell through an opening. Used for eye sockets, not dark
        // discs pasted on a sealed skull. The deep rim and inner wall are visible.
        public void Socket(Vector3 center, float width, float height, float lip, float depth, Quaternion rotation)
        {
            Vector3[] outer = new Vector3[16], inner = new Vector3[16], rear = new Vector3[16];
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8;
                Vector3 p = new Vector3(Mathf.Cos(a) * width, Mathf.Sin(a) * height, 0);
                outer[i] = center + rotation * (p + new Vector3(Mathf.Cos(a) * lip, Mathf.Sin(a) * lip, 0));
                inner[i] = center + rotation * (p - Vector3.forward * .025f);
                rear[i] = center + rotation * (p * .78f - Vector3.forward * depth);
            }
            for (int i = 0; i < 16; i++)
            {
                int n = (i + 1) % 16;
                Quad(outer[i], outer[n], inner[n], inner[i]);
                Quad(inner[i], inner[n], rear[n], rear[i]);
                Quad(outer[n], outer[i], rear[i], rear[n]);
            }
        }

        public Mesh Mesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ?
                IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.SetUVs(0, uv);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
