Shader "Unlit/GaussianBlur"
{
	Properties
	{
		_MainTex("MainTex",2D) = "white"
	}
	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalRenderPipeline"
			"Queue" = "Geometry"
		}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		cbuffer UnityPerMaterial
		{
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			int _BlurRadius;
			float _BlurWeight[7];
		}

		struct Vertex
		{
			float3 positionOS:POSITION;
			float2 uv:TEXCOORD0;
		};

		struct Pixel
		{
			float4 positionCS:SV_POSITION;
			float2 uv[7]:TEXCOORD0;
		};

		Pixel VerticalVertexPass(const Vertex vertex)
		{
			Pixel pixel;
			pixel.positionCS = TransformObjectToHClip(vertex.positionOS);

			for (int i = 0; i < 7; ++i)
				pixel.uv[i] = vertex.uv + float2(0, (i - _BlurRadius) * _MainTex_TexelSize.y);

			return pixel;
		}
		Pixel HorizontalVertexPass(const Vertex vertex)
		{
			Pixel pixel;
			pixel.positionCS = TransformObjectToHClip(vertex.positionOS);

			for (int i = 0; i < 7; ++i)
				pixel.uv[i] = vertex.uv + float2((i - _BlurRadius) * _MainTex_TexelSize.x, 0);

			return pixel;
		}

		float4 PixelPass(const Pixel pixel):SV_TARGET
		{
			float4 result = 0;
			for (int i = 0; i <= _BlurRadius * 2; ++i)
				result += tex2D(_MainTex, pixel.uv[i]) * _BlurWeight[i];
			return result;
		}
		ENDHLSL

		ZTest Always Cull Off ZWrite Off
		Pass
		{
			HLSLPROGRAM
			#pragma vertex HorizontalVertexPass
			#pragma fragment PixelPass
			ENDHLSL
		}
		Pass
		{
			HLSLPROGRAM
			#pragma vertex VerticalVertexPass
			#pragma fragment PixelPass
			ENDHLSL
		}
	}
}