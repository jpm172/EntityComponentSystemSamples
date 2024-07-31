Shader "Unlit/Destructible Terrain"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType"="TransparentCutout" }
        LOD 100

        Zwrite off //set off for transparent shader
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
            uniform float4 _MainTex_TexelSize;
            
            float4 _BlockPosition;
            float4 _CompositeOrigin;
            int _BlockSize;
            
            StructuredBuffer<int> _PointsBuffer;


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

                //map the uv to the position of the pixel in the point array
                int x = (i.uv.x/_MainTex_TexelSize.x) - _CompositeOrigin.x - _BlockPosition.x;
                int y = (i.uv.y/ _MainTex_TexelSize.y)- _CompositeOrigin.y - _BlockPosition.y;
                
                x = clamp(x, 0, _BlockSize-1);
                y = clamp(y, 0, _BlockSize-1);

                int index = x + _BlockSize*y;

                clip(_PointsBuffer[index] - 1);//remove any "destroyed" pixels


                return col;
            }
            ENDCG
        }
    }
}
