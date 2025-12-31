using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public static class ProjectSpriteHelper
{
  static ProjectSpriteHelper()
  {
    EditorApplication.projectWindowItemOnGUI += OnProjectItemGUI;
  }

  private static void OnProjectItemGUI(string guid, Rect rect)
  {
    string path = AssetDatabase.GUIDToAssetPath(guid);
    if (string.IsNullOrEmpty(path)) return;

    var mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
    if (mainType == typeof(Texture2D))
    {
      var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
          .Where(obj => obj is Sprite)
          .Cast<Sprite>()
          .ToArray();

      if (sprites.Length > 0)
      {
        if (rect.height > 20)
        {
          DrawSpriteBadge(rect, sprites.Length);
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
        {
          ShowTextureContextMenu(path);
          Event.current.Use();
        }
      }
    }
  }

  private static void DrawSpriteBadge(Rect rect, int spriteCount)
  {
    Rect badgeRect = new Rect(rect.x + rect.width - 20, rect.y, 20, 20);

    GUI.color = new Color(0.2f, 0.6f, 1f, 0.8f);
    GUI.DrawTexture(badgeRect, EditorGUIUtility.whiteTexture);
    GUI.color = Color.white;

    GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel)
    {
      alignment = TextAnchor.MiddleCenter,
      normal = { textColor = Color.white },
      fontSize = 9
    };

    GUI.Label(badgeRect, spriteCount > 99 ? "99+" : spriteCount.ToString(), style);
  }

  private static void ShowTextureContextMenu(string texturePath)
  {
    GenericMenu menu = new GenericMenu();

    menu.AddItem(new GUIContent("打开精灵切割器"), false, () =>
    {
      var window = EditorWindow.GetWindow<SpriteBatchSlicerEditor>("精灵切割器");
      Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
      window.SetSelectedTexture(texture);
      window.Focus();
    });

    menu.AddSeparator("");

    menu.AddItem(new GUIContent("快速切割/2×2网格"), false, () =>
    {
      QuickSliceTextureNewAPI(texturePath, new Vector2Int(2, 2));
    });

    menu.AddItem(new GUIContent("快速切割/3×3网格"), false, () =>
    {
      QuickSliceTextureNewAPI(texturePath, new Vector2Int(3, 3));
    });

    menu.AddItem(new GUIContent("快速切割/4×4网格"), false, () =>
    {
      QuickSliceTextureNewAPI(texturePath, new Vector2Int(4, 4));
    });

    menu.AddSeparator("");

    menu.AddItem(new GUIContent("展开显示精灵"), false, () =>
    {
      var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
      Selection.activeObject = texture;

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
    });

    menu.AddItem(new GUIContent("选中所有精灵"), false, () =>
    {
      var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
              .Where(obj => obj is Sprite)
              .Cast<Sprite>()
              .ToArray();

      if (sprites.Length > 0)
      {
        Selection.objects = sprites;
        EditorGUIUtility.PingObject(sprites[0]);
      }
    });

    menu.ShowAsContext();
  }

  private static void QuickSliceTextureNewAPI(string texturePath, Vector2Int grid)
  {
    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
    if (texture == null) return;

    TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
    if (importer == null) return;

    int sliceWidth = texture.width / grid.x;
    int sliceHeight = texture.height / grid.y;

    // 使用新的Sprite Data Provider API
    var spriteDataProviderFactories = new SpriteDataProviderFactories();
    spriteDataProviderFactories.Init();

    var dataProvider = spriteDataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
    if (dataProvider == null) return;

    dataProvider.InitSpriteEditorDataProvider();

    var spriteRects = dataProvider.GetSpriteRects()?.ToList() ?? new List<SpriteRect>();
    spriteRects.Clear();

    for (int y = grid.y - 1; y >= 0; y--)
    {
      for (int x = 0; x < grid.x; x++)
      {
        string spriteName = $"{texture.name}_{y * grid.x + x}";

        SpriteRect spriteRect = new SpriteRect
        {
          name = spriteName,
          rect = new Rect(x * sliceWidth, y * sliceHeight, sliceWidth, sliceHeight),
          pivot = new Vector2(0.5f, 0.5f),
          alignment = SpriteAlignment.Custom,
          border = Vector4.zero,
          spriteID = new GUID(System.Guid.NewGuid().ToString())
        };

        spriteRects.Add(spriteRect);
      }
    }

    dataProvider.SetSpriteRects(spriteRects.ToArray());
    dataProvider.Apply();

    importer.textureType = TextureImporterType.Sprite;
    importer.spriteImportMode = SpriteImportMode.Multiple;
    importer.spritePixelsPerUnit = 100f;
    importer.SaveAndReimport();

    Debug.Log($"快速切割: {texture.name} → {grid.x}×{grid.y}网格");
    AssetDatabase.Refresh();
  }
}

