Shader "Unlit/Lit"
{
	Properties
	{
		_AlbedoTex ("AlbedoTex", 2D) = "white" {}
		_NormalTex("NormalTex",2D) = "bump" {}
		_MetallicTex("MetallicTex",2D) = "black" {}
		_SmoothnessTex("SmoothnessTex",2D) = "white" {}
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
				float3 positionOS : POSITION;
				float3 normalsOS:NORMAL;
				float4 tangentOS:TANGENT;
				float2 texcoord : TEXCOORD0;
			};

			struct Fragment
			{
				float4 positionCS:SV_POSITION;
				float3 positionWS:TEXCOORD0;
				float3 normalWS:NORMAL;
				float4 tangentWS:TANGENT;
				float2 texcoord:TEXCOORD1;
			};

			sampler2D _AlbedoTex;
			float4 _AlbedoTex_ST;
			sampler2D _NormalTex;
			sampler2D _MetallicTex;
			sampler2D _SmoothnessTex;

			Fragment VertexPass(Vertex vertex)
			{
				Fragment fragment;
				fragment.positionCS = TransformObjectToHClip(vertex.positionOS);
				fragment.positionWS = TransformObjectToWorld(vertex.positionOS);
				fragment.normalWS = TransformObjectToWorldNormal(vertex.normalsOS);
				fragment.tangentWS = float4(TransformObjectToWorldDir(vertex.tangentOS.xyz), vertex.tangentOS.w);
				fragment.texcoord = TRANSFORM_TEX(vertex.texcoord, _AlbedoTex);
				return fragment;
			}

			float4 FragmentPass(Fragment fragment) : SV_Target
			{
				//获取纹理数据
				float3 albedo = tex2D(_AlbedoTex, fragment.texcoord);
				float3 normalTS = UnpackNormal(tex2D(_NormalTex, fragment.texcoord));
				float metallic = tex2D(_MetallicTex, fragment.texcoord);
				float smoothness = tex2D(_SmoothnessTex, fragment.texcoord);

				//计算微表面参数
				float3 diffuse = lerp(albedo * 0.96, 0, metallic);
				float3 specular = lerp(0.04, albedo, metallic);
				float roughness = pow(1 - smoothness, 2);
				float3 normalWS = normalize(fragment.normalWS);
				float3 tangentWS = normalize(fragment.tangentWS.xyz);
				float3 binormalWS = cross(normalWS, tangentWS) * fragment.tangentWS.w;
				float3x3 tangentSpace = transpose(float3x3(tangentWS, binormalWS, normalWS));
				float3 normal = mul(tangentSpace, normalTS);
				float3 viewDir = normalize(GetCameraPositionWS() - fragment.positionWS);


				Light light = GetMainLight();

				float3 lightDir = light.direction;
				float3 halfDir = normalize(lightDir + viewDir);
				
				float D = (roughness * roughness) / pow(1 + pow(dot(halfDir, normal), 2) * (roughness * roughness - 1), 2);
				float GF = 1 / (pow(dot(lightDir, halfDir), 2) * (roughness + 0.5));
				float specularTerm = (D * GF) / 4;

				float3 brdf = diffuse + specular * specularTerm;
				float3 irradiance = light.color * light.distanceAttenuation * light.shadowAttenuation;
				float3 radiance = irradiance * dot(normal, light.direction);
				float3 color = brdf * radiance;

				return float4(color, 1);
			}
			ENDHLSL
		}
	}
}