Shader "Effects/TerrainScanning"
{
	Properties {}
	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalRenderPipeline"
		}

		Pass
		{
			ZTest Off
			ZWrite Off
			Blend Srcalpha OneMinusSrcalpha

			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment PixelPass
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			sampler2D _CameraDepthTexture;
			float _Distance;
			float _Width;

			struct VertexInput
			{
				float3 positionOS:POSITION;
				float2 uv:TEXCOORD0;
			};

			struct Pixel
			{
				float4 positionCS:SV_POSITION;
				float2 uv:TEXCOORD0;
			};

			Pixel VertexPass(const VertexInput vertex)
			{
				Pixel pixel;
				pixel.positionCS = TransformObjectToHClip(vertex.positionOS);
				pixel.uv = vertex.uv;
				return pixel;
			}

			float4 PixelPass(const Pixel pixel):SV_TARGET
			{
				const float depth = tex2D(_CameraDepthTexture, pixel.uv);
				const float linearDepth = Linear01Depth(depth, _ZBufferParams);
				const float distance = linearDepth * (1 / _ZBufferParams.w);

				float4 color;
				color.rgb = 1;
				color.a = saturate(1 - abs(_Distance - distance) / _Width);
				return color;
			}
			ENDHLSL
		}
	}
}