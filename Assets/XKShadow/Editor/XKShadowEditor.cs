using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(XKShadow), true)]
public class XKShadowEditor : Editor
{
    public override void OnInspectorGUI()
    {
		XKShadow xkShadow = target as XKShadow;

		BeginBox();
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowColor"), new GUIContent("阴影颜色"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowLayers"), new GUIContent("阴影层"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowArea"), new GUIContent("阴影覆盖范围", "黄色线框"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowResolution"), new GUIContent("阴影清晰度", "RenderTexture的尺寸\n1:256x256\n2:512x512\n3:1024x1024\n4:2048x2048"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowExpansion"), new GUIContent("阴影扩大"));
		}
		EndBox();

		BeginBox();
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("lightSourceNear"), new GUIContent("近平面位置"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("lightSourceFar"), new GUIContent("远平面位置"));
		}
		EndBox();

		BeginBox();
		{
			EditorGUI.BeginChangeCheck();
			var prop = serializedObject.FindProperty("followingTargetTag");
			prop.stringValue = EditorGUILayout.TagField("跟随目标的Tag", prop.stringValue);
			if(EditorGUI.EndChangeCheck())
			{
				xkShadow.ResetFollowingTarget(prop.stringValue);
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("centerOffset"), new GUIContent("中心偏移"));
		}
		EndBox();

		BeginBox();
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("blurTimes"), new GUIContent("软阴影等级"));
			if(serializedObject.FindProperty("blurTimes").intValue > 0)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("blurDownSampleLevel"), new GUIContent("软阴影向下采样等级"));
			}
		}
		EndBox();

		BeginBox();
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("isFadeOn"), new GUIContent("开启渐隐"));
			if(serializedObject.FindProperty("isFadeOn").boolValue)
			{
				var prop = serializedObject.FindProperty("fadeWorldY");
				Vector2 fade = prop.vector2Value;
				fade.x = EditorGUILayout.FloatField("渐隐起始Y", fade.x);
				fade.y = EditorGUILayout.FloatField("渐隐终止Y", fade.y);
				prop.vector2Value = fade;
			}
		}
		EndBox();

		BeginBox();
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("debugRT"), new GUIContent("调试RT"));
		}
		EndBox();

		serializedObject.ApplyModifiedProperties();
    }

    public void OnSceneGUI()
    {
        XKShadow xkShadow = target as XKShadow;
        xkShadow.RotateLightDir();
    }

	private void BeginBox()
	{
		EditorGUILayout.BeginVertical(GUI.skin.GetStyle("Box"));
	}

	private void EndBox()
	{
		EditorGUILayout.EndVertical();
	}
}
