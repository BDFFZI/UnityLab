Shader "Hidden/BaseProcess"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Intensity("Intensity",Range(0.0,2.0)) = 1.0
		_Saturate("Saturate",Range(0.0,2.0)) = 1.0
		_Contrast("Contrast",Range(0.0,2.0)) = 1.0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

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
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			half _Intensity;
			half _Saturate;
			half _Contrast;

			half4 frag(v2f i) : SV_Target
			{
				half4 color = tex2D(_MainTex, i.uv);
				color.rgb = color.rgb * _Intensity;
				color.rgb = lerp(0.299 * color.r + 0.587 * color.g + 0.114 * color.b, color.rgb, _Saturate);
				color.rgb = lerp(0.5, color.rgb, _Contrast);
				return color;
			}
			ENDCG
		}
	}
}