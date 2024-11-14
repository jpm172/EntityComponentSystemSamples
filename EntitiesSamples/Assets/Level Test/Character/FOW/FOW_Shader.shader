Shader "Unlit/FOW_Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SeenTex ("Seen Texture", 2D) = "white" {}
        _MapTex ("Map Texture", 2D) = "white" {}
        _SeenColor ("Seen Color", Color) = (1,1,1,1) 
        _SeenDist ("Seen Distance", Int) = 4
        
        _Smoothness ("Feather", Range(0,0.1)) = 0.005
        _Noise ("Nosie", Range(0,1)) = 0.1
        
        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcFactor("Src Factor", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstFactor("Dst Factor", Float) = 10
        [Enum(UnityEngine.Rendering.BlendOp)]
        _Opp("Operation", Float) = 0
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType"="Transparent"  }
        //Tags {"RenderType"="Opaque"  }
        LOD 100

        //Zwrite off //set off for transparent shader
        Blend SrcAlpha OneMinusSrcAlpha
        //Blend [_SrcFactor] [_DstFactor]
        //BlendOp [_Opp]
        
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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 screenPos : POSITION_SS;
            };

            sampler2D _MainTex;
            sampler2D _SeenTex;
            sampler2D _MapTex;
            
            float4 _SeenColor;
            
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _Smoothness;
            float _Noise;
            int _SeenDist;
            
            float random (float2 uv)
            {
                return frac(sin(dot(uv,float2(12.9898 + _Time.x,78.233)))*43758.5453123);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos =  ComputeScreenPos(o.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 mapCol = tex2D(_MapTex, i.uv);
                
                float rand = random(i.uv) - ( (1 - _Noise)-0.5);
                rand = floor(rand + 0.5);

                float xRand = random(float2(i.screenPos.x, 0)) * _Noise;
                xRand = floor(xRand + 0.5);
             
                //col.a = abs(1 - col.r );
                //clip(col.a - 1);
                //float4 result = lerp(float4(0,0,0,1), float4(0,0,0,0), col.r * blurred.r - xRand);
                float seen = 0;
                float visible = 0;
                if(mapCol.r > 0)
                {
                
                
                    fixed4 visibleCol = tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y*_SeenDist));
                    visible = visibleCol.r;
                    visibleCol = tex2D(_MainTex, i.uv - float2(0, _MainTex_TexelSize.y*_SeenDist));
                    visible = max(visible,visibleCol.r);
                    visibleCol = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x*_SeenDist, 0));
                    visible = max(visible,visibleCol.r);
                    visibleCol = tex2D(_MainTex, i.uv - float2(_MainTex_TexelSize.x*_SeenDist, 0));
                    visible = max(visible,visibleCol.r);
                    
                        
                    if(visible < .1)
                    {
                        fixed4 seenCol = tex2D(_SeenTex, i.uv + float2(0, _MainTex_TexelSize.y*_SeenDist));
                        seen = seenCol.r;
                        seenCol = tex2D(_SeenTex, i.uv - float2(0, _MainTex_TexelSize.y*_SeenDist));
                        seen = max(seen, seenCol.r);
                        seenCol = tex2D(_SeenTex, i.uv + float2(_MainTex_TexelSize.x*_SeenDist, 0));
                        seen = max(seen, seenCol.r);
                        seenCol = tex2D(_SeenTex, i.uv - float2(_MainTex_TexelSize.x*_SeenDist, 0));
                        seen = max(seen, seenCol.r);
                    
                    }
                }
                
                //float4 result = lerp(float4(0,0,0,1), float4(0,0,0,0), col.r);
                //float4 result = lerp(float4(0,0,0,1), float4(0,0,0,0), max(col.r, visible));
                float4 result = lerp(_SeenColor*float4(seen, seen,seen, 1), float4(0,0,0,0), max(col.r, visible));

                //float4 seenCol = _SeenColor * seen;
                //seenCol.a = result.a;
                return result;
            }
            
            
            
            ENDCG
            
        }
        
    }
}
