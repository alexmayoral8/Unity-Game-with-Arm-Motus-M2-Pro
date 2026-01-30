Shader "Custom/RibbonUnlitAlpha"
{
    Properties{
        _MainTex ("Texture", 2D) = "white" {}
        _Color   ("Color (RGBA)", Color) = (1,1,1,0.6)
        _Tiling  ("Tiling X", Float) = 6.0
    }
    SubShader{
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Tiling;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Tiling en X para evitar estiramiento en curvas
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.x *= _Tiling;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = tex * _Color;
                return col; // Alpha viene de textura * color.a
            }
            ENDCG
        }
    }
}
