Shader "Hidden/acg-importer/Swizzle"
{
    Properties
    {
        _MainTex1 ("Texture 1", 2D) = "black" {}
        _MainTex2 ("Texture 2", 2D) = "black" {}
        _MainTex3 ("Texture 3", 2D) = "black" {}
        _MainTex4 ("Texture 4", 2D) = "black" {}

        _Swizzle1 ("Swizzle 1", Vector) = (1,0,0,0)
        _Swizzle2 ("Swizzle 2", Vector) = (0,1,0,0)
        _Swizzle3 ("Swizzle 3", Vector) = (0,0,1,0)
        _Swizzle4 ("Swizzle 4", Vector) = (0,0,0,1)
        
        _Flip1 ("Flip 1", Float) = 0
        _Flip2 ("Flip 2", Float) = 0
        _Flip3 ("Flip 3", Float) = 0
        _Flip4 ("Flip 4", Float) = 0
    }
    SubShader
    {
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

            sampler2D _MainTex1;
            sampler2D _MainTex2;
            sampler2D _MainTex3;
            sampler2D _MainTex4;

            float4 _Swizzle1;
            float4 _Swizzle2;
            float4 _Swizzle3;
            float4 _Swizzle4;

            float _Flip1;
            float _Flip2;
            float _Flip3;
            float _Flip4;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex1 = tex2D(_MainTex1, i.uv);
                fixed4 tex2 = tex2D(_MainTex2, i.uv);
                fixed4 tex3 = tex2D(_MainTex3, i.uv);
                fixed4 tex4 = tex2D(_MainTex4, i.uv);

                fixed4 swizzle;
                swizzle.r = dot(tex1, _Swizzle1) * (1 - _Flip1) + (1 - dot(tex1, _Swizzle1)) * _Flip1;
                swizzle.g = dot(tex2, _Swizzle2) * (1 - _Flip2) + (1 - dot(tex2, _Swizzle2)) * _Flip2;
                swizzle.b = dot(tex3, _Swizzle3) * (1 - _Flip3) + (1 - dot(tex3, _Swizzle3)) * _Flip3;
                swizzle.a = dot(tex4, _Swizzle4) * (1 - _Flip4) + (1 - dot(tex4, _Swizzle4)) * _Flip4;

                return swizzle;
            }
            ENDCG
        }
    }
}