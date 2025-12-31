using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public class SpriteBatchSlicerEditor : EditorWindow
{
  private Texture2D selectedTexture;
  private Vector2Int gridSize = new Vector2Int(4, 1);
  private int padding = 0;
  private int spriteBorder = 0;
  private float pixelsPerUnit = 100f;
  private Vector2 pivot = new Vector2(0.5f, 0.5f);

  private string inputFolderPath = "Assets";
  private string[] texturePaths = new string[0];
  private bool includeSubfolders = true;

  private Vector2 scrollPos;
  private List<Sprite> generatedSprites = new List<Sprite>();
  private Vector2 previewScroll;

  private bool isProcessing = false;
  private float progress = 0f;
  private string status = "就绪";
  private int processedCount = 0;
  private int totalCount = 0;

  private int selectedTab = 0;  // 记录当前选中的标签页索引
  private Vector2 batchFileScrollPos = Vector2.zero;  // 批量处理文件列表的滚动位置

  [MenuItem("Tools/精灵批量切割器")]
  public static void ShowWindow()
  {
    GetWindow<SpriteBatchSlicerEditor>("精灵切割器");
  }

  private void OnGUI()
  {
    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

    EditorGUILayout.Space(10);
    EditorGUILayout.LabelField("Unity精灵批量切割工具", EditorStyles.largeLabel);
    EditorGUILayout.Space(5);
    EditorGUILayout.LabelField("批量切割图片为精灵，在原纹理下展开显示", EditorStyles.centeredGreyMiniLabel);
    EditorGUILayout.Space(10);

    int newTab = GUILayout.Toolbar(selectedTab, new[] { "单图切割", "批量处理", "查看精灵" });
    // 如果标签页发生变化
    if (newTab != selectedTab)
    {
      selectedTab = newTab;

      // 切换到批量处理标签页时自动扫描纹理
      if (selectedTab == 1 && Directory.Exists(inputFolderPath))
      {
        ScanTextures();
      }

      // 切换到查看精灵标签页时自动刷新精灵列表
      if (selectedTab == 2)
      {
        RefreshSpriteList();
      }
    }

    EditorGUILayout.Space(10);

    switch (selectedTab)  // 使用 selectedTab 而不是 tab
    {
      case 0: DrawSingleTab(); break;
      case 1: DrawBatchTab(); break;
      case 2: DrawViewTab(); break;
    }

    EditorGUILayout.EndScrollView();

    DrawStatusBar();
  }

  private void RefreshSpriteList()
  {
    generatedSprites.Clear();

    // 扫描项目中所有精灵
    string[] allTexturePaths = AssetDatabase.FindAssets("t:Texture2D")
        .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
        .ToArray();

    foreach (string path in allTexturePaths)
    {
      Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path)
          .Where(obj => obj is Sprite)
          .Cast<Sprite>()
          .ToArray();

      generatedSprites.AddRange(sprites);
    }
  }

  #region 单图切割标签页
  private void DrawSingleTab()
  {
    EditorGUILayout.BeginVertical("box");

    EditorGUILayout.LabelField("选择纹理", EditorStyles.boldLabel);
    EditorGUILayout.BeginHorizontal();
    selectedTexture = (Texture2D)EditorGUILayout.ObjectField(selectedTexture, typeof(Texture2D), false);
    if (GUILayout.Button("从选择加载", GUILayout.Width(80)))
    {
      if (Selection.activeObject is Texture2D texture)
      {
        selectedTexture = texture;
        generatedSprites.Clear();
      }
    }
    EditorGUILayout.EndHorizontal();

    if (selectedTexture != null)
    {
      EditorGUILayout.Space(5);
      EditorGUILayout.LabelField($"尺寸: {selectedTexture.width} × {selectedTexture.height}", EditorStyles.miniLabel);

      Rect previewRect = GUILayoutUtility.GetRect(200, 150, GUILayout.ExpandWidth(true));
      GUI.DrawTexture(previewRect, selectedTexture, ScaleMode.ScaleToFit);

      EditorGUILayout.Space(10);

      EditorGUILayout.LabelField("切割设置", EditorStyles.boldLabel);
      gridSize = EditorGUILayout.Vector2IntField("网格 (列×行)", gridSize);
      gridSize.x = Mathf.Max(1, gridSize.x);
      gridSize.y = Mathf.Max(1, gridSize.y);

      padding = EditorGUILayout.IntField("内边距", padding);
      padding = Mathf.Max(0, padding);

      spriteBorder = EditorGUILayout.IntField("精灵边框", spriteBorder);
      spriteBorder = Mathf.Max(0, spriteBorder);

      pixelsPerUnit = EditorGUILayout.FloatField("像素每单位", pixelsPerUnit);

      EditorGUILayout.Space(10);

      int sliceWidth = Mathf.FloorToInt((selectedTexture.width - padding * (gridSize.x - 1)) / (float)gridSize.x);
      int sliceHeight = Mathf.FloorToInt((selectedTexture.height - padding * (gridSize.y - 1)) / (float)gridSize.y);
      int totalSlices = gridSize.x * gridSize.y;

      EditorGUILayout.HelpBox(
          $"将生成 {totalSlices} 个精灵\n每个 {sliceWidth} × {sliceHeight} 像素",
          MessageType.Info
      );

      EditorGUILayout.Space(10);

      EditorGUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();

      if (GUILayout.Button("预览网格", GUILayout.Width(100)))
      {
        PreviewGrid();
      }

      GUI.enabled = !isProcessing;
      if (GUILayout.Button("切割纹理", GUILayout.Width(100)))
      {
        SliceTexture();
      }
      GUI.enabled = true;

      GUILayout.FlexibleSpace();
      EditorGUILayout.EndHorizontal();
    }
    else
    {
      EditorGUILayout.HelpBox("请选择一张纹理", MessageType.Info);
    }

    EditorGUILayout.EndVertical();
  }

  private void PreviewGrid()
  {
    if (selectedTexture == null) return;

    Handles.BeginGUI();

    Rect previewRect = GUILayoutUtility.GetLastRect();
    float scale = Mathf.Min(previewRect.width / selectedTexture.width, 150 / selectedTexture.height);

    float drawWidth = selectedTexture.width * scale;
    float drawHeight = selectedTexture.height * scale;
    float offsetX = previewRect.x + (previewRect.width - drawWidth) / 2;
    float offsetY = previewRect.y + previewRect.height + 10;

    int sliceWidth = Mathf.FloorToInt((selectedTexture.width - padding * (gridSize.x - 1)) / (float)gridSize.x);
    int sliceHeight = Mathf.FloorToInt((selectedTexture.height - padding * (gridSize.y - 1)) / (float)gridSize.y);

    Handles.color = Color.green;

    for (int i = 1; i < gridSize.x; i++)
    {
      float x = offsetX + (sliceWidth + padding) * i * scale;
      Handles.DrawLine(new Vector3(x, offsetY), new Vector3(x, offsetY + drawHeight));
    }

    for (int i = 1; i < gridSize.y; i++)
    {
      float y = offsetY + (sliceHeight + padding) * i * scale;
      Handles.DrawLine(new Vector3(offsetX, y), new Vector3(offsetX + drawWidth, y));
    }

    Handles.EndGUI();
  }
  #endregion

  #region 批量处理标签页
  private void DrawBatchTab()
  {
    Debug.Log("打开批量处理标签页");
    EditorGUILayout.BeginVertical("box");

    EditorGUILayout.LabelField("批量处理", EditorStyles.boldLabel);

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("输入文件夹:", GUILayout.Width(80));
    inputFolderPath = EditorGUILayout.TextField(inputFolderPath);
    if (GUILayout.Button("浏览", GUILayout.Width(60)))
    {
      string path = EditorUtility.OpenFolderPanel("选择文件夹", Application.dataPath, "");
      if (!string.IsNullOrEmpty(path))
      {
        inputFolderPath = "Assets" + path.Replace(Application.dataPath, "");
      }
    }
    EditorGUILayout.EndHorizontal();

    includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", includeSubfolders);

    EditorGUILayout.Space(10);

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("扫描图片"))
    {
      ScanTextures();
    }

    if (GUILayout.Button("清空列表"))
    {
      texturePaths = new string[0];
    }
    EditorGUILayout.EndHorizontal();

    if (texturePaths.Length > 0)
    {
      EditorGUILayout.Space(5);
      EditorGUILayout.LabelField($"找到 {texturePaths.Length} 个纹理文件:", EditorStyles.miniBoldLabel);

      // 计算显示区域高度
      float lineHeight = 20f;
      float maxHeight = 250f;
      float contentHeight = texturePaths.Length * lineHeight;
      float scrollViewHeight = Mathf.Min(maxHeight, contentHeight);

      // 开始滚动视图
      batchFileScrollPos = GUILayout.BeginScrollView(
          batchFileScrollPos,
          false,
          true,
          GUILayout.Height(scrollViewHeight)
      );

      // 使用 GUILayout 控制内容高度
      GUILayout.BeginVertical(GUILayout.Height(contentHeight));

      for (int i = 0; i < texturePaths.Length; i++)
      {
        string path = texturePaths[i];

        // 每行使用 BeginHorizontal/EndHorizontal
        EditorGUILayout.BeginHorizontal();

        // 序号
        EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(30));

        // 纹理信息
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null)
        {
          // 显示纹理图标和尺寸
          GUIContent content = new GUIContent(
              texture.name,
              AssetDatabase.GetCachedIcon(path),
              $"{texture.width}×{texture.height}\n{path}"
          );

          // 可点击的按钮，点击后在Project窗口定位
          if (GUILayout.Button(content, EditorStyles.label, GUILayout.Height(20)))
          {
            // 在Project窗口定位并选中纹理
            Selection.activeObject = texture;
            EditorGUIUtility.PingObject(texture);
          }

          // 尺寸标签
          EditorGUILayout.LabelField($"{texture.width}×{texture.height}",
              GUILayout.Width(80));
        }
        else
        {
          // 如果纹理加载失败，显示路径
          if (GUILayout.Button(path, EditorStyles.label, GUILayout.Height(20)))
          {
            // 尝试定位文件
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj != null)
            {
              Selection.activeObject = obj;
              EditorGUIUtility.PingObject(obj);
            }
          }
        }

        EditorGUILayout.EndHorizontal();
      }

      GUILayout.EndVertical();
      GUILayout.EndScrollView();

      // 添加操作按钮
      EditorGUILayout.Space(5);
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button("在Project窗口显示所有", GUILayout.Width(150)))
      {
        SelectAllTexturesInProject();
      }

      if (GUILayout.Button("复制文件列表", GUILayout.Width(100)))
      {
        CopyFileListToClipboard();
      }

      EditorGUILayout.EndHorizontal();
    }
    else
    {
      EditorGUILayout.HelpBox("没有找到纹理文件，请扫描文件夹", MessageType.Info);
    }

    EditorGUILayout.Space(10);

    EditorGUILayout.LabelField("批量设置", EditorStyles.boldLabel);
    gridSize = EditorGUILayout.Vector2IntField("网格大小", gridSize);
    pixelsPerUnit = EditorGUILayout.FloatField("像素每单位", pixelsPerUnit);

    EditorGUILayout.Space(10);

    EditorGUILayout.BeginHorizontal();
    GUILayout.FlexibleSpace();

    GUI.enabled = texturePaths.Length > 0 && !isProcessing;
    if (GUILayout.Button("批量切割", GUILayout.Width(120), GUILayout.Height(30)))
    {
      BatchSlice();
    }
    GUI.enabled = true;

    if (isProcessing && GUILayout.Button("取消", GUILayout.Width(80), GUILayout.Height(30)))
    {
      isProcessing = false;
      status = "已取消";
    }

    GUILayout.FlexibleSpace();
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.EndVertical();
  }

  // 在Project窗口选中所有文件
  private void SelectAllTexturesInProject()
  {
    List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

    foreach (string path in texturePaths)
    {
      Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
      if (tex != null)
      {
        objects.Add(tex);
      }
    }

    if (objects.Count > 0)
    {
      Selection.objects = objects.ToArray();
      EditorGUIUtility.PingObject(objects[0]);

      // 展开Project窗口
      EditorApplication.ExecuteMenuItem("Window/General/Project");
    }
  }

  // 复制文件列表到剪贴板
  private void CopyFileListToClipboard()
  {
    System.Text.StringBuilder sb = new System.Text.StringBuilder();

    sb.AppendLine($"找到 {texturePaths.Length} 个纹理文件：");
    sb.AppendLine();

    for (int i = 0; i < texturePaths.Length; i++)
    {
      string path = texturePaths[i];
      Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

      sb.Append($"{i + 1}. {path}");

      if (tex != null)
      {
        sb.Append($"  [{tex.width}×{tex.height}]");
      }

      sb.AppendLine();
    }

    GUIUtility.systemCopyBuffer = sb.ToString();
    Debug.Log($"已复制 {texturePaths.Length} 个文件路径到剪贴板");
  }

  private void ScanTextures()
  {
    if (!Directory.Exists(inputFolderPath))
    {
      EditorUtility.DisplayDialog("错误", "文件夹不存在", "确定");
      return;
    }

    string[] extensions = { "*.png", "*.jpg", "*.jpeg", "*.tga", "*.bmp" };
    List<string> files = new List<string>();

    foreach (string ext in extensions)
    {
      files.AddRange(Directory.GetFiles(inputFolderPath, ext,
          includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
    }

    texturePaths = files.Select(f => f.Replace(Application.dataPath, "Assets")).ToArray();
  }
  #endregion

  #region 查看精灵标签页
  private void DrawViewTab()
  {
    EditorGUILayout.BeginVertical("box");

    EditorGUILayout.LabelField("生成的精灵", EditorStyles.boldLabel);

    if (generatedSprites.Count == 0)
    {
      EditorGUILayout.HelpBox("还没有生成的精灵\n请先切割纹理", MessageType.Info);
    }
    else
    {
      EditorGUILayout.LabelField($"共 {generatedSprites.Count} 个精灵", EditorStyles.miniBoldLabel);

      previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.Height(300));

      float viewWidth = EditorGUIUtility.currentViewWidth - 40;
      int columns = Mathf.Max(1, Mathf.FloorToInt(viewWidth / 120f));
      float cellSize = 110f;

      int rows = Mathf.CeilToInt(generatedSprites.Count / (float)columns);
      Rect gridRect = GUILayoutUtility.GetRect(columns * cellSize, rows * cellSize);

      for (int i = 0; i < generatedSprites.Count; i++)
      {
        int row = i / columns;
        int col = i % columns;

        Sprite sprite = generatedSprites[i];
        if (sprite == null) continue;

        float x = gridRect.x + col * cellSize;
        float y = gridRect.y + row * cellSize;

        EditorGUI.DrawRect(new Rect(x, y, 110, 110), new Color(0.1f, 0.1f, 0.1f, 0.3f));

        if (sprite.texture != null)
        {
          Rect texCoords = sprite.textureRect;
          texCoords.x /= sprite.texture.width;
          texCoords.width /= sprite.texture.width;
          texCoords.y /= sprite.texture.height;
          texCoords.height /= sprite.texture.height;

          GUI.DrawTextureWithTexCoords(new Rect(x + 10, y + 25, 90, 90), sprite.texture, texCoords);
        }

        GUI.Label(new Rect(x + 5, y + 5, 100, 20), sprite.name, EditorStyles.miniLabel);
        GUI.Label(new Rect(x + 5, y + 120, 100, 20),
            $"{(int)sprite.rect.width}×{(int)sprite.rect.height}",
            EditorStyles.centeredGreyMiniLabel);

        if (GUI.Button(new Rect(x + 80, y + 5, 25, 18), "▶"))
        {
          Selection.activeObject = sprite;
          EditorGUIUtility.PingObject(sprite);
        }
      }

      EditorGUILayout.EndScrollView();

      EditorGUILayout.Space(10);

      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("在Project窗口选中"))
      {
        SelectInProject();
      }

      if (GUILayout.Button("清除列表"))
      {
        generatedSprites.Clear();
      }

      if (GUILayout.Button("展开所有纹理"))
      {
        ExpandAllTextures();
      }
      EditorGUILayout.EndHorizontal();
    }

    EditorGUILayout.EndVertical();
  }

  private void SelectInProject()
  {
    if (generatedSprites.Count > 0)
    {
      Selection.objects = generatedSprites.ToArray();
      EditorGUIUtility.PingObject(generatedSprites[0]);
    }
  }

  private void ExpandAllTextures()
  {
    var textures = generatedSprites
        .Select(s => s.texture)
        .Distinct()
        .Where(t => t != null)
        .ToArray();

    Selection.objects = textures;
    EditorApplication.ExecuteMenuItem("Window/General/Project");
  }
  #endregion

  #region 核心功能 - 使用新的Sprite Data Provider API
  private void SliceTexture()
  {
    if (selectedTexture == null) return;

    string path = AssetDatabase.GetAssetPath(selectedTexture);
    if (SliceTextureWithNewAPI(path))
    {
      GetSpritesFromTexture(path);
      ExpandTexture(path);
    }
  }

  private void BatchSlice()
  {
    if (texturePaths.Length == 0) return;

    isProcessing = true;
    totalCount = texturePaths.Length;
    processedCount = 0;

    EditorApplication.update += ProcessNextTexture;
  }

  private void ProcessNextTexture()
  {
    if (processedCount >= totalCount)
    {
      isProcessing = false;
      EditorApplication.update -= ProcessNextTexture;

      AssetDatabase.Refresh();

      generatedSprites.Clear();
      foreach (string path1 in texturePaths)
      {
        GetSpritesFromTexture(path1);
      }

      EditorUtility.DisplayDialog("完成",
          $"已处理 {totalCount} 个纹理文件\n生成 {generatedSprites.Count} 个精灵",
          "确定");

      status = "批量处理完成";
      return;
    }

    string path = texturePaths[processedCount];
    status = $"处理中 ({processedCount + 1}/{totalCount}): {Path.GetFileName(path)}";
    progress = (float)processedCount / totalCount;

    SliceTextureWithNewAPI(path);

    processedCount++;
    progress = (float)processedCount / totalCount;

    if (processedCount % 5 == 0)
    {
      AssetDatabase.SaveAssets();
      Repaint();
    }
  }

  private bool SliceTextureWithNewAPI(string texturePath)
  {
    TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
    if (importer == null) return false;

    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
    if (texture == null) return false;

    // 计算切片
    int sliceWidth = Mathf.FloorToInt((texture.width - padding * (gridSize.x - 1)) / (float)gridSize.x);
    int sliceHeight = Mathf.FloorToInt((texture.height - padding * (gridSize.y - 1)) / (float)gridSize.y);

    // 使用新的Sprite Editor Data Provider API
    var spriteDataProviderFactories = new SpriteDataProviderFactories();
    spriteDataProviderFactories.Init();

    var dataProvider = spriteDataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
    if (dataProvider == null)
    {
      Debug.LogError($"无法获取Sprite Data Provider: {texturePath}");
      return false;
    }

    dataProvider.InitSpriteEditorDataProvider();

    // 获取现有的Sprite Rect列表
    var spriteRects = dataProvider.GetSpriteRects()?.ToList() ?? new List<SpriteRect>();
    spriteRects.Clear();

    // 创建新的Sprite Rects
    for (int y = gridSize.y - 1; y >= 0; y--)
    {
      for (int x = 0; x < gridSize.x; x++)
      {
        int posX = x * (sliceWidth + padding);
        int posY = y * (sliceHeight + padding);

        int currentWidth = Mathf.Min(sliceWidth, texture.width - posX);
        int currentHeight = Mathf.Min(sliceHeight, texture.height - posY);

        if (currentWidth <= 0 || currentHeight <= 0) continue;

        string spriteName = $"{Path.GetFileNameWithoutExtension(texturePath)}_{y * gridSize.x + x}";

        SpriteRect spriteRect = new SpriteRect
        {
          name = spriteName,
          rect = new Rect(posX, posY, currentWidth, currentHeight),
          pivot = pivot,
          alignment = SpriteAlignment.Custom,
          border = new Vector4(spriteBorder, spriteBorder, spriteBorder, spriteBorder)
        };

        // 设置唯一ID
        spriteRect.spriteID = new GUID(System.Guid.NewGuid().ToString());

        spriteRects.Add(spriteRect);
      }
    }

    // 设置Sprite Rects
    dataProvider.SetSpriteRects(spriteRects.ToArray());

    // 应用设置
    dataProvider.Apply();

    // 更新Importer设置
    importer.textureType = TextureImporterType.Sprite;
    importer.spriteImportMode = SpriteImportMode.Multiple;
    importer.spritePixelsPerUnit = pixelsPerUnit;
    importer.mipmapEnabled = false;
    importer.isReadable = true;

    EditorUtility.SetDirty(importer);
    importer.SaveAndReimport();

    Debug.Log($"✓ 切割成功: {texture.name} → {spriteRects.Count} 个精灵");
    return true;
  }

  // 兼容旧版Unity的备用方法
  private bool SliceTextureWithOldAPI(string texturePath)
  {
    try
    {
      // 使用反射访问旧API（仅作备用）
      TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
      if (importer == null) return false;

      // 尝试通过反射设置spritesheet
      var spritesField = typeof(TextureImporter).GetField("m_SpriteSheet",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

      if (spritesField != null)
      {
        // 这里可以添加旧版API的逻辑
        Debug.Log("使用旧版API（反射）");
      }

      return false;
    }
    catch
    {
      return false;
    }
  }

  private void GetSpritesFromTexture(string texturePath)
  {
    Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
        .Where(obj => obj is Sprite)
        .Cast<Sprite>()
        .ToArray();

    generatedSprites.AddRange(sprites);
  }

  private void ExpandTexture(string texturePath)
  {
    UnityEngine.Object texture = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(texturePath);
    Selection.activeObject = texture;
    EditorGUIUtility.PingObject(texture);

    System.Type projectType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
    var browser = EditorWindow.GetWindow(projectType);

    if (browser != null)
    {
      var expandMethod = projectType.GetMethod("SetExpandedRecursive",
          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
      if (expandMethod != null)
      {
        expandMethod.Invoke(browser, new object[] { texture, true });
      }
    }
  }
  #endregion

  #region 状态栏
  private void DrawStatusBar()
  {
    EditorGUILayout.Space(10);

    Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));

    Rect statusRect = new Rect(rect.x, rect.y, rect.width - 100, 20);
    GUI.Label(statusRect, status, EditorStyles.miniLabel);

    if (isProcessing)
    {
      Rect progressRect = new Rect(rect.x, rect.y + 20, rect.width, 20);
      EditorGUI.ProgressBar(progressRect, progress, $"{progress:P0}");
    }
  }
  #endregion

  #region 公共方法
  public void SetSelectedTexture(Texture2D texture)
  {
    selectedTexture = texture;
    Repaint();
  }

  public void SetGridSize(int columns, int rows)
  {
    gridSize = new Vector2Int(columns, rows);
  }
  #endregion
}