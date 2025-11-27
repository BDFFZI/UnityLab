Shader "Unlit/Skybox"
{
	Properties
	{
		_MainTex ("Texture", Cube) = "white" {}
		_Rotation("Rotation",Range(0,360)) = 0
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
			Cull Front

			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment FragmentPass

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Vertex
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Fragment
			{
				float4 positionCS : SV_POSITION;
				float3 uv : TEXCOORD0;
			};

			samplerCUBE _MainTex;
			float _Rotation;

			Fragment VertexPass(Vertex vertex)
			{
				Fragment fragment;
				fragment.positionCS = TransformObjectToHClip(vertex.positionOS);

				#if UNITY_REVERSED_Z
				fragment.positionCS.z = 0;
				#else
				fragment.positionCS.z = 1;
				#endif

				vertex.uv.x = (vertex.uv.x + _Rotation / 360);
				float2 sphereCoord = vertex.uv * float2(TWO_PI, PI);
				fragment.uv = float3(
					sin(sphereCoord.y) * cos(sphereCoord.x),
					-cos(sphereCoord.y),
					sin(sphereCoord.y) * sin(sphereCoord.x)
				);
				return fragment;
			}

			float4 FragmentPass(Fragment fragment) : SV_Target
			{
				return texCUBE(_MainTex, fragment.uv);
			}
			ENDHLSL
		}
	}
}