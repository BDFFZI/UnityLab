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
                // Light lights[MAX_VISIBLE_LIGHTS];
                // int linght

                //物体表面属性
                float3 albedo = 1;
                float metallic = 0.5;
                float smoothness = 0.5;
                float3 normal = fragment.normalWS;
                //双向反射权重
                float3 diffuse = lerp(albedo * (1 - kDieletricSpec.rgb), 0, metallic);
                float3 specular = lerp(kDieletricSpec.rgb, albedo, metallic);
                //直接光照信息、辐照度、辐射率
                Light light = GetMainLight();
                float3 irradiance = light.color * light.distanceAttenuation * light.shadowAttenuation;
                float3 radiance = irradiance * saturate(dot(normal, light.direction));
                //向量信息
                float3 n = normalize(normal);
                float3 l = light.direction;
                float3 v = normalize(GetCameraPositionWS() - fragment.positionWS);
                float3 h = normalize(v + l);
                //双向反射系数
                float3 diffuseTerm = 

                float3 diffuse = saturate(dot(n, l));
                float3 specular = r * r / pow(pow(dot(h, n), 2) * (r * r - 1) + 1, 2);
                float3 envDiffuse = SampleSH(n);

                float pr = 1 - 0.9; //直觉上的粗糙度（perceptualRoughness）
                float mipLevel = pr * (1.7 - 0.7 * pr) * UNITY_SPECCUBE_LOD_STEPS;
                float4 encodedIrradiance = unity_SpecCube0.
                    SampleLevel(samplerunity_SpecCube0, reflect(-v, n), mipLevel);
                float3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
                float3 envSpecular = irradiance;


                return float4(diffuse + specular + envDiffuse + envSpecular, 1);

                float4 col = tex2D(_MainTex, fragment.uv);
                return col;
            }
            ENDHLSL
        }
    }
}