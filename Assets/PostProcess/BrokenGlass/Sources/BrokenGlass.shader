Shader "Hidden/BrokenGlass"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_ColorTex("Color",2D) = "Black"{}
		[Normal]_NormalTex("Normal",2D) = "Bump"{}
		_Intensity("Intensity",Float) = 1
		_Offset("Offset",Float) = 0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _ColorTex;
			half4 _ColorTex_ST;
			sampler2D _NormalTex;
			half _Intensity;
			half _Offset;

			half4 frag(v2f i) : SV_Target
			{
				half2 texUV = i.uv * _ColorTex_ST.xy + _ColorTex_ST.zw;
				texUV.x *= _ScreenParams.x / _ScreenParams.y;

				half4 color = tex2D(_ColorTex, texUV);
				half2 normal = UnpackNormal(tex2D(_NormalTex, texUV));

				half4 source = tex2D(_MainTex, i.uv + normal.xy * _Offset);

				return float4(lerp(source.rgb, color.rgb, saturate(color.a * _Intensity)), 1);
			}
			ENDHLSL
		}
	}
}