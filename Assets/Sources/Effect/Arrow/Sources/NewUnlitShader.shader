Shader "CustomRenderTexture/Simple"
{
	Properties {}

	SubShader
	{
		Lighting Off
		Blend One Zero

		Pass
		{
			HLSLPROGRAM
			#include "UnityCustomRenderTexture.cginc"
			#include "Assets/Plugins/HlslLibrary/Noise.hlsl"
			#pragma vertex CustomRenderTextureVertexShader
			#pragma fragment frag
			#pragma target 3.0


			float4 frag(v2f_customrendertexture IN) : COLOR
			{
				return Hubris::FBM(IN.globalTexcoord * 10, 4, 0.5, 2);
			}
			ENDHLSL
		}
	}
}