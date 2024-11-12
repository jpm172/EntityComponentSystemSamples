Shader "Universal Render Pipeline/Custom/DotsCutOutFade"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _Center ("Center", Vector) = (0,0,0) 
        _Radius ("Radius", float) = 2
        _Hardness ("Hardness", float) = .1
        _Strength ("Strength", float) = 1
    }

    SubShader
    {
    
        Tags { "RenderType"="Opaque" }
        Blend SrcColor DstColor
        BlendOp Max

        Pass
        {
           

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            uniform float4 _BaseMap_TexelSize;
            float3 _Center;
            float _Radius;
            float _Hardness;
            float _Strength;

            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                    UNITY_DOTS_INSTANCED_PROP(float3, _Center)
                    UNITY_DOTS_INSTANCED_PROP(float, _Radius)
                    UNITY_DOTS_INSTANCED_PROP(float, _Hardness)
                    UNITY_DOTS_INSTANCED_PROP(float, _Strength)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
                #define _BaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
            #endif

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings UnlitPassVertex(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                const VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = positionInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = float4(input.uv.x, input.uv.y, 0, 1);
             

                // Set positionWS to the screen space position of the vertex
                output.worldPos = positionInputs.positionWS.xyz;
                
                return output;
            }

            float random (float2 uv)
            {
                return frac(sin(dot(uv,float2(12.9898 +  _Time.x,78.233)))*43758.5453123);
            }

            float mask(float3 position, float3 center, float radius, float hardness)
            {
                //float m = distance(center, position) + random(position.xy) - 0.5;
                float m = distance(center, position);
                return 1 - smoothstep(radius*hardness, radius, m);
            }

            half4 UnlitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float m = mask(input.worldPos, _Center, _Radius, _Hardness);
                float edge = m *_Strength;
                
               // return float4(input.worldPos.xy, 0, 1);
                return lerp(float4(0,0,0,0), float4(1,0,0,1), edge);
                
                
                //return baseMap * _BaseColor * input.color;
            }
            ENDHLSL
        }
    }
}