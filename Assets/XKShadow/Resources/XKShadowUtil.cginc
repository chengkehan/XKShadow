#ifndef __XK_SHADOW_UTIL__
#define __XK_SHADOW_UTIL__

uniform float _XKShadowRTMargin;
uniform float4x4 _XKShadowVP;
uniform sampler2D _XKShadowRT;
uniform float _XKShadowFarClipPlane;
uniform float4x4 _XKShadowCamViewMat;
uniform fixed4 _XKShadowColor;
uniform float _XKShadowExpansion;
uniform float2 _XKShadowFadeWorldY;

#define XK_SHADOW_V2F(id) float3 xk_shadow : TEXCOORD##id

#define XK_SHADOW_DATA(input) input.xk_shadow

#define XK_SHADOW_TRANSFORM float4 xk_shadow_pos = mul(_XKShadowVP, mul(_Object2World, v.vertex)); \
							o.xk_shadow.xy = xk_shadow_pos.xy * 0.5 + 0.5; \
							o.xk_shadow.xy = o.xk_shadow.xy * (1 - _XKShadowRTMargin * 2) + _XKShadowRTMargin; \
							o.xk_shadow.z = -mul(_XKShadowCamViewMat, mul(_Object2World, v.vertex)).z;

inline fixed3 xk_shadow_atten(float3 xk_shadow)
{
	fixed4 rtC = tex2D(_XKShadowRT, xk_shadow.xy);
	float depth = DecodeFloatRG(rtC.rg);
	float thisDepth = clamp(xk_shadow.z / _XKShadowFarClipPlane, 0, 0.99f);
	if (thisDepth > depth)
	{
		return min(_XKShadowColor.rgb + rtC.b + rtC.a, 1);
	}
	else
	{
		return fixed3(1,1,1);
	}
}

inline fixed xk_shadow_fade(float worldY)
{
	return min(max(worldY - _XKShadowFadeWorldY.x, 0) / (_XKShadowFadeWorldY.y - _XKShadowFadeWorldY.x), 1);
}

#endif