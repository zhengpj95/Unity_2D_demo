using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class UICreateObjectMenu
{
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

    var list = root.AddComponent<VirtualListEx>();
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
}
