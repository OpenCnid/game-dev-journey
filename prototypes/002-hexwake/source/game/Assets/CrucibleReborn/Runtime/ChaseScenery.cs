using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.CrucibleReborn
{
    // Original distant furnace works beyond the physical cathedral enclosure.
    // Silhouettes use tapered polygonal flues, broken lips, pipes and iron straps.
    public static class ChaseScenery
    {
        static readonly List<Mesh> owned = new List<Mesh>();
        static readonly List<Renderer> optional = new List<Renderer>();
        static Cubemap reflection;
        public static int TriangleCount { get; private set; }

        public static void Build(Transform parent, Material masonry, Material wrought)
        {
            if (reflection == null)
            {
                // Retained licensed surface resource and cubemap conversion math;
                // no former sky-dome, cliff or other model geometry is reused.
                var panorama = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                panorama.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(
                    Application.streamingAssetsPath, "FantasyTrialB", "Sky", "sky.jpg")));
                const int size = 128;
                reflection = new Cubemap(size, TextureFormat.RGBA32, true) {
                    name = "I smoke environment reflection"
                };
                for (int face = 0; face < 6; face++)
                {
                    var pixels = new Color[size * size];
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            float u = (x + .5f) / size * 2 - 1, v = (y + .5f) / size * 2 - 1;
                            Vector3 direction;
                            switch ((CubemapFace)face)
                            {
                                case CubemapFace.PositiveX: direction = new Vector3(1, -v, -u); break;
                                case CubemapFace.NegativeX: direction = new Vector3(-1, -v, u); break;
                                case CubemapFace.PositiveY: direction = new Vector3(u, 1, v); break;
                                case CubemapFace.NegativeY: direction = new Vector3(u, -1, -v); break;
                                case CubemapFace.PositiveZ: direction = new Vector3(u, -v, 1); break;
                                default: direction = new Vector3(-u, -v, -1); break;
                            }
                            direction.Normalize();
                            pixels[y * size + x] = panorama.GetPixelBilinear(
                                Mathf.Atan2(direction.x, direction.z) / (2 * Mathf.PI) + .5f,
                                Mathf.Asin(direction.y) / Mathf.PI + .5f) * new Color(.45f, .22f, .11f, 1);
                        }
                    reflection.SetPixels(pixels, (CubemapFace)face);
                }
                reflection.Apply(true, true); Object.Destroy(panorama);
            }
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = reflection;
            RenderSettings.reflectionIntensity = .7f;
            var stone = new RebornEnvironmentGeometry();
            var iron = new RebornEnvironmentGeometry();
            for (int side = -1; side <= 1; side += 2)
                for (int flue = 0; flue < 5; flue++)
                {
                    Vector3 p = new Vector3(side * (56 + flue % 2 * 7), -1, -34 + flue * 18);
                    float height = 14 + flue % 3 * 5;
                    // Course gaps are modeled. Each chimney is hollow at its
                    // asymmetric broken crown, with readable wall thickness.
                    for (int row = 0; row < (int)height; row++)
                    {
                        float radius = 3.5f - row / height * 1.9f;
                        for (int k = 0; k < 12; k++)
                        {
                            float a = k * Mathf.PI / 6, b = (k + .96f) * Mathf.PI / 6;
                            if (row > height - 3 && (k + flue) % 5 == 0) continue;
                            Vector2[] profile = { new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius),
                                new Vector2(Mathf.Cos(b) * radius, Mathf.Sin(b) * radius),
                                new Vector2(Mathf.Cos(b) * (radius - .55f), Mathf.Sin(b) * (radius - .55f)),
                                new Vector2(Mathf.Cos(a) * (radius - .55f), Mathf.Sin(a) * (radius - .55f)) };
                            stone.Extrude(profile, .96f, Matrix4x4.TRS(p + Vector3.up * (row + .5f),
                                Quaternion.Euler(90, 0, 0), Vector3.one));
                        }
                        if (row % 4 == 1)
                        {
                            var path = new List<Vector3>(); var widths = new List<float>();
                            for (int k = 0; k <= 16; k++)
                            {
                                float a = k * Mathf.PI / 8;
                                path.Add(p + new Vector3(Mathf.Cos(a) * (radius + .1f), row,
                                    Mathf.Sin(a) * (radius + .1f)));
                                widths.Add(.11f);
                            }
                            iron.Tube(path, widths, 6);
                        }
                    }
                    iron.Tube(new[] { p + Vector3.up * 2,
                        p + new Vector3(-side * 3, 2, 0), p + new Vector3(-side * 6, 4, 0),
                        p + new Vector3(-side * 7, 7, 0) }, new[] { .55f, .55f, .4f, .35f }, 10);
                    for (int k = 0; k < 5; k++)
                        stone.BrokenPier(p + new Vector3((k - 2) * 2.2f, 0, -4),
                            2.1f, 3.7f + k % 3, 2.2f, Quaternion.Euler(0, k * 13, 0));
                }
            Add(parent, "I distant ruined furnace masonry", stone, masonry, false);
            Add(parent, "I forged flue collars and exhaust elbows", iron, wrought, true);
        }
        static void Add(Transform parent, string name, RebornEnvironmentGeometry kit, Material material, bool detail)
        {
            Mesh mesh = kit.Mesh(name);
            owned.Add(mesh); TriangleCount += mesh.triangles.Length / 3;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            Renderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (detail) optional.Add(renderer);
        }
        public static void SetQuality(int preset)
        { foreach (Renderer renderer in optional) if (renderer != null) renderer.enabled = preset > 0; }
        public static void Dispose()
        {
            foreach (Mesh mesh in owned) Object.Destroy(mesh);
            owned.Clear(); optional.Clear(); TriangleCount = 0;
            RenderSettings.customReflectionTexture = null;
            Object.Destroy(reflection); reflection = null;
        }
    }
}
