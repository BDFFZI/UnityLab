Shader "Unlit/BaseProcess"
{
	Properties
	{
		_MainTex("MainTex",2D) = "white" {}
		_Intensity("Intensity",Range(0.0,2.0)) = 1.0
		_Saturate("Saturate",Range(0.0,2.0)) = 1.0
		_Contrast("Contrast",Range(0.0,2.0)) = 1.0
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

			cbuffer UnityPerMaterial
			{
				sampler2D _MainTex;
				float _Intensity;
				float _Saturate;
				float _Contrast;
			}

			struct Vertex
			{
				float3 positionOS:POSITION;
				float2 uv:TEXCOORD0;
			};

			struct Pixel
			{
				float4 positionCS:SV_POSITION;
				float2 uv:TEXCOORD0;
			};

			Pixel VertexPass(const Vertex vertex)
			{
				Pixel pixel;
				pixel.positionCS = TransformObjectToHClip(vertex.positionOS);
				pixel.uv = vertex.uv;
				return pixel;
			}

			float4 PixelPass(const Pixel pixel):SV_TARGET
			{
				float4 color = tex2D(_MainTex, pixel.uv);
				color.rgb *= _Intensity;
				color.rgb = lerp(0.299 * color.r + 0.587 * color.g + 0.114 * color.b, color.rgb, _Saturate);
				color.rgb = lerp(0.5, color.rgb, _Contrast);
				return color;
			}
			ENDHLSL
		}
	}
}