using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 提供 Unity 创建菜单中的快捷对象和预制体操作。
/// </summary>
public class UICreateObjectMenu
{
  /// <summary>
  /// 在 Project 窗口当前目录创建仅包含 RectTransform 的空 UI 根对象预制体。
  /// </summary>
  [MenuItem("Assets/Create/New Prefab %#p", false, 1)]
  private static void CreatePrefabAsset(MenuCommand menuCommand)
  {
    var folderPath = GetSelectedAssetFolder(menuCommand);
    if (string.IsNullOrEmpty(folderPath))
    {
      Debug.LogError("创建预制体失败：无法确定 Assets 下的有效目标文件夹。");
      return;
    }

    var prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/New Prefab.prefab");
    var temporaryObject = new GameObject("New Prefab", typeof(RectTransform), typeof(CanvasRenderer));
    temporaryObject.layer = LayerMask.NameToLayer("UI");
    try
    {
      var prefab = PrefabUtility.SaveAsPrefabAsset(temporaryObject, prefabPath, out var success);
      if (!success || prefab == null)
      {
        Debug.LogError($"创建预制体失败：无法将空 UI 根对象保存到“{prefabPath}”。");
        return;
      }

      Selection.activeObject = prefab;
      EditorGUIUtility.PingObject(prefab);
    }
    finally
    {
      Object.DestroyImmediate(temporaryObject);
    }
  }

  private static string GetSelectedAssetFolder(MenuCommand menuCommand)
  {
    var selected = menuCommand.context as Object ?? Selection.activeObject;
    var selectedPath = AssetDatabase.GetAssetPath(selected);
    if (string.IsNullOrEmpty(selectedPath))
      selectedPath = "Assets";

    if (AssetDatabase.IsValidFolder(selectedPath))
      return selectedPath.StartsWith("Assets") ? selectedPath : string.Empty;

    var separatorIndex = selectedPath.LastIndexOf('/');
    if (separatorIndex < 0)
      return "Assets";

    var folderPath = selectedPath.Substring(0, separatorIndex);
    return folderPath.StartsWith("Assets") && AssetDatabase.IsValidFolder(folderPath) ? folderPath : string.Empty;
  }

  private static void ResetRectTransform(RectTransform rect)
  {
    if (rect == null)
      return;

    rect.localRotation = Quaternion.identity;
    rect.localScale = Vector3.one;
    rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
  }

  private static GameObject CreateVirtualListUIObject(string name, Transform parent, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
  {
    var go = new GameObject(name, typeof(RectTransform));
    Undo.RegisterCreatedObjectUndo(go, "Create Virtual List");
    go.layer = LayerMask.NameToLayer("UI");

    if (parent != null)
    {
      GameObjectUtility.SetParentAndAlign(go, parent.gameObject);
    }

    var rect = go.GetComponent<RectTransform>();
    rect.anchorMin = anchorMin;
    rect.anchorMax = anchorMax;
    rect.pivot = pivot;
    rect.sizeDelta = sizeDelta;
    rect.anchoredPosition = Vector2.zero;
    ResetRectTransform(rect);
    return go;
  }

  [MenuItem("GameObject/UI/Virtual List", false, 0)]
  private static void CreateVirtualList(MenuCommand menuCommand)
  {
    var parent = (menuCommand.context as GameObject)?.transform ?? Selection.activeTransform;

    var root = CreateVirtualListUIObject("List", parent, new Vector2(300f, 200f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    var rootRect = root.GetComponent<RectTransform>();

    var list = root.AddComponent<VirtualList>();
    var image = root.AddComponent<Image>();
    image.color = new Color(1f, 1f, 1f, 0f);

    var viewportGo = CreateVirtualListUIObject("Viewport", root.transform, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
    var viewportRect = viewportGo.GetComponent<RectTransform>();
    viewportRect.offsetMin = Vector2.zero;
    viewportRect.offsetMax = Vector2.zero;
    viewportGo.AddComponent<RectMask2D>();

    var contentGo = CreateVirtualListUIObject("Content", viewportGo.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
    var contentRect = contentGo.GetComponent<RectTransform>();

    var renderGo = CreateVirtualListUIObject("render", root.transform, new Vector2(300f, 40f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
    var renderRect = renderGo.GetComponent<RectTransform>();
    renderGo.SetActive(false);

    Undo.RecordObject(list, "Create Virtual List");
    list.viewport = viewportRect;
    list.content = contentRect;
    list.ItemTemplate = renderRect;

    EditorApplication.delayCall += () =>
    {
      if (list == null)
      {
        return;
      }

      var serializedObject = new SerializedObject(list);
      serializedObject.Update();
      serializedObject.ApplyModifiedProperties();
      EditorUtility.SetDirty(list);
    };

    Selection.activeGameObject = root;
    EditorUtility.SetDirty(root);
    EditorUtility.SetDirty(list);
    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
  }

  /// <summary>
  /// 从 GameObject/UI 右键菜单创建单列垂直异高虚拟列表。
  /// 层级与普通 Virtual List 保持一致，模板置于根节点以避免参与 Content 的滚动布局。
  /// </summary>
  [MenuItem("GameObject/UI/Variable Height Virtual List", false, 1)]
  private static void CreateVariableHeightVirtualList(MenuCommand menuCommand)
  {
    var parent = (menuCommand.context as GameObject)?.transform ?? Selection.activeTransform;

    var root = CreateVirtualListUIObject("Variable Height List", parent, new Vector2(300f, 200f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    var list = root.AddComponent<VariableHeightVirtualList>();
    var image = root.AddComponent<Image>();
    image.color = new Color(1f, 1f, 1f, 0f);

    var viewportGo = CreateVirtualListUIObject("Viewport", root.transform, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
    var viewportRect = viewportGo.GetComponent<RectTransform>();
    viewportRect.offsetMin = Vector2.zero;
    viewportRect.offsetMax = Vector2.zero;
    viewportGo.AddComponent<RectMask2D>();

    var contentGo = CreateVirtualListUIObject("Content", viewportGo.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
    var contentRect = contentGo.GetComponent<RectTransform>();

    var renderGo = CreateVirtualListUIObject("render", root.transform, new Vector2(300f, 80f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
    var renderRect = renderGo.GetComponent<RectTransform>();
    renderGo.SetActive(false);

    Undo.RecordObject(list, "Create Variable Height Virtual List");
    list.viewport = viewportRect;
    list.content = contentRect;
    list.ItemTemplate = renderRect;

    EditorApplication.delayCall += () =>
    {
      if (list == null)
        return;

      var serializedObject = new SerializedObject(list);
      serializedObject.Update();
      serializedObject.ApplyModifiedProperties();
      EditorUtility.SetDirty(list);
    };

    Selection.activeGameObject = root;
    EditorUtility.SetDirty(root);
    EditorUtility.SetDirty(list);
    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
  }
}
