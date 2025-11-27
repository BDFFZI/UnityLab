Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Factor("Factor",Range(0,1)) =0
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertexPass
            #pragma fragment FragmentPass

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Vertex
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS:NORMAL;
            };

            struct Fragment
            {
                float4 positionCS_SV : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS:NORMAL;
                float3 positionWS:TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Factor;

            Fragment VertexPass(Vertex v)
            {
                Fragment fragment;
                fragment.positionCS_SV = TransformObjectToHClip(v.positionOS);
                fragment.uv = TRANSFORM_TEX(v.uv, _MainTex);
                fragment.normalWS = TransformObjectToWorldNormal(v.normalOS);
                fragment.positionWS = TransformObjectToWorld(v.positionOS);
                return fragment;
            }

            float4 FragmentPass(Fragment fragment) : SV_Target
            {
                Light light = GetMainLight();
                float3 n = normalize(fragment.normalWS);
                float3 l = light.direction;
                float3 v = normalize(GetCameraPositionWS() - fragment.positionWS);
                float a = max(HALF_MIN_SQRT, pow(1 - 1, 2));

                float diffuse = saturate(dot(n, l));
                float3 h = normalize(v + l);
                float specular = a * a / pow(pow(dot(h, n), 2) * (a * a - 1) + 1, 2);


                return diffuse + specular;

                float4 col = tex2D(_MainTex, fragment.uv);
                return col;
            }
            ENDHLSL
        }
    }
}