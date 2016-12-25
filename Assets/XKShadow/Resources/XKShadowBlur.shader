Shader "Hidden/XKShadow/XKShadowBlur"
{
	Properties
	{
		_MainTex ("Main Tex", 2D) = "white" { }
	}
	
	CGINCLUDE
	
	#include "UnityCG.cginc"

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		v2f vert (appdata v)
		{
			v2f o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			o.uv = v.uv;
			return o;
		}
		
		fixed4 fragBlur (v2f i) : SV_Target
		{
			fixed4 c = fixed4(0,0,0,0);
			fixed4 c0 = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y));
			fixed4 c1 = tex2D(_MainTex, i.uv + float2(-_MainTex_TexelSize.x, -_MainTex_TexelSize.y));
			fixed4 c2 = tex2D(_MainTex, i.uv + float2(-_MainTex_TexelSize.x, _MainTex_TexelSize.y));
			fixed4 c3 = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y));
			c.b = (c0.b + c1.b + c2.b + c3.b) * 0.25;
			return c;
		}
		
		fixed4 fragPrePostBlur (v2f i) : SV_Target
		{
			return tex2D(_MainTex, i.uv);
		}
	
	ENDCG
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		ColorMask B

		// PrePostBlur
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragPrePostBlur
			ENDCG
		}

		// Blur
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragBlur
			ENDCG
		}
	}
}
