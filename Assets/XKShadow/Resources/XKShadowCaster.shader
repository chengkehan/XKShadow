﻿Shader "Hidden/XKShadow/XKShadowCaster"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Cull Off
		ZTest LEqual

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "XKShadowUtil.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float depth : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				v.vertex.xyz += v.normal * _XKShadowExpansion;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.depth = -mul(_XKShadowCamViewMat, mul(_Object2World, v.vertex)).z;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float depth = clamp(i.depth / _XKShadowFarClipPlane, 0, 0.99f);
				fixed4 c = fixed4(0, 0, 0, 0);
				c.rg = EncodeFloatRG(depth);
				return c;
			}
			ENDCG
		}
	}
}
