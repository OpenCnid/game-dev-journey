using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.CrucibleReborn
{
    // Original I construction: a union of tapered anatomical solid fields,
    // constructive facial voids, welded tetrahedral extraction and continuous
    // skin weights. No imported geometry or H profile/assembly inputs.
    internal sealed class RebornAnatomy
    {
        struct Solid
        {
            internal Vector3 A, B, Radius;
            internal float End;
            internal int Material;
        }

        internal readonly Mesh Detail, Low;
        internal static readonly Vector3 Head = new Vector3(0, 2.02f, .015f);
        internal const float HeadRadius = .17f;
        internal static readonly Vector3[] Pivots =
        {
            Vector3.zero, new Vector3(0, 1.84f, 0),
            new Vector3(-.34f, 1.65f, 0), new Vector3(.34f, 1.65f, 0),
            new Vector3(-.17f, .99f, 0), new Vector3(.17f, .99f, 0)
        };
        readonly List<Solid> solids = new List<Solid>();
        readonly ChaseEnemyKind kind;
        float resolvedTubeRadius;
        static readonly int[,] Tetrahedra =
        {
            {0, 1, 3, 7}, {0, 3, 2, 7}, {0, 2, 6, 7},
            {0, 6, 4, 7}, {0, 4, 5, 7}, {0, 5, 1, 7}
        };
        static readonly int[,] Edges = { {0,1}, {0,2}, {0,3}, {1,2}, {1,3}, {2,3} };

        internal RebornAnatomy(ChaseArt owner, ChaseEnemyKind enemyKind)
        {
            kind = enemyKind;
            Sculpt();
            Detail = owner.Own(Extract(.032f));
            Low = owner.Own(Extract(.055f));
        }

        void Oval(Vector3 center, Vector3 radii, int material = 0)
            => solids.Add(new Solid { A = center, B = center, Radius = radii, End = 1, Material = material });

        void Tendon(Vector3 from, Vector3 to, float radius, float taper = 1, int material = 0)
            => solids.Add(new Solid { A = from, B = to, Radius = Vector3.one * radius,
                End = taper, Material = material });

        void Sculpt()
        {
            bool demon = kind == ChaseEnemyKind.Demon;
            bool elite = kind == ChaseEnemyKind.Elite;
            // Sternum, rib cage, waist and pelvic bowl form a single trunk.
            Oval(new Vector3(0, 1.48f, -.025f), new Vector3(elite ? .36f : .29f, .29f, .19f));
            Oval(new Vector3(0, 1.20f, 0), new Vector3(.19f, .26f, .135f));
            Oval(new Vector3(0, .99f, -.025f), new Vector3(.255f, .16f, .17f));
            Tendon(new Vector3(0, 1.63f, -.04f), new Vector3(0, 1.92f, 0), .115f);
            Oval(Head, new Vector3(.18f, .225f, .175f));
            // Distinct cheek arches, jaw hinge, chin, brow and nose bridge.
            Oval(new Vector3(0, 1.91f, .085f), new Vector3(.147f, .105f, .13f));
            Oval(new Vector3(0, 1.84f, .12f), new Vector3(.10f, .05f, .08f));
            Tendon(new Vector3(0, 2.075f, .145f), new Vector3(0, 1.97f, .205f), .037f, .7f);
            for (int side = -1; side <= 1; side += 2)
            {
                Oval(new Vector3(side * .12f, 1.975f, .12f), new Vector3(.063f, .053f, .075f));
                Tendon(new Vector3(side * .028f, 2.085f, .145f),
                    new Vector3(side * .15f, 2.065f, .12f), .041f, .8f);
                float s = side;
                var shoulder = new Vector3(s * .34f, 1.65f, 0);
                var elbow = new Vector3(s * .47f, 1.32f, .025f);
                var wrist = new Vector3(s * .54f, 1.06f, .105f);
                Tendon(new Vector3(s * .18f, 1.6f, 0), shoulder, .135f);
                Tendon(shoulder, elbow, demon ? .12f : .10f, .7f);
                Tendon(elbow, wrist, .087f, .65f);
                Oval(wrist + new Vector3(s * .015f, -.065f, .035f), new Vector3(.05f, .10f, .13f));
                // Inward-facing palm: fingers spread in Z, with curled tips in X.
                // Four hooked fingers and opposed thumb remain joined to palm.
                for (int finger = 0; finger < 4; finger++)
                {
                    float z = .03f + finger * .074f;
                    var knuckle = new Vector3(s * (.535f + finger * .013f), .995f, z);
                    var tip = new Vector3(s * (.57f + finger * .013f), .84f + Mathf.Abs(finger - 1.5f) * .02f, z + .018f);
                    Tendon(wrist + new Vector3(0, -.025f, .005f), knuckle, .044f, .9f);
                    Tendon(knuckle, tip, .04f, .65f);
                }
                Tendon(wrist, wrist + new Vector3(-s * .09f, -.10f, .10f), .043f, .7f);
                var hip = new Vector3(s * .17f, .99f, 0);
                var knee = new Vector3(s * .205f, .57f, demon ? .11f : .015f);
                var ankle = new Vector3(s * .215f, demon ? .23f : .12f, demon ? -.15f : .015f);
                Tendon(hip, knee, .145f, .7f);
                Tendon(knee, ankle, .105f, .65f);
                var toe = new Vector3(s * .215f, .075f, .16f);
                Tendon(ankle, toe, .082f, 1.1f);
                Oval(toe, new Vector3(.115f, .073f, .155f));
                if (!demon)
                    for (int rib = 0; rib < 5; rib++)
                        Tendon(new Vector3(s * .035f, 1.63f - rib * .065f, .145f),
                            new Vector3(s * (.23f - rib * .015f), 1.56f - rib * .065f, .105f), .033f, 1);
                if (demon)
                {
                    var horn = new Vector3(s * .14f, 2.15f, -.02f);
                    var bend = new Vector3(s * .26f, 2.36f, -.12f);
                    Tendon(horn, bend, .08f, .55f, 1);
                    Tendon(bend, new Vector3(s * .19f, 2.49f, -.19f), .044f, .05f, 1);
                    Tendon(new Vector3(s * .26f, 1.57f, -.12f),
                        new Vector3(s * .40f, 1.78f, -.14f), .073f, .1f, 1);
                }
                if (elite)
                {
                    // Plate has actual raised volume and bevels, fused to the
                    // underlying body; no floating armor primitives.
                    Oval(shoulder, new Vector3(.18f, .145f, .19f), 1);
                    Oval(new Vector3(s * .14f, 1.49f, .145f), new Vector3(.17f, .235f, .10f), 1);
                    Tendon(elbow, wrist, .101f, .75f, 1);
                    Tendon(knee + Vector3.forward * .04f, ankle, .115f, .65f, 1);
                    Tendon(new Vector3(s * .16f, 2.13f, -.06f),
                        new Vector3(s * .145f, 1.99f, -.13f), .07f, 1, 1);
                }
            }
        }

        static float Distance(Solid solid, Vector3 point, float minimumTubeRadius = 0)
        {
            var axis = solid.B - solid.A;
            float t = axis.sqrMagnitude < .000001f ? 0 :
                Mathf.Clamp01(Vector3.Dot(point - solid.A, axis) / axis.sqrMagnitude);
            var radius = solid.Radius * Mathf.Lerp(1, solid.End, t);
            if (axis.sqrMagnitude >= .000001f)
                radius = Vector3.Max(radius, Vector3.one * minimumTubeRadius);
            var delta = point - Vector3.Lerp(solid.A, solid.B, t);
            return (new Vector3(delta.x / radius.x, delta.y / radius.y, delta.z / radius.z).magnitude - 1)
                * Mathf.Min(radius.x, Mathf.Min(radius.y, radius.z));
        }

        float Field(Vector3 point)
        {
            float value = 10;
            foreach (var solid in solids)
            {
                float d = Distance(solid, point, resolvedTubeRadius);
                const float blend = .025f;
                float h = Mathf.Clamp01(.5f + .5f * (d - value) / blend);
                value = Mathf.Lerp(d, value, h) - blend * h * (1 - h);
            }
            // True cut-in eye sockets, nasal aperture and tooth-separated mouth.
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = new Solid { A = new Vector3(side * .079f, 2.025f, .174f),
                    B = new Vector3(side * .079f, 2.025f, .174f), Radius = new Vector3(.053f, .038f, .06f), End = 1 };
                value = Mathf.Max(value, -Distance(eye, point));
            }
            var nose = new Solid { A = new Vector3(0, 1.956f, .20f), B = new Vector3(0, 1.956f, .20f),
                Radius = new Vector3(.024f, .038f, .044f), End = 1 };
            value = Mathf.Max(value, -Distance(nose, point));
            if (point.z > .17f && Mathf.Abs(point.x) < .115f && Mathf.Abs(point.y - 1.902f) < .018f)
            {
                float tooth = Mathf.Abs(Mathf.Repeat(point.x + .12f, .034f) - .017f);
                value = Mathf.Max(value, Mathf.Min(.018f - Mathf.Abs(point.y - 1.902f), .010f - tooth));
            }
            return value;
        }

        int MaterialAt(Vector3 point)
        {
            // Recessed socket backs retain a source glow without a floating eye.
            if (Mathf.Abs(point.y - 2.025f) < .034f && point.z > .105f && point.z < .14f &&
                Mathf.Abs(Mathf.Abs(point.x) - .079f) < .04f) return 2;
            float nearest = 10;
            int material = 0;
            foreach (var solid in solids)
            {
                float d = Distance(solid, point);
                if (d < nearest) { nearest = d; material = solid.Material; }
            }
            return material;
        }

        internal static BoneWeight Weight(Vector3 point)
        {
            int bone = 0;
            float weight = 0;
            if (point.y > 1.78f && Mathf.Abs(point.x) < .31f)
            { bone = 1; weight = Mathf.SmoothStep(0, 1, (point.y - 1.78f) / .13f); }
            else if (Mathf.Abs(point.x) > .24f && point.y > .80f)
            { bone = point.x < 0 ? 2 : 3; weight = Mathf.SmoothStep(0, 1, (Mathf.Abs(point.x) - .24f) / .16f); }
            else if (point.y < 1.08f)
            { bone = point.x < 0 ? 4 : 5; weight = Mathf.SmoothStep(0, 1, (1.08f - point.y) / .21f); }
            return new BoneWeight { boneIndex0 = bone, weight0 = weight, boneIndex1 = 0, weight1 = 1 - weight };
        }

        Mesh Extract(float spacing)
        {
            // A taper thinner than the grid's half diagonal can become isolated
            // terminal islands. Keep tubes resolvable at each LOD rather than
            // removing disconnected anatomy after extracting it.
            resolvedTubeRadius = spacing * .87f;
            var min = new Vector3(-.85f, -.08f, -.4f);
            int nx = Mathf.CeilToInt(1.7f / spacing) + 1;
            int ny = Mathf.CeilToInt(2.65f / spacing) + 1;
            int nz = Mathf.CeilToInt(.95f / spacing) + 1;
            var positions = new Vector3[nx * ny * nz];
            var values = new float[positions.Length];
            for (int z = 0; z < nz; z++)
                for (int y = 0; y < ny; y++)
                    for (int x = 0; x < nx; x++)
                    {
                        int i = x + nx * (y + ny * z);
                        positions[i] = min + new Vector3(x, y, z) * spacing;
                        values[i] = Field(positions[i]);
                    }
            var vertices = new List<Vector3>();
            var indices = new[] { new List<int>(), new List<int>(), new List<int>() };
            var welded = new Dictionary<ulong, int>();
            var cube = new int[8];
            var tetra = new int[4];
            var polygon = new int[4];
            for (int z = 0; z < nz - 1; z++)
                for (int y = 0; y < ny - 1; y++)
                    for (int x = 0; x < nx - 1; x++)
                    {
                        for (int c = 0; c < 8; c++)
                            cube[c] = x + (c & 1) + nx * (y + ((c >> 1) & 1) + ny * (z + ((c >> 2) & 1)));
                        for (int t = 0; t < 6; t++)
                        {
                            for (int c = 0; c < 4; c++) tetra[c] = cube[Tetrahedra[t, c]];
                            int count = 0;
                            Vector3 inside = Vector3.zero, outside = Vector3.zero;
                            int insideCount = 0, outsideCount = 0;
                            for (int c = 0; c < 4; c++)
                                if (values[tetra[c]] < 0) { inside += positions[tetra[c]]; insideCount++; }
                                else { outside += positions[tetra[c]]; outsideCount++; }
                            if (insideCount == 0 || outsideCount == 0) continue;
                            for (int e = 0; e < 6; e++)
                            {
                                int a = tetra[Edges[e, 0]], b = tetra[Edges[e, 1]];
                                if ((values[a] < 0) == (values[b] < 0)) continue;
                                ulong key = ((ulong)(uint)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);
                                if (!welded.TryGetValue(key, out int vertex))
                                {
                                    vertex = vertices.Count;
                                    vertices.Add(Vector3.Lerp(positions[a], positions[b], values[a] / (values[a] - values[b])));
                                    welded.Add(key, vertex);
                                }
                                polygon[count++] = vertex;
                            }
                            // Angular order in the tetra's intersection plane avoids
                            // crossed quads and gives consistent outward winding.
                            var normal = (outside / outsideCount - inside / insideCount).normalized;
                            var center = Vector3.zero;
                            for (int c = 0; c < count; c++) center += vertices[polygon[c]];
                            center /= count;
                            var u = (vertices[polygon[0]] - center).normalized;
                            var v = Vector3.Cross(normal, u).normalized;
                            for (int a = 1; a < count; a++)
                                for (int b = a; b > 0; b--)
                                {
                                    var pa = vertices[polygon[b]] - center;
                                    var pb = vertices[polygon[b - 1]] - center;
                                    if (Mathf.Atan2(Vector3.Dot(pa, v), Vector3.Dot(pa, u)) >=
                                        Mathf.Atan2(Vector3.Dot(pb, v), Vector3.Dot(pb, u))) break;
                                    int swap = polygon[b]; polygon[b] = polygon[b - 1]; polygon[b - 1] = swap;
                                }
                            for (int c = 1; c < count - 1; c++)
                            {
                                int a = polygon[0], b = polygon[c], d = polygon[c + 1];
                                if (Vector3.Dot(Vector3.Cross(vertices[b] - vertices[a], vertices[d] - vertices[a]), normal) < 0)
                                { int swap = b; b = d; d = swap; }
                                var list = indices[MaterialAt((vertices[a] + vertices[b] + vertices[d]) / 3)];
                                list.Add(a); list.Add(b); list.Add(d);
                            }
                        }
                    }
            var mesh = new Mesh { name = "I field anatomy " + kind + " grid " + spacing,
                indexFormat = IndexFormat.UInt32, subMeshCount = 3 };
            mesh.SetVertices(vertices);
            for (int i = 0; i < 3; i++) mesh.SetTriangles(indices[i], i);
            var weights = new BoneWeight[vertices.Count];
            var uv = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                weights[i] = Weight(vertices[i]);
                uv[i] = new Vector2(vertices[i].x * 2 + vertices[i].z, vertices[i].y * 2);
            }
            var bind = new Matrix4x4[6];
            for (int i = 0; i < 6; i++) bind[i] = Matrix4x4.Translate(-Pivots[i]);
            mesh.boneWeights = weights;
            mesh.bindposes = bind;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
