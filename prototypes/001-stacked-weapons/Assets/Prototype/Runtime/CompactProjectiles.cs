using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Journey
{
    // CPU simulation remains authoritative. Only visible, interpolated positions cross to the GPU.
    public sealed class CompactProjectiles : IDisposable
    {
        readonly NativeArray<float4> data = new NativeArray<float4>(BossAbilities.ProjectileCapacity, Allocator.Persistent);
        readonly NativeArray<byte> visible = new NativeArray<byte>(BossAbilities.ProjectileCapacity, Allocator.Persistent);
        NativeArray<float4> planes = new NativeArray<float4>(6, Allocator.Persistent);
        NativeArray<int> count = new NativeArray<int>(1, Allocator.Persistent);
        readonly GraphicsBuffer.IndirectDrawIndexedArgs[] command = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        GraphicsBuffer positions, arguments;
        Material material;
        public NativeArray<float4> Data => data;

        public int Prepare(NativeArray<Vector3> current, NativeArray<Vector3> previous, int total,
            float alpha, Plane[] frustum, int batch)
        {
            if (total == 0) { count[0] = 0; return 0; }
            for (int i = 0; i < 6; i++) planes[i] = new float4(frustum[i].normal, frustum[i].distance);
            var build = new Build { Current = current, Previous = previous, Planes = planes, Data = data,
                Visible = visible, Alpha = math.clamp(alpha, 0, 1) }.Schedule(total, batch);
            new Compact { Data = data, Visible = visible, Count = count, Total = total }.Schedule(build).Complete();
            return count[0];
        }
        public void Draw(Mesh mesh, int total)
        {
            if (total == 0) return;
            if (positions == null)
            {
                Shader shader = Shader.Find("Journey/CompactProjectile");
                if (shader == null || !shader.isSupported) throw new InvalidOperationException("Compact projectile shader unavailable.");
                material = new Material(shader);
                material.SetColor("_BaseColor", new Color(1, .15f, .75f));
                positions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, BossAbilities.ProjectileCapacity, 16);
                arguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
                material.SetBuffer("_Positions", positions);
            }
            positions.SetData(data, 0, 0, total);
            command[0] = new GraphicsBuffer.IndirectDrawIndexedArgs { indexCountPerInstance = mesh.GetIndexCount(0),
                instanceCount = (uint)total, startIndex = mesh.GetIndexStart(0), baseVertexIndex = mesh.GetBaseVertex(0) };
            arguments.SetData(command);
            var parameters = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false, worldBounds = new Bounds(Vector3.zero, new Vector3(120, 8, 120)) };
            Graphics.RenderMeshIndirect(parameters, mesh, arguments);
            StageProfile.InstanceBatches++; StageProfile.SubmittedInstances += total;
        }
        public void Dispose()
        {
            data.Dispose(); visible.Dispose(); planes.Dispose(); count.Dispose();
            positions?.Dispose(); arguments?.Dispose(); if (material != null) UnityEngine.Object.Destroy(material);
        }
        [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Strict)]
        struct Build : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> Current, Previous;
            [ReadOnly] public NativeArray<float4> Planes;
            [WriteOnly] public NativeArray<float4> Data;
            [WriteOnly] public NativeArray<byte> Visible;
            public float Alpha;
            public void Execute(int i)
            {
                float3 p = (float3)Previous[i] + ((float3)Current[i] - (float3)Previous[i]) * Alpha + new float3(0, 1, 0);
                for (int j = 0; j < 6; j++) if (math.dot(Planes[j].xyz, p) + Planes[j].w < -.17f)
                { Visible[i] = 0; return; }
                Visible[i] = 1; Data[i] = new float4(p, .18f);
            }
        }
        [BurstCompile(CompileSynchronously = true)]
        struct Compact : IJob
        {
            public NativeArray<float4> Data;
            [ReadOnly] public NativeArray<byte> Visible;
            public NativeArray<int> Count;
            public int Total;
            public void Execute()
            {
                int n = 0;
                for (int i = 0; i < Total; i++) if (Visible[i] != 0) Data[n++] = Data[i];
                Count[0] = n;
            }
        }
    }
}
