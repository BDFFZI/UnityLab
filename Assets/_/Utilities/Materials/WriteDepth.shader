Shader "Unlit/WriteDepth"
{
	Properties {}
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
		}

		Pass
		{
			Tags
			{
				"LightMode"="UniversalForward"
			}
			ColorMask 0
		}
		Pass
		{
			Tags
			{

				"LightMode"="DepthOnly"
			}
			ColorMask 0
		}
	}
}