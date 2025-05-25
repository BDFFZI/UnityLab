Shader "Unlit/ShowNormal"
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
				float3 normalOS:NORMAL;
			};

			struct Pixel
			{
				float4 positionCS:SV_POSITION;
				float3 normalWS:NORMAL;
			};

			Pixel VertexPass(const Vertex vertex)
			{
				Pixel pixel;
				pixel.positionCS = TransformObjectToHClip(vertex.positionOS);
				pixel.normalWS = TransformObjectToWorldNormal(vertex.normalOS);
				return pixel;
			}

			float4 PixelPass(const Pixel pixel):SV_TARGET
			{
				return float4(normalize(pixel.normalWS), 1);
			}
			ENDHLSL
		}
	}
}