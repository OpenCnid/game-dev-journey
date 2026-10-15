using System;
using System.IO;
using UnityEngine;

namespace GameDevHarness.FantasyTrialB
{
    // Exact pinned Rock Face 01 contract: one indexed primitive, tightly packed
    // float POSITION/NORMAL/UV arrays and ushort indices. No general glTF importer.
    public sealed class PhotoRock : IDisposable
    {
        public Mesh Mesh { get; }
        public Material Material { get; }
        readonly Texture2D diffuse, normal, occlusion;

        [Serializable] sealed class Document
        {
            public Buffer[] buffers = Array.Empty<Buffer>();
            public BufferView[] bufferViews = Array.Empty<BufferView>();
            public Accessor[] accessors = Array.Empty<Accessor>();
            public MeshDefinition[] meshes = Array.Empty<MeshDefinition>();
        }
        [Serializable] sealed class Buffer { public string uri = ""; public int byteLength = 0; }
        [Serializable] sealed class BufferView { public int byteOffset = 0, byteLength = 0, buffer = 0; }
        [Serializable] sealed class Accessor { public int bufferView = 0, componentType = 0, count = 0, byteOffset = 0; public string type = ""; }
        [Serializable] sealed class MeshDefinition { public Primitive[] primitives = Array.Empty<Primitive>(); }
        [Serializable] sealed class Primitive { public Attributes attributes = new Attributes(); public int indices = 0; }
        [Serializable] sealed class Attributes { public int POSITION = 0, NORMAL = 0, TEXCOORD_0 = 0; }

        public PhotoRock(Shader shader)
        {
            string root = Path.Combine(Application.streamingAssetsPath, "FantasyTrialB/PhotoRock");
            var doc = JsonUtility.FromJson<Document>(File.ReadAllText(Path.Combine(root, "rock_face_01_1k.gltf")));
            if (doc.buffers.Length != 1 || doc.meshes.Length != 1 || doc.meshes[0].primitives.Length != 1)
                throw new InvalidOperationException("Pinned photo-rock mesh contract differs.");
            byte[] buffer = File.ReadAllBytes(Path.Combine(root, doc.buffers[0].uri));
            if (buffer.Length != doc.buffers[0].byteLength)
                throw new InvalidOperationException("Pinned photo-rock buffer length differs.");
            var primitive = doc.meshes[0].primitives[0];
            var positions = doc.accessors[primitive.attributes.POSITION];
            var normals = doc.accessors[primitive.attributes.NORMAL];
            var coordinates = doc.accessors[primitive.attributes.TEXCOORD_0];
            var indices = doc.accessors[primitive.indices];
            if (positions.type != "VEC3" || normals.type != "VEC3" || coordinates.type != "VEC2" ||
                positions.componentType != 5126 || normals.componentType != 5126 ||
                coordinates.componentType != 5126 || indices.componentType != 5123)
                throw new InvalidOperationException("Pinned photo-rock accessor layout differs.");
            var vertices = new Vector3[positions.count];
            var n = new Vector3[positions.count];
            var uv = new Vector2[positions.count];
            var triangles = new int[indices.count];
            int p = doc.bufferViews[positions.bufferView].byteOffset + positions.byteOffset;
            int no = doc.bufferViews[normals.bufferView].byteOffset + normals.byteOffset;
            int u = doc.bufferViews[coordinates.bufferView].byteOffset + coordinates.byteOffset;
            int ix = doc.bufferViews[indices.bufferView].byteOffset + indices.byteOffset;
            var bounds = new Bounds();
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(BitConverter.ToSingle(buffer, p + i * 12),
                    BitConverter.ToSingle(buffer, p + i * 12 + 4), -BitConverter.ToSingle(buffer, p + i * 12 + 8));
                n[i] = new Vector3(BitConverter.ToSingle(buffer, no + i * 12),
                    BitConverter.ToSingle(buffer, no + i * 12 + 4), -BitConverter.ToSingle(buffer, no + i * 12 + 8));
                uv[i] = new Vector2(BitConverter.ToSingle(buffer, u + i * 8), 1 - BitConverter.ToSingle(buffer, u + i * 8 + 4));
                if (i == 0) bounds = new Bounds(vertices[i], Vector3.zero); else bounds.Encapsulate(vertices[i]);
            }
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i] - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                vertices[i] = new Vector3(v.x / bounds.size.x, v.y / bounds.size.y, v.z / bounds.size.z);
                n[i] = new Vector3(n[i].x * bounds.size.x, n[i].y * bounds.size.y, n[i].z * bounds.size.z).normalized;
            }
            for (int i = 0; i < triangles.Length; i += 3)
            {
                triangles[i] = BitConverter.ToUInt16(buffer, ix + i * 2);
                triangles[i + 1] = BitConverter.ToUInt16(buffer, ix + (i + 2) * 2);
                triangles[i + 2] = BitConverter.ToUInt16(buffer, ix + (i + 1) * 2);
            }
            Mesh = new Mesh { name = "Pinned CC0 rock_face_01 photogrammetry", vertices = vertices,
                normals = n, uv = uv, triangles = triangles };
            Mesh.RecalculateBounds(); Mesh.RecalculateTangents();
            ArtParts.GeneratedMeshes.Add(Mesh);
            diffuse = Load(Path.Combine(root, "textures/rock_face_01_diff_1k.jpg"), false);
            normal = Load(Path.Combine(root, "textures/rock_face_01_nor_gl_1k.jpg"), true);
            var arm = Load(Path.Combine(root, "textures/rock_face_01_arm_1k.jpg"), true);
            var pixels = arm.GetPixels32();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(pixels[i].r, pixels[i].r, pixels[i].r, 255);
            occlusion = new Texture2D(arm.width, arm.height, TextureFormat.RGBA32, true, true);
            occlusion.SetPixels32(pixels); occlusion.Apply();
            UnityEngine.Object.Destroy(arm);
            Material = new Material(shader) { name = "CC0 photogrammetry stone", mainTexture = diffuse, enableInstancing = true };
            Material.color = new Color(0.75f, 0.8f, 0.77f);
            Material.SetFloat("_Metallic", 0); Material.SetFloat("_Glossiness", 0.1f);
            Material.SetTexture("_BumpMap", normal); Material.SetFloat("_BumpScale", 0.8f); Material.EnableKeyword("_NORMALMAP");
            Material.SetTexture("_OcclusionMap", occlusion); Material.SetFloat("_OcclusionStrength", 0.6f);
        }

        static Texture2D Load(string path, bool linear)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
                throw new InvalidOperationException("Pinned photo-rock texture could not decode.");
            texture.wrapMode = TextureWrapMode.Repeat; texture.anisoLevel = 8;
            return texture;
        }

        public GameObject Create(Transform parent, Vector3 position, Vector3 size, Quaternion rotation, bool solid)
        {
            var obj = new GameObject("CC0 scanned weathered outcrop", typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = size; obj.transform.localRotation = rotation;
            obj.GetComponent<MeshFilter>().sharedMesh = Mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = Material;
            obj.layer = 8;
            if (solid)
            {
                var collider = obj.AddComponent<BoxCollider>();
                collider.center = Vector3.up * 0.5f; collider.size = Vector3.one;
            }
            return obj;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(Material);
            UnityEngine.Object.Destroy(diffuse); UnityEngine.Object.Destroy(normal); UnityEngine.Object.Destroy(occlusion);
        }
    }
}
