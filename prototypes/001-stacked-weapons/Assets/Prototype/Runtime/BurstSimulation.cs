using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Journey
{
    // Persistent staging buffers retain the reference simulation's managed state and
    // deterministic ordering. Copies and Complete waits are included in stage timings.
    public sealed class BurstCrowd : IDisposable
    {
        readonly NativeArray<Vector3> input = new NativeArray<Vector3>(CombatSimulation.Capacity, Allocator.Persistent);
        readonly NativeArray<Vector3> output = new NativeArray<Vector3>(CombatSimulation.Capacity, Allocator.Persistent);
        readonly NativeArray<int> next = new NativeArray<int>(CombatSimulation.Capacity, Allocator.Persistent);
        readonly NativeArray<int> cells = new NativeArray<int>(1024, Allocator.Persistent);
        readonly NativeArray<int2> results = new NativeArray<int2>(CombatSimulation.Capacity, Allocator.Persistent);
        readonly NativeArray<int2> totals = new NativeArray<int2>(1, Allocator.Persistent);

        public int Step(Vector3[] positions, int count, int start, Vector3 player, float dt, int batch, out bool contact)
        {
            contact = false;
            if (count == 0) return 0;
            NativeArray<Vector3>.Copy(positions, input, count);
            int checks = Run(input, output, count, start, player, dt, batch, out contact);
            NativeArray<Vector3>.Copy(output, positions, count);
            return checks;
        }
        public int StepNative(NativeArray<Vector3> positions, NativeArray<Vector3> previous, int count, int start,
            Vector3 player, float dt, int batch, out bool contact)
        {
            contact = false; if (count == 0) return 0;
            NativeArray<Vector3>.Copy(positions, previous, count);
            return Run(previous, positions, count, start, player, dt, batch, out contact);
        }
        int Run(NativeArray<Vector3> source, NativeArray<Vector3> destination, int count, int start,
            Vector3 player, float dt, int batch, out bool contact)
        {
            JobHandle grid = new GridJob { Positions = source, Cells = cells, Next = next, Count = count, Start = start }.Schedule();
            JobHandle move = new MoveJob { Positions = source, Output = destination, Cells = cells, Next = next,
                Results = results, Player = player, Dt = dt }.Schedule(count, batch, grid);
            new SumJob { Results = results, Totals = totals, Count = count }.Schedule(move).Complete();
            contact = totals[0].y > 0;
            return totals[0].x;
        }
        public void Dispose() { input.Dispose(); output.Dispose(); next.Dispose(); cells.Dispose(); results.Dispose(); totals.Dispose(); }
        static int CellX(float v) => math.clamp((int)((v + 32) / 2), 0, 31);

        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Strict)]
        struct GridJob : IJob
        {
            [ReadOnly] public NativeArray<Vector3> Positions;
            public NativeArray<int> Cells, Next;
            public int Count, Start;
            public void Execute()
            {
                for (int i = 0; i < Cells.Length; i++) Cells[i] = -1;
                for (int n = 0; n < Count; n++)
                {
                    int i = (n + Start) % Count;
                    Vector3 p = Positions[i]; int cell = CellX(p.z) * 32 + CellX(p.x);
                    Next[i] = Cells[cell]; Cells[cell] = i;
                }
            }
        }
        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Strict)]
        struct MoveJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> Positions;
            [ReadOnly] public NativeArray<int> Cells, Next;
            [WriteOnly] public NativeArray<Vector3> Output;
            [WriteOnly] public NativeArray<int2> Results;
            public float3 Player;
            public float Dt;
            public void Execute(int i)
            {
                float3 p = Positions[i], separation = 0, toward = Player - p; toward.y = 0;
                int cx = CellX(p.x), cz = CellX(p.z), checks = 0;
                for (int z = math.max(0, cz - 1); z <= math.min(31, cz + 1); z++)
                for (int x = math.max(0, cx - 1); x <= math.min(31, cx + 1); x++)
                {
                    int visited = 0;
                    for (int j = Cells[z * 32 + x]; j != -1 && visited < CombatSimulation.NeighborsPerCell; j = Next[j], visited++)
                    {
                        checks++; if (j == i) continue;
                        float3 delta = p - (float3)Positions[j]; float distance = math.dot(delta, delta);
                        if (distance > .0001f && distance < 1.44f)
                            separation += delta * ((1.2f - math.sqrt(distance)) / math.sqrt(distance));
                    }
                }
                float length = math.sqrt(math.dot(toward, toward));
                float3 velocity = (length > .00001f ? toward / length : float3.zero) * 3.1f + separation * 5;
                float sqr = math.dot(velocity, velocity);
                if (sqr > 25) velocity = velocity / math.sqrt(sqr) * 5;
                p += velocity * Dt;
                p.x = math.clamp(p.x, -CombatSimulation.ArenaLimit, CombatSimulation.ArenaLimit);
                p.z = math.clamp(p.z, -CombatSimulation.ArenaLimit, CombatSimulation.ArenaLimit);
                Output[i] = p;
                float3 difference = p - Player; difference.y = 0;
                Results[i] = new int2(checks, math.dot(difference, difference) < 1.25f ? 1 : 0);
            }
        }
        [BurstCompile(CompileSynchronously = true)]
        struct SumJob : IJob
        {
            [ReadOnly] public NativeArray<int2> Results;
            public NativeArray<int2> Totals;
            public int Count;
            public void Execute() { int2 sum = 0; for (int i = 0; i < Count; i++) sum += Results[i]; Totals[0] = sum; }
        }
    }

    public sealed class BurstProjectiles : IDisposable
    {
        readonly NativeArray<Vector3> positions = new NativeArray<Vector3>(BossAbilities.ProjectileCapacity, Allocator.Persistent);
        readonly NativeArray<Vector3> previous = new NativeArray<Vector3>(BossAbilities.ProjectileCapacity, Allocator.Persistent);
        readonly NativeArray<Vector3> velocities = new NativeArray<Vector3>(BossAbilities.ProjectileCapacity, Allocator.Persistent);
        readonly NativeArray<float> life = new NativeArray<float>(BossAbilities.ProjectileCapacity, Allocator.Persistent);
        readonly NativeArray<byte> hits = new NativeArray<byte>(BossAbilities.ProjectileCapacity, Allocator.Persistent);
        readonly NativeArray<int2> totals = new NativeArray<int2>(1, Allocator.Persistent);

        public int Step(Vector3[] p, Vector3[] old, Vector3[] v, float[] ttl, int count,
            Vector3 player, Vector3 previousPlayer, float dt, int batch, out int hitCount)
        {
            hitCount = 0; if (count == 0) return 0;
            NativeArray<Vector3>.Copy(p, positions, count); NativeArray<Vector3>.Copy(v, velocities, count);
            NativeArray<float>.Copy(ttl, life, count);
            int remaining = StepNative(positions, previous, velocities, life, count, player, previousPlayer, dt, batch, out hitCount);
            NativeArray<Vector3>.Copy(positions, p, remaining); NativeArray<Vector3>.Copy(previous, old, remaining);
            NativeArray<Vector3>.Copy(velocities, v, remaining); NativeArray<float>.Copy(life, ttl, remaining);
            return remaining;
        }
        public int StepNative(NativeArray<Vector3> positions, NativeArray<Vector3> previous,
            NativeArray<Vector3> velocities, NativeArray<float> life, int count,
            Vector3 player, Vector3 previousPlayer, float dt, int batch, out int hitCount)
        {
            hitCount = 0; if (count == 0) return 0;
            JobHandle advance = new AdvanceJob { Positions = positions, Previous = previous, Velocities = velocities,
                Life = life, Hits = hits, Player = player, PreviousPlayer = previousPlayer, Dt = dt }.Schedule(count, batch);
            new CompactJob { Positions = positions, Previous = previous, Velocities = velocities,
                Life = life, Hits = hits, Totals = totals, Count = count }.Schedule(advance).Complete();
            int remaining = totals[0].x; hitCount = totals[0].y;
            return remaining;
        }
        public void Dispose() { positions.Dispose(); previous.Dispose(); velocities.Dispose(); life.Dispose(); hits.Dispose(); totals.Dispose(); }

        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Strict)]
        struct AdvanceJob : IJobParallelFor
        {
            public NativeArray<Vector3> Positions;
            [WriteOnly] public NativeArray<Vector3> Previous;
            [ReadOnly] public NativeArray<Vector3> Velocities;
            public NativeArray<float> Life;
            [WriteOnly] public NativeArray<byte> Hits;
            public float3 Player, PreviousPlayer;
            public float Dt;
            public void Execute(int i)
            {
                float3 a = Positions[i], b = a + (float3)Velocities[i] * Dt;
                Previous[i] = a; Positions[i] = b; Life[i] -= Dt;
                a -= PreviousPlayer; b -= Player;
                float3 delta = b - a; float sqr = math.dot(delta, delta);
                float t = sqr > .000001f ? math.clamp(-math.dot(a, delta) / sqr, 0, 1) : 0;
                float3 closest = a + delta * t;
                Hits[i] = (byte)(math.dot(closest, closest) <= .49f ? 1 : 0);
            }
        }
        [BurstCompile(CompileSynchronously = true)]
        struct CompactJob : IJob
        {
            public NativeArray<Vector3> Positions, Previous, Velocities;
            public NativeArray<float> Life;
            [ReadOnly] public NativeArray<byte> Hits;
            public NativeArray<int2> Totals;
            public int Count;
            public void Execute()
            {
                int remaining = Count, hits = 0;
                // Preserve the reference path's descending swap-remove order.
                for (int i = Count - 1; i >= 0; i--)
                {
                    hits += Hits[i];
                    if (Hits[i] == 0 && Life[i] > 0) continue;
                    int last = --remaining;
                    Positions[i] = Positions[last]; Previous[i] = Previous[last];
                    Velocities[i] = Velocities[last]; Life[i] = Life[last];
                }
                Totals[0] = new int2(remaining, hits);
            }
        }
    }
}
