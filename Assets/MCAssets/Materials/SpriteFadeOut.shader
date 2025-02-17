Shader "Custom/SpriteFadeOut"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _FadeDistance ("Fade Distance", Range(0,1)) = 0.5
        _FadePower ("Fade Power", Range(0.1,5)) = 1
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
        }

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
            float _FadeDistance;
            float _FadePower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float dist = distance(i.uv, float2(0.5, 0.5));
                float fade = 1 - saturate(pow(dist / _FadeDistance, _FadePower));
                col.a *= fade;
                return col;
            }
            ENDCG
        }
    }
}