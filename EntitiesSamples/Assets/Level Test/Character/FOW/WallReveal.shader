Shader "Unlit/WallReveal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MapTex ("Map Texture", 2D) = "white" {}
        _Scale ("Scale", Vector) = (1,1, 0, 0 )
        _ShowAll ("Show All", int) = 0 
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
            };

            sampler2D _MainTex;
            sampler2D _MapTex;
            float4 _MainTex_ST;
            float4 _Scale;
            int _ShowAll;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv * _Scale);
                fixed4 wallCol = tex2D(_MapTex, i.uv);
                
                if(_ShowAll == 1 && wallCol.r > 0)
                {
                    return float4(0,1,0,1);
                }
                else if(wallCol.r > 0 && col.r > 0)
                {
                    return float4(0,1,0,1);
                }
                    
                // apply fog

                return col;
            }
            ENDCG
        }
    }
}
