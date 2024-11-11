Shader "Unlit/FOW_Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            float4 _MainTex_ST;
            float _Smoothness;
            float _Noise;
            
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
                
                 half4 gaussianH   = tex2D (_MainTex, i.uv + float2(-_Smoothness,0))*0.25;
                gaussianH  += tex2D (_MainTex,  i.uv                          )*0.5  ;
                gaussianH  += tex2D (_MainTex,  i.uv + float2( _Smoothness,0))*0.25;
    
                half4 gaussianV   = tex2D (_MainTex,  i.uv + float2(0,-_Smoothness))*0.25;
                gaussianV  += tex2D (_MainTex,  i.uv                        ) *0.5  ;
                gaussianV  += tex2D (_MainTex,  i.uv + float2(0, _Smoothness))*0.25;
    
                half4 blurred = (gaussianH+ gaussianV)*0.5;
                
                
                //clip(1 - col.r);
                //col.a = abs(1 - col.r);
                //clip(col.a - 1);
                
                
                float rand = random(i.uv) - ( (1 - _Noise)-0.5);
                rand = floor(rand + 0.5);

                float xRand = random(float2(i.screenPos.x, 0)) * _Noise;
                xRand = floor(xRand + 0.5);
             
                //col.a = abs(1 - col.r );
                //clip(col.a - 1);
                return lerp(float4(0,0,0,1), float4(0,0,0,0), col.r * blurred.r - xRand);
                
                return col;
            }
            
            
            
            ENDCG
            
        }
        
    }
}
