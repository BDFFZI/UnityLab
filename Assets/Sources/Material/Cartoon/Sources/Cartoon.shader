Shader "Hidden/Cartoon"
{
	Properties
	{
		_BaseColorMap ("BaseColorMap", 2D) = "gray" {}
		_MetalnessMap ("MetalnessMap", 2D) = "white" {}
		_Metalness ("Metalness", Range(0,1)) = 0
		_SmoothnessMap ("SmoothnessMap", 2D) = "white" {}
		_Smoothness("Smoothness",Range(0,1)) = 1
		_NormalMap ("NormalMap", 2D) = "bump" {}
		_OcclusionMap ("OcclusionMap", 2D) = "white" {}
		_DiffuseRamp("DiffuseRamp",2D) = "white"{}
		_TintLayer1Color("TintLayer1Color",Color) = (0.8,0.8,0.8,1)
		_TintLayer1Offset("TintLayer1Offset",Range(-1,1)) = 0
		_TintLayer1Softness("TintLayer1Softness",Range(0,1)) = 0.1
		_TintLayer2Color("TintLayer1Color",Color) = (0.85,0.85,0.85,1)
		_TintLayer2Offset("TintLayer1Offset",Range(-1,1)) = 0.3
		_TintLayer2Softness("TintLayer1Softness",Range(0,1)) = 0.2
		_SpecShininess("SpecShininess",Float) = 128
		_SpecIntensity("SpecIntensity",Float) = 10
		_EnvIntensity("EnvIntensity",Float) = 0.5
		_OutlineWidth("OutlineWidth",Float) = 0.2
		_OutlineColor("OutlineColor",Color) = (0.5,0.5,0.5,1)
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
			Tags
			{
				"LightMode"="UniversalForward"
			}

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
				float4 tangentOS:TANGENT;
			};

			struct Fragment
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 normalWS : NORMAL;
				float3 tangentWS : TANGENT;
				float3 bitangentWS : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
			};

			sampler2D _BaseColorMap;
			float4 _BaseColorMap_ST;
			sampler2D _MetalnessMap;
			sampler2D _SmoothnessMap;
			sampler2D _NormalMap;
			sampler2D _OcclusionMap;
			sampler2D _DiffuseRamp;
			float _Metalness;
			float _Smoothness;
			float4 _TintLayer1Color;
			float _TintLayer1Offset;
			float _TintLayer1Softness;
			float4 _TintLayer2Color;
			float _TintLayer2Offset;
			float _TintLayer2Softness;
			float _SpecShininess;
			float _SpecIntensity;
			float _EnvIntensity;

			Fragment VertexPass(Vertex vertex)
			{
				Fragment fragment;
				fragment.positionCS = TransformObjectToHClip(vertex.positionOS);
				fragment.uv = TRANSFORM_TEX(vertex.uv, _BaseColorMap);
				fragment.normalWS = TransformObjectToWorldNormal(vertex.normalOS);
				fragment.tangentWS = TransformObjectToWorldDir(vertex.tangentOS.xyz);
				fragment.bitangentWS = cross(fragment.normalWS, fragment.tangentWS) * vertex.tangentOS.w;
				fragment.positionWS = TransformObjectToWorld(vertex.positionOS);
				return fragment;
			}

			float3 ComputeLight(Light light, float3 diffuse, float3 specular, float3 normalDir, float3 viewDir, float smoothness, float occlusion)
			{
				float3 lightDir = light.direction;
				float3 irradiance = light.color * light.distanceAttenuation * light.shadowAttenuation;

				//漫射光
				float diffuseTerm = (dot(normalDir, lightDir) + 1) * 0.5 * occlusion;
				// return diffuseTerm;
				float2 tint1RampCoord = float2(diffuseTerm + _TintLayer1Offset, _TintLayer1Softness);
				float tint1Ramp = tex2D(_DiffuseRamp, tint1RampCoord).r * _TintLayer1Color.a;
				float3 tint1Color = lerp(float3(1, 1, 1), _TintLayer1Color.rgb, tint1Ramp);
				diffuse *= tint1Color;
				float2 tint2RampCoord = float2(diffuseTerm + _TintLayer2Offset, _TintLayer2Softness);
				float tint2Ramp = tex2D(_DiffuseRamp, tint2RampCoord).r * _TintLayer2Color.a;
				float3 tint2Color = lerp(float3(1, 1, 1), _TintLayer2Color.rgb, tint2Ramp);
				diffuse *= tint2Color;
				//镜射光
				float3 halfDir = SafeNormalize(lightDir + viewDir);
				float NdotH = saturate(dot(halfDir, normalDir));
				float specularTerm = pow(NdotH, max(0.0001, _SpecShininess * smoothness)) * occlusion;
				specular *= specularTerm * _SpecIntensity;

				return (diffuse + specular) * irradiance;
			}

			float4 FragmentPass(Fragment fragment) : SV_Target
			{
				//材质输入
				float4 baseColor = tex2D(_BaseColorMap, fragment.uv);
				float metalness = tex2D(_MetalnessMap, fragment.uv).r * _Metalness;
				float smoothness = tex2D(_SmoothnessMap, fragment.uv).r * _Smoothness;
				float3 normalTS = UnpackNormal(tex2D(_NormalMap, fragment.uv));
				float occlusion = tex2D(_OcclusionMap, fragment.uv).r;

				//顶点输入
				float3 normalWS = normalize(fragment.normalWS);
				float3 tangentWS = normalize(fragment.tangentWS);
				float3 bitangentWS = normalize(fragment.bitangentWS);
				float3 positionWS = fragment.positionWS;

				//计算属性
				float3 diffuse = lerp(baseColor.rgb * 0.96, 0, metalness);
				float3 specular = lerp(0.04, baseColor.rgb, metalness);
				float3x3 worldToTangent = float3x3(tangentWS, bitangentWS, normalWS);
				float3 normalDir = mul(normalTS, worldToTangent);
				float3 viewDir = normalize(GetCameraPositionWS() - fragment.positionWS);

				//计算灯光
				float3 light = 0;
				float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
				Light mainLight = GetMainLight(shadowCoord, positionWS, 1);
				light += ComputeLight(mainLight, diffuse, specular, normalDir, viewDir, smoothness, occlusion);
				for (int i = 0; i < GetAdditionalLightsCount(); ++i)
				{
					Light additionalLight = GetAdditionalLight(i, positionWS, 1);
					light += ComputeLight(additionalLight, diffuse, specular, normalDir, viewDir, smoothness, occlusion);
				}

				//环境光
				float3 reflectDir = reflect(-viewDir, normalDir);
				float3 envLight = GlossyEnvironmentReflection(reflectDir, 1 - smoothness, occlusion);
				float fresnel = pow(saturate(1 - dot(viewDir, normalDir)), 5);
				light += envLight * fresnel * _EnvIntensity;

				return float4(light, baseColor.a);
			}
			ENDHLSL
		}

		//描边
		Pass
		{
			Cull Front

			Tags
			{
				"LightMode"="SRPDefaultUnlit"
			}

			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment FragmentPass

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Vertex
			{
				float3 positionOS : POSITION;
				float3 normalOS:NORMAL;
				float2 uv:TEXCOORD0;
			};

			struct Fragment
			{
				float4 positionCS : SV_POSITION;
				float2 uv:TEXCOORD0;
			};

			sampler2D _BaseColorMap;
			float4 _BaseColorMap_ST;
			float _OutlineWidth;
			float4 _OutlineColor;

			Fragment VertexPass(Vertex vertex)
			{
				float3 positionVS = mul(UNITY_MATRIX_MV, float4(vertex.positionOS, 1)).xyz;
				float3 normalVS = TransformWorldToViewNormal(TransformObjectToWorldNormal(vertex.normalOS));
				positionVS += normalVS * _OutlineWidth * 0.01f;

				Fragment fragment;
				fragment.positionCS = TransformWViewToHClip(positionVS);
				fragment.uv = TRANSFORM_TEX(vertex.uv, _BaseColorMap);
				return fragment;
			}

			float4 FragmentPass(Fragment fragment) : SV_Target
			{
				clip(_OutlineWidth - FLT_EPS);
				float3 baseColor = tex2D(_BaseColorMap, fragment.uv).rgb;
				return float4(baseColor * baseColor * _OutlineColor.rgb, _OutlineColor.a);
			}
			ENDHLSL
		}
	}
}