// 右键菜单扩展
public class TextureContextMenu
{
  [MenuItem("Assets/快速切割精灵/4×4网格", true)]
  [MenuItem("Assets/快速切割精灵/3×3网格", true)]
  [MenuItem("Assets/快速切割精灵/2×2网格", true)]
  [MenuItem("Assets/打开精灵切割器", true)]
  private static bool ValidateTextureMenu()
  {
    return Selection.activeObject is Texture2D;
  }

  [MenuItem("Assets/快速切割精灵/4×4网格")]
  private static void QuickSlice4x4()
  {
    QuickSlice(new Vector2Int(4, 4));
  }

  [MenuItem("Assets/快速切割精灵/3×3网格")]
  private static void QuickSlice3x3()
  {
    QuickSlice(new Vector2Int(3, 3));
  }

  [MenuItem("Assets/快速切割精灵/2×2网格")]
  private static void QuickSlice2x2()
  {
    QuickSlice(new Vector2Int(2, 2));
  }

  [MenuItem("Assets/打开精灵切割器")]
  private static void OpenSpriteSlicer()
  {
    var window = EditorWindow.GetWindow<SpriteBatchSlicerEditor>("精灵切割器");

    if (Selection.activeObject is Texture2D texture)
    {
      window.SetSelectedTexture(texture);
    }

    window.Focus();
  }

  private static void QuickSlice(Vector2Int grid)
  {
    foreach (UnityEngine.Object obj in Selection.objects)
    {
      if (obj is Texture2D texture)
      {
        string path = AssetDatabase.GetAssetPath(texture);
        QuickSliceTextureNewAPI(path, grid);
      }
    }
  }

  private static void QuickSliceTextureNewAPI(string texturePath, Vector2Int grid)
  {
    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
    if (texture == null) return;

    TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
    if (importer == null) return;

    int sliceWidth = texture.width / grid.x;
    int sliceHeight = texture.height / grid.y;

    var spriteDataProviderFactories = new SpriteDataProviderFactories();
    spriteDataProviderFactories.Init();

    var dataProvider = spriteDataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
    if (dataProvider == null) return;

    dataProvider.InitSpriteEditorDataProvider();

    var spriteRects = dataProvider.GetSpriteRects()?.ToList() ?? new List<SpriteRect>();
    spriteRects.Clear();

    for (int y = grid.y - 1; y >= 0; y--)
    {
      for (int x = 0; x < grid.x; x++)
      {
        string spriteName = $"{texture.name}_{y * grid.x + x}";

        SpriteRect spriteRect = new SpriteRect
        {
          name = spriteName,
          rect = new Rect(x * sliceWidth, y * sliceHeight, sliceWidth, sliceHeight),
          pivot = new Vector2(0.5f, 0.5f),
          alignment = SpriteAlignment.Custom,
          border = Vector4.zero,
          spriteID = new GUID(System.Guid.NewGuid().ToString())
        };

        spriteRects.Add(spriteRect);
      }
    }

    dataProvider.SetSpriteRects(spriteRects.ToArray());
    dataProvider.Apply();

    importer.textureType = TextureImporterType.Sprite;
    importer.spriteImportMode = SpriteImportMode.Multiple;
    importer.spritePixelsPerUnit = 100f;
    importer.SaveAndReimport();

    Debug.Log($"快速切割: {texture.name} → {grid.x}×{grid.y}网格");
  }

  // 兼容旧Unity版本的备用方法
  private static void QuickSliceTextureOldAPI(string texturePath, Vector2Int grid)
  {
    try
    {
      TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
      if (importer == null) return;

      // 尝试通过反射使用旧API
      var spritesField = typeof(TextureImporter).GetField("m_SpriteSheet",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

      if (spritesField != null)
      {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        int sliceWidth = texture.width / grid.x;
        int sliceHeight = texture.height / grid.y;

        List<UnityEditor.SpriteMetaData> sprites = new List<UnityEditor.SpriteMetaData>();

        for (int y = grid.y - 1; y >= 0; y--)
        {
          for (int x = 0; x < grid.x; x++)
          {
            UnityEditor.SpriteMetaData sprite = new UnityEditor.SpriteMetaData
            {
              name = $"{texture.name}_{y * grid.x + x}",
              rect = new Rect(x * sliceWidth, y * sliceHeight, sliceWidth, sliceHeight),
              pivot = new Vector2(0.5f, 0.5f),
              alignment = 9
            };
            sprites.Add(sprite);
          }
        }

        spritesField.SetValue(importer, sprites.ToArray());

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.SaveAndReimport();

        Debug.Log($"快速切割(旧API): {texture.name} → {grid.x}×{grid.y}网格");
      }
    }
    catch (System.Exception e)
    {
      Debug.LogError($"快速切割失败: {e.Message}");
    }
  }
}