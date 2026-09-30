Shader "Journey/CompactProjectile"
{
    Properties { _BaseColor("Color", Color) = (1, .15, .75, 1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            StructuredBuffer<float4> _Positions;
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Varyings { float4 positionCS : SV_POSITION; half fog : TEXCOORD0; };
            Varyings vert(float3 positionOS : POSITION, uint instanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                float4 p = _Positions[GetIndirectInstanceID(instanceID)];
                Varyings o;
                o.positionCS = TransformWorldToHClip(p.xyz + positionOS * p.w);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(Varyings i) : SV_Target { return half4(MixFog(_BaseColor.rgb, i.fog), 1); }
            ENDHLSL
        }
    }
}
