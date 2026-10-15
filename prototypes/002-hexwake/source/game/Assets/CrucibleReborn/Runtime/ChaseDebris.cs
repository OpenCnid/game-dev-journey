using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GameDevHarness.CrucibleReborn
{
    // Original I ossuary kit: open orbital rims, nasal cavity, separate teeth,
    // mandible, curved rib cage and articulated long bones over charred ash.
    // No external model, old loader, old rock, or H skull mesh is an input.
    public static class ChaseDebris
    {
        static readonly List<Mesh> owned = new List<Mesh>();
        public static readonly Vector3[] PileCenters = { new Vector3(-5, 0, -4),
            new Vector3(5, 0, 8), new Vector3(-36, 0, 15),
            new Vector3(36, 0, 15), new Vector3(0, 0, 25) };
        public static int TriangleCount { get; private set; }

        public static void Build(Transform parent, Material boneMaterial, Material ashMaterial,
            Material ironMaterial, List<NavMeshBuildSource> sources)
        {
            var bones = new RebornEnvironmentGeometry();
            var iron = new RebornEnvironmentGeometry();
            for (int i = 0; i < PileCenters.Length; i++)
            {
                Vector3 p = PileCenters[i];
                var terrain = new RebornEnvironmentGeometry();
                AshMound(terrain, p, 2.8f, i == 4 ? 1.4f : 1.2f, i);
                Add(parent, "I charred ossuary footing " + i, terrain, ashMaterial, sources);
                // Anatomy is embedded above the mound, not placed on invisible
                // tall rock collision. Broad footing is jumpable from every side.
                for (int k = 0; k < 5; k++)
                {
                    float angle = (k * 71 + i * 19) * Mathf.Deg2Rad;
                    Vector3 q = p + new Vector3(Mathf.Cos(angle) * 1.1f,
                        .66f + .12f * (k % 2), Mathf.Sin(angle) * 1.1f);
                    Skull(bones, q, Quaternion.Euler(12 + k * 7, k * 71 + i * 19, -8),
                        .48f + .05f * (k % 3));
                }
                RibCage(bones, p + new Vector3(.45f, 1.05f, -.25f),
                    Quaternion.Euler(0, i * 37, 23));
                for (int k = 0; k < 8; k++)
                {
                    Vector3 q = p + new Vector3((k % 3 - 1) * .65f, .75f, (k / 3 - 1) * .65f);
                    Femur(bones, q, Quaternion.Euler(80, k * 43 + i * 29, k * 7), .9f);
                }
                // Collapsed iron reliquary hoops visibly pierce the ash.
                for (int k = 0; k < 2; k++)
                {
                    var path = new List<Vector3>();
                    var radii = new List<float>();
                    for (int j = 0; j <= 14; j++)
                    {
                        float a = j * Mathf.PI / 14;
                        path.Add(p + new Vector3(Mathf.Cos(a) * 1.8f, Mathf.Sin(a) * 1.6f + .1f,
                            k * 1.2f - .6f));
                        radii.Add(.045f);
                    }
                    iron.Tube(path, radii, 6);
                }
            }
            Add(parent, "I exposed orbital teeth ribs and femurs", bones, boneMaterial, null);
            Add(parent, "I collapsed reliquary hoops", iron, ironMaterial, null);
        }

        static void Add(Transform parent, string name, RebornEnvironmentGeometry kit,
            Material material, List<NavMeshBuildSource> sources)
        {
            Mesh mesh = kit.Mesh(name);
            owned.Add(mesh); TriangleCount += mesh.triangles.Length / 3;
            var go = new GameObject(name) { layer = sources == null ? 0 : 8 };
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (sources != null)
            {
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = mesh, transform = go.transform.localToWorldMatrix, area = 0 });
            }
        }

        public static void AshMound(RebornEnvironmentGeometry kit, Vector3 origin, float radius, float height, int variant)
        {
            const int segments = 18;
            var rings = new Vector3[3][];
            for (int r = 0; r < 3; r++)
            {
                rings[r] = new Vector3[segments];
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2 / segments;
                    float shape = 1 + .09f * Mathf.Sin(i * 2.3f + variant) + .04f * Mathf.Cos(i * 4.1f);
                    float width = radius * (r == 0 ? 1 : r == 1 ? .67f : .24f) * shape;
                    float y = r == 0 ? .015f : r == 1 ? height * .57f : height * .92f;
                    rings[r][i] = origin + new Vector3(Mathf.Cos(a) * width, y, Mathf.Sin(a) * width * .72f);
                }
            }
            for (int i = 0; i < segments; i++)
            {
                int n = (i + 1) % segments;
                for (int r = 0; r < 2; r++)
                    kit.Quad(rings[r][n], rings[r][i], rings[r + 1][i], rings[r + 1][n]);
                kit.Face(rings[2][n], rings[2][i], origin + Vector3.up * height);
                kit.Face(origin, rings[0][i], rings[0][n]);
            }
        }

        public static void Skull(RebornEnvironmentGeometry kit, Vector3 origin, Quaternion rotation, float scale)
        {
            Matrix4x4 m = Matrix4x4.TRS(origin, rotation, Vector3.one * scale);
            // Cranium shell covers only the rear and superior face. The facial
            // plane below y=.36 is absent: sockets open into a real cranial void.
            Vector3[,] dome = new Vector3[9, 17];
            for (int j = 0; j <= 8; j++)
                for (int k = 0; k <= 16; k++)
                {
                    float latitude = -.22f + j * 1.77f / 8;
                    float azimuth = k * Mathf.PI * 2 / 16;
                    dome[j, k] = m.MultiplyPoint3x4(new Vector3(Mathf.Cos(azimuth) *
                        Mathf.Cos(latitude) * .68f, .28f + Mathf.Sin(latitude) * .83f,
                        -.2f + Mathf.Sin(azimuth) * Mathf.Cos(latitude) * .64f));
                }
            for (int j = 0; j < 8; j++)
                for (int k = 0; k < 16; k++)
                {
                    // Front hemisphere intentionally excised for eye/nose voids.
                    if (j < 3 && k < 8) continue;
                    kit.Quad(dome[j, k], dome[j + 1, k], dome[j + 1, k + 1], dome[j, k + 1]);
                }
            for (int side = -1; side <= 1; side += 2)
            {
                kit.Socket(m.MultiplyPoint3x4(new Vector3(side * .32f, .22f, .37f)),
                    .22f * scale, .2f * scale, .075f * scale, .32f * scale, rotation);
                // Zygomatic strut ends beside the void rather than plugging it.
                BoneCurve(kit, m, new[] { new Vector3(side * .54f, .18f, .35f),
                    new Vector3(side * .64f, -.04f, .22f), new Vector3(side * .38f, -.2f, .4f) },
                    new[] { .09f, .08f, .1f }, scale);
                BoneCurve(kit, m, new[] { new Vector3(side * .1f, .28f, .4f),
                    new Vector3(side * .14f, -.08f, .47f), new Vector3(side * .08f, -.2f, .45f) },
                    new[] { .06f, .035f, .045f }, scale);
            }
            // Open U-shaped mandible with separate ascending rami and a mouth gap.
            BoneCurve(kit, m, new[] { new Vector3(-.53f, .05f, -.12f), new Vector3(-.49f, -.43f, .2f),
                new Vector3(-.28f, -.5f, .53f), new Vector3(0, -.52f, .59f),
                new Vector3(.28f, -.5f, .53f), new Vector3(.49f, -.43f, .2f),
                new Vector3(.53f, .05f, -.12f) }, new[] { .07f, .09f, .09f, .095f, .09f, .09f, .07f }, scale);
            for (int tooth = 0; tooth < 10; tooth++)
            {
                float angle = Mathf.Lerp(-1.08f, 1.08f, tooth / 9f);
                float x = Mathf.Sin(angle) * .4f, z = .33f + Mathf.Cos(angle) * .2f;
                for (int jaw = 0; jaw < 2; jaw++)
                {
                    Vector3 q = m.MultiplyPoint3x4(new Vector3(x, jaw == 0 ? -.28f : -.42f, z));
                    kit.Block(q, new Vector3(.067f, tooth == 1 || tooth == 8 ? .15f : .1f, .085f) * scale,
                        rotation * Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), .012f * scale);
                }
            }
            BoneCurve(kit, m, new[] { new Vector3(-.38f, -.21f, .45f), new Vector3(0, -.23f, .58f),
                new Vector3(.38f, -.21f, .45f) }, new[] { .065f, .075f, .065f }, scale);
        }

        static void BoneCurve(RebornEnvironmentGeometry kit, Matrix4x4 m,
            Vector3[] points, float[] radii, float scale)
        {
            var transformed = new Vector3[points.Length];
            var widths = new float[radii.Length];
            for (int i = 0; i < points.Length; i++)
            { transformed[i] = m.MultiplyPoint3x4(points[i]); widths[i] = radii[i] * scale; }
            kit.Tube(transformed, widths, 7);
        }

        public static void Femur(RebornEnvironmentGeometry kit, Vector3 origin, Quaternion rotation, float length)
        {
            var m = Matrix4x4.TRS(origin, rotation, Vector3.one);
            BoneCurve(kit, m, new[] { new Vector3(-.11f, 0, 0), new Vector3(.06f, .11f, 0),
                new Vector3(.05f, length * .32f, .035f), new Vector3(-.02f, length * .77f, 0),
                new Vector3(0, length * .91f, 0), new Vector3(0, length, 0) },
                new[] { .16f, .1f, .065f, .07f, .15f, .13f }, 1);
            BoneCurve(kit, m, new[] { new Vector3(-.11f, 0, 0), new Vector3(-.25f, .1f, 0),
                new Vector3(-.24f, .17f, 0) }, new[] { .12f, .16f, .09f }, 1);
            // Paired distal condyles and a notch, not a single capsule end.
            for (int side = -1; side <= 1; side += 2)
                BoneCurve(kit, m, new[] { new Vector3(side * .1f, length * .87f, 0),
                    new Vector3(side * .13f, length * .97f, .04f),
                    new Vector3(side * .1f, length * 1.04f, 0) }, new[] { .08f, .115f, .08f }, 1);
        }

        public static void RibCage(RebornEnvironmentGeometry kit, Vector3 origin, Quaternion rotation)
        {
            var m = Matrix4x4.TRS(origin, rotation, Vector3.one);
            for (int rib = 0; rib < 8; rib++)
            {
                float y = rib * .14f - .45f;
                float size = .72f * Mathf.Sin((rib + 2) * Mathf.PI / 12);
                for (int side = -1; side <= 1; side += 2)
                {
                    var path = new Vector3[11];
                    var radii = new float[11];
                    for (int j = 0; j < path.Length; j++)
                    {
                        float angle = j * Mathf.PI * .87f / 10;
                        path[j] = new Vector3(side * Mathf.Sin(angle) * size,
                            y - .12f * Mathf.Sin(angle), -.35f + (1 - Mathf.Cos(angle)) * .4f);
                        radii[j] = .034f - j * .0016f;
                    }
                    BoneCurve(kit, m, path, radii, 1);
                }
                kit.Block(m.MultiplyPoint3x4(new Vector3(0, y, -.36f)),
                    new Vector3(.18f, .11f, .16f), rotation, .025f);
            }
            BoneCurve(kit, m, new[] { new Vector3(0, -.45f, .39f), new Vector3(0, .12f, .41f),
                new Vector3(0, .57f, .36f) }, new[] { .045f, .07f, .06f }, 1);
        }

        public static void Dispose()
        {
            foreach (Mesh mesh in owned) Object.Destroy(mesh);
            owned.Clear(); TriangleCount = 0;
        }
    }
}
