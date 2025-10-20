Shader "Custom/HeightFog" {
    Properties {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _FogColor ("Fog Color", Color) = (0.7, 0.7, 0.8, 1)
        _FogStart ("Fog Start Height (Local)", Float) = 0
        _FogEnd ("Fog End Height (Local)", Float) = 10
        _FogDensity ("Max Fog Density", Range(0, 1)) = 0.8
        _FogSmoothness ("Fog Transition Smoothness", Range(0.1, 2)) = 1
    }
    
    SubShader {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 localPos : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _FogColor;
            float _FogStart;
            float _FogEnd;
            float _FogDensity;
            float _FogSmoothness;
            
            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.localPos = v.vertex.xyz;
                
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                // Sample the main texture
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;
                
                // Calculate height-based fog using local coordinates
                float heightRange = _FogEnd - _FogStart;
                float heightFactor = saturate((i.localPos.y - _FogStart) / heightRange);
                
                // Apply smoothness curve for more natural fog transition
                heightFactor = pow(heightFactor, _FogSmoothness);
                
                // Calculate final fog intensity
                float fogIntensity = heightFactor * _FogDensity;
                
                // Blend between original color and fog color
                fixed4 finalColor;
                finalColor.rgb = lerp(texColor.rgb, _FogColor.rgb, fogIntensity);
                finalColor.a = lerp(texColor.a, _FogColor.a, fogIntensity * 0.5); // Preserve some transparency
                
                // Apply Unity's built-in fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}