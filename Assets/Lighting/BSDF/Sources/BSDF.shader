Shader "Unlit/BSDF"
{
	Properties
	{
		_MainTex("MainTex",2D)="white"{}
		_Color("Color",Color) = (1,1,1,1)
		_Smoothness("Smoothness",Range(0,1)) = 0
		_Transmittivity("Transmittivity",Range(0,1)) = 0
		_Thickness("Thickness",2D) = "white" {}
		_Disturbance("Disturbance",Range(0,1)) = 0
		_Range("Range",Range(0,1)) = 0
		_Power("Power",Float) = 3
		_Scale("Scale",Float) = 4
	}
	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
		}
		LOD 100

		Pass
		{
			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment FragmentPass

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Vertex
			{
				float3 positionOS:POSITION;
				float3 normalOS:NORMAL;
				float2 texcoord:TEXCOORD0;
			};

			struct Fragment
			{
				float3 positionWS:TEXCOORD1;
				float3 normalWS:TEXCOORD2;
				float2 texcoord:TEXCOORD0;
				float4 positionCS:SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			float _Smoothness;
			float _Disturbance;
			float _Transmittivity;
			float _Range;
			float _Power;
			float _Scale;
			sampler2D _Thickness;

			Fragment VertexPass(Vertex vertex)
			{
				Fragment fragment;
				fragment.positionWS = TransformObjectToWorld(vertex.positionOS);
				fragment.normalWS = TransformObjectToWorldNormal(vertex.normalOS);
				fragment.texcoord = TRANSFORM_TEX(vertex.texcoord, _MainTex);
				fragment.positionCS = TransformObjectToHClip(vertex.positionOS);
				return fragment;
			}

			float4 FragmentPass(Fragment fragment):SV_TARGET
			{
				float3 normalWS = normalize(fragment.normalWS);
				float3 viewDirWS = normalize(GetCameraPositionWS() - fragment.positionWS);

				float3 color = 0;

				int lightCount = 1 + GetAdditionalLightsCount();
				Light lights[MAX_VISIBLE_LIGHTS];

				lights[0] = GetMainLight();
				for (int i = 0; i < GetAdditionalLightsCount(); i++)
					lights[i + 1] = GetAdditionalLight(i, fragment.positionWS);

				for (int i = 0; i < lightCount; i++)
				{
					Light light = lights[i];
					//直接漫射
					float diffuseTerm = max(0.0f, dot(light.direction, normalWS));
					float3 diffuseLightTerm = light.color * light.distanceAttenuation * light.shadowAttenuation;
					color += diffuseTerm * diffuseLightTerm * (1 - _Transmittivity);

					//直接透射
					float3 backLightDir = -normalize(light.direction + fragment.normalWS * _Disturbance);
					float transmitTerm = saturate((dot(backLightDir, viewDirWS) + _Range) / (1 + _Range));
					transmitTerm = pow(transmitTerm, _Power) * _Scale;
					float3 transmitLightTerm = light.color * light.distanceAttenuation;
					float thickness = tex2D(_Thickness, fragment.texcoord);
					color += transmitTerm * transmitLightTerm * thickness * _Transmittivity;
				}

				//间接漫射
				color += SampleSH(normalWS);

				color *= _Color;

				//间接镜射
				float fresnel = pow(saturate(1 - dot(normalWS, viewDirWS)), 2);
				color += GlossyEnvironmentReflection(reflect(-viewDirWS, normalWS), 1 - _Smoothness, fresnel);

				return float4(color, 1);
			}
			ENDHLSL
		}
	}
}