using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.FantasyTrialB
{
    public sealed class ArtPalette : IDisposable
    {
        public PhotoRock PhotoRock { get; }
        public SkyPanorama Sky { get; }
        public Material Ground, Rock, Concrete, Bark, Foliage, Steel, Dark, Paint, Brass,
            Black, Glass, Amber, Teal, Blue, Cloth, Skin, White;
        readonly List<Material> materials = new List<Material>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        readonly Shader shader;

        public ArtPalette(Shader worldShader)
        {
            shader = worldShader != null ? worldShader : Shader.Find("Standard");
            Ground = Surface("ground", new Color(0.68f, 0.74f, 0.66f), 0, 0.12f, 14);
            Rock = Surface("concrete", new Color(0.62f, 0.65f, 0.61f), 0, 0.18f, 2);
            Concrete = Surface("concrete", Color.white, 0, 0.14f, 2);
            Bark = Surface("bark", new Color(0.65f, 0.62f, 0.53f), 0, 0.12f, 2);
            Steel = Surface("metal", new Color(0.7f, 0.72f, 0.68f), 0.65f, 0.4f, 1);
            Dark = Make(new Color(0.085f, 0.1f, 0.105f), 0.72f, 0.42f);
            Paint = Surface("metal", new Color(0.5f, 0.58f, 0.48f), 0.3f, 0.22f, 1);
            Brass = Make(new Color(0.59f, 0.39f, 0.17f), 0.7f, 0.48f);
            Black = Make(new Color(0.018f, 0.021f, 0.02f), 0.05f, 0.15f);
            Glass = Make(new Color(0.12f, 0.25f, 0.23f), 0.7f, 0.92f);
            Cloth = Make(new Color(0.12f, 0.16f, 0.13f), 0, 0.1f);
            Skin = Make(new Color(0.42f, 0.28f, 0.18f), 0, 0.16f);
            White = Make(new Color(0.63f, 0.63f, 0.53f), 0.2f, 0.3f);
            Amber = Make(new Color(1, 0.44f, 0.06f), 0.3f, 0.5f, 1.5f);
            Teal = Make(new Color(0.12f, 0.75f, 0.57f), 0.3f, 0.5f, 1.4f);
            Blue = Make(new Color(0.35f, 0.59f, 1), 0.3f, 0.5f, 1.5f);
            Foliage = Make(new Color(0.24f, 0.33f, 0.16f), 0, 0.1f);
            Foliage.SetFloat("_Cull", 0);
            Foliage.enableInstancing = true;
            PhotoRock = new PhotoRock(shader);
            Sky = new SkyPanorama();
        }

        public Material Make(Color color, float metallic = 0, float smoothness = 0.25f, float emission = 0)
        {
            var mat = new Material(shader) { color = color, enableInstancing = true };
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);
            if (emission > 0)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
            }
            materials.Add(mat);
            return mat;
        }

        Material Surface(string prefix, Color color, float metal, float smoothness, float tiling)
        {
            var mat = Make(color, metal, smoothness);
            mat.mainTexture = Load(prefix + "_diff.jpg", false);
            mat.mainTextureScale = Vector2.one * tiling;
            mat.SetTexture("_BumpMap", Load(prefix + "_normal.jpg", true));
            mat.SetTextureScale("_BumpMap", Vector2.one * tiling);
            mat.SetFloat("_BumpScale", 0.65f);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_OcclusionMap", Load(prefix + "_ao.jpg", true));
            mat.SetTextureScale("_OcclusionMap", Vector2.one * tiling);
            mat.SetFloat("_OcclusionStrength", 0.7f);
            return mat;
        }

        Texture2D Load(string name, bool linear)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "FantasyTrialB", name);
            if (!File.Exists(path)) throw new FileNotFoundException("Showcase texture is missing.", path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
                throw new InvalidOperationException("Showcase texture could not decode: " + name);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.anisoLevel = 8;
            textures.Add(texture);
            return texture;
        }

        public Material Accent(WeaponKind kind)
        {
            return kind == WeaponKind.Ember ? Amber : kind == WeaponKind.Whirlwind ? Teal :
                kind == WeaponKind.Spark ? Blue : Brass;
        }

        public void Dispose()
        {
            PhotoRock.Dispose();
            Sky.Dispose();
            foreach (var mat in materials) UnityEngine.Object.Destroy(mat);
            foreach (var texture in textures) UnityEngine.Object.Destroy(texture);
        }
    }

    public static class ArtParts
    {
        public static readonly List<Mesh> GeneratedMeshes = new List<Mesh>();
        static readonly Dictionary<Vector4, Mesh> boxMeshes = new Dictionary<Vector4, Mesh>();

        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size,
            Material material, float bevel = 0.025f, bool solid = false, int layer = 2)
        {
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.layer = layer;
            var mesh = BeveledBox(size, bevel);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (solid) obj.AddComponent<BoxCollider>().size = size;
            return obj;
        }

        public static GameObject Cylinder(string name, Transform parent, Vector3 position,
            float radius, float length, Material material, int layer = 2)
        {
            return Primitive(name, PrimitiveType.Cylinder, parent, position,
                new Vector3(radius * 2, length / 2, radius * 2), material, layer);
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 position,
            Vector3 size, Material material, int layer = 2)
        {
            return Primitive(name, PrimitiveType.Sphere, parent, position, size, material, layer);
        }

        static GameObject Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 size, Material material, int layer)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.layer = layer;
            var collider = obj.GetComponent<Collider>();
            collider.enabled = false;
            UnityEngine.Object.Destroy(collider);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        public static GameObject Ring(string name, Transform parent, Vector3 position,
            float radius, float tube, Material material, int segments = 24)
        {
            const int sides = 8;
            var vertices = new Vector3[segments * sides];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * sides * 6];
            for (int i = 0; i < segments; i++)
                for (int j = 0; j < sides; j++)
                {
                    float a = i * Mathf.PI * 2 / segments, b = j * Mathf.PI * 2 / sides;
                    Vector3 radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                    Vector3 normal = radial * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    int v = i * sides + j;
                    vertices[v] = radial * radius + normal * tube;
                    normals[v] = normal;
                    uv[v] = new Vector2(i / (float)segments, j / (float)sides);
                    int nextI = (i + 1) % segments, nextJ = (j + 1) % sides, t = v * 6;
                    triangles[t] = v; triangles[t + 1] = nextI * sides + j; triangles[t + 2] = nextI * sides + nextJ;
                    triangles[t + 3] = v; triangles[t + 4] = nextI * sides + nextJ; triangles[t + 5] = i * sides + nextJ;
                }
            var mesh = new Mesh { name = name, vertices = vertices, normals = normals, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            GeneratedMeshes.Add(mesh);
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            obj.layer = 2;
            return obj;
        }

        static Mesh BeveledBox(Vector3 size, float bevel)
        {
            var key = new Vector4(size.x, size.y, size.z, bevel);
            if (boxMeshes.TryGetValue(key, out var existing)) return existing;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            var half = size / 2;
            float r = Mathf.Min(Mathf.Min(half.x, half.y), half.z) * 0.8f;
            r = Mathf.Min(r, Mathf.Max(0.001f, bevel));
            Vector3 inner = half - Vector3.one * r;
            Vector3[] axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 normal in axes)
            {
                Vector3 u = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.Cross(Vector3.up, normal);
                Vector3 v = Vector3.Cross(normal, u);
                float hn = Vector3.Dot(half, new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z)));
                float hu = Vector3.Dot(half, new Vector3(Mathf.Abs(u.x), Mathf.Abs(u.y), Mathf.Abs(u.z)));
                float hv = Vector3.Dot(half, new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z)));
                float[] us = { -hu, -hu + r, hu - r, hu }, vs = { -hv, -hv + r, hv - r, hv };
                int start = vertices.Count;
                for (int y = 0; y < 4; y++)
                    for (int x = 0; x < 4; x++)
                    {
                        Vector3 p = normal * hn + u * us[x] + v * vs[y];
                        Vector3 clamped = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x),
                            Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                        Vector3 n = (p - clamped).normalized;
                        vertices.Add(clamped + n * r);
                        normals.Add(n);
                        uv.Add(new Vector2((us[x] / hu + 1) / 2, (vs[y] / hv + 1) / 2));
                    }
                for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 3; x++)
                    {
                        int a = start + y * 4 + x, b = a + 1, c = a + 4, d = c + 1;
                        triangles.Add(a); triangles.Add(b); triangles.Add(d);
                        triangles.Add(a); triangles.Add(d); triangles.Add(c);
                    }
            }
            var mesh = new Mesh { name = "Original beveled hard-surface part" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            GeneratedMeshes.Add(mesh);
            boxMeshes.Add(key, mesh);
            return mesh;
        }

        public static void CombineStatic(Transform root)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || filter.sharedMesh == null) continue;
                if (filter.sharedMesh.name == "Pinned CC0 rock_face_01 photogrammetry") continue;
                var mat = renderer.sharedMaterial;
                if (!groups.TryGetValue(mat, out var items)) groups.Add(mat, items = new List<CombineInstance>());
                items.Add(new CombineInstance { mesh = filter.sharedMesh,
                    transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                renderer.enabled = false;
            }
            foreach (var group in groups)
            {
                var obj = new GameObject("Batched " + group.Key.name, typeof(MeshFilter), typeof(MeshRenderer));
                obj.transform.SetParent(root, false);
                var mesh = new Mesh { name = "Original batched showcase geometry", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                GeneratedMeshes.Add(mesh);
                obj.GetComponent<MeshFilter>().sharedMesh = mesh;
                obj.GetComponent<MeshRenderer>().sharedMaterial = group.Key;
                obj.layer = 2;
            }
        }

        public static void DisposeMeshes()
        {
            foreach (var mesh in GeneratedMeshes) UnityEngine.Object.Destroy(mesh);
            GeneratedMeshes.Clear();
            boxMeshes.Clear();
        }
    }
}
