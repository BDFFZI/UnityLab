Shader "Unlit/Scanning"
{
	Properties
	{
		[KeywordEnum(Sphere,Cube,Radar,Line)]_Mode("Mode",Float) = 0
		_Color ("Color", Color) = (1,1,1,1)
		_Origin("Origin",Vector) = (0,0,0,0)
		_Distance("Distance",Float) = 0
		_PaddingDistance("PaddingDistance",Float) = 0
		_PaddingPower("PaddingPower",Float) = 0.001
		_RadarRotation("RadarRotation",Float) = 0
		_LineDirection("LineDirection",Vector) = (0,0,0,0)
	}
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
		}

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment FragmentPass
			#pragma shader_feature_local _ _MODE_SPHERE _MODE_CUBE _MODE_RADAR _MODE_LINE

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Vertex
			{
				float3 positionOS : POSITION;
			};

			struct Fragment
			{
				float4 positionCS_SV : SV_POSITION;
				float3 positionWS :TEXCOORD0;
			};

			float4 _Color;
			float3 _Origin;
			float _Distance;
			float _PaddingDistance;
			float _PaddingPower;
			float _RadarRotation;
			float3 _LineDirection;

			Fragment VertexPass(Vertex v)
			{
				Fragment fragment;
				fragment.positionCS_SV = TransformObjectToHClip(v.positionOS);
				fragment.positionWS = TransformObjectToWorld(v.positionOS);
				return fragment;
			}

			float4 FragmentPass(Fragment fragment) : SV_Target
			{
				float currentDistance;

				#if _MODE_SPHERE
				currentDistance = distance(fragment.positionWS, _Origin);
				#elif _MODE_CUBE
				float3 componentDistance = abs(fragment.positionWS-_Origin);
				componentDistance.y *= 2;
				currentDistance = componentDistance.x + componentDistance.y + componentDistance.z;//曼哈顿距离
				#elif _MODE_RADAR
				float3 vec = fragment.positionWS - _Origin;
				currentDistance = saturate(atan2(vec.z, vec.x) / TWO_PI + 0.5) * 360;
				currentDistance -= _RadarRotation;
				currentDistance = clamp(currentDistance - floor(currentDistance / 360) * 360, 0, 360);
				#elif _MODE_LINE
				float3 vecOD = _LineDirection;
				float3 vecOW = fragment.positionWS - _Origin;
				currentDistance = dot(vecOW, vecOD);
				#endif


				float startDistance = _PaddingDistance == 0 ? 0 : _Distance - _PaddingDistance;
				float alpha = saturate((currentDistance - startDistance) / (_Distance - startDistance));
				alpha *= step(alpha, 0.999);
				alpha = pow(alpha, _PaddingPower);

				return float4(_Color.rgb, _Color.a * alpha);
			}
			ENDHLSL
		}
	}
}