Shader "RenderFeature/CameraNormalTexture"
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
				float3 normalOS:NORMAL;
			};

			struct Fragment
			{
				float4 positionCS:SV_POSITION;
				float3 normalWS:NORMAL;
			};

			Fragment VertexPass(Vertex vertex)
			{
				Fragment fragment;
				fragment.positionCS = TransformObjectToHClip(vertex.positionOS);
				fragment.normalWS = TransformObjectToWorldNormal(vertex.normalOS);
				return fragment;
			}

			float4 FragmentPass(Fragment fragment):SV_TARGET
			{
				return float4(fragment.normalWS * 0.5 + 0.5, 1);
			}
			ENDHLSL
		}
	}
}