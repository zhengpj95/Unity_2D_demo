using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTools
{
  /// <summary>
  /// 为 RectTransform 增加一组快速锚点预设按钮，方便在编辑器中快速调整 UI 布局。
  /// 这个脚本只在 Unity 编辑器中生效，作用于选中的 RectTransform 对象。
  /// </summary>
  // [CustomEditor(typeof(RectTransform))]
  [CanEditMultipleObjects]
  public class RectTransformPresetEditor : Editor
  {
    // 当前选中的 RectTransform 数组
    private RectTransform[] rects;
    private SerializedProperty anchorMinProp;
    private SerializedProperty anchorMaxProp;
    private SerializedProperty pivotProp;
    private SerializedProperty anchoredPositionProp;
    private SerializedProperty sizeDeltaProp;
    // 是否在应用预设时保持当前尺寸和位置不变
    private bool preserveCurrentSize;
    // 按钮样式
    private GUIStyle presetButtonStyle;
    // 分组标题样式
    private GUIStyle sectionHeaderStyle;

    private void OnEnable()
    {
      // 初始化当前选中的 RectTransform
      rects = System.Array.ConvertAll(targets, t => t as RectTransform);

      // 获取序列化属性，方便在 Inspector 中显示和编辑 RectTransform 的关键字段
      anchorMinProp = serializedObject.FindProperty("m_AnchorMin");
      anchorMaxProp = serializedObject.FindProperty("m_AnchorMax");
      pivotProp = serializedObject.FindProperty("m_Pivot");
      anchoredPositionProp = serializedObject.FindProperty("m_AnchoredPosition");
      sizeDeltaProp = serializedObject.FindProperty("m_SizeDelta");
    }

    public override void OnInspectorGUI()
    {
      // 每次绘制前刷新当前选中的对象，避免选中对象变化后显示不一致
      rects = System.Array.ConvertAll(targets, t => t as RectTransform);

      // 先绘制 Unity 默认的 RectTransform Inspector，保留原生显示
      DrawDefaultInspector();

      EditorGUILayout.Space(10);

      DrawAnchorPresets();

      EditorGUILayout.Space(10);

      DrawInfoPanel();
    }

    private void DrawAnchorPresets()
    {
      EnsureStyles();

      // 使用 helpBox 包起来，视觉上更像一个独立的编辑器分组
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Anchor Presets", sectionHeaderStyle);

      // 允许用户选择：应用预设时是否保持当前尺寸和位置
      EditorGUILayout.BeginHorizontal();
      preserveCurrentSize = GUILayout.Toggle(
          preserveCurrentSize,
          new GUIContent("Keep current size", "Apply presets while preserving the current rect size."),
          EditorStyles.miniButton);
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space(2);

      // 典型的 3x3 锚点布局预设
      DrawRow("↖", "↑", "↗", TopLeft, TopCenter, TopRight);

      DrawRow("←", "•", "→", MiddleLeft, MiddleCenter, MiddleRight);

      DrawRow("↙", "↓", "↘", BottomLeft, BottomCenter, BottomRight);

      EditorGUILayout.Space();

      // 拉伸类预设：水平拉伸、垂直拉伸、满屏拉伸
      DrawRow("H Stretch", "V Stretch", "Full", StretchHorizontal, StretchVertical, StretchFull);
      EditorGUILayout.EndVertical();
    }

    // 初始化自定义按钮和标题样式，尽量贴近 Unity 原生 Inspector 的视觉效果
    private void EnsureStyles()
    {
      if (presetButtonStyle == null)
      {
        presetButtonStyle = new GUIStyle(EditorStyles.miniButton);
        presetButtonStyle.fixedHeight = 24;
        presetButtonStyle.padding = new RectOffset(6, 6, 4, 4);
        presetButtonStyle.fontStyle = FontStyle.Bold;
      }

      if (sectionHeaderStyle == null)
      {
        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
        sectionHeaderStyle.fontSize = 12;
        sectionHeaderStyle.margin = new RectOffset(0, 0, 4, 4);
      }
    }

    // 绘制一行 3 个预设按钮
    private void DrawRow(
        string label1,
        string label2,
        string label3,
        System.Action<RectTransform> action1,
        System.Action<RectTransform> action2,
        System.Action<RectTransform> action3)
    {
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button(label1, presetButtonStyle))
        Apply(action1);

      if (GUILayout.Button(label2, presetButtonStyle))
        Apply(action2);

      if (GUILayout.Button(label3, presetButtonStyle))
        Apply(action3);

      EditorGUILayout.EndHorizontal();
    }

    // 执行预设逻辑，并根据设置决定是否保持当前尺寸与位置
    private void Apply(System.Action<RectTransform> action)
    {
      if (rects == null || rects.Length == 0 || action == null)
        return;

      Undo.RecordObjects(rects, "RectTransform Preset");
      serializedObject.Update();

      foreach (var r in rects)
      {
        if (r == null)
          continue;

        // 记录当前尺寸和位置，供“保持当前尺寸”模式使用
        var sizeDelta = r.sizeDelta;
        var anchoredPosition = r.anchoredPosition;

        action.Invoke(r);

        if (preserveCurrentSize)
        {
          r.sizeDelta = sizeDelta;
          r.anchoredPosition = anchoredPosition;
        }

        EditorUtility.SetDirty(r);

        if (PrefabUtility.IsPartOfPrefabInstance(r))
          PrefabUtility.RecordPrefabInstancePropertyModifications(r);
      }

      serializedObject.Update();
      RefreshLayout();
    }

    // 在应用预设后刷新布局和 SceneView，立即看到效果
    private void RefreshLayout()
    {
      if (rects == null)
        return;

      foreach (var r in rects)
      {
        if (r == null)
          continue;

        LayoutRebuilder.ForceRebuildLayoutImmediate(r);
      }

      Canvas.ForceUpdateCanvases();
      SceneView.RepaintAll();
      Repaint();
    }

    #region Presets

    // 以下方法分别对应不同的锚点/拉伸预设

    private void TopLeft(RectTransform rt)
    {
      SetAnchor(
          rt,
          new Vector2(0, 1),
          new Vector2(0, 1),
          new Vector2(0, 1));
    }

    private void TopCenter(RectTransform rt)
    {
      SetAnchor(
          rt,
          new Vector2(.5f, 1),
          new Vector2(.5f, 1),
          new Vector2(.5f, 1));
    }

    private void TopRight(RectTransform rt)
    {
      SetAnchor(
          rt,
          new Vector2(1, 1),
          new Vector2(1, 1),
          new Vector2(1, 1));
    }

    private void MiddleLeft(RectTransform rt)
    {
      SetAnchor(
          rt,
          new Vector2(0, .5f),
          new Vector2(0, .5f),
          new Vector2(0, .5f));
    }

    private void MiddleCenter(RectTransform rt)
    {
      SetAnchor(
          rt,
          new Vector2(.5f, .5f),
          new Vector2(.5f, .5f),
          new Vector2(.5f, .5f));
    }

    private void MiddleRight(RectTransform rt)
    {
      SetAnchor(
          rt,
          new Vector2(1, .5f),
          new Vector2(1, .5f),
          new Vector2(1, .5f));
    }

    private void BottomLeft(RectTransform rt)
    {
      SetAnchor(
          rt,
          new Vector2(0, 0),
          new Vector2(0, 0),
          new Vector2(0, 0));
    }

    private void BottomCenter(RectTransform rt)
    {
      SetAnchor(
          rt,
          new Vector2(.5f, 0),
          new Vector2(.5f, 0),
          new Vector2(.5f, 0));
    }

    private void BottomRight(RectTransform rt)
    {
      SetAnchor(
          rt,
          new Vector2(1, 0),
          new Vector2(1, 0),
          new Vector2(1, 0));
    }

    private void StretchHorizontal(RectTransform rt)
    {
      rt.anchorMin = new Vector2(0, .5f);
      rt.anchorMax = new Vector2(1, .5f);
      rt.pivot = new Vector2(.5f, .5f);
    }

    private void StretchVertical(RectTransform rt)
    {
      rt.anchorMin = new Vector2(.5f, 0);
      rt.anchorMax = new Vector2(.5f, 1);
      rt.pivot = new Vector2(.5f, .5f);
    }

    private void StretchFull(RectTransform rt)
    {
      rt.anchorMin = Vector2.zero;
      rt.anchorMax = Vector2.one;
      rt.pivot = new Vector2(.5f, .5f);
    }

    private void SetAnchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
      rt.anchorMin = min;
      rt.anchorMax = max;
      rt.pivot = pivot;
    }

    #endregion

    // 绘制 RectTransform 的信息面板，方便查看当前各项属性
    private void DrawInfoPanel()
    {
      EditorGUILayout.LabelField("Rect Info", EditorStyles.boldLabel);
      serializedObject.Update();

      EditorGUILayout.PropertyField(anchorMinProp, new GUIContent("Anchor Min"));
      EditorGUILayout.PropertyField(anchorMaxProp, new GUIContent("Anchor Max"));
      EditorGUILayout.PropertyField(pivotProp, new GUIContent("Pivot"));
      EditorGUILayout.PropertyField(anchoredPositionProp, new GUIContent("Anchored Pos"));
      EditorGUILayout.PropertyField(sizeDeltaProp, new GUIContent("Size Delta"));

      if (rects.Length == 1)
      {
        EditorGUILayout.Vector2Field("Size", rects[0].rect.size);
        EditorGUILayout.Vector3Field("Local Pos", rects[0].localPosition);
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}