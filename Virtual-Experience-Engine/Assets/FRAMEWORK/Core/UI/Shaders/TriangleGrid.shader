Shader "Custom/TriangleGridUI"
{
    Properties
    {
        _ColorA ("Primary Color", Color) = (0.0, 0.8, 1.0, 1.0)
        _ColorB ("Secondary Color", Color) = (1.0, 0.3, 0.3, 1.0)
        _GridScale ("Grid Scale", Float) = 10.0
        _FlickerSpeed ("Flicker Speed", Float) = 2.0
        _Distortion ("Distortion Strength", Float) = 0.1
        _LineWidth ("Line Width", Float) = 0.1
        _PatternSpeed ("Pattern Speed", Float) = 1.0
        _NoiseIntensity ("Noise Intensity", Float) = 1.0
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _TilingX ("Tiling X", Float) = 1.0
        _TilingY ("Tiling Y", Float) = 1.0
        _OffsetX ("Offset X", Float) = 0.0
        _OffsetY ("Offset Y", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityInstancing.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float _GridScale;
            float _FlickerSpeed;
            float _Distortion;
            float _LineWidth;
            float _PatternSpeed;
            float _NoiseIntensity;
            float _TilingX;
            float _TilingY;
            float _OffsetX;
            float _OffsetY;
            fixed4 _ColorA;
            fixed4 _ColorB;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 hash(float2 p) {
                p = frac(p * float2(5.3983, 5.4427));  
                p += dot(p, p.yx + 19.19);
                return frac(float3(p.x * p.y, p.x + p.y, p.x - p.y));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Use sprite UV coordinates to ensure consistent pattern display
                float2 uv = (i.uv * float2(_TilingX, _TilingY) + float2(_OffsetX, _OffsetY)) * _GridScale;
                float2 grid = floor(uv);
                float3 noise = hash(grid);
                float flicker = abs(sin(_Time.y * _FlickerSpeed + noise.x * 10.0 * _NoiseIntensity));

                // Offset UVs for distortion effect
                uv += sin(uv.yx * 10.0 + _Time.y * _PatternSpeed) * _Distortion;

                // Create diagonal grid effect in both directions
                float distA = abs(frac(uv.x + uv.y) - 0.5); // Distance from diagonal center
                float distB = abs(frac(uv.x - uv.y) - 0.5); // Distance from opposite diagonal

                float patternA = smoothstep(_LineWidth, _LineWidth * 0.5, distA); 
                float patternB = smoothstep(_LineWidth, _LineWidth * 0.5, distB); 

                float pattern = max(patternA, patternB); // Combine both patterns

                // Sample the sprite's alpha channel
                fixed4 spriteColor = tex2D(_MainTex, i.uv);
                float alpha = spriteColor.a;

                // Blend colors based on flicker and pattern, and mask with sprite's alpha
                fixed4 col = lerp(_ColorA, _ColorB, pattern * flicker);
                col.a *= alpha;

                return col;
            }
            ENDCG
        }
    }
}