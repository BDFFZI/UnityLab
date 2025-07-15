Shader "Material/Lit"
{
	Properties
	{
		_AlbedoMap("AlbedoMap",2D)="white"{}
		_Albedo("Albedo",Color)=(1,1,1,1)
		_MetallicTex("MetallicMap",2D) = "white"{}
		_Metallic("Metallic",Range(0.0,1.0)) = 0
		_SmoothnessMap("SmoothnessMap",2D) = "white"{}
		_Smoothness("Smoothness",Range(0.0,1.0)) = 1
		[Normal]_NormalMap("NormalMap",2D) = "bump"{}
		_OcclusionMap("OcclusionMap",2D) = "white"{}
		_HeightMap("HeightMap",2D)="white"{}
		_Height("Height",Range(0,0.08))=0.005
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
			#pragma multi_compile _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT

			struct Vertex
			{
				float3 positionOS:POSITION;
				float3 normalOS:NORMAL;
				float4 tangentOS:TANGENT;
				float2 texcoord:TEXCOORD0;
			};

			struct Fragment
			{
				float3 positionWS:TEXCOORD1;
				float3 normalWS:TEXCOORD2;
				float3 tangentWS:TEXCOORD3;
				float3 binormalWS:TEXCOORD4;
				float2 texcoord:TEXCOORD0;
				float4 positionCS:SV_POSITION;
			};

			float4 _AlbedoMap_ST;
			sampler2D _AlbedoMap;
			float4 _Albedo;
			sampler2D _MetallicTex;
			float _Metallic;
			sampler2D _SmoothnessMap;
			float _Smoothness;
			sampler2D _NormalMap;
			sampler2D _OcclusionMap;
			sampler2D _HeightMap;
			float _Height;

			Fragment VertexPass(Vertex vertex)
			{
				Fragment fragment;
				fragment.positionWS = TransformObjectToWorld(vertex.positionOS);
				fragment.normalWS = TransformObjectToWorldNormal(vertex.normalOS);
				fragment.tangentWS = TransformObjectToWorldDir(vertex.tangentOS.xyz);
				fragment.binormalWS = cross(fragment.normalWS, fragment.tangentWS) * vertex.tangentOS.w;
				fragment.texcoord = TRANSFORM_TEX(vertex.texcoord, _AlbedoMap);
				fragment.positionCS = TransformObjectToHClip(vertex.positionOS);
				return fragment;
			}

			float3 ComputeLight(Light light, float3 viewDir, float3 diffuse, float3 specular, float3 normal, float roughness, float roughness2)
			{
				float3 lightDir = light.direction;
				float3 halfDir = SafeNormalize(lightDir + viewDir);
				float HdotN = saturate(dot(halfDir, normal));
				float HdotL = saturate(dot(halfDir, lightDir));

				//辐射度量学
				float3 irradiance = light.color * light.distanceAttenuation * light.shadowAttenuation;
				float3 radiance = saturate(dot(normal, lightDir)) * irradiance;

				//双向反射分布函数（Cook-Torrance）
				float D = roughness2 / pow(1 + HdotN * HdotN * (roughness2 - 1), 2);
				float GF = 1 / (max(0.1h, HdotL * HdotL) * (roughness + 0.5));
				float specularTerm = D * GF / 4;
				float3 brdf = diffuse + specular * specularTerm;

				return brdf * radiance;
			}

			float4 FragmentPass(Fragment fragment):SV_TARGET
			{
				//构建切线空间
				float3 normalWS = normalize(fragment.normalWS);
				float3 tangentWS = normalize(fragment.tangentWS);
				float3 binormalWS = normalize(fragment.binormalWS);
				float3x3 tangentToWorld = transpose(float3x3(tangentWS, binormalWS, normalWS));

				//获取相机方向
				float3 viewDir = normalize(GetCameraPositionWS() - fragment.positionWS);

				//解析高度图
				float3 viewDirTS = mul(viewDir, tangentToWorld);
				float2 unitOffset = viewDirTS.xy / (viewDirTS.z + 0.42f);
				float2 texcoord = fragment.texcoord;
				texcoord += unitOffset * (tex2D(_HeightMap, texcoord) - 0.5) * (_Height / 3);
				texcoord += unitOffset * (tex2D(_HeightMap, texcoord) - 0.5) * (_Height / 3);
				texcoord += unitOffset * (tex2D(_HeightMap, texcoord) - 0.5) * (_Height / 3);

				//读取输入数据
				float4 albedo = tex2D(_AlbedoMap, texcoord) * _Albedo;
				float metallic = tex2D(_MetallicTex, texcoord) * _Metallic;
				float smoothness = tex2D(_SmoothnessMap, texcoord) * _Smoothness;
				float3 normalTS = UnpackNormal(tex2D(_NormalMap, texcoord));
				float occlusion = tex2D(_OcclusionMap, texcoord);

				//解析PBR参数
				float3 diffuse = lerp(albedo.rgb * 0.96, 0, metallic);
				float3 specular = lerp(0.04, albedo.rgb, metallic);
				float3 normal = mul(tangentToWorld, normalTS);
				float roughness = max(HALF_MIN_SQRT, pow(1 - smoothness, 2));
				float roughness2 = roughness * roughness;

				float3 color = 0;

				//直接光照
				color += ComputeLight(
					GetMainLight(TransformWorldToShadowCoord(fragment.positionWS)),
					viewDir, diffuse, specular, normal, roughness, roughness2
				);
				for (int i = GetAdditionalLightsCount(); i >= 0; i--)
				{
					color += ComputeLight(
						GetAdditionalLight(i, fragment.positionWS, 1),
						viewDir, diffuse, specular, normal, roughness, roughness2
					);
				}

				//间接漫射光照
				float3 envDiffuseRadiance = SampleSH(normal);
				color += diffuse * envDiffuseRadiance * occlusion;

				//间接镜射光
				float F = pow(1 - saturate(dot(normal, viewDir)), 4);
				float3 envSpecular = lerp(specular, saturate(lerp(0.04, 1, metallic) + smoothness), F);
				float3 envSpecularRadiance = GlossyEnvironmentReflection(reflect(-viewDir, normal), 1 - smoothness, 1);
				color += envSpecular * envSpecularRadiance * occlusion / (roughness2 + 1);

				return float4(color, 1);
			}
			ENDHLSL
		}

		UsePass "Universal Render Pipeline/Lit/ShadowCaster"
	}
}