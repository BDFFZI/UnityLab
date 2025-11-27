Shader "Unlit/ScreenTexture"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[HDR]_Color("Color",Color) = (1,1,1,1)
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
				half3 positionOS:POSITION;
			};

			struct Fragment
			{
				half4 positionCS_SV:SV_POSITION;
				half4 positionCS:TEXCOORD0;
			};

			sampler2D _MainTex;
			half4 _MainTex_ST;
			half4 _Color;

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

			half3 FragmentPass(Fragment fragment):SV_TARGET
			{
				half2 screenUV = fragment.positionCS.xy / fragment.positionCS.w * 0.5f + 0.5f;
				screenUV = screenUV * _MainTex_ST.xy + _MainTex_ST.zw;
				return tex2D(_MainTex, screenUV) * _Color;
			}
			ENDHLSL
		}
	}
}