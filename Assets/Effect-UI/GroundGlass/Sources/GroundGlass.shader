Shader "UI/Template"
{
	Properties
	{
		// PerRendererData 表明纹理由每个渲染器提供，似乎是起某种优化作用。
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		_Color ("Tint", Color) = (1,1,1,1)

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		// 启用Clip功能，Clip掉的像素将不会参与模板测试等。
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}
	SubShader
	{
		LOD 100 //内置无光照系列Shader的LOD一般为100
		Tags
		{
			"Queue"="Transparent" // 渲染顺序为透明队列
			"PreviewType"="Plane" // 材质预览效果为平面
			"RenderType"="Transparent" // 渲染类型为透明
			"CanUseSpriteAtlas"="True" // 表明兼容 LegacySpritePacker 图集功能。
			"IgnoreProjector"="True" // 表明不受内置管线中投影器功能的影响。
		}

		//UI利用模板功能实现遮罩效果
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		//渲染器将自动排序，再加上UI都是半透明物体，所以无需深度功能
		ZWrite Off
		ZTest [unity_GUIZTestMode] // unity_GUIZTestMode 根据当前画布的渲染模式自动设置

		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment PixelPass
			#pragma shader_feature _ UNITY_UI_ALPHACLIP
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			cbuffer UnityPerMaterial
			{
				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _Color;
				sampler2D _BlurTex;
			}

			struct Vertex
			{
				float3 positionOS:POSITION;
				float4 color : COLOR;
				float2 uv:TEXCOORD0;
			};

			struct Pixel
			{
				float4 positionCS:SV_POSITION;
				float4 color : COLOR;
				float2 uv:TEXCOORD0;
				float2 uvSS:TEXCOORD1;
			};


			Pixel VertexPass(const Vertex vertex)
			{
				Pixel pixel;
				pixel.positionCS = TransformObjectToHClip(vertex.positionOS);
				pixel.color = vertex.color * _Color;
				pixel.uv = TRANSFORM_TEX(vertex.uv, _MainTex);
				pixel.uvSS = pixel.positionCS.xy / pixel.positionCS.w;
				pixel.uvSS.y *= _ProjectionParams.x;
				pixel.uvSS = pixel.uvSS * 0.5 + 0.5;
				return pixel;
			}

			half4 PixelPass(const Pixel pixel):SV_TARGET
			{
				half4 blueColor = tex2D(_BlurTex, pixel.uvSS);
				half4 color = tex2D(_MainTex, pixel.uv) * pixel.color;
				#ifdef UNITY_UI_ALPHACLIP
				clip (pixel.color.a - 0.001);
				#endif

				return color * blueColor;
			}
			ENDHLSL
		}
	}
}