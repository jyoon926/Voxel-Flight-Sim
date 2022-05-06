Shader "Custom/VertexColorShader" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        float _Glossiness;
        float _Metallic;
        float4 _Color;

        struct Input {
            float3 vertColors;
        };

        void vert(inout appdata_full v, out Input o) {
            o.vertColors = v.color.rgb;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half3 c = IN.vertColors.rgb * _Color.rgb;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
