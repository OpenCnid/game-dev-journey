using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace GameDevHarness.FantasyTrialB
{
    public sealed class FantasyWorld
    {
        public Transform Root;
        public List<NavMeshBuildSource> Sources = new List<NavMeshBuildSource>();
        public Vector3[] SpawnPoints;
        public Bounds Bounds;

        internal bool PreviousFog;
        internal Color PreviousFogColor, PreviousSky, PreviousEquator, PreviousGround;
        internal float PreviousFogDensity;
        internal FogMode PreviousFogMode;
        internal AmbientMode PreviousAmbientMode;
        internal Light PreviousSun, Sun;

        public void Dispose()
        {
            // Lighting and the reflection probe are children; shared mesh/material ownership
            // stays with ArtParts and ArtPalette, including the meshes used by nav sources.
            if (Root == null) return;
            if (RenderSettings.sun == Sun)
            {
                RenderSettings.sun = PreviousSun;
                RenderSettings.fog = PreviousFog;
                RenderSettings.fogColor = PreviousFogColor;
                RenderSettings.fogDensity = PreviousFogDensity;
                RenderSettings.fogMode = PreviousFogMode;
                RenderSettings.ambientMode = PreviousAmbientMode;
                RenderSettings.ambientSkyColor = PreviousSky;
                RenderSettings.ambientEquatorColor = PreviousEquator;
                RenderSettings.ambientGroundColor = PreviousGround;
            }
            Root.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(Root.gameObject);
            Root = null;
            Sources.Clear();
        }
    }

    public static class FantasyWorldArt
    {
        public static FantasyWorld Create(Transform parent, ArtPalette palette, int seed)
        {
            var world = new Builder(parent, palette, seed).Build();
            palette.Sky.Create(world.Root);
            return world;
        }

        sealed class Builder
        {
            readonly FantasyWorld world = new FantasyWorld();
            readonly ArtPalette p;
            readonly System.Random random;
            readonly Transform[] sectors = new Transform[8];
            readonly float noiseOffset;

            public Builder(Transform parent, ArtPalette palette, int seed)
            {
                p = palette;
                random = new System.Random(seed);
                noiseOffset = Range(20, 800);
                world.Root = Group("Overgrown relay clearing", parent, Vector3.zero);
                for (int i = 0; i < sectors.Length; i++)
                    sectors[i] = Group("Scenery sector " + i, world.Root, Vector3.zero);
            }

            public FantasyWorld Build()
            {
                Terrain();
                Boundary();
                Clearing();
                Forest();
                RelayStation();
                Lighting();
                // Keep sectors separate for frustum culling. Colliders and navigation
                // retain their original objects, independent of render-only batching.
                foreach (var sector in sectors)
                {
                    ArtParts.CombineStatic(sector);
                    foreach (var renderer in sector.GetComponentsInChildren<MeshRenderer>())
                        if (renderer.enabled) renderer.gameObject.layer = 8;
                }
                world.SpawnPoints = new Vector3[24];
                for (int i = 0; i < world.SpawnPoints.Length; i++)
                    world.SpawnPoints[i] = world.Root.TransformPoint(Polar(i * 360f / 24, 18.5f));
                world.Bounds = new Bounds(world.Root.TransformPoint(new Vector3(0, 5, 0)),
                    Vector3.Scale(new Vector3(48, 16, 48), world.Root.lossyScale));
                return world;
            }

            float Range(float min, float max)
            {
                return min + (float)random.NextDouble() * (max - min);
            }

            static Vector3 Polar(float degrees, float radius)
            {
                float a = degrees * Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius;
            }

            static Transform Group(string name, Transform parent, Vector3 pos)
            {
                var obj = new GameObject(name);
                obj.transform.SetParent(parent, false);
                obj.transform.localPosition = pos;
                return obj.transform;
            }

            Transform Sector(Vector3 pos)
            {
                float angle = Mathf.Atan2(pos.x, pos.z) * Mathf.Rad2Deg;
                return sectors[Mathf.FloorToInt(Mathf.Repeat(angle, 360) / 45)];
            }

            float Height(float x, float z)
            {
                // The entire playable disc, including all spawn points, is flat.
                // Uneven land begins behind the physical cliff boundary.
                float radius = new Vector2(x, z).magnitude;
                float edge = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(22, 32, radius));
                float noise = Mathf.PerlinNoise(x * 0.055f + noiseOffset, z * 0.055f + noiseOffset);
                return edge * (0.4f + noise * 3.6f);
            }

            void Terrain()
            {
                const int cells = 128;
                var vertices = new Vector3[(cells + 1) * (cells + 1)];
                var uv = new Vector2[vertices.Length];
                var indices = new int[cells * cells * 6];
                for (int z = 0; z <= cells; z++)
                    for (int x = 0; x <= cells; x++)
                    {
                        int i = z * (cells + 1) + x;
                        float px = x - 64, pz = z - 64;
                        vertices[i] = new Vector3(px, Height(px, pz), pz);
                        uv[i] = new Vector2(x / (float)cells, z / (float)cells);
                        if (x == cells || z == cells) continue;
                        int t = (z * cells + x) * 6;
                        indices[t] = i; indices[t + 1] = i + cells + 1; indices[t + 2] = i + 1;
                        indices[t + 3] = i + 1; indices[t + 4] = i + cells + 1;
                        indices[t + 5] = i + cells + 2;
                    }
                var floor = MeshObject("Textured forest earth", world.Root,
                    Mesh("Uneven forest terrain", vertices, uv, indices), p.Ground, 9);
                floor.AddComponent<MeshCollider>().sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh;
                MeshSource(floor, 0);
            }

            void MeshSource(GameObject obj, int area)
            {
                world.Sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = obj.GetComponent<MeshFilter>().sharedMesh,
                    transform = obj.transform.localToWorldMatrix,
                    area = area
                });
            }

            void BoxSource(GameObject obj, Vector3 size)
            {
                world.Sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = obj.transform.localToWorldMatrix,
                    size = size,
                    area = 1
                });
            }

            GameObject SolidBox(string name, Transform parent, Vector3 pos, Vector3 size, Material mat,
                float bevel = 0.06f)
            {
                var obj = ArtParts.Box(name, parent, pos, size, mat, bevel, true, 8);
                return obj;
            }

            void Boundary()
            {
                // Overlapping tangent slabs seal the ~44 m clearing independently of
                // artistic rock silhouette variation. These are concealed inside cliffs.
                for (int i = 0; i < 48; i++)
                {
                    float angle = i * 7.5f;
                    Vector3 pos = Polar(angle, 23.3f);
                    var wall = SolidBox("Cliff collision core", Sector(pos), pos + Vector3.up * 1.2f,
                        new Vector3(3.4f, 2.8f, 2.6f), p.Rock, 0.3f);
                    wall.transform.localRotation = Quaternion.Euler(0, angle, 0);
                    wall.GetComponent<MeshRenderer>().enabled = false;
                    BoxSource(wall, new Vector3(3.4f, 2.8f, 2.6f));
                    Rock(pos + Vector3.up * Range(0, 0.4f),
                        new Vector3(Range(4, 6), Range(1.8f, 3.2f), Range(3, 5)), false);
                    Vector3 upper = Polar(angle + Range(-3, 3), Range(27, 31));
                    Rock(upper + Vector3.up * Height(upper.x, upper.z),
                        new Vector3(Range(4, 7), Range(2, 4.2f), Range(4, 7)), false);
                }
                for (int i = 0; i < 20; i++)
                {
                    Vector3 pos = Polar(i * 18 + 6, Range(43, 56));
                    Rock(pos + Vector3.up * Height(pos.x, pos.z),
                        new Vector3(Range(6, 12), Range(9, 19), Range(5, 10)), false);
                }
            }

            void Clearing()
            {
                // Four sparse islands leave an unobstructed center, a wide orbital
                // route at r=16..21, and broad radial approaches between islands.
                float[] angles = { 48, 138, 232, 305 };
                for (int i = 0; i < angles.Length; i++)
                {
                    Vector3 pos = Polar(angles[i] + Range(-6, 6), Range(11.4f, 12.8f));
                    Rock(pos, new Vector3(Range(1.3f, 2), Range(0.8f, 1.6f), Range(1.1f, 1.8f)), true);
                    FernPatch(pos + Polar(angles[i] + 50, 1.5f), 5, 0.8f);
                    if (i % 2 == 0) Relic(pos + Polar(angles[i] - 45, 1.7f));
                }
                // Inert remnants, not interactables or hazards.
                var wreck = Group("Collapsed service gantry", Sector(new Vector3(-13, 0, 6)),
                    new Vector3(-13, 0, 6));
                wreck.localRotation = Quaternion.Euler(0, -25, 0);
                var footing = SolidBox("Cracked plinth", wreck, new Vector3(0, 0.22f, 0),
                    new Vector3(3, 0.44f, 2), p.Concrete);
                BoxSource(footing, new Vector3(3, 0.44f, 2));
                var beam = SolidBox("Buckled gantry spine", wreck, new Vector3(0, 0.7f, 0),
                    new Vector3(3.4f, 0.45f, 0.6f), p.Paint);
                beam.transform.localRotation = Quaternion.Euler(0, 8, -13);
                BoxSource(beam, new Vector3(3.4f, 0.45f, 0.6f));
                for (int i = 0; i < 5; i++)
                    ArtParts.Box("Gantry ribs", wreck, new Vector3(-1.4f + i * 0.7f, 0.55f, 0.3f),
                        new Vector3(0.12f, 0.7f, 1.15f), p.Steel, 0.015f, false, 8);
                Pipe(wreck, new Vector3(-1.5f, 0.6f, -0.65f), new Vector3(1.2f, 0.32f, -0.65f),
                    0.12f, p.Dark);
                for (int i = 0; i < 70; i++)
                {
                    Vector3 pos = Polar(Range(0, 360), Range(20.3f, 21.8f));
                    FernPatch(pos, random.Next(2, 5), Range(0.3f, 0.65f));
                }
            }

            void Rock(Vector3 pos, Vector3 size, bool solid)
            {
                var obj = p.PhotoRock.Create(Sector(pos), pos, size,
                    Quaternion.Euler(0, Range(0, 360), 0), solid);
                if (solid)
                {
                    world.Sources.Add(new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Box,
                        transform = obj.transform.localToWorldMatrix * Matrix4x4.Translate(Vector3.up * 0.5f),
                        size = Vector3.one, area = 1
                    });
                }
            }

            void Forest()
            {
                for (int i = 0; i < 78; i++)
                {
                    Vector3 pos = Polar(i * 137.508f, Range(27, 47));
                    pos.y = Height(pos.x, pos.z);
                    Tree(pos, Range(7, 13));
                    FernPatch(pos + Polar(Range(0, 360), 1.1f), random.Next(4, 8), Range(0.7f, 1.4f));
                }
                // Ground-cover banks sit behind the open spawn corridor.
                for (int i = 0; i < 120; i++)
                {
                    Vector3 pos = Polar(Range(0, 360), Range(24, 38));
                    pos.y = Height(pos.x, pos.z);
                    FernPatch(pos, random.Next(3, 7), Range(0.5f, 1.4f));
                }
            }

            void Tree(Vector3 pos, float height)
            {
                var tree = Group("Wind-shaped forest tree", Sector(pos), pos);
                tree.localRotation = Quaternion.Euler(0, Range(0, 360), 0);
                float radius = height * Range(0.028f, 0.043f);
                var spine = new Vector3[7];
                var widths = new float[7];
                float bend = Range(-1.1f, 1.1f);
                for (int i = 0; i < spine.Length; i++)
                {
                    float f = i / 6f;
                    spine[i] = new Vector3(bend * f * f, height * f, Mathf.Sin(f * 3) * 0.25f);
                    widths[i] = Mathf.Lerp(radius * 1.45f, 0.035f, f);
                }
                Tube("Fluted bark trunk", tree, spine, widths, p.Bark, 10);
                for (int i = 0; i < 5; i++)
                {
                    Vector3 tip = Polar(i * 72 + Range(-15, 15), radius * 3.2f);
                    Tube("Exposed root", tree, new[] { new Vector3(0, radius * 1.5f, 0), tip * 0.6f,
                        tip + Vector3.down * 0.12f }, new[] { radius * 0.65f, radius * 0.32f, 0.025f }, p.Bark, 7);
                }
                for (int i = 0; i < 9; i++)
                {
                    float f = Range(0.45f, 0.9f);
                    Vector3 start = new Vector3(bend * f * f, height * f, 0);
                    Vector3 reach = Polar(i * 137.5f, Range(1.7f, 3.5f));
                    Vector3 end = start + reach + Vector3.up * Range(0.6f, 1.9f);
                    Tube("Crooked canopy branch", tree, new[] { start, start + reach * 0.5f + Vector3.up * 0.3f, end },
                        new[] { radius * 0.38f, radius * 0.2f, 0.025f }, p.Bark, 7);
                    LeafSpray(tree, end, Range(1.2f, 2), 22, false);
                }
                LeafSpray(tree, spine[6], 1.6f, 24, false);
            }

            void FernPatch(Vector3 pos, int count, float scale)
            {
                var patch = Group("Fern and blade ground cover", Sector(pos), pos);
                for (int i = 0; i < count; i++)
                {
                    Vector3 offset = Polar(Range(0, 360), Range(0, 0.7f));
                    LeafSpray(patch, offset, scale * Range(0.7f, 1.1f), 14, true);
                }
            }

            void LeafSpray(Transform parent, Vector3 pos, float size, int count, bool fern)
            {
                var vertices = new List<Vector3>();
                var uv = new List<Vector2>();
                var triangles = new List<int>();
                for (int i = 0; i < count; i++)
                {
                    Vector3 outward = Polar(i * 137.5f + Range(-20, 20), 1);
                    Vector3 basePos = fern ? pos : pos + outward * Range(0, size * 0.6f) +
                        Vector3.up * Range(-size * 0.3f, size * 0.3f);
                    float length = size * Range(0.5f, 1.1f);
                    Vector3 tip = basePos + outward * length + Vector3.up * length * (fern ? 0.45f : 0.12f);
                    Vector3 middle = Vector3.Lerp(basePos, tip, 0.48f) + Vector3.up * length * 0.24f;
                    Vector3 side = Vector3.Cross(Vector3.up, outward) * length * (fern ? 0.12f : 0.2f);
                    AddLeaf(vertices, uv, triangles, basePos, middle, tip, side);
                    if (!fern) continue;
                    for (int j = 1; j <= 4; j++)
                    {
                        float f = j / 5f;
                        Vector3 stem = Vector3.Lerp(basePos, tip, f) + Vector3.up * Mathf.Sin(f * Mathf.PI) * length * 0.2f;
                        for (int sign = -1; sign <= 1; sign += 2)
                        {
                            Vector3 leafTip = stem + side * sign * 2.3f * (1 - f) + outward * length * 0.16f;
                            AddLeaf(vertices, uv, triangles, stem, (stem + leafTip) * 0.5f + Vector3.up * 0.035f,
                                leafTip, outward * length * 0.035f);
                        }
                    }
                }
                MeshObject(fern ? "Original fern fronds" : "Original lance leaf canopy", parent,
                    Mesh("Folded two-sided leaves", vertices.ToArray(), uv.ToArray(), triangles.ToArray()), p.Foliage, 8);
            }

            static void AddLeaf(List<Vector3> v, List<Vector2> uv, List<int> t,
                Vector3 root, Vector3 middle, Vector3 tip, Vector3 side)
            {
                int n = v.Count;
                v.Add(root); v.Add(middle - side); v.Add(middle + Vector3.up * side.magnitude * 0.25f);
                v.Add(middle + side); v.Add(tip);
                uv.Add(new Vector2(0.5f, 0)); uv.Add(new Vector2(0, 0.5f));
                uv.Add(new Vector2(0.5f, 0.5f)); uv.Add(new Vector2(1, 0.5f)); uv.Add(new Vector2(0.5f, 1));
                // Separate back-face vertices prevent opposing normals cancelling during
                // RecalculateNormals, and work with the palette's opaque PBR shader.
                for (int i = 0; i < 5; i++)
                {
                    v.Add(v[n + i]);
                    uv.Add(uv[n + i]);
                }
                int[] faces = { 0, 2, 1, 0, 3, 2, 1, 2, 4, 2, 3, 4 };
                for (int i = 0; i < faces.Length; i += 3)
                {
                    t.Add(n + faces[i]); t.Add(n + faces[i + 1]); t.Add(n + faces[i + 2]);
                    t.Add(n + 5 + faces[i + 2]); t.Add(n + 5 + faces[i + 1]); t.Add(n + 5 + faces[i]);
                }
            }

            void Relic(Vector3 pos)
            {
                var relic = Group("Dormant survey marker", Sector(pos), pos);
                relic.localRotation = Quaternion.Euler(0, Range(0, 360), 9);
                var body = SolidBox("Weathered marker casing", relic, new Vector3(0, 0.6f, 0),
                    new Vector3(0.65f, 1.2f, 0.5f), p.Paint);
                BoxSource(body, new Vector3(0.65f, 1.2f, 0.5f));
                ArtParts.Box("Inset black sensor panel", relic, new Vector3(0, 0.8f, -0.26f),
                    new Vector3(0.44f, 0.33f, 0.06f), p.Black, 0.025f, false, 8);
                ArtParts.Box("Sleeping amber glyph", relic, new Vector3(0, 0.8f, -0.3f),
                    new Vector3(0.24f, 0.025f, 0.025f), p.Amber, 0.005f, false, 8);
                for (int i = 0; i < 3; i++)
                    ArtParts.Box("Marker cooling fin", relic, new Vector3(0.35f, 0.35f + i * 0.17f, 0),
                        new Vector3(0.09f, 0.05f, 0.4f), p.Steel, 0.01f, false, 8);
            }

            void RelayStation()
            {
                // Forward vista: asymmetric collapsed station framing a tall relay mast.
                // All industrial silhouette geometry remains beyond the playable disc.
                var station = Group("Abandoned ridge relay station", sectors[0], new Vector3(4, 2.2f, 31));
                station.localRotation = Quaternion.Euler(0, -18, 0);
                ArtParts.Box("Exposed foundation", station, new Vector3(0, 0.5f, 0),
                    new Vector3(13, 1, 8), p.Concrete, 0.16f, false, 8);
                for (int i = 0; i < 6; i++)
                {
                    float x = -5 + i * 2;
                    ArtParts.Box("Retained station upright", station, new Vector3(x, 4, 2.8f),
                        new Vector3(0.5f, 7, 0.6f), p.Concrete, 0.08f, false, 8);
                    var roof = ArtParts.Box("Torn roof cassette", station, new Vector3(x, 7.3f, 0.8f),
                        new Vector3(1.7f, 0.16f, 5.8f), p.Paint, 0.025f, false, 8);
                    roof.transform.localRotation = Quaternion.Euler(Range(-9, 15), 0, Range(-8, 8));
                    Pipe(station, new Vector3(x, 6.8f, -2), new Vector3(x, 6.8f, 3.1f), 0.1f, p.Steel);
                }
                ArtParts.Box("Weathered rear wall", station, new Vector3(-2, 3.4f, 3.3f),
                    new Vector3(8, 5.8f, 0.35f), p.Concrete, 0.1f, false, 8);
                for (int i = 0; i < 4; i++)
                    ArtParts.Box("Recessed dark window", station, new Vector3(-5 + i * 2, 4.3f, 3.08f),
                        new Vector3(1.45f, 1.15f, 0.09f), p.Glass, 0.04f, false, 8);
                var door = ArtParts.Box("Fallen station door", station, new Vector3(-3, 1.1f, -3),
                    new Vector3(2.3f, 0.2f, 3.6f), p.Paint, 0.04f, false, 8);
                door.transform.localRotation = Quaternion.Euler(0, 24, 9);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 a = new Vector3(6.5f + i * 0.55f, 1.1f, -3);
                    Vector3 b = a + Vector3.forward * 7;
                    Pipe(station, a, b, 0.18f, p.Steel);
                    Pipe(station, b, b + Vector3.up * 3.4f, 0.18f, p.Steel);
                    var flange = ArtParts.Ring("Oxidized pipe flange", station, a + Vector3.forward * 1.5f,
                        0.24f, 0.05f, p.Brass);
                    flange.layer = 8;
                }
                var mast = Group("Broken sky relay", station, new Vector3(3.8f, 0.8f, 1));
                for (int i = 0; i < 3; i++)
                {
                    Vector3 foot = Polar(i * 120, 1.4f);
                    Pipe(mast, foot, foot * 0.3f + Vector3.up * 17, 0.12f, p.Steel);
                    for (int j = 0; j < 6; j++)
                    {
                        Vector3 next = Polar((i + 1) * 120, 1.4f);
                        Pipe(mast, foot * Mathf.Lerp(1, 0.3f, j / 6f) + Vector3.up * (j * 2.7f),
                            next * Mathf.Lerp(1, 0.3f, (j + 1) / 6f) + Vector3.up * ((j + 1) * 2.7f),
                            0.045f, p.Dark);
                    }
                }
                Pipe(mast, new Vector3(-2.8f, 13, 0), new Vector3(2.8f, 13, 0), 0.085f, p.Steel);
                for (int i = 0; i < 7; i++)
                    Pipe(mast, new Vector3(-2.4f + i * 0.8f, 13, -0.75f),
                        new Vector3(-2.4f + i * 0.8f, 13, 0.75f), 0.04f, p.Steel);
                ArtParts.Box("Residual relay light", mast, new Vector3(0, 17.3f, 0),
                    new Vector3(0.18f, 0.28f, 0.18f), p.Amber, 0.035f, false, 8);
                var ring = ArtParts.Ring("Relic induction halo", mast, new Vector3(0, 10.7f, 0),
                    1.75f, 0.12f, p.Brass, 32);
                ring.transform.localRotation = Quaternion.Euler(12, 0, 0);
                ring.layer = 8;
                for (int i = 0; i < 6; i++)
                    ArtParts.Box("Teal induction inset", mast,
                        new Vector3(Mathf.Cos(i * Mathf.PI / 3) * 1.75f,
                            10.7f + Mathf.Sin(i * Mathf.PI / 3) * 1.75f, -0.1f),
                        new Vector3(0.13f, 0.13f, 0.08f), p.Teal, 0.015f, false, 8);
                Tree(new Vector3(-8, Height(-8, 33), 33), 12);
                FernPatch(new Vector3(10, Height(10, 28), 28), 10, 1.5f);
                // Secondary distant landmark offsets the station rather than enclosing
                // the view with identical trees.
                var tower = Group("Distant leaning condenser", sectors[6], new Vector3(-34, 3, 10));
                tower.localRotation = Quaternion.Euler(0, 15, -8);
                Tube("Ribbed condenser shell", tower,
                    new[] { Vector3.zero, Vector3.up * 2, Vector3.up * 9, Vector3.up * 10.5f },
                    new[] { 2f, 2.3f, 2.1f, 1.4f }, p.Paint, 18);
                for (int i = 0; i < 6; i++)
                {
                    var band = ArtParts.Ring("Condenser structural band", tower,
                        Vector3.up * (1 + i * 1.6f), 2.2f, 0.1f, p.Dark);
                    band.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    band.layer = 8;
                }
                Pipe(tower, new Vector3(2.5f, 0, 0), new Vector3(2.5f, 8, 0), 0.3f, p.Steel);
            }

            void Pipe(Transform parent, Vector3 a, Vector3 b, float radius, Material material)
            {
                Tube("Industrial conduit", parent, new[] { a, b }, new[] { radius, radius }, material, 10);
            }

            static void Tube(string name, Transform parent, Vector3[] path, float[] radii,
                Material material, int sides)
            {
                var v = new Vector3[path.Length * (sides + 1) + 2];
                var uv = new Vector2[v.Length];
                var t = new List<int>();
                float distance = 0;
                for (int i = 0; i < path.Length; i++)
                {
                    if (i > 0) distance += Vector3.Distance(path[i - 1], path[i]);
                    Vector3 direction = i == path.Length - 1 ? path[i] - path[i - 1] : path[i + 1] - path[i];
                    Quaternion orientation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                    for (int j = 0; j <= sides; j++)
                    {
                        float angle = j * Mathf.PI * 2 / sides;
                        float flute = 1 + Mathf.Sin(j * Mathf.PI * 0.8f) * 0.065f;
                        int n = i * (sides + 1) + j;
                        v[n] = path[i] + orientation * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radii[i] * flute;
                        uv[n] = new Vector2(j / (float)sides, distance * 0.4f);
                        if (i == path.Length - 1 || j == sides) continue;
                        t.Add(n); t.Add(n + sides + 1); t.Add(n + 1);
                        t.Add(n + 1); t.Add(n + sides + 1); t.Add(n + sides + 2);
                    }
                }
                v[v.Length - 2] = path[0]; v[v.Length - 1] = path[path.Length - 1];
                for (int j = 0; j < sides; j++)
                {
                    t.Add(v.Length - 2); t.Add(j); t.Add(j + 1);
                    t.Add(v.Length - 1); t.Add((path.Length - 1) * (sides + 1) + j + 1);
                    t.Add((path.Length - 1) * (sides + 1) + j);
                }
                MeshObject(name, parent, Mesh(name, v, uv, t.ToArray()), material, 8);
            }

            static Mesh Mesh(string name, Vector3[] v, Vector2[] uv, int[] triangles)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.vertices = v;
                mesh.uv = uv;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                ArtParts.GeneratedMeshes.Add(mesh);
                return mesh;
            }

            static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, int layer)
            {
                var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                obj.transform.SetParent(parent, false);
                obj.layer = layer;
                obj.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = obj.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                return obj;
            }

            void Lighting()
            {
                world.PreviousFog = RenderSettings.fog;
                world.PreviousFogColor = RenderSettings.fogColor;
                world.PreviousFogDensity = RenderSettings.fogDensity;
                world.PreviousFogMode = RenderSettings.fogMode;
                world.PreviousAmbientMode = RenderSettings.ambientMode;
                world.PreviousSky = RenderSettings.ambientSkyColor;
                world.PreviousEquator = RenderSettings.ambientEquatorColor;
                world.PreviousGround = RenderSettings.ambientGroundColor;
                world.PreviousSun = RenderSettings.sun;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.38f, 0.48f, 0.58f);
                RenderSettings.ambientEquatorColor = new Color(0.22f, 0.29f, 0.32f);
                RenderSettings.ambientGroundColor = new Color(0.13f, 0.16f, 0.13f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.39f, 0.48f, 0.51f);
                RenderSettings.fogDensity = 0.018f;
                var sun = Group("Late golden sunlight", world.Root, Vector3.zero).gameObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.transform.localRotation = Quaternion.Euler(32, -38, 0);
                sun.color = new Color(1, 0.82f, 0.61f);
                sun.intensity = 1.3f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.82f;
                sun.shadowBias = 0.035f;
                sun.shadowNormalBias = 0.25f;
                sun.shadowResolution = LightShadowResolution.High;
                world.Sun = sun;
                RenderSettings.sun = sun;
                var probe = Group("Clearing reflection capture", world.Root, new Vector3(0, 3, 0))
                    .gameObject.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                probe.resolution = 128;
                probe.size = new Vector3(80, 30, 80);
                probe.nearClipPlane = 0.3f;
                probe.farClipPlane = 100;
                probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
                probe.backgroundColor = RenderSettings.fogColor;
                probe.intensity = 0.65f;
                probe.boxProjection = true;
            }
        }
    }
}
