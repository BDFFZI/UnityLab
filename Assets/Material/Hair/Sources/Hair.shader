Shader "Material/Hair"
{
	Properties
	{
		_AlbedoMap("AlbedoMap",2D)="white"{}
		_Albedo("Albedo",Color)=(1,1,1,1)
		_MetallicTex("MetallicMap",2D) = "white"{}
		_Metallic("Metallic",Range(0.0,1.0)) = 0
		[Normal]_NormalMap("NormalMap",2D) = "bump"{}
		_OcclusionMap("OcclusionMap",2D) = "white"{}
		_HeightMap("HeightMap",2D)="white"{}
		_Height("Height",Range(0,0.08))=0.005
		_ShiftMap("ShiftMap",2D) = "black"{}

		_Glossiness("Glossiness",Float) = 100
		_Intensity("Intensity",FLoat) = 10
		_ShiftMapIntensity("ShiftMapIntensity",Range(0,2)) = 0
		_Shift("Shift",Range(-1,1)) = 0

		_Glossiness2("Glossiness2",Float) = 100
		_Intensity2("Intensity2",FLoat) = 10
		_ShiftMapIntensity2("ShiftMapIntensity2",Range(0,2)) = 0
		_Shift2("Shift2",Range(-1,1)) = 0
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
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

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

			struct HairHighlight
			{
				float glossiness;
				float intensity;
				float shift;
			};

			float4 _AlbedoMap_ST;
			sampler2D _AlbedoMap;
			float4 _Albedo;
			sampler2D _MetallicTex;
			float _Metallic;
			sampler2D _NormalMap;
			sampler2D _OcclusionMap;
			sampler2D _HeightMap;
			float _Height;

			sampler2D _ShiftMap;
			float4 _ShiftMap_ST;
			float _Glossiness;
			float _Intensity;
			float _ShiftMapIntensity;
			float _Shift;
			float _Glossiness2;
			float _Intensity2;
			float _ShiftMapIntensity2;
			float _Shift2;

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

			float3 ComputeLight(Light light, float3 viewDir,
			                    float3 diffuse, float3 specular,
			                    float3 normal, float3 binormal,
			                    HairHighlight hairHighlights[2]
			)
			{
				float3 lightDir = light.direction;
				float3 halfDir = SafeNormalize(lightDir + viewDir);
				float NdotL = saturate(dot(normal, lightDir) * 0.5 + 0.5);

				//辐射度量学
				float3 irradiance = light.color * light.distanceAttenuation * light.shadowAttenuation;
				float3 radiance = NdotL * irradiance;

				float specularTerm = 0;
				for (int i = 0; i < 2; i++)
				{
					HairHighlight highlight = hairHighlights[i];
					float sinBH = sqrt(1 - pow(dot(normalize(binormal + normal * highlight.shift), halfDir), 2));
					specularTerm += pow(sinBH, highlight.glossiness) * highlight.intensity;
				}

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
				float3 normalTS = UnpackNormal(tex2D(_NormalMap, texcoord));
				float occlusion = tex2D(_OcclusionMap, texcoord);
				float shiftMapValue = tex2D(_ShiftMap, texcoord * _ShiftMap_ST.xy + _ShiftMap_ST.zw) * 2 - 1;

				//解析PBR参数
				float3 diffuse = lerp(albedo.rgb * 0.96, 0, metallic);
				float3 specular = lerp(0.04, albedo.rgb, metallic);
				float3 normal = mul(tangentToWorld, normalTS);

				float3 color = 0;

				HairHighlight hairHighlight[2];
				hairHighlight[0].glossiness = _Glossiness;
				hairHighlight[0].intensity = _Intensity;
				hairHighlight[0].shift = shiftMapValue * _ShiftMapIntensity + _Shift;
				hairHighlight[1].glossiness = _Glossiness2;
				hairHighlight[1].intensity = _Intensity2;
				hairHighlight[1].shift = shiftMapValue * _ShiftMapIntensity2 + _Shift2;

				//直接光照
				color += ComputeLight(
					GetMainLight(TransformWorldToShadowCoord(fragment.positionWS)),
					viewDir, diffuse, specular, normal, binormalWS, hairHighlight
				);
				for (int i = 0; i < GetAdditionalLightsCount(); i++)
				{
					color += ComputeLight(
						GetAdditionalLight(i, fragment.positionWS, 1),
						viewDir, diffuse, specular, normal, binormalWS, hairHighlight
					);
				}

				//间接漫射光照
				float3 envDiffuseRadiance = SampleSH(normal);
				color += diffuse * envDiffuseRadiance * occlusion * shiftMapValue;

				//间接镜射光
				float F = pow(1 - saturate(dot(normal, viewDir)), 4);
				float3 envSpecular = lerp(specular, saturate(lerp(0.04, 1, metallic) + metallic), F);
				float3 envSpecularRadiance = GlossyEnvironmentReflection(reflect(-viewDir, normal), 1 - metallic, 1);
				color += envSpecular * envSpecularRadiance * occlusion * shiftMapValue;

				return float4(color, 1);
			}
			ENDHLSL
		}

		UsePass "Universal Render Pipeline/Lit/DepthOnly"
		UsePass "Universal Render Pipeline/Lit/ShadowCaster"
	}
}