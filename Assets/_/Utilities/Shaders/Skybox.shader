Shader "Unlit/Skybox"
{
	Properties
	{
		_MainTex ("Texture", Cube) = "white" {}
		_Lod("Lod",Float) = 0
		_MinIntensity("_MinIntensity", Float) = 0
		_MaxIntensity("_MaxIntensity", Float) = 1
		_Color("Color",Color) = (1,1,1,1)
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
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float3 normal : NORMAL;
				float4 vertex : SV_POSITION;
			};

			samplerCUBE _MainTex;
			float _Lod;
			float _MinIntensity;
			float _MaxIntensity;
			float4 _Color;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex);
				o.normal = v.vertex.xyz;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float3 col = texCUBElod(_MainTex, float4(i.normal, _Lod)).rgb;
				col = smoothstep(_MinIntensity, _MaxIntensity, col);
				col *= _Color;
				// col = ToneMapping(col);
				return float4(col, 1);
			}
			ENDHLSL
		}
	}
}