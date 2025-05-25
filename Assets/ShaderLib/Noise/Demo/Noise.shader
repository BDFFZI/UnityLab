Shader "Unlit/Noise"
{
	Properties
	{
		[KeywordEnum(Mosaic,Value,Perlin,Simplex,Worley,Perlin_FBM,Simplex_FBM)]_Noise("Noise",Float) = 0
		_Scale("Scale",Range(0,100)) = 10
	}
	SubShader
	{
		Tags
		{
			"RenderPipeline"="UniversalRenderPipeline"
			"Queue"="Geometry"
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment PixelPass
			#pragma shader_feature_local _ _NOISE_MOSAIC _NOISE_VALUE _NOISE_PERLIN _NOISE_SIMPLEX _NOISE_WORLEY _NOISE_PERLIN_FBM _NOISE_SIMPLEX_FBM

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/core.hlsl"
			#include "../Sources/Noise.hlsl"

			float _Scale;

			struct Pixel
			{
				half3 positionWS:TEXCOORD0;
				half4 positionCS:SV_POSITION;
			};


			Pixel VertexPass(const half3 positionOS:POSITION)
			{
				Pixel pixel;
				pixel.positionWS = TransformObjectToWorld(positionOS);
				pixel.positionCS = TransformWorldToHClip(pixel.positionWS);
				return pixel;
			}

			half4 PixelPass(const Pixel pixel) : SV_Target
			{
				float3 uv = pixel.positionWS * _Scale;
				uv.z = _Time.y;

				//生成噪波值
				float noise;
				#if _NOISE_MOSAIC
				noise = MosaicNoise(uv);
				#elif _NOISE_VALUE
				noise = ValueNoise(uv);
				#elif _NOISE_PERLIN
				noise = PerlinNoise(uv);
				#elif _NOISE_SIMPLEX
				noise = SimplexNoise(uv);
				#elif _NOISE_WORLEY
				noise = WorleyNoise(uv);

				//分型噪波
				#elif _NOISE_PERLIN_FBM
				noise = PerlinNoise(uv * 1) * (4.0 / 7) +
				        PerlinNoise(uv * 2) * (2.0 / 7) +
				        PerlinNoise(uv * 4) * (1.0 / 7);
				#elif _NOISE_SIMPLEX_FBM
				noise = SimplexNoise(uv * 1) * (4.0 / 7) +
				        SimplexNoise(uv * 2) * (2.0 / 7) +
				        SimplexNoise(uv * 4) * (1.0 / 7);
				
				#else
				noise = 0;
				#endif

				//验证噪波值是否在[0,1]区间后，用其做颜色
				float3 color;
				if (noise > 1)
					color = float3(1, 0, 0);
				else if (noise < 0)
					color = float3(0, 0, 1);
				else
					color = pow(noise, 2.2); //扩大暗区可以增强细节

				// return float4(pixel.positionWS, 1);
				return float4(color, 1);
			}
			ENDHLSL
		}

	}
}