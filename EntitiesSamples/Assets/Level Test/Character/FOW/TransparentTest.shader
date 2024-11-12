Shader "Unlit/TransparentTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Smoothness ("Feather", Range(0,0.1)) = 0.005
    }
    SubShader
    {
        Tags {"RenderType"="Transparent"  }
        //Tags { "RenderType"="Opaque" }
        LOD 100

        //Zwrite off //set off for transparent shader
        Blend SrcAlpha DstAlpha
        //Blend SrcAlpha DstAlpha
        BlendOp Add
        
        Lighting off

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
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Smoothness;
            
            float random (float2 uv)
            {
                return frac(sin(dot(uv,float2(12.9898 +  _Time.x,78.233)))*43758.5453123);
            }

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
                fixed4 col = tex2D(_MainTex, i.uv);
                
                col.a = 0.5;
                return float4(1,0,0,.5);
                return col;
            }
            
            
            
            ENDCG
            
        }
        
    }
}
