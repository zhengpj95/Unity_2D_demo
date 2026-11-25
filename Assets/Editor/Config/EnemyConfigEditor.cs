using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VampireSurvivorsLike;

namespace EditorTools
{
  /// <summary>
  /// 敌人配置编辑窗口：新建并编辑 AnimFrameConfig（序列帧）与 EnemyTypeConfig（类型数值）。
  /// 两类资源共用同一入口；EnemyTypeConfig 提供单帧预览与碰撞体可视化拖拽，便于对齐美术资源调整数值。
  /// </summary>
  public sealed class EnemyConfigEditor : EditorWindow
  {
    private const string AnimConfigFolder = "Assets/Configs/Survivor/EnemyAnimConfig";
    private const string TypeConfigFolder = "Assets/Configs/Survivor/EnemyTypeConfig";
    private const float PreviewMaxHeight = 320f;

    private enum DragHandle
    {
      None,
      Center,
      TopLeft,
      TopRight,
      BottomLeft,
      BottomRight
    }

    private AnimFrameConfig _selectedAnimConfig;
    private EnemyTypeConfig _selectedTypeConfig;
    [SerializeField] private GameObject _enemyCommonPrefab;
    private SerializedObject _animSerialized;
    private SerializedObject _typeSerialized;
    private Vector2 _scrollPosition;
    private string _statusMessage = string.Empty;

    private Sprite _previewSprite;
    private string _previewSourceInfo = string.Empty;
    private DragHandle _activeHandle = DragHandle.None;
    private Vector2 _dragStartMouse;
    private Vector2 _dragStartOffset;
    private Vector2 _dragStartSize;

    /// <summary>从 Tools 菜单打开敌人配置编辑窗口。</summary>
    [MenuItem("Tools/05 Enemy Config Editor", false, 0)]
    public static void Open()
    {
      GetWindow<EnemyConfigEditor>("敌人配置编辑器").Show();
    }

