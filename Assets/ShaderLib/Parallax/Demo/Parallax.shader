Shader "Unlit/Parallax"
{
	Properties
	{
		_MainTex("MainTex",2D) = "white" {}
		_NormalTex("NormalTex",2D) = "bump" {}
		_HeightTex("HeightTex",2D) = "gray" {}
		_Height("Height", Range(0.000, 0.08)) = 0
		[Toggle]_GenerateNormalTex("GenerateNormalTex",Int) = 0
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
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "../Sources/Parallax.hlsl"

			#pragma shader_feature_local _ _GENERATENORMALTEX_ON

			cbuffer UnityPerMaterial
			{
				sampler2D _MainTex;
				float4 _MainTex_ST;
				sampler2D _NormalTex;
				sampler2D _HeightTex;
				float4 _HeightTex_TexelSize;
				float _Height;
			}

			struct Vertex
			{
				float3 positionOS:POSITION;
				float2 uv:TEXCOORD0;
				float3 normalOS:NORMAL;
				float4 tangentOS:TANGENT;
			};

			struct Pixel
			{
				float4 positionCS:SV_POSITION;
				float2 uv:TEXCOORD0;
				float3 normalWS:TEXCOORD1;
				float3 tangentWS:TEXCOORD2;
				float3 bitangentWS:TEXCOORD3;
				float3 viewDirWS:TEXCOORD4;
				float3 positionWS:TEXCOORD5;
			};

			Pixel VertexPass(const Vertex vertex)
			{
				Pixel pixel;
				pixel.positionCS = TransformObjectToHClip(vertex.positionOS);
				pixel.uv = vertex.uv;

				VertexNormalInputs normalInputs = GetVertexNormalInputs(vertex.normalOS, vertex.tangentOS);
				pixel.normalWS = normalInputs.normalWS;
				pixel.tangentWS = normalInputs.tangentWS;
				pixel.bitangentWS = normalInputs.bitangentWS;
				pixel.positionWS = TransformObjectToWorld(vertex.positionOS);
				pixel.viewDirWS = GetWorldSpaceViewDir(pixel.positionWS);

				return pixel;
			}

			float4 PixelPass(const Pixel pixel):SV_TARGET
			{
				float3x3 worldToTangent = float3x3(
					normalize(pixel.tangentWS),
					normalize(pixel.bitangentWS),
					normalize(pixel.normalWS)
				);
				float3x3 tangentToWorld = transpose(worldToTangent); //正交矩阵的逆矩阵等于其转置矩阵
				//解析视差贴图
				float3 viewDirTS = mul(worldToTangent, pixel.viewDirWS);
				float2 offset = ParallaxMaskingMapping(5, normalize(viewDirTS), _HeightTex, pixel.uv, _Height);
				float2 uv = pixel.uv + offset;
				float2 heightDifference = float2(
					tex2D(_HeightTex, uv + float2(_HeightTex_TexelSize.x, 0)).r - tex2D(_HeightTex, uv - float2(_HeightTex_TexelSize.x, 0)).r,
					tex2D(_HeightTex, uv + float2(0, _HeightTex_TexelSize.y)).r - tex2D(_HeightTex, uv - float2(0, _HeightTex_TexelSize.y)).r
				) * _Height;
				//解析法线贴图
				#ifndef _GENERATENORMALTEX_ON
				float3 normalTS = UnpackNormal(tex2D(_NormalTex, uv));
				#else
				float3 normalTS = normalize(float3(-heightDifference * 200, 1));
				#endif

				//获取表面信息
				float3 albedo = tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex)).rgb;
				float3 normalWS = mul(tangentToWorld, normalTS);
				float occlusion = saturate(1 - length(heightDifference) * 40);

				//计算光照
				float4 color = tex2D(_MainTex, uv) * 0.2 * occlusion;
				color.rgb += dot(GetMainLight().direction, normalWS) * albedo * GetMainLight().color;

				return color;
			}
			ENDHLSL
		}
	}
}