Shader "Custom/RibbonUnlitAlpha"
{
    Properties{
        _MainTex   ("Base Texture", 2D) = "white" {}
        _Color     ("Base Color (RGBA)", Color) = (0.35,0.65,1,0.5)
        _Tiling    ("Tiling X", Float) = 6.0
        _ScrollX   ("Scroll Speed X", Float) = 0.2

        _GlowTex   ("Glow Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0.6,0.9,1,1)
        _GlowTiling("Glow Tiling X", Float) = 8.0
        _GlowScrollX("Glow Scroll X", Float) = 0.35
        _GlowIntensity ("Glow Intensity", Float) = 1.0
    }
    SubShader{
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Cull Off
        Lighting Off

        // ----- Pass 1: base transparente -----
        Blend SrcAlpha OneMinusSrcAlpha
        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color;
            float _Tiling, _ScrollX;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v){
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                uv.x = uv.x * _Tiling + _Time.y * _ScrollX;
                o.uv = uv; return o;
            }
            fixed4 frag(v2f i):SV_Target{
                fixed4 tex = tex2D(_MainTex, i.uv);
                return tex * _Color; // alpha suave desde textura * color.a
            }
            ENDCG
        }

        // ----- Pass 2: glow aditivo -----
        Blend One One
        Pass{
            CGPROGRAM
            #pragma vertex vert2
            #pragma fragment frag2
            #include "UnityCG.cginc"

            sampler2D _GlowTex; float4 _GlowTex_ST;
            fixed4 _GlowColor;
            float _GlowTiling, _GlowScrollX, _GlowIntensity;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert2(appdata v){
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                float2 uv = TRANSFORM_TEX(v.uv, _GlowTex);
                uv.x = uv.x * _GlowTiling + _Time.y * _GlowScrollX;
                o.uv = uv; return o;
            }
            fixed4 frag2(v2f i):SV_Target{
                fixed4 g = tex2D(_GlowTex, i.uv) * _GlowColor;
                g.rgb *= _GlowIntensity;
                return fixed4(g.rgb, 0); // alpha no importa en aditivo
            }
            ENDCG
        }
    }
}