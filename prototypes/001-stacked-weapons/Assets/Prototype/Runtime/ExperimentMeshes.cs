using UnityEngine;

namespace Journey
{
    public static class ExperimentMeshes
    {
        public static Mesh Octahedron()
        {
            var mesh = new Mesh { name = "Projectile octahedron",
                vertices = new[] { Vector3.up * .5f, Vector3.down * .5f, Vector3.right * .5f,
                    Vector3.forward * .5f, Vector3.left * .5f, Vector3.back * .5f },
                triangles = new[] { 0,3,2, 0,4,3, 0,5,4, 0,2,5, 1,2,3, 1,3,4, 1,4,5, 1,5,2 } };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        // Same body/head geometry and normals, one material with a two-color point-sampled atlas.
        public static Mesh Character(Mesh cube)
        {
            Vector3[] source = cube.vertices, normals = cube.normals;
            int[] triangles = cube.triangles;
            var v = new Vector3[source.Length * 2]; var n = new Vector3[v.Length]; var uv = new Vector2[v.Length];
            var indices = new int[triangles.Length * 2];
            for (int part = 0; part < 2; part++)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    int index = part * source.Length + i;
                    v[index] = Vector3.Scale(source[i], part == 0 ? new Vector3(.85f, 1.4f, .85f) : Vector3.one * .45f)
                        + Vector3.up * (part == 0 ? .8f : 1.6f);
                    n[index] = normals[i]; uv[index] = new Vector2(part == 0 ? .25f : .75f, .5f);
                }
                for (int i = 0; i < triangles.Length; i++) indices[part * triangles.Length + i] = triangles[i] + part * source.Length;
            }
            var mesh = new Mesh { name = "Combined drone", vertices = v, normals = n, uv = uv, triangles = indices };
            mesh.RecalculateBounds(); return mesh;
        }
    }
}
