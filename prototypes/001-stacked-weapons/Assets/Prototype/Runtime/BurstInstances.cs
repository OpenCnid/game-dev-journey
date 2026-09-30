using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Journey
{
    // CPU frustum culling changes only submitted geometry. Simulation never reads visibility.
    // Stable compaction keeps reference instance order and avoids atomic append contention.
    public sealed class BurstInstances : IDisposable
    {
        const int Capacity = BossAbilities.ProjectileCapacity;
        readonly NativeArray<Vector3> positions = new NativeArray<Vector3>(Capacity, Allocator.Persistent);
        readonly NativeArray<Vector3> previous = new NativeArray<Vector3>(Capacity, Allocator.Persistent);
        NativeArray<float4> planes = new NativeArray<float4>(6, Allocator.Persistent);
        readonly NativeArray<byte> visible = new NativeArray<byte>(Capacity, Allocator.Persistent);
        NativeArray<int> count = new NativeArray<int>(1, Allocator.Persistent);
        public readonly NativeArray<Matrix4x4> A = new NativeArray<Matrix4x4>(Capacity, Allocator.Persistent);
        public readonly NativeArray<Matrix4x4> B = new NativeArray<Matrix4x4>(Capacity, Allocator.Persistent);
        JobHandle pending;
        bool scheduled;

        public int Prepare(Vector3[] current, Vector3[] old, int total, float alpha, Plane[] frustum,
            Vector3 offsetA, Vector3 scaleA, Vector3 offsetB, Vector3 scaleB, Vector3 boundOffset, float radius, bool pair, int batch)
        {
            Schedule(current, old, total, alpha, frustum, offsetA, scaleA, offsetB, scaleB, boundOffset, radius, pair, batch);
            return Complete();
        }
        public void Schedule(Vector3[] current, Vector3[] old, int total, float alpha, Plane[] frustum,
            Vector3 offsetA, Vector3 scaleA, Vector3 offsetB, Vector3 scaleB, Vector3 boundOffset, float radius, bool pair, int batch)
        {
            if (scheduled) throw new InvalidOperationException("Complete the previous preparation before reusing its buffers.");
            NativeArray<Vector3>.Copy(current, positions, total); NativeArray<Vector3>.Copy(old, previous, total);
            ScheduleNative(positions, previous, total, alpha, frustum, offsetA, scaleA, offsetB, scaleB, boundOffset, radius, pair, batch);
        }
        public void ScheduleNative(NativeArray<Vector3> current, NativeArray<Vector3> old, int total, float alpha, Plane[] frustum,
            Vector3 offsetA, Vector3 scaleA, Vector3 offsetB, Vector3 scaleB, Vector3 boundOffset, float radius, bool pair, int batch)
        {
            if (scheduled) throw new InvalidOperationException("Complete the previous preparation before reusing its buffers.");
            if (total == 0) { count[0] = 0; return; }
            for (int i = 0; i < 6; i++) planes[i] = new float4(frustum[i].normal, frustum[i].distance);
            JobHandle build = new BuildJob { Positions = current, Previous = old, Planes = planes, Visible = visible,
                A = A, B = B, OffsetA = offsetA, OffsetB = offsetB, ScaleA = scaleA, ScaleB = scaleB,
                BoundOffset = boundOffset, Radius = radius, Pair = pair, Alpha = math.clamp(alpha, 0, 1) }.Schedule(total, batch);
            pending = new CompactJob { Visible = visible, A = A, B = B, Count = count, Total = total, Pair = pair }.Schedule(build);
            scheduled = true;
        }
        public int Complete()
        {
            if (scheduled) { pending.Complete(); scheduled = false; }
            return count[0];
        }
        public static bool SphereVisible(Plane[] frustum, Vector3 center, float radius)
        {
            for (int i = 0; i < frustum.Length; i++) if (frustum[i].GetDistanceToPoint(center) < -radius) return false;
            return true;
        }
        public void Dispose() { Complete(); positions.Dispose(); previous.Dispose(); planes.Dispose(); visible.Dispose(); count.Dispose(); A.Dispose(); B.Dispose(); }

        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Strict)]
        struct BuildJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> Positions, Previous;
            [ReadOnly] public NativeArray<float4> Planes;
            [WriteOnly] public NativeArray<byte> Visible;
            [WriteOnly] public NativeArray<Matrix4x4> A, B;
            public float3 OffsetA, OffsetB, ScaleA, ScaleB, BoundOffset;
            public float Radius, Alpha;
            public bool Pair;
            public void Execute(int i)
            {
                float3 p = (float3)Previous[i] + ((float3)Positions[i] - (float3)Previous[i]) * Alpha;
                float3 center = p + BoundOffset;
                for (int j = 0; j < 6; j++)
                {
                    float4 plane = Planes[j];
                    if (math.dot(plane.xyz, center) + plane.w < -Radius) { Visible[i] = 0; return; }
                }
                Visible[i] = 1; A[i] = Matrix(p + OffsetA, ScaleA);
                if (Pair) B[i] = Matrix(p + OffsetB, ScaleB);
            }
            static Matrix4x4 Matrix(float3 p, float3 scale)
            {
                var m = new Matrix4x4(); m.m00 = scale.x; m.m11 = scale.y; m.m22 = scale.z; m.m33 = 1;
                m.m03 = p.x; m.m13 = p.y; m.m23 = p.z; return m;
            }
        }
        [BurstCompile(CompileSynchronously = true)]
        struct CompactJob : IJob
        {
            [ReadOnly] public NativeArray<byte> Visible;
            public NativeArray<Matrix4x4> A, B;
            public NativeArray<int> Count;
            public int Total;
            public bool Pair;
            public void Execute()
            {
                int count = 0;
                for (int i = 0; i < Total; i++)
                {
                    if (Visible[i] == 0) continue;
                    A[count] = A[i]; if (Pair) B[count] = B[i]; count++;
                }
                Count[0] = count;
            }
        }
    }
}
