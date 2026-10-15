using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    // Only owns I's generated meshes. No primitive or previous art generator.
    internal sealed class ChaseArt : IDisposable
    {
        readonly List<Mesh> meshes = new List<Mesh>();
        internal readonly RebornAnatomy[] Anatomy = new RebornAnatomy[3];

        internal Mesh Own(Mesh mesh) { meshes.Add(mesh); return mesh; }

        internal static Transform Group(Transform parent, string name, Vector3 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            return t;
        }

        public void Dispose()
        {
            foreach (var mesh in meshes) UnityEngine.Object.Destroy(mesh);
            meshes.Clear();
        }
    }
}
