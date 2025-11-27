Shader "Hidden/VolumeLight"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Pass
		{
			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment FragmentPass

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#define  ADDITIONAL_LIGHT_CALCULATE_SHADOWS
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Assets/Plugins/BDXK/Runtime/Graphics/Rendering/ShaderLibrary/Light.hlsl"

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
			float _MaxDistance = 30;
			float _SamplingCount = 10;
			float _LightPower = 1;
			float _LightIntensity = 1;

			Fragment VertexPass(Vertex v)
			{
				Fragment fragment;
				fragment.positionCS_SV = TransformObjectToHClip(v.positionOS);
				fragment.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return fragment;
			}

			float4 FragmentPass(Fragment fragment) : SV_Target
			{
				float3 positionWS = tex2D(_CameraPositionTexture, fragment.uv);
				float3 viewPosWS = GetCameraPositionWS();
				float noise = GenerateHashedRandomFloat((fragment.uv + _Time.y * 13) * _ScreenParams.xy);
				float stepCount = noise * 2 * _SamplingCount;
				float stepLength = _MaxDistance / stepCount;
				float3 stepDirection = normalize(positionWS - viewPosWS);
				float maxDistance = min(distance(positionWS, viewPosWS), _MaxDistance);

				float3 radiance = 0;
				for (int i = 1; i <= stepCount; ++i)
				{
					float depth = i * stepLength;
					if (depth > maxDistance)
						break;

					float3 stepPosition = viewPosWS + stepDirection * depth;

					int lightCount;
					float3 lightDirections[MaxLightCount];
					float3 lightIntensities[MaxLightCount];
					GetLights(stepPosition, lightCount, lightDirections, lightIntensities);
					//TODO 无法采样多个附加灯光的深度贴图

					for (int lightIndex = 0; lightIndex < lightCount; lightIndex++)
						radiance += lightIntensities[lightIndex] / depth;
				}

				float3 color = pow(FastTonemap(radiance), _LightPower) * _LightIntensity;
				float3 lastColor = tex2D(_MainTex, fragment.uv);
				return float4(lerp(lastColor, color, 0.3), 1);
			}
			ENDHLSL
		}
	}
}