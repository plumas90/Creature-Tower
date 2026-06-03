Shader "Custom/ExplosionShockwave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 0.35, 0.05, 1)
        _Radius ("Current Radius", Range(0, 1)) = 0.0
        _Thickness ("Ring Thickness", Range(0, 0.5)) = 0.12
        _Softness ("Softness", Range(0.001, 0.2)) = 0.03
        _Glow ("Glow Intensity", Range(1, 5)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            fixed4 _Color;
            float _Radius;
            float _Thickness;
            float _Softness;
            float _Glow;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Map UV to (-0.5, 0.5) to calculate radial distance from center
                float2 uv = IN.texcoord - 0.5;
                float dist = length(uv) * 2.0;

                // Create ring pattern based on radius, thickness, and edge softness
                float outerEdge = smoothstep(_Radius, _Radius - _Softness, dist);
                float innerEdge = smoothstep(_Radius - _Thickness, _Radius - _Thickness + _Softness, dist);
                float ring = outerEdge * innerEdge;

                // Fade out alpha as the ring expands towards the boundary
                float alphaFade = 1.0 - _Radius;

                fixed4 finalCol = IN.color;
                finalCol.rgb *= _Glow; // HDR glow simulation
                finalCol.a = ring * alphaFade * IN.color.a;

                return finalCol;
            }
            ENDCG
        }
    }
}
