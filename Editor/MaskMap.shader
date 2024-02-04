Shader "Hidden/MaskMap"
{
    Properties
    {
        _MetallicMap ("Metallic Map", 2D) = "black" {}
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _RoughnessMap ("Roughness Map", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            sampler2D _MetallicMap;
            sampler2D _OcclusionMap;
            sampler2D _RoughnessMap;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 metallic = tex2D(_MetallicMap, i.uv);
                fixed4 occlusion = tex2D(_OcclusionMap, i.uv);
                fixed4 roughness = tex2D(_RoughnessMap, i.uv);

                fixed4 mask;
                mask.r = metallic.r;
                mask.g = occlusion.r;
                mask.b = 0;
                mask.a = 1 - roughness.r;

                return mask;
            }
            ENDCG
        }
    }
}
