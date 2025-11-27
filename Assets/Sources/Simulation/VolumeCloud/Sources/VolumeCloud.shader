Shader "Hidden/VolumeCloud"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
				float2 uv : TEXCOORD0;
			};

			struct Fragment
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _CameraDepthTexture;

			float4x4 _ClipToWorld_OpenGL;
			sampler3D _DensityTex;
			float3 _DensityTexPosition;
			float3 _DensityTexBound;
			float _DensityThreshold;
			float _DensityPower;
			float _DensityScale;
			sampler2D _SamplingNoiseTex;
			float _LightOutMaxDistance;
			int _LightOutSamplingCount;
			float _LightInMaxDistance;
			int _LightInSamplingCount;
			float3 _WindSpeed;
			float4 _CloudColor;
			float _CloudAlphaPower;
			sampler2D _LightAttenuationTex;

			Fragment VertexPass(Vertex vertex)
			{
				Fragment fragment;
				fragment.positionCS = TransformObjectToHClip(vertex.positionOS);
				fragment.uv = TRANSFORM_TEX(vertex.uv, _MainTex);
				return fragment;
			}

			///定义云的形状
			float SampleDensity(float3 positionWS)
			{
				float3 uv = saturate((positionWS - _DensityTexPosition) / _DensityTexBound + float3(0.5, 0, 0.5));
				//消除范围外云朵
				float3 mask3D = 1 - abs(uv * 2 - 1);
				float mask = min(min(mask3D.x, mask3D.y), mask3D.z);

				float density = tex3Dlod(_DensityTex, float4(uv + _WindSpeed * _Time.y, 0));
				return pow(saturate(density - _DensityThreshold) * mask, _DensityPower) * _DensityScale;
			}

			float4 FragmentPass(Fragment fragment) : SV_Target
			{
				//计算步进方向
				float3 viewPosWS = GetCameraPositionWS();
				float4 positionWS = mul(_ClipToWorld_OpenGL, float4(fragment.uv * 2 - 1, -1, 1));
				float3 stepDirection = normalize(positionWS.xyz / positionWS.w - viewPosWS);

				//计算步进起点（对于云层外的相机要拉到最近的云层表面）
				float3 stepOrigin = viewPosWS;
				if (viewPosWS.y < _DensityTexPosition.y)
					stepOrigin += stepDirection.xyz / stepDirection.y * (_DensityTexPosition.y - viewPosWS.y);
				else if (viewPosWS.y > _DensityTexPosition.y + _DensityTexBound.y)
					stepOrigin += stepDirection.xyz / stepDirection.y * (_DensityTexPosition.y + _DensityTexBound.y - viewPosWS.y);

				//利用步进起点在的视图空间的深度方向和大小，判断是否背向或被遮挡
				float depth = dot(stepOrigin - viewPosWS, stepDirection);
				if (depth < 0 || depth > LinearEyeDepth(tex2D(_CameraDepthTexture, fragment.uv), _ZBufferParams))
					return 0;

				//计算步进次数和长度
				float lightOutStepDepth = _LightOutMaxDistance / _LightOutSamplingCount;
				float lightInStepDepth = _LightInMaxDistance / _LightInSamplingCount;
				float noise = tex2D(_SamplingNoiseTex, fragment.uv * _Time.y * 13) * 5; //通过设置不同的偏移值，配合模糊，实现均匀采样，以更低的开销获得更好的效果
				float lightOutStepDepthOffset = lightOutStepDepth * noise;
				float lightInStepDepthOffset = lightInStepDepth * noise;


				//获取基本光照信息
				Light light = GetMainLight();

				float lightOutIntensity = 0;
				float lightOutDensity = 0;
				stepOrigin += stepDirection * lightOutStepDepthOffset;
				for (int lightOutIndex = 1; lightOutIndex <= _LightOutSamplingCount; ++lightOutIndex)
				{
					//光反射时要经过的云密度
					float3 lightOutPositionWS = stepOrigin + stepDirection * lightOutStepDepth * lightOutIndex;
					float stepLightOutDensity = SampleDensity(lightOutPositionWS);
					if (stepLightOutDensity == 0) //密度等于1意味着没有反射介质
						continue;
					lightOutDensity += stepLightOutDensity;
					if (lightOutDensity >= 1) //密度大于1意味着后续反射光全被吸收
						break;

					//入射光到达此处时经过的云密度
					float lightInDensity = 0;
					lightOutPositionWS += light.direction * lightInStepDepthOffset;
					for (int lightInIndex = 1; lightInIndex <= _LightInSamplingCount; ++lightInIndex)
					{
						float3 lightInPositionWS = lightOutPositionWS + light.direction * lightInStepDepth * lightInIndex;
						lightInDensity += SampleDensity(lightInPositionWS);
					}
					lightInDensity = saturate(lightInDensity);

					//计算该位置最终能返回相机的光强
					float lightInAttenuation = 1 - lightInDensity;
					float lightOutAttenuation = 1 - lightOutDensity;
					lightOutIntensity += pow(lightInAttenuation * lightOutAttenuation, 2);
				}

				//计算光照信息
				float3 viewDir = -stepDirection;
				float transmission = pow(saturate(dot(viewDir, -light.direction) * 0.5 + 0.5), 10) * 10;

				float3 cloudColor = NeutralTonemap(light.color * lerp(transmission, lightOutIntensity, 0.8) * _CloudColor.rgb);
				float cloudAlpha = saturate(pow(lightOutDensity, _CloudAlphaPower) * _CloudColor.a);
				float4 cloud = float4(cloudColor, cloudAlpha);
				float4 lastCloud = tex2D(_MainTex, fragment.uv);
				return lerp(lastCloud, cloud, 0.3);
			}
			ENDHLSL
		}
	}
}