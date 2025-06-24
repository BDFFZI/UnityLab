Shader "Hidden/Bloom"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Threshold("Threshold",Float) = 1
		_BlurRadius("BlurRadius",Float) = 1
		_Intensity("Intensity",Float) = 1
	}
	SubShader
	{
		HLSLINCLUDE
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

		sampler2D _MainTex;
		half4 _MainTex_TexelSize;
		half _BlurRadius;

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = TransformObjectToHClip(v.vertex);
			o.uv = v.uv;
			return o;
		}

		half4 SampleBlurColor(half2 uv)
		{
			half4 color = 0;
			color += tex2D(_MainTex, uv + _MainTex_TexelSize.xy * _BlurRadius * float2(-1, 1));
			color += tex2D(_MainTex, uv + _MainTex_TexelSize.xy * _BlurRadius * float2(1, 1));
			color += tex2D(_MainTex, uv + _MainTex_TexelSize.xy * _BlurRadius * float2(1, -1));
			color += tex2D(_MainTex, uv + _MainTex_TexelSize.xy * _BlurRadius * float2(-1, -1));
			return color * 0.25f;
		}
		ENDHLSL

		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass//找出发光区域
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			half _Threshold;

			half4 frag(v2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);
				half maxColor = max(max(col.r, col.g), col.b);
				half intensity = max(0, maxColor - _Threshold) / max(maxColor, 0.0001f);
				return col * intensity;
			}
			ENDHLSL
		}

		Pass//降采样模糊
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			half4 frag(v2f i) : SV_Target
			{
				return SampleBlurColor(i.uv);
			}
			ENDHLSL
		}

		Pass//升采样模糊增亮
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _BloomTex;

			half4 frag(v2f i) : SV_Target
			{
				return SampleBlurColor(i.uv) + tex2D(_BloomTex, i.uv);
			}
			ENDHLSL
		}

		Pass//输出合并
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _BloomTex;
			half _Intensity;

			half4 frag(v2f i) : SV_Target
			{
				return tex2D(_MainTex, i.uv) + tex2D(_BloomTex, i.uv) * _Intensity;
			}
			ENDHLSL
		}
	}
}