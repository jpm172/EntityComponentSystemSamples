Shader "Unlit/CutOutShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "red" {}
        _Center ("Center", Vector) = (0,0,0) 
        _Radius ("Radius", float) = 1
        _Hardness ("Hardness", float) = .1
        _Power("Power", float) = 1
        _Strength ("Strength", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : POSITION_WS;

            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float3 _Center;
            float _Radius;
            float _Hardness;
            float _Strength;
            float _Power;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            
            float mask(float3 position, float3 center, float radius, float hardness)
            {
                float m = pow( distance(center, position),_Power);
                return 1 -  smoothstep(radius*hardness, radius, m);
            }
            

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture 
                fixed4 col = tex2D(_MainTex, i.uv);
                
                float m = mask(i.worldPos, _Center, _Radius, _Hardness);
                float edge = m *_Strength;
               
                
                return lerp(float4(0,0,0,0), float4(1,0,0,1), edge);
            }
            ENDCG
        }
    }
}
