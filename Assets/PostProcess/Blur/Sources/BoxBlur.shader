Shader "Hidden/BoxBlur"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BlurRadius("BlurRadius",Float) = 0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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
		int _BlurRadius;

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = TransformObjectToHClip(v.vertex);
			o.uv = v.uv;
			return o;
		}

		half4 frag_2x2filter(v2f i) : SV_Target
		{
			half4 col = 0;
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(1, 1)); //(1,1)
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(1, -1)); //(1,-1)
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(-1, -1)); //(-1,-1)
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(-1, 1)); //(-1,1)
			return col / 4.0;
		}

		half4 frag_3x3filter(v2f i):SV_Target
		{
			half4 col = tex2D(_MainTex, i.uv);
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(1, 1)); //(1,1)
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(1, -1)); //(1,-1)
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(-1, -1)); //(-1,-1)
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(-1, 1)); //(-1,1)
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(-1, 0)); //(-1,0)
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(0, 1)); //(0,1)
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(1, 0)); //(1,0)
			col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * _BlurRadius * half2(0, -1)); //(0,-1)
			return col / 9;
		}
		ENDHLSL

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag_2x2filter
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag_3x3filter
			ENDHLSL
		}
	}
}