    private void OnGUI()
    {
      EditorGUILayout.LabelField("敌人配置编辑器", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(
        "选择已有资源进行编辑，或新建资源。新建后会自动选中并保存到对应的 Configs 子目录。", MessageType.Info);

      _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
      DrawConfigSection<AnimFrameConfig>(
        "序列帧动画配置", AnimConfigFolder, "NewAnimFrameConfig",
        ref _selectedAnimConfig, ref _animSerialized, CreateAnimConfig);
      EditorGUILayout.Space(12f);
      DrawConfigSection<EnemyTypeConfig>(
        "敌人类型配置", TypeConfigFolder, "NewEnemyTypeConfig",
        ref _selectedTypeConfig, ref _typeSerialized, CreateTypeConfig);
      EditorGUILayout.Space(8f);
      DrawSceneSpawnSection();
      if (_selectedAnimConfig != null || _selectedTypeConfig != null)
      {
        EditorGUILayout.Space(8f);
        DrawColliderPreview();
      }
      EditorGUILayout.EndScrollView();

      if (!string.IsNullOrEmpty(_statusMessage))
      {
        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
      }
    }

    private void DrawConfigSection<T>(
      string title, string folder, string defaultFileName,
      ref T selected, ref SerializedObject serialized,
      System.Action onCreate) where T : ScriptableObject
    {
      EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
      using (new EditorGUILayout.HorizontalScope())
      {
        T next = (T)EditorGUILayout.ObjectField("当前资源", selected, typeof(T), false);
        if (next != selected)
        {
          // 切换资源时立即同步选中项与对应 SerializedObject，避免当前帧继续绘制旧对象。
          selected = next;
          _previewSprite = null;
          _activeHandle = DragHandle.None;
          serialized = next == null ? null : new SerializedObject(next);
        }

        if (GUILayout.Button("新建", GUILayout.Width(64f)))
        {
          onCreate();
        }
      }

      if (serialized == null)
      {
        EditorGUILayout.HelpBox("拖入一个已有资源，或点击“新建”创建配置。", MessageType.None);
        return;
      }

      serialized.Update();
      SerializedProperty iterator = serialized.GetIterator();
      if (iterator.NextVisible(true))
      {
        do
        {
          // m_Script 是 Unity 内部字段，不暴露给配置编辑。
          if (iterator.name == "m_Script") continue;
          EditorGUILayout.PropertyField(iterator, true);
        }
        while (iterator.NextVisible(false));
      }

      if (serialized.hasModifiedProperties)
      {
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(serialized.targetObject);
      }
    }

    private void CreateAnimConfig()
    {
      AnimFrameConfig config = CreateAsset<AnimFrameConfig>(AnimConfigFolder, "NewAnimFrameConfig", "新建序列帧动画配置");
      if (config == null)
      {
        return;
      }

      using (SerializedObject serialized = new SerializedObject(config))
      {
        serialized.FindProperty("configName").stringValue = config.name;
        serialized.FindProperty("defaultClipName").stringValue = "Move";
        serialized.ApplyModifiedPropertiesWithoutUndo();
      }

      _selectedAnimConfig = config;
      _animSerialized = new SerializedObject(config);
      _previewSprite = null;
      _statusMessage = $"已创建动画配置：{AssetDatabase.GetAssetPath(config)}";
    }

    private void CreateTypeConfig()
    {
      EnemyTypeConfig config = CreateAsset<EnemyTypeConfig>(TypeConfigFolder, "NewEnemyTypeConfig", "新建敌人类型配置");
      if (config == null)
      {
        return;
      }

      using (SerializedObject serialized = new SerializedObject(config))
      {
        serialized.FindProperty("typeId").stringValue = config.name;
        serialized.ApplyModifiedPropertiesWithoutUndo();
      }

      _selectedTypeConfig = config;
      _typeSerialized = new SerializedObject(config);
      _previewSprite = null;
      _activeHandle = DragHandle.None;
      _statusMessage = $"已创建类型配置：{AssetDatabase.GetAssetPath(config)}";
    }

    /// <summary>在目标目录创建 SO 资源；目录不存在时自动创建。</summary>
    private static T CreateAsset<T>(string folder, string defaultFileName, string dialogTitle) where T : ScriptableObject
    {
      EnsureFolder(folder);
      string path = EditorUtility.SaveFilePanelInProject(dialogTitle, defaultFileName, "asset", "选择配置保存位置", folder);
      if (string.IsNullOrEmpty(path))
      {
        return null;
      }

      T config = CreateInstance<T>();
      AssetDatabase.CreateAsset(config, path);
      AssetDatabase.SaveAssets();
      return config;
    }

    /// <summary>逐级创建 Assets 下的目录，已存在时直接返回。</summary>
    private static void EnsureFolder(string folder)
    {
      if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
      {
        return;
      }

      int lastSlash = folder.LastIndexOf('/');
      if (lastSlash <= 0)
      {
        Debug.LogError($"[EnemyConfigEditor] 无法创建目录：{folder}");
        return;
      }

      string parent = folder.Substring(0, lastSlash);
      string child = folder.Substring(lastSlash + 1);
      EnsureFolder(parent);
      if (!AssetDatabase.IsValidFolder(folder))
      {
        AssetDatabase.CreateFolder(parent, child);
      }
    }

    /// <summary>
    /// 取预览图。依次尝试手动拖入、上方选中的动画配置、类型配置引用的动画；
    /// 每个配置先找默认 Clip，再遍历所有 Clip 的每一帧，返回第一张非空 Sprite。
    /// 每次绘制都重新读取，因此切换配置、新增 Clip 或换帧都会即时反映到预览。
    /// </summary>
    private Sprite GetPreviewSprite()
    {
      _previewSourceInfo = string.Empty;

      if (_previewSprite != null)
      {
        _previewSourceInfo = "来源：手动拖入的预览图片。";
        return _previewSprite;
      }

      if (_selectedAnimConfig != null)
      {
        Sprite frame = FindPreviewFrame(_selectedAnimConfig, "上方选中的序列帧配置");
        if (frame != null)
        {
          return frame;
        }
      }

      AnimFrameConfig typeAnim = _selectedTypeConfig == null ? null : _selectedTypeConfig.AnimConfig;
      if (typeAnim != null && typeAnim != _selectedAnimConfig)
      {
        Sprite frame = FindPreviewFrame(typeAnim, "敌人类型配置的 Anim Config");
        if (frame != null)
        {
          return frame;
        }
      }

      if (_selectedAnimConfig == null && typeAnim == null)
      {
        _previewSourceInfo = "未选中序列帧配置，且敌人类型配置未指定 Anim Config。";
      }

      return null;
    }

    /// <summary>在单个动画配置里查找第一张有效帧，并把查找过程写入诊断信息。</summary>
    private Sprite FindPreviewFrame(AnimFrameConfig anim, string sourceLabel)
    {
      if (anim == null)
      {
        return null;
      }

      AnimFrameClip[] clips = anim.Clips;
      if (clips == null || clips.Length == 0)
      {
        _previewSourceInfo += $"{sourceLabel}「{anim.name}」没有配置任何 Clip。";
        return null;
      }

      if (!string.IsNullOrEmpty(anim.DefaultClipName))
      {
        foreach (AnimFrameClip clip in clips)
        {
          if (clip == null || clip.AnimName != anim.DefaultClipName)
          {
            continue;
          }

          Sprite frame = FindFirstValidFrame(clip);
          if (frame != null)
          {
            _previewSourceInfo = $"来源：{sourceLabel}「{anim.name}」的默认 Clip「{clip.AnimName}」。";
            return frame;
          }

          _previewSourceInfo += $"{sourceLabel}「{anim.name}」的默认 Clip「{clip.AnimName}」没有有效帧。";
        }
      }

      foreach (AnimFrameClip clip in clips)
      {
        if (clip == null)
        {
          continue;
        }

        Sprite frame = FindFirstValidFrame(clip);
        if (frame != null)
        {
          _previewSourceInfo = $"来源：{sourceLabel}「{anim.name}」的 Clip「{clip.AnimName}」。";
          return frame;
        }
      }

      _previewSourceInfo += $"{sourceLabel}「{anim.name}」的 {clips.Length} 个 Clip 都没有有效帧图片。";
      return null;
    }

    /// <summary>遍历 Clip 的每一帧，返回第一张非空 Sprite。</summary>
    private static Sprite FindFirstValidFrame(AnimFrameClip clip)
    {
      for (int i = 0; i < clip.FrameCount; i++)
      {
        Sprite frame = clip.GetFrame(i);
        if (frame != null)
        {
          return frame;
        }
      }

      return null;
    }

    /// <summary>把当前选中的两份配置及其 Clip/帧状态输出到 Console，供排查预览无图问题。</summary>
    private void LogPreviewDiagnostics()
    {
      System.Text.StringBuilder builder = new System.Text.StringBuilder();
      builder.AppendLine("[EnemyConfigEditor] 预览诊断");
      string selectedAnimName = (UnityEngine.Object)_selectedAnimConfig == null ? "null" : _selectedAnimConfig.name;
      string selectedTypeName = (UnityEngine.Object)_selectedTypeConfig == null ? "null" : _selectedTypeConfig.name;
      AnimFrameConfig typeAnim = (UnityEngine.Object)_selectedTypeConfig == null ? null : _selectedTypeConfig.AnimConfig;
      string typeAnimName = (UnityEngine.Object)typeAnim == null ? "null" : typeAnim.name;
      string manualSpriteName = (UnityEngine.Object)_previewSprite == null ? "null" : _previewSprite.name;
      builder.AppendLine($"  上方选中 AnimFrameConfig: {selectedAnimName}");
      builder.AppendLine($"  类型配置 EnemyTypeConfig: {selectedTypeName}");
      builder.AppendLine($"  类型配置.AnimConfig: {typeAnimName}");
      builder.AppendLine($"  手动预览 Sprite: {manualSpriteName}");

      AppendAnimDiagnostics(builder, "上方选中", _selectedAnimConfig);
      if (typeAnim != _selectedAnimConfig)
      {
        AppendAnimDiagnostics(builder, "类型配置引用", typeAnim);
      }

      Sprite resolved = GetPreviewSprite();
      string resolvedName = (UnityEngine.Object)resolved == null ? "null" : resolved.name;
      builder.AppendLine($"  最终预览 Sprite: {resolvedName}");
      builder.AppendLine($"  诊断摘要: {_previewSourceInfo}");
      Debug.Log(builder.ToString());
    }

    private static void AppendAnimDiagnostics(System.Text.StringBuilder builder, string label, AnimFrameConfig anim)
    {
      if ((UnityEngine.Object)anim == null)
      {
        builder.AppendLine($"  [{label}] 配置为 null");
        return;
      }

      builder.AppendLine($"  [{label}] {anim.name} defaultClipName={anim.DefaultClipName}");
      AnimFrameClip[] clips = anim.Clips;
      builder.AppendLine($"  [{label}] Clips 数量={(clips == null ? -1 : clips.Length)}");
      if (clips == null)
      {
        return;
      }

      for (int c = 0; c < clips.Length; c++)
      {
        AnimFrameClip clip = clips[c];
        if (clip == null)
        {
          builder.AppendLine($"    Clip[{c}] = null");
          continue;
        }

        builder.AppendLine($"    Clip[{c}] name={clip.AnimName} FrameCount={clip.FrameCount}");
        for (int f = 0; f < clip.FrameCount; f++)
        {
          Sprite frame = clip.GetFrame(f);
          string frameName = (UnityEngine.Object)frame == null ? "null" : frame.name;
          builder.AppendLine($"      Frame[{f}] = {frameName}");
        }
      }
    }

    /// <summary>绘制序列帧首帧预览，并叠加碰撞体矩形，支持拖拽编辑碰撞体偏移与尺寸。</summary>
    private void DrawColliderPreview()
    {
      EditorGUILayout.LabelField("碰撞体可视化预览", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(
        "拖动矩形中心调整碰撞体偏移，拖动四角调整碰撞体尺寸。绿色为 Vertical，橙色为 Horizontal。", MessageType.None);

      EditorGUI.BeginChangeCheck();
      _previewSprite = (Sprite)EditorGUILayout.ObjectField("预览图片（可选）", _previewSprite, typeof(Sprite), false);
      if (EditorGUI.EndChangeCheck())
      {
        _activeHandle = DragHandle.None;
      }

      // 先解析预览来源，再显示诊断，保证提示与本次绘制使用的资源一致。
      Sprite sprite = GetPreviewSprite();
      if (!string.IsNullOrEmpty(_previewSourceInfo))
      {
        EditorGUILayout.LabelField(_previewSourceInfo, EditorStyles.wordWrappedMiniLabel);
      }

      if (GUILayout.Button("输出预览诊断到 Console", GUILayout.Width(180f)))
      {
        LogPreviewDiagnostics();
      }

      if (sprite == null || sprite.texture == null)
      {
        EditorGUILayout.HelpBox(
          $"没有可用预览图。{_previewSourceInfo}请在动画配置的 Clip 里拖入 Sprite 帧，或在上方“预览图片”手动指定。",
          MessageType.Warning);
        return;
      }

      Rect area = GUILayoutUtility.GetRect(position.width - 40f, PreviewMaxHeight, GUILayout.ExpandWidth(true));
      EditorGUI.DrawRect(area, new Color(0.12f, 0.12f, 0.12f, 1f));

      float pixelsPerUnit = Mathf.Max(0.01f, sprite.pixelsPerUnit);
      Vector2 worldSize = new Vector2(sprite.rect.width, sprite.rect.height) / pixelsPerUnit;
      float scale = Mathf.Min(area.width / worldSize.x, area.height / worldSize.y) * 0.9f;
      float drawWidth = worldSize.x * scale;
      float drawHeight = worldSize.y * scale;
      Rect spriteRect = new Rect(area.center.x - drawWidth * 0.5f, area.center.y - drawHeight * 0.5f, drawWidth, drawHeight);

      Texture2D texture = sprite.texture;
      Rect uv = new Rect(
        sprite.rect.x / texture.width,
        1f - (sprite.rect.y + sprite.rect.height) / texture.height,
        sprite.rect.width / texture.width,
        sprite.rect.height / texture.height);
      GUI.DrawTextureWithTexCoords(spriteRect, texture, uv);

      if (_selectedTypeConfig == null)
      {
        EditorGUILayout.HelpBox("当前只选中了动画配置；选中或新建 EnemyTypeConfig 后可叠加编辑碰撞体矩形。", MessageType.None);
        return;
      }

      Vector2 offset = _selectedTypeConfig.ColliderOffset;
      Vector2 size = _selectedTypeConfig.ColliderSize;
      Rect colliderRect = GetColliderScreenRect(spriteRect, scale, offset, size);
      Color colliderColor = _selectedTypeConfig.ColliderDirection == CapsuleDirection2D.Horizontal
        ? new Color(1f, 0.75f, 0.1f, 0.95f)
        : new Color(0.1f, 1f, 0.4f, 0.95f);
      DrawColliderGizmo(colliderRect, colliderColor);
      EditorGUILayout.LabelField(
        $"碰撞体偏移: ({offset.x:F3}, {offset.y:F3})  尺寸: ({size.x:F3}, {size.y:F3})  {_selectedTypeConfig.ColliderDirection}",
        EditorStyles.miniLabel);
      HandleColliderInteraction(colliderRect, scale, offset, size);
    }

    /// <summary>把世界坐标下的碰撞体 offset/size 换算成预览窗口像素矩形。</summary>
    private static Rect GetColliderScreenRect(Rect spriteRect, float scale, Vector2 offset, Vector2 size)
    {
      Vector2 centerPx = new Vector2(
        spriteRect.center.x + offset.x * scale,
        spriteRect.center.y - offset.y * scale);
      Vector2 sizePx = new Vector2(size.x * scale, size.y * scale);
      return new Rect(centerPx.x - sizePx.x * 0.5f, centerPx.y - sizePx.y * 0.5f, sizePx.x, sizePx.y);
    }

    /// <summary>绘制半透明矩形、边框和四角控制点。</summary>
    private static void DrawColliderGizmo(Rect rect, Color border)
    {
      EditorGUI.DrawRect(rect, new Color(border.r, border.g, border.b, 0.18f));

      const float thickness = 2f;
      EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), border);
      EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), border);
      EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), border);
      EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), border);

      Vector2[] handles = GetHandlePositions(rect);
      foreach (Vector2 handle in handles)
      {
        EditorGUI.DrawRect(new Rect(handle.x - 5f, handle.y - 5f, 10f, 10f), Color.white);
        EditorGUI.DrawRect(new Rect(handle.x - 3f, handle.y - 3f, 6f, 6f), border);
      }
    }

    private static Vector2[] GetHandlePositions(Rect rect)
    {
      return new[]
      {
        new Vector2(rect.x, rect.y),
        new Vector2(rect.xMax, rect.y),
        new Vector2(rect.x, rect.yMax),
        new Vector2(rect.xMax, rect.yMax)
      };
    }

    /// <summary>处理碰撞体矩形的拖动：中心平移、四角以对角为锚点缩放。</summary>
    private void HandleColliderInteraction(Rect rect, float scale, Vector2 offset, Vector2 size)
    {
      Event evt = Event.current;
      Vector2 mouse = evt.mousePosition;
      if (!rect.Contains(mouse) && _activeHandle == DragHandle.None)
      {
        return;
      }

      if (evt.type == EventType.MouseDown && evt.button == 0)
      {
        DragHandle hit = HitTestHandle(rect, mouse);
        if (hit != DragHandle.None)
        {
          _activeHandle = hit;
          _dragStartMouse = mouse;
          _dragStartOffset = offset;
          _dragStartSize = size;
          evt.Use();
          Repaint();
        }
      }
      else if (evt.type == EventType.MouseDrag && _activeHandle != DragHandle.None)
      {
        Vector2 deltaWorld = new Vector2((mouse.x - _dragStartMouse.x) / scale, -(mouse.y - _dragStartMouse.y) / scale);
        Vector2 newOffset;
        Vector2 newSize;

        if (_activeHandle == DragHandle.Center)
        {
          newOffset = _dragStartOffset + deltaWorld;
          newSize = _dragStartSize;
        }
        else
        {
          Vector2 topLeft = new Vector2(_dragStartOffset.x - _dragStartSize.x * 0.5f, _dragStartOffset.y + _dragStartSize.y * 0.5f);
          Vector2 topRight = new Vector2(_dragStartOffset.x + _dragStartSize.x * 0.5f, _dragStartOffset.y + _dragStartSize.y * 0.5f);
          Vector2 bottomLeft = new Vector2(_dragStartOffset.x - _dragStartSize.x * 0.5f, _dragStartOffset.y - _dragStartSize.y * 0.5f);
          Vector2 bottomRight = new Vector2(_dragStartOffset.x + _dragStartSize.x * 0.5f, _dragStartOffset.y - _dragStartSize.y * 0.5f);

          Vector2 anchor;
          Vector2 corner;
          switch (_activeHandle)
          {
            case DragHandle.TopLeft:
              anchor = bottomRight;
              corner = topLeft + deltaWorld;
              break;
            case DragHandle.TopRight:
              anchor = bottomLeft;
              corner = topRight + deltaWorld;
              break;
            case DragHandle.BottomLeft:
              anchor = topRight;
              corner = bottomLeft + deltaWorld;
              break;
            default:
              anchor = topLeft;
              corner = bottomRight + deltaWorld;
              break;
          }

          newOffset = (anchor + corner) * 0.5f;
          newSize = new Vector2(
            Mathf.Max(0.02f, Mathf.Abs(corner.x - anchor.x)),
            Mathf.Max(0.02f, Mathf.Abs(corner.y - anchor.y)));
        }

        ApplyColliderValues(newOffset, newSize);
        evt.Use();
        Repaint();
      }
      else if (evt.type == EventType.MouseUp && _activeHandle != DragHandle.None)
      {
        _activeHandle = DragHandle.None;
        evt.Use();
        Repaint();
      }
    }

    private static DragHandle HitTestHandle(Rect colliderRect, Vector2 mouse)
    {
      Vector2[] handles = GetHandlePositions(colliderRect);
      const float radius = 12f;
      if (Vector2.Distance(mouse, handles[0]) < radius) return DragHandle.TopLeft;
      if (Vector2.Distance(mouse, handles[1]) < radius) return DragHandle.TopRight;
      if (Vector2.Distance(mouse, handles[2]) < radius) return DragHandle.BottomLeft;
      if (Vector2.Distance(mouse, handles[3]) < radius) return DragHandle.BottomRight;
      return colliderRect.Contains(mouse) ? DragHandle.Center : DragHandle.None;
    }

    /// <summary>把拖拽结果写回碰撞体字段，保持与字段面板同步。</summary>
    private void ApplyColliderValues(Vector2 offset, Vector2 size)
    {
      if (_typeSerialized == null)
      {
        return;
      }

      SetVector2("colliderOffset", offset);
      SetVector2("colliderSize", size);
      _typeSerialized.ApplyModifiedProperties();
      EditorUtility.SetDirty(_selectedTypeConfig);
      Repaint();
    }

    /// <summary>把当前选中的敌人类型配置实例化到编辑场景，便于直接检查不同 SO 的效果。</summary>
    private void DrawSceneSpawnSection()
    {
      EditorGUILayout.LabelField("场景实例化", EditorStyles.boldLabel);
      _enemyCommonPrefab = (GameObject)EditorGUILayout.ObjectField(
        "通用敌人 Prefab", _enemyCommonPrefab, typeof(GameObject), false);
      EditorGUILayout.HelpBox(
        "选中 EnemyTypeConfig 并指定 EnemyCommon Prefab 后点击按钮，实例会放到 Scene 视图中心并自动聚焦。", MessageType.None);
      if (_selectedTypeConfig == null || _enemyCommonPrefab == null)
        EditorGUILayout.HelpBox("请先在上方选中 EnemyTypeConfig，并拖入通用敌人 Prefab。", MessageType.Warning);

      using (new EditorGUI.DisabledScope(_selectedTypeConfig == null || _enemyCommonPrefab == null))
      {
        if (GUILayout.Button("实例化选中配置到场景"))
          SpawnSelectedTypeToScene();
      }
    }

    /// <summary>用通用 Prefab 创建场景实例，并注入当前选中的敌人类型配置。</summary>
    private void SpawnSelectedTypeToScene()
    {
      GameObject instance = PrefabUtility.InstantiatePrefab(_enemyCommonPrefab) as GameObject;
      if (instance == null)
      {
        Debug.LogError("[EnemyConfigEditor] 无法实例化通用敌人 Prefab。");
        return;
      }

      // 放在 Scene 视图中心并压到 z=0 平面，避免落在相机视线外或地表背面。
      SceneView sceneView = SceneView.lastActiveSceneView;
      if (sceneView != null)
      {
        Vector3 pivot = sceneView.pivot;
        pivot.z = 0f;
        instance.transform.position = pivot;
      }
      else
      {
        instance.transform.position = Vector3.zero;
        Debug.LogWarning("[EnemyConfigEditor] 未找到 Scene 视图，实例放在世界原点。");
      }

      if (!EnemyConfigurator.Apply(instance, _selectedTypeConfig))
      {
        UnityEngine.Object.DestroyImmediate(instance);
        return;
      }

      instance.name = string.IsNullOrEmpty(_selectedTypeConfig.TypeId) ? _selectedTypeConfig.name : _selectedTypeConfig.TypeId;
      EditorSceneManager.MarkSceneDirty(instance.scene);
      Selection.activeGameObject = instance;
      EditorGUIUtility.PingObject(instance);
      if (sceneView != null)
      {
        sceneView.FrameSelected();
        sceneView.Repaint();
      }

      Debug.Log($"[EnemyConfigEditor] 已在场景中实例化 '{instance.name}'，位置 {instance.transform.position}");
      _statusMessage = $"已在场景中实例化敌人配置：{_selectedTypeConfig.name}（Scene 视图已聚焦）";
    }

    private void SetVector2(string propertyName, Vector2 value)
    {
      SerializedProperty property = _typeSerialized.FindProperty(propertyName);
      if (property != null)
      {
        property.vector2Value = value;
      }
    }
  }
}
