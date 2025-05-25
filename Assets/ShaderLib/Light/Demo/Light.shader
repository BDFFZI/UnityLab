Shader "Lit/Light"
{
	Properties
	{
		[KeywordEnum(None,Lambert,Phong,BlinnPhong,CookTorrance_Unity,CookTorrance)]_Model("Model",Float) = 0
		_Albedo("Albedo",Color) = (1,1,1,1)
		_Metallic("Metallic",Range(0,1)) = 0
		_Smoothness("Smoothness",Range(0,1)) = 0
	}
	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalRenderPipeline"
			"Queue" = "Geometry"
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment PixelPass
			#pragma shader_feature_local _ _MODEL_LAMBERT _MODEL_PHONG _MODEL_BLINNPHONG _MODEL_COOKTORRANCE_UNITY _MODEL_COOKTORRANCE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "../Sources/Light.hlsl"

			cbuffer UnityPerMaterial
			{
				float4 _Albedo;
				float _Metallic;
				float _Smoothness;
			}

			struct VertexInput
			{
				float3 positionOS:POSITION;
				float3 normalOS:NORMAL;
				float2 uv:TEXCOORD0;
			};

			struct Pixel
			{
				float3 positionWS:COLOR;
				float3 normalWS:NORMAL;
				float2 uv:TEXCOORD0;
				float4 positionCS:SV_POSITION;
			};

			Pixel VertexPass(const VertexInput vertex)
			{
				Pixel pixel;
				pixel.positionWS = TransformObjectToWorld(vertex.positionOS);
				pixel.normalWS = TransformObjectToWorldNormal(vertex.normalOS);
				pixel.uv = vertex.uv;
				pixel.positionCS = TransformWorldToHClip(pixel.positionWS);
				return pixel;
			}

			float4 ComputeLight(float3 ambientColor, float3 cameraDirection, float3 surfaceNormal, float4 surfaceColor,
			                    int lightsCount, float3 lightDirections[MaxLightCount], float3 lightIntensities[MaxLightCount])
			{
				float4 color = 1;

				#if _MODEL_LAMBERT
				color.rgb = Lambert(surfaceNormal, surfaceColor.rgb, lightDirections, lightIntensities, lightsCount);
				#elif _MODEL_PHONG
				color.rgb = Phong(ambientColor, surfaceColor.rgb, surfaceColor.rgb,
				      1, 1, 1, 100,
				      surfaceNormal, cameraDirection, lightDirections, lightIntensities, lightsCount);
				#elif _MODEL_BLINNPHONG
				color.rgb = BlinnPhong(ambientColor, surfaceColor.rgb, surfaceColor.rgb,
				      1, 1, 1, 100,
				      surfaceNormal, cameraDirection, lightDirections, lightIntensities, lightsCount);
				#elif _MODEL_COOKTORRANCE
				color.rgb = CookTorrance(surfaceColor.rgb,_Metallic,_Smoothness,surfaceNormal, cameraDirection,
					 lightDirections, lightIntensities, lightsCount);
				#elif _MODEL_COOKTORRANCE_UNITY
				color.rgb = CookTorrance_Unity(surfaceColor.rgb,_Metallic,_Smoothness,surfaceNormal, cameraDirection,
					 lightDirections, lightIntensities, lightsCount);
				#endif

				return color;
			}

			float4 PixelPass(const Pixel pixel):SV_TARGET
			{
				float4 color = _Albedo;
				float3 n = normalize(pixel.normalWS);
				float3 v = normalize(GetCameraPositionWS() - pixel.positionWS);
				float3 l = GetMainLight().direction;
				float3 h = normalize(v + l);
				
				int lightCount;
				float3 lightDirections[MaxLightCount];
				float3 lightIntensities[MaxLightCount];
				GetLights(pixel.positionWS, lightCount, lightDirections, lightIntensities);

				return ComputeLight(unity_AmbientSky.rgb, v, n, color,
				                     lightCount, lightDirections, lightIntensities);
			}
			ENDHLSL
		}
	}
}