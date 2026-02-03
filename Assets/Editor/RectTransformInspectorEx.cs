using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RectTransform))]
[CanEditMultipleObjects]
public class RectTransformInspectorEx : Editor
{
  private Editor _defaultEditor;

  private void OnEnable()
  {
    // 拿到 Unity 原生 RectTransform Inspector
    var type = typeof(Editor).Assembly.GetType("UnityEditor.RectTransformEditor");
    _defaultEditor = CreateEditor(targets, type);
  }

  private void OnDisable()
  {
    if (_defaultEditor != null)
    {
      DestroyImmediate(_defaultEditor);
    }
  }

  public override void OnInspectorGUI()
  {
    // ① 先画 Unity 原生 RectTransform Inspector
    if (_defaultEditor != null)
    {
      _defaultEditor.OnInspectorGUI();
    }

    // ② 再画我们自己的 Debug 信息
    DrawDebugInfo();
  }

  private void DrawDebugInfo()
  {
    RectTransform rt = target as RectTransform;
    if (rt == null) return;

    EditorGUILayout.Space(8);
    EditorGUILayout.LabelField("RectTransform Debug Info", EditorStyles.boldLabel);
    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

    EditorGUI.BeginDisabledGroup(true); // 只读

    EditorGUILayout.Vector2Field("Size Delta", rt.sizeDelta);
    EditorGUILayout.Vector3Field("Local Position", rt.localPosition);
    EditorGUILayout.Vector3Field("World Position", rt.position);
    EditorGUILayout.Vector2Field("Anchored Position", rt.anchoredPosition);
    EditorGUILayout.Vector3Field("Anchored Position 3D", rt.anchoredPosition3D);

    EditorGUI.EndDisabledGroup();
  }
}
