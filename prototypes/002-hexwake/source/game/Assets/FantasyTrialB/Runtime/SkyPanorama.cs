using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.FantasyTrialB
{
    public sealed class SkyPanorama : IDisposable
    {
        readonly Texture2D texture;
        readonly Material material;
        readonly Mesh mesh;

        public SkyPanorama()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "FantasyTrialB/Sky/sky.jpg");
            var source = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!ImageConversion.LoadImage(source, File.ReadAllBytes(path), false))
                throw new InvalidOperationException("Pinned cloud panorama could not decode.");
            var target = RenderTexture.GetTemporary(2048, 1024, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, target);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            texture = new Texture2D(2048, 1024, TextureFormat.RGB24, true);
            texture.ReadPixels(new Rect(0, 0, 2048, 1024), 0, 0);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Repeat;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.Destroy(source);
            material = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Pinned CC0 cloud panorama", mainTexture = texture,
                color = new Color(0.78f, 0.83f, 0.88f), renderQueue = 1000
            };
            const int columns = 64, rows = 32;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[columns * rows * 6];
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= columns; x++)
                {
                    float latitude = (y / (float)rows - 0.5f) * Mathf.PI;
                    float longitude = x / (float)columns * Mathf.PI * 2;
                    int i = y * (columns + 1) + x;
                    Vector3 point = new Vector3(Mathf.Sin(longitude) * Mathf.Cos(latitude),
                        Mathf.Sin(latitude), Mathf.Cos(longitude) * Mathf.Cos(latitude));
                    vertices[i] = point * 100;
                    normals[i] = -point;
                    uv[i] = new Vector2(1 - x / (float)columns, y / (float)rows);
                    if (x == columns || y == rows) continue;
                    int t = (y * columns + x) * 6;
                    triangles[t] = i; triangles[t + 1] = i + columns + 2; triangles[t + 2] = i + 1;
                    triangles[t + 3] = i; triangles[t + 4] = i + columns + 1; triangles[t + 5] = i + columns + 2;
                }
            mesh = new Mesh { name = "Pinned CC0 panorama backdrop", vertices = vertices,
                normals = normals, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
        }

        public void Create(Transform parent)
        {
            var go = new GameObject("Natural cloud backdrop", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(0, 105, 0);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            go.layer = 2;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(material);
            UnityEngine.Object.Destroy(texture);
            UnityEngine.Object.Destroy(mesh);
        }
    }
}
