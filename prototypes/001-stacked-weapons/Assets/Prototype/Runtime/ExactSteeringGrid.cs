using System;
using UnityEngine;

namespace Journey
{
    // Conservative broad phase only. Stable IDs preserve the reference's float addition order.
    // Two-metre cells cover the 1.2m separation radius with the complete adjacent 3x3 stencil.
    sealed class ExactSteeringGrid
    {
        const int Width = 32;
        readonly int[] heads = new int[Width * Width], next;
        public readonly int[] Candidates;
        public ExactSteeringGrid(int capacity)
        { next = new int[capacity]; Candidates = new int[capacity]; }
        static int Cell(float position) => Mathf.Clamp(Mathf.FloorToInt((position + 32) * .5f), 0, Width - 1);
        public void Build(SharedEnemy[] enemies)
        {
            for (int i = 0; i < heads.Length; i++) heads[i] = -1;
            for (int i = enemies.Length - 1; i >= 0; i--)
            {
                Vector3 p = enemies[i].Position;
                int cell = Cell(p.z) * Width + Cell(p.x);
                next[i] = heads[cell]; heads[cell] = i;
            }
        }
        public int Gather(Vector3 position)
        {
            int x = Cell(position.x), z = Cell(position.z), count = 0;
            for (int row = Math.Max(0, z - 1); row <= Math.Min(Width - 1, z + 1); row++)
                for (int column = Math.Max(0, x - 1); column <= Math.Min(Width - 1, x + 1); column++)
                    for (int id = heads[row * Width + column]; id >= 0; id = next[id])
                        Candidates[count++] = id;
            Array.Sort(Candidates, 0, count);
            return count;
        }
    }
}
