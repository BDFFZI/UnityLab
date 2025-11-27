Shader "RenderFeature/LandmarkTexture"
{
	Properties
	{
		_PositionY("PositionY",Float) = -2
		_PositionYThreshold("PositionYThreshold",Float) = 0.5
		_Direction("Direction",Vector) = (0,1,0,0)
		_DirectionThreshold("DirectionThreshold",Float) = 0.9
		_Origin("Origin",Vector) = (0,0,0)
		_DistanceThreshold("DistanceThreshold",Vector) = (0,1000,0,0)
		_PointMaskScale("PointMaskScale",Float) = 4
	}
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
			};

			struct Fragment
			{
				float4 positionCS:SV_POSITION;
				float2 texcoord :TEXCOORD0;
			};

			sampler2D _CameraPositionTexture;
			sampler2D _CameraNormalTexture;
			sampler2D _CameraDepthTexture;

			float _PositionY;
			float _PositionYThreshold;
			float3 _Direction;
			float _DirectionThreshold;
			float2 _Origin;
			float2 _DistanceThreshold;
			float _PointMaskScale;

			Fragment VertexPass(Vertex vertex)
			{
				Fragment fragment;
				fragment.positionCS = TransformObjectToHClip(vertex.positionOS);
				fragment.texcoord = vertex.positionOS;
				return fragment;
			}

			float4 FragmentPass(Fragment fragment):SV_TARGET
			{
				float3 positionWS = tex2D(_CameraPositionTexture, fragment.texcoord);
				float3 normalWS = tex2D(_CameraNormalTexture, fragment.texcoord) * 2 - 1;
				float depth = tex2D(_CameraDepthTexture, fragment.texcoord);
				float dis = distance(positionWS, _Origin);

				float positionYMask = distance(positionWS.y, _PositionY) < _PositionYThreshold;
				float directionMask = dot(normalWS, _Direction) > _DirectionThreshold;
				float distanceMask = dis > _DistanceThreshold.x && dis < _DistanceThreshold.y;

				float3 pointMask3D = step(1 - LinearEyeDepth(depth, _ZBufferParams) * 0.05, frac(positionWS * _PointMaskScale));
				float pointMask = step(1.5, pointMask3D.x + pointMask3D.z);

				float mask = directionMask * positionYMask * distanceMask * pointMask;
				if (mask < 0.001)
					return float4(0, -10000, 0, 0);

				return float4(positionWS, mask);
			}
			ENDHLSL
		}
	}
}