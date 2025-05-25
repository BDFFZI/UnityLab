Shader "Unlit/VolumeCloud"
{
	Properties {}
	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalRenderPipeline"
			"Queue"="Transparent"
		}

		Pass
		{
			//			Blend Srcalpha OneMinusSrcalpha

			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal@10.10.1/ShaderLibrary/core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal@10.10.1/ShaderLibrary/Lighting.hlsl"
			#include "Assets/Sources/Shader.hlsl"
			#pragma vertex VertexPass
			#pragma fragment PixelPass

			float3 _BoundsMin;
			float3 _BoundsMax;
			sampler2D _CameraDepthTexture;

			float4 VertexPass(const float3 positionOS:POSITION):SV_POSITION
			{
				return TransformObjectToHClip(positionOS);
			}

			//返回（相机到容器的距离，光线从容器中出去的距离）
			float2 rayBoxDst(
				//边界框最小值       边界框最大值         
				float3 boundsMin, float3 boundsMax,
				//世界相机位置      光线方向倒数
				float3 rayOrigin, float3 invRaydir)
			{
				float3 t0 = (boundsMin - rayOrigin) * invRaydir;
				float3 t1 = (boundsMax - rayOrigin) * invRaydir;
				float3 tmin = min(t0, t1);
				float3 tmax = max(t0, t1);

				float dstA = max(max(tmin.x, tmin.y), tmin.z); //进入点
				float dstB = min(tmax.x, min(tmax.y, tmax.z)); //出去点

				float dstToBox = max(0, dstA);
				float dstInsideBox = max(0, dstB - dstToBox);
				return float2(dstToBox, dstInsideBox);
			}

			float cloudRayMarching(float3 startPoint, float3 direction)
			{
				float3 testPoint = startPoint;
				direction *= 0.5; //每次步进间隔
				float sum = 0.0;
				for (int i = 0; i < 256; i++) //步进总长度
				{
					testPoint += direction;
					if (testPoint.x < 10.0 && testPoint.x > -10.0 &&
						testPoint.z < 10.0 && testPoint.z > -10.0 &&
						testPoint.y < 10.0 && testPoint.y > -10.0)
						sum += 0.01;
				}
				return sum;
			}


			float4 PixelPass(float4 screenPos :VPOS):SV_TARGET
			{
				float4 screenParams = GetScaledScreenParams();
				float depth = tex2D(_CameraDepthTexture, screenPos.xy / screenParams.xy);
				if(depth > 1)
					return float4(1,0,0,1);
				if (depth < 0)
					return float4(0,0,1,1);
				return float4((float3)depth, 1);

				// 	//重建屏幕空间uv
				// float4 screenParams = GetScaledScreenParams();
				// float2 uv = (screenPos.xy - screenParams.xy / 2) / screenParams.y;
				// //重建世界位置和方向
				// const float3 cameraWS = GetCameraPositionWS();
				// const float3 normalWS = mul(normalize(float3(uv, 1)), unity_WorldToCamera);
				//
				// float cloud = cloudRayMarching(cameraWS, normalWS);
				// return 1 * cloud;
			}
			ENDHLSL
		}
	}
}