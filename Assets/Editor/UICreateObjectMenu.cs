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

  [MenuItem("GameObject/UI/Virtual List", false, 0)]
  private static void CreateVirtualList()
  {
    var parent = Selection.activeTransform;

    var root = new GameObject("List", typeof(RectTransform));
    Undo.RegisterCreatedObjectUndo(root, "Create Virtual List");
    root.layer = LayerMask.NameToLayer("UI");

    if (parent != null)
    {
      Undo.SetTransformParent(root.transform, parent, "Create Virtual List");
    }

    var rootRect = root.GetComponent<RectTransform>();
    rootRect.anchorMin = new Vector2(0.5f, 0.5f);
    rootRect.anchorMax = new Vector2(0.5f, 0.5f);
    rootRect.sizeDelta = new Vector2(300f, 200f);
    rootRect.anchoredPosition = Vector2.zero;
    ResetRectTransform(rootRect);

    var list = root.AddComponent<VirtualListEx>();
    var image = root.AddComponent<Image>();
    image.color = new Color(1f, 1f, 1f, 0f);

    var viewportGo = new GameObject("Viewport", typeof(RectTransform));
    Undo.RegisterCreatedObjectUndo(viewportGo, "Create Virtual List");
    viewportGo.layer = LayerMask.NameToLayer("UI");
    viewportGo.transform.SetParent(root.transform, false);

    var viewportRect = viewportGo.GetComponent<RectTransform>();
    viewportRect.anchorMin = Vector2.zero;
    viewportRect.anchorMax = Vector2.one;
    viewportRect.offsetMin = Vector2.zero;
    viewportRect.offsetMax = Vector2.zero;
    viewportRect.sizeDelta = Vector2.zero;
    ResetRectTransform(viewportRect);

    viewportGo.AddComponent<RectMask2D>();

    var contentGo = new GameObject("Content", typeof(RectTransform));
    Undo.RegisterCreatedObjectUndo(contentGo, "Create Virtual List");
    contentGo.layer = LayerMask.NameToLayer("UI");
    contentGo.transform.SetParent(viewportGo.transform, false);

    var contentRect = contentGo.GetComponent<RectTransform>();
    contentRect.anchorMin = new Vector2(0f, 1f);
    contentRect.anchorMax = new Vector2(0f, 1f);
    contentRect.pivot = new Vector2(0f, 1f);
    contentRect.sizeDelta = new Vector2(0f, 0f);
    contentRect.anchoredPosition = Vector2.zero;
    ResetRectTransform(contentRect);

    var renderGo = new GameObject("render", typeof(RectTransform));
    Undo.RegisterCreatedObjectUndo(renderGo, "Create Virtual List");
    renderGo.layer = LayerMask.NameToLayer("UI");
    renderGo.transform.SetParent(root.transform, false);

    var renderRect = renderGo.GetComponent<RectTransform>();
    renderRect.anchorMin = new Vector2(0f, 1f);
    renderRect.anchorMax = new Vector2(0f, 1f);
    renderRect.pivot = new Vector2(0f, 1f);
    renderRect.sizeDelta = new Vector2(300f, 40f);
    renderRect.anchoredPosition = Vector2.zero;
    ResetRectTransform(renderRect);
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
