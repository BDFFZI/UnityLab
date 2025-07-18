Shader "RenderFeature/CameraOutlineTexture"
{
	Properties {}
	SubShader
	{
		Pass
		{
			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment FragmentPass

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Vertex
			{
				float3 positionOS:POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct Fragment
			{
				float4 positionCS:SV_POSITION;
				float2 texcoord:TEXCOORD0;
			};

			sampler2D _CameraDepthTexture;
			sampler2D _CameraNormalTexture;
			sampler2D _CameraPositionTexture;
			float _LineWidth;
			float _DepthEdgeThreshold;
			float _NormalEdgeThreshold;
			float _EdgeFaceThreshold;

			Fragment VertexPass(Vertex vertex)
			{
				Fragment fragment;
				fragment.positionCS = TransformObjectToHClip(vertex.positionOS);
				fragment.texcoord = vertex.texcoord;
				return fragment;
			}

			float4 FragmentPass(Fragment fragment):SV_TARGET
			{
				float2 unitUV = _LineWidth / _ScreenParams.xy;
				float2 leftButtom = fragment.texcoord + unitUV * float2(-1, -1);
				float2 leftTop = fragment.texcoord + unitUV * float2(-1, 1);
				float2 rightTop = fragment.texcoord + unitUV * float2(1, 1);
				float2 rightButtom = fragment.texcoord + unitUV * float2(1, -1);

				float leftButtomDepth = LinearEyeDepth(tex2D(_CameraDepthTexture, leftButtom), _ZBufferParams);
				float leftTopDepth = LinearEyeDepth(tex2D(_CameraDepthTexture, leftTop), _ZBufferParams);
				float rightTopDepth = LinearEyeDepth(tex2D(_CameraDepthTexture, rightTop), _ZBufferParams);
				float rightButtomDepth = LinearEyeDepth(tex2D(_CameraDepthTexture, rightButtom), _ZBufferParams);

				float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, fragment.texcoord), _ZBufferParams);
				float depthDifference = max(distance(leftButtomDepth, rightTopDepth), distance(leftTopDepth, rightButtomDepth));
				float depthEdge = step(_DepthEdgeThreshold + depth * 0.01, depthDifference);

				float3 leftButtomNormal = tex2D(_CameraNormalTexture, leftButtom) * 2 - 1;
				float3 leftTopNormal = tex2D(_CameraNormalTexture, leftTop) * 2 - 1;
				float3 rightTopNormal = tex2D(_CameraNormalTexture, rightTop) * 2 - 1;
				float3 rightButtomNormal = tex2D(_CameraNormalTexture, rightButtom) * 2 - 1;

				float normalEdge;
				if (distance(leftButtomNormal + leftTopNormal + rightTopNormal + rightButtomNormal, 0) < 0.0001f)
					normalEdge = 0;
				else
				{
					float normalDifference = max(1 - saturate(dot(leftButtomNormal, rightTopNormal)), 1 - saturate(dot(leftTopNormal, rightButtomNormal)));
					normalEdge = step(_NormalEdgeThreshold, normalDifference);
				}

				float edge = max(depthEdge, normalEdge);

				float3 normalWS = tex2D(_CameraNormalTexture, fragment.texcoord) * 2 - 1;
				float3 positionWS = tex2D(_CameraPositionTexture, fragment.texcoord);
				float3 viewDir = normalize(GetCameraPositionWS() - positionWS);
				float oneMinusNdotV = saturate(1 - dot(viewDir, normalWS));
				float face = step(_EdgeFaceThreshold, oneMinusNdotV);

				if (face > 0.5 && normalEdge < 0.5)
					return 0;

				return edge;
			}
			ENDHLSL
		}
	}
}