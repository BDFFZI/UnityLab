Shader "Unlit/ScreenTexture"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
				float4 positionCS_SV:SV_POSITION;
				float4 positionCS:TEXCOORD0;
			};

			sampler2D _MainTex;

			Fragment VertexPass(Vertex vertex)
			{
				Fragment fragment;
				fragment.positionCS_SV = TransformObjectToHClip(vertex.positionOS);
				fragment.positionCS = fragment.positionCS_SV;
				#if UNITY_UV_STARTS_AT_TOP
				fragment.positionCS.y *= -1;
				#endif

				return fragment;
			}

			float4 FragmentPass(Fragment fragment):SV_TARGET
			{
				float2 screenUV = fragment.positionCS.xy / fragment.positionCS.w * 0.5f + 0.5f;
				return tex2D(_MainTex, screenUV);
			}
			ENDHLSL
		}
	}
}