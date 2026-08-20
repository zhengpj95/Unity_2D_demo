using UnityEngine;

[DisallowMultipleComponent]
public class UISortingOrder : MonoBehaviour
{
  [SerializeField]
  private int sortingOrder = 0;

  private Canvas canvas;
  private Renderer targetRenderer;

  public int SortingOrder
  {
    get => sortingOrder;
    set
    {
      if (sortingOrder == value)
        return;

      sortingOrder = value;
      Refresh();
    }
  }

  /// <summary>
  /// 当前节点最终的 SortingOrder
  /// </summary>
  public int FinalSortingOrder { get; private set; }

  private void Awake()
  {
    Init();
  }

  private void OnEnable()
  {
    Refresh();
  }

#if UNITY_EDITOR
  private void OnValidate()
  {
    if (Application.isPlaying)
    {
      Refresh();
    }
  }
#endif

  private void Init()
  {
    canvas = GetComponent<Canvas>();
    targetRenderer = GetComponent<Renderer>();

    // UGUI Image / TMP / Button 等
    if (canvas == null && targetRenderer == null)
    {
      canvas = gameObject.AddComponent<Canvas>();
    }

    Apply();
  }

  public void Refresh()
  {
    int parentOrder = GetParentSortingOrder();

    FinalSortingOrder = parentOrder + sortingOrder;

    Apply();
  }

  private int GetParentSortingOrder()
  {
    // ① 优先寻找最近的 UISortingOrder
    Transform parent = transform.parent;

    while (parent != null)
    {
      UISortingOrder parentSorting = parent.GetComponent<UISortingOrder>();

      if (parentSorting != null)
      {
        return parentSorting.FinalSortingOrder;
      }

      parent = parent.parent;
    }

    // ② 找不到 UISortingOrder
    //    直接使用所属 UI Layer Canvas
    Canvas layerCanvas = GetParentCanvas();

    if (layerCanvas != null)
    {
      return layerCanvas.sortingOrder;
    }

    return 0;
  }

  private Canvas GetParentCanvas()
  {
    Transform parent = transform.parent;

    while (parent != null)
    {
      Canvas canvas = parent.GetComponent<Canvas>();

      if (canvas != null)
      {
        return canvas;
      }

      parent = parent.parent;
    }

    return null;
  }

  private void Apply()
  {
    if (canvas != null)
    {
      canvas.overrideSorting = true;
      canvas.sortingOrder = FinalSortingOrder;
    }

    if (targetRenderer != null)
    {
      targetRenderer.sortingOrder = FinalSortingOrder;
    }

    // 同步子级排序
    RefreshChildren();
  }

  private void RefreshChildren()
  {
    UISortingOrder[] children = GetComponentsInChildren<UISortingOrder>(true);

    foreach (var child in children)
    {
      if (child == this)
        continue;

      child.Refresh();
    }
  }
}