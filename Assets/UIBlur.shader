Shader "UI/BlurBehind"
{
    Properties
    {
        _Size ("Blur Size", Range(1, 10)) = 2
        _Tint ("Tint", Color) = (0,0,0,0.5) // default: semi-transparent black
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        GrabPass { }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
            float _Size;
            fixed4 _Tint;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uvgrab : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uvgrab = ComputeGrabScreenPos(o.pos).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uvgrab;
                float2 offset = _GrabTexture_TexelSize.xy * _Size;

                // Blur
                fixed4 col = tex2D(_GrabTexture, uv);
                col += tex2D(_GrabTexture, uv + offset);
                col += tex2D(_GrabTexture, uv - offset);
                col += tex2D(_GrabTexture, uv + float2(offset.x, -offset.y));
                col += tex2D(_GrabTexture, uv + float2(-offset.x, offset.y));
                col /= 5;

                // Blend with tint (instead of multiplying)
                col = lerp(col, _Tint, _Tint.a);

                return col;
            }
            ENDCG
        }
    }
}
