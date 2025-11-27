Shader "Hidden/Fog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorTex ("Texture", 2D) = "white" {}
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
            };

            struct Fragment
            {
                float4 positionCS_SV : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _CameraPositionTexture;
            sampler2D _ColorTex;
            float4 _Color;
            float _DepthStart;
            float _DepthEnd;
            float _HeightStart;
            float _HeightEnd;
            float _SkyHeightStart;
            float _SkyHeightEnd;
            float _SunScattering;
            float _SunIntensity;

            Fragment VertexPass(Vertex v)
            {
                Fragment fragment;
                fragment.positionCS_SV = TransformObjectToHClip(v.positionOS);
                fragment.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return fragment;
            }

            float4 FragmentPass(Fragment fragment) : SV_Target
            {
                float3 background = tex2D(_MainTex, fragment.uv);

                float3 positionWS = tex2D(_CameraPositionTexture, fragment.uv);
                float dis = distance(positionWS, GetCameraPositionWS());

                float depthDensity = saturate((dis - _DepthStart) / (_DepthEnd - _DepthStart));
                float heightDensity = 1 - saturate((positionWS.y - _HeightStart) / (_HeightEnd - _HeightStart));
                float skyHeightDensity = 1 - saturate(
                    (positionWS.y - _SkyHeightStart) / (_SkyHeightEnd - _SkyHeightStart));

                float density;
                float3 color;
                if (dis >= _ProjectionParams.z)
                {
                    density = skyHeightDensity;
                    color = tex2D(_ColorTex, 1) * _Color.rgb;
                }
                else
                {
                    density = saturate(depthDensity + heightDensity) * pow(depthDensity, 0.5);
                    color = tex2D(_ColorTex, density) * _Color.rgb;
                }


                Light mainLight = GetMainLight();
                float radiance = dot(-mainLight.direction, GetWorldSpaceNormalizeViewDir(positionWS));
                float halfRadiance = saturate(radiance * 0.5 + 0.5);
                float finalRadiance = pow(halfRadiance, _SunScattering) * _SunIntensity;
                color = lerp(color, mainLight.color, finalRadiance);

                // return _Color.rgba;
                float3 finalColor = lerp(background, color, density * _Color.a);
                return float4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}