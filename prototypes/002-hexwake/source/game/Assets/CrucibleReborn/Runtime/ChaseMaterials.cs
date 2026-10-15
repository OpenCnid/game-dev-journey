using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.CrucibleReborn
{
    // Built-in Standard, not an SRP material. External CC0 maps are loaded once per kind.
    internal static class ChaseMaterials
    {
        static readonly string[] surfaceNames = { "dark_rock", "rust_coarse_01", "sandstone_cracks",
            "brown_leather", "brown_leather", "burned_ground_01" };
        static readonly Texture2D[] surfaces = new Texture2D[6];
        static readonly Texture2D[] normals = new Texture2D[6];
        static readonly Texture2D[] occlusion = new Texture2D[6];
        static readonly Texture2D[] metallicSmoothness = new Texture2D[6];
        static Texture2D fog;
        static Texture2D fire;

        internal static Texture2D Flame()
        {
            if (fire != null) return fire;
            const int size = 128;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + .5f) / size, v = (y + .5f) / size;
                    float width = .43f * Mathf.Pow(1 - v, .7f) + .015f;
                    float sway = .08f * Mathf.Sin(v * 12);
                    float center = Mathf.Clamp01(1 - Mathf.Abs(u - .5f - sway) / width);
                    float alpha = Mathf.Pow(center, .6f) * Mathf.Pow(Mathf.Sin(v * Mathf.PI), .4f);
                    alpha *= .55f + .45f * Mathf.PerlinNoise(u * 9, v * 16);
                    Color color = Color.Lerp(new Color(1, .055f, .005f),
                        new Color(1, .88f, .35f), center * (1 - v * .7f));
                    color.a = alpha; pixels[y * size + x] = color;
                }
            fire = new Texture2D(size, size, TextureFormat.RGBA32, true) {
                name = "H tapered fire ribbons", wrapMode = TextureWrapMode.Clamp
            };
            fire.SetPixels(pixels); fire.Apply();
            return fire;
        }

        internal static void Weather(Material material, int kind, float repeats = 1)
        {
            if (surfaces[kind] == null && !LoadSurface(kind)) surfaces[kind] = Paint(kind);
            material.mainTexture = surfaces[kind];
            material.mainTextureScale = Vector2.one * repeats;
            material.SetTexture("_BumpMap", normals[kind]);
            material.SetTextureScale("_BumpMap", Vector2.one * repeats);
            material.SetFloat("_BumpScale", 1);
            material.SetTexture("_OcclusionMap", occlusion[kind]);
            material.SetTextureScale("_OcclusionMap", Vector2.one * repeats);
            material.SetFloat("_OcclusionStrength", .85f);
            material.SetTexture("_MetallicGlossMap", metallicSmoothness[kind]);
            material.SetTextureScale("_MetallicGlossMap", Vector2.one * repeats);
            material.SetFloat("_GlossMapScale", 1);
            // Smoothness comes from the packed map alpha, never from albedo alpha.
            material.SetFloat("_SmoothnessTextureChannel", 0);
            material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICGLOSSMAP");
        }

        internal static void WeatherAsh(Material material, float repeats = 1)
        {
            Weather(material, 5, repeats);
        }

        static bool LoadSurface(int kind)
        {
            // Hide and leather have the same asset. Their artistic recolor remains
            // on the caller's material, not in a second 2K copy of every map.
            int shared = kind == 3 ? 4 : kind == 4 ? 3 : -1;
            if (shared >= 0 && surfaces[shared] != null)
            {
                surfaces[kind] = surfaces[shared]; normals[kind] = normals[shared];
                occlusion[kind] = occlusion[shared];
                metallicSmoothness[kind] = metallicSmoothness[shared];
                return true;
            }
            string directory = Path.Combine(Application.streamingAssetsPath, "InfernalRealism",
                surfaceNames[kind]);
            // This retained Windows player uses filesystem StreamingAssets, not Android/WebGL URLs.
            Texture2D albedo = null, normal = null, ao = null, roughness = null;
            try
            {
                string prefix = surfaceNames[kind];
                albedo = LoadMap(Path.Combine(directory, prefix +
                    (kind == 3 || kind == 4 ? "_albedo_2k.jpg" : "_diff_2k.jpg")), false);
                normal = LoadMap(Path.Combine(directory, prefix + "_nor_gl_2k.jpg"), true);
                ao = LoadMap(Path.Combine(directory, prefix + "_ao_2k.jpg"), true);
                roughness = LoadMap(Path.Combine(directory, prefix + "_rough_2k.jpg"), true);
                if (kind == 2)
                {
                    // This is adapted sandstone, not a bone scan. Retain its cracks
                    // but remove the mineral hue before the caller's ivory tint.
                    var mineral = albedo.GetPixels32();
                    for (int i = 0; i < mineral.Length; i++)
                    {
                        var source = mineral[i];
                        float shade = .35f + .65f *
                            (.299f * source.r + .587f * source.g + .114f * source.b) / 255f;
                        mineral[i] = new Color32((byte)(238 * shade), (byte)(226 * shade),
                            (byte)(198 * shade), 255);
                    }
                    albedo.SetPixels32(mineral);
                }
                var normalPixels = normal.GetPixels32();
                for (int i = 0; i < normalPixels.Length; i++)
                {
                    var source = normalPixels[i];
                    // OpenGL tangent normal (+Y). Desktop Standard UnpackNormalmapRGorAG
                    // multiplies R by A: R=1, A=X supports its DXT5nm/RG-or-AG path.
                    normalPixels[i] = new Color32(255, source.g, source.b, source.r);
                }
                // LoadImage changes JPG textures to RGB24 even when constructed as RGBA32.
                // Allocate explicit RGBA maps so normal X / smoothness survive in alpha.
                var packedNormal = LinearMap(normal.name + " Standard packed", normal.width,
                    normal.height, normalPixels);
                UnityEngine.Object.Destroy(normal); normal = packedNormal;
                var roughPixels = roughness.GetPixels32();
                for (int i = 0; i < roughPixels.Length; i++)
                {
                    byte rough = roughPixels[i].r;
                    // No metalness map was acquired. Artistic iron/oxide approximation:
                    // the roughest rust is dielectric, exposed smoother iron is metallic.
                    byte metal = kind == 1 ? (byte)(255 * Mathf.Lerp(.9f, .08f,
                        Mathf.InverseLerp(.4f, .9f, rough / 255f))) : (byte)0;
                    roughPixels[i] = new Color32(metal, 0, 0, (byte)(255 - rough));
                }
                var packedRoughness = LinearMap(roughness.name + " metallic smoothness",
                    roughness.width, roughness.height, roughPixels);
                UnityEngine.Object.Destroy(roughness); roughness = packedRoughness;
                // Standard samples AO from green. Source AO is greyscale.
                albedo.Apply(true, true); ao.Apply(true, true);
                surfaces[kind] = albedo; normals[kind] = normal;
                occlusion[kind] = ao; metallicSmoothness[kind] = roughness;
                return true;
            }
            catch (Exception exception) when (exception is IOException ||
                exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                UnityEngine.Object.Destroy(albedo); UnityEngine.Object.Destroy(normal);
                UnityEngine.Object.Destroy(ao); UnityEngine.Object.Destroy(roughness);
                Debug.LogWarning("H PBR surface unavailable at " + directory +
                    "; using generated relief fallback. " + exception.Message);
                return false;
            }
        }

        static Texture2D LoadMap(string path, bool linear)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear) {
                name = "H " + Path.GetFileName(Path.GetDirectoryName(path)) + " " +
                    Path.GetFileNameWithoutExtension(path),
                wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear,
                anisoLevel = 16
            };
            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                throw new IOException("Cannot decode PBR image " + path);
            }
            return texture;
        }

        internal static void Mist(Material material)
        {
            if (fog == null)
            {
                const int size = 128;
                fog = new Texture2D(size, size, TextureFormat.RGBA32, true);
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float u = (x + .5f) / size, v = (y + .5f) / size;
                        float edge = Mathf.Pow(Mathf.Sin(u * Mathf.PI) * Mathf.Sin(v * Mathf.PI), 2);
                        float cloud = .35f + .65f * Mathf.PerlinNoise(u * 5, v * 5);
                        pixels[y * size + x] = new Color(.8f, .78f, .74f, edge * cloud);
                    }
                fog.SetPixels(pixels); fog.Apply();
                fog.wrapMode = TextureWrapMode.Clamp;
            }
            material.mainTexture = fog;
        }

        static Texture2D Paint(int kind)
        {
            const int size = 512;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) {
                name = "H fallback " + new[] { "mortared stone", "pitted iron", "cracked bone",
                    "scarred hide", "worn leather", "burned ash" }[kind],
                wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 16
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    uint hash = unchecked((uint)(x * 374761393 + y * 668265263 + kind * 7919));
                    hash = (hash ^ (hash >> 13)) * 1274126177u;
                    float grain = (hash & 65535) / 65535f;
                    float broad = TileNoise(x / (float)size, y / (float)size, 8, kind * 3);
                    float fine = TileNoise(x / (float)size, y / (float)size, 32, kind);
                    float pores = TileNoise(x / (float)size, y / (float)size, 96, kind + 11);
                    float shade = .55f + broad * .3f + grain * .12f;
                    Color tint = Color.white;
                    if (kind == 0)
                    {
                        int row = y / 64, bx = (x + (row % 2) * 64) % 128;
                        int edge = Math.Min(Math.Min(bx, 127 - bx), Math.Min(y % 64, 63 - y % 64));
                        bool mortar = edge < 2 || (edge < 6 && fine < .32f);
                        shade = mortar ? .22f + grain * .08f : .53f + broad * .35f + grain * .1f;
                        float block = Mathf.PerlinNoise((x + (row % 2) * 64) / 128f, row * 3.7f);
                        if (!mortar) shade *= .7f + block * .5f;
                        if (fine < .38f) shade *= .65f;
                        tint = edge < 7 && broad < .48f ? new Color(.56f, .55f, .39f) :
                            Color.Lerp(new Color(.72f, .68f, .58f), new Color(1, .91f, .77f), broad);
                    }
                    else if (kind == 1)
                    {
                        bool scratch = Mathf.PerlinNoise(x / 2f, y / 45f) > .72f;
                        shade = scratch ? .95f : .45f + grain * .2f + broad * .3f;
                        if (fine < .35f) tint = new Color(.72f, .38f, .19f);
                    }
                    else if (kind == 2)
                    {
                        shade = .64f + broad * .3f;
                        if (fine < .3f && broad < .55f) shade *= .65f;
                        shade *= .8f + .2f * Mathf.PerlinNoise(x / 3f, y / 70f);
                        tint = Color.Lerp(new Color(.68f, .48f, .29f), new Color(1, .94f, .78f), broad);
                    }
                    else if (kind == 3)
                    {
                        shade = .5f + broad * .45f;
                        tint = fine < .4f ? new Color(.64f, .42f, .35f) : new Color(.94f, .79f, .72f);
                        if (Mathf.Abs(Mathf.Sin(x * .035f + y * .09f + broad * 4)) < .08f) shade *= .55f;
                    }
                    else if (kind == 4)
                    {
                        shade = .5f + broad * .3f + grain * .1f;
                        if (fine < .25f || (y % 48 < 2 && x % 8 < 3)) shade *= .5f;
                        tint = new Color(.91f, .72f, .54f);
                    }
                    else
                    {
                        shade = .24f + broad * .3f + fine * .2f;
                        tint = Color.Lerp(new Color(.24f, .22f, .2f),
                            new Color(.65f, .6f, .52f), pores);
                    }
                    pixels[y * size + x] = tint * shade * (.86f + .14f * pores);
                    pixels[y * size + x].a = 1;
                }
            texture.SetPixels(pixels); texture.Apply(true, true);
            var normalPixels = new Color[pixels.Length];
            var aoPixels = new Color[pixels.Length];
            var packedPixels = new Color[pixels.Length];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = pixels[y * size + (x + 1) % size].grayscale -
                        pixels[y * size + (x + size - 1) % size].grayscale;
                    float dy = pixels[((y + 1) % size) * size + x].grayscale -
                        pixels[((y + size - 1) % size) * size + x].grayscale;
                    Vector3 n = new Vector3(-dx * 5, -dy * 5, 1).normalized;
                    int index = y * size + x;
                    normalPixels[index] = new Color(1, n.y * .5f + .5f,
                        n.z * .5f + .5f, n.x * .5f + .5f);
                    float height = pixels[index].grayscale;
                    aoPixels[index] = Color.white * Mathf.Lerp(.65f, 1, height);
                    packedPixels[index] = new Color(kind == 1 ? .9f : 0, 0, 0,
                        Mathf.Lerp(kind == 1 ? .24f : .08f, kind == 1 ? .55f : .28f, height));
                }
            normals[kind] = new Texture2D(size, size, TextureFormat.RGBA32, true, true) {
                name = texture.name + " relief", wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear, anisoLevel = 16
            };
            normals[kind].SetPixels(normalPixels); normals[kind].Apply(true, true);
            occlusion[kind] = LinearMap(texture.name + " AO", size, aoPixels);
            metallicSmoothness[kind] = LinearMap(texture.name + " metallic smoothness", size, packedPixels);
            return texture;
        }

        static float TileNoise(float u, float v, float frequency, float seed)
        {
            float x = u * frequency + seed, y = v * frequency + seed;
            return Mathf.Lerp(Mathf.Lerp(Mathf.PerlinNoise(x, y),
                Mathf.PerlinNoise(x - frequency, y), u),
                Mathf.Lerp(Mathf.PerlinNoise(x, y - frequency),
                Mathf.PerlinNoise(x - frequency, y - frequency), u), v);
        }

        static Texture2D LinearMap(string name, int size, Color[] pixels)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true) {
                name = name, wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear, anisoLevel = 16
            };
            texture.SetPixels(pixels); texture.Apply(true, true);
            return texture;
        }

        static Texture2D LinearMap(string name, int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true, true) {
                name = name, wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear, anisoLevel = 16
            };
            texture.SetPixels32(pixels); texture.Apply(true, true);
            return texture;
        }

        internal static void ProjectUV(Mesh mesh, float scale)
        {
            var vertices = mesh.vertices;
            if (mesh.normals.Length != vertices.Length) mesh.RecalculateNormals();
            var sourceNormals = mesh.normals; var sourceColors = mesh.colors;
            var sourceUv2 = mesh.uv2;
            var positions = new List<Vector3>(); var shadingNormals = new List<Vector3>();
            var uv = new List<Vector2>(); var colors = new List<Color>(); var uv2 = new List<Vector2>();
            var submeshes = new int[mesh.subMeshCount][];
            // A single projection per triangle avoids a UV fold when its vertices choose
            // different axes. Split projection seams while retaining authored smooth normals.
            for (int submesh = 0; submesh < submeshes.Length; submesh++)
            {
                var indices = mesh.GetTriangles(submesh);
                var output = new int[indices.Length];
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 face = Vector3.Cross(vertices[indices[i + 1]] - vertices[indices[i]],
                        vertices[indices[i + 2]] - vertices[indices[i]]);
                    float ax = Mathf.Abs(face.x), ay = Mathf.Abs(face.y), az = Mathf.Abs(face.z);
                    for (int corner = 0; corner < 3; corner++)
                    {
                        int index = indices[i + corner]; var p = vertices[index];
                        output[i + corner] = positions.Count;
                        positions.Add(p); shadingNormals.Add(sourceNormals[index]);
                        uv.Add((ay >= ax && ay >= az ? new Vector2(p.x, p.z) :
                            az >= ax ? new Vector2(p.x, p.y) : new Vector2(p.z, p.y)) * scale);
                        if (sourceColors.Length == vertices.Length) colors.Add(sourceColors[index]);
                        if (sourceUv2.Length == vertices.Length) uv2.Add(sourceUv2[index]);
                    }
                }
                submeshes[submesh] = output;
            }
            mesh.Clear();
            mesh.indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(positions); mesh.SetNormals(shadingNormals); mesh.SetUVs(0, uv);
            if (colors.Count > 0) mesh.SetColors(colors);
            if (uv2.Count > 0) mesh.SetUVs(1, uv2);
            mesh.subMeshCount = submeshes.Length;
            for (int i = 0; i < submeshes.Length; i++) mesh.SetTriangles(submeshes[i], i);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        internal static void Dispose()
        {
            // Shared leather/hide maps are destroyed once, then all cache slots reset.
            var released = new HashSet<Texture2D>();
            for (int i = 0; i < surfaces.Length; i++)
            {
                foreach (var texture in new[] { surfaces[i], normals[i], occlusion[i], metallicSmoothness[i] })
                    if (texture != null && released.Add(texture)) UnityEngine.Object.Destroy(texture);
                surfaces[i] = null; normals[i] = null;
                occlusion[i] = null; metallicSmoothness[i] = null;
            }
            UnityEngine.Object.Destroy(fog); fog = null;
            UnityEngine.Object.Destroy(fire); fire = null;
        }
    }
}
