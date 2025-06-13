Shader "Hidden/Vignette"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Intensity("Intensity",Float) = 1
		_Roundness("Roundness",Float) = 1
		_Smoothness("Smoothness",Float) = 1
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

			sampler2D _MainTex;
			half _Intensity;
			half _Roundness;
			half _Smoothness;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				half2 dis = abs(i.uv - 0.5) * _Intensity;
				dis = pow(dis, _Roundness);
				half vFactor = pow(1 - dot(dis, dis), _Smoothness);

				return half4(tex2D(_MainTex, i.uv).rgb * vFactor, 1);
			}
			ENDCG
		}
	}
}