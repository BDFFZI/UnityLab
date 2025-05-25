Shader "Unlit/ShowPosition"
{
	Properties {}
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

			struct Vertex
			{
				float3 positionOS:POSITION;
			};

			struct Pixel
			{
				float4 positionCS:SV_POSITION;
				float3 positionWS:TEXCOORD0;
			};

			Pixel VertexPass(const Vertex vertex)
			{
				Pixel pixel;
				pixel.positionWS = TransformObjectToWorld(vertex.positionOS);
				pixel.positionCS = TransformWorldToHClip(pixel.positionWS);
				return pixel;
			}

			float4 PixelPass(const Pixel pixel):SV_TARGET
			{
				return float4(pixel.positionWS, 1);
			}
			ENDHLSL
		}
	}
}