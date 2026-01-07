Shader "Custom/ImageBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Size ("Blur Size (px)", Range(0,64)) = 8
        _Directions ("Directions", Range(1,64)) = 16
        _Quality ("Quality", Range(1,16)) = 3
    }
    
    // Thank you https://www.shadertoy.com/user/existical for the implementation idea

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // (1/width, 1/height, width, height)
            fixed4 _Color;
            float _Size;       // in pixels
            float _Directions; // treated as integer
            float _Quality;    // treated as integer

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                // Convert to ints for loop counts (clamp sensible ranges)
                int dirCount = max(1, (int)round(_Directions));
                int quality = max(1, (int)round(_Quality));

                // 2*PI
                const float TWO_PI = 6.283185307179586;

                // Convert pixel radius to UV radius (x and y)
                float2 radiusUV = float2(_Size * _MainTex_TexelSize.x, _Size * _MainTex_TexelSize.y);

                // base sample
                float2 uv = i.texcoord;
                float4 accum = tex2D(_MainTex, uv);
                float totalWeight = 1.0;

                // accumulate angular samples
                for (int d = 0; d < dirCount; ++d)
                {
                    float ang = TWO_PI * (float)d / (float)dirCount;
                    float2 dir = float2(cos(ang), sin(ang));

                    for (int q = 1; q <= quality; ++q)
                    {
                        float t = (float)q / (float)quality; // 0..1
                        float2 sampleUV = uv + dir * radiusUV * t;

                        // clamp to edges
                        sampleUV = clamp(sampleUV, 0.0, 1.0);

                        accum += tex2D(_MainTex, sampleUV);
                        totalWeight += 1.0;
                    }
                }

                float4 col = accum / totalWeight;
                return col * i.color;
            }
            ENDCG
        }
    }
}