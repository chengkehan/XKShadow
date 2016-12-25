Shader "XKShadow/Cutout" {
	Properties{
		_Color("Main Color", Color) = (1, 1, 1, 1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_Cutoff("Alpha cutoff", Range(0, 1)) = 0.5
	}

	SubShader{
			Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
			LOD 200

			CGPROGRAM
			#pragma surface surf Lambert alphatest:_Cutoff vertex:vert finalcolor:mycolor
			#pragma exclude_renderers xbox360 ps3
			#include "XKShadowUtil.cginc"

			sampler2D _MainTex;
			fixed4 _Color;

			struct Input {
				float2 uv_MainTex;
				XK_SHADOW_V2F(0);
			};

			void mycolor(Input IN, SurfaceOutput o, inout fixed4 color)
			{
				color.rgb *= xk_shadow_atten(XK_SHADOW_DATA(IN));
			}

			void vert(inout appdata_full v, out Input o) {
				UNITY_INITIALIZE_OUTPUT(Input, o);

				XK_SHADOW_TRANSFORM;
			}

			void surf(Input IN, inout SurfaceOutput o) {
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
				o.Albedo = c.rgb;
				o.Alpha = c.a;
			}
			ENDCG
		}

		Fallback "Transparent/Cutout/VertexLit"
}
