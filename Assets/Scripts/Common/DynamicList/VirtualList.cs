using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif


/**
 * 虚拟列表，支持 vertical, horizontal, grid
 */
[ExecuteInEditMode]
public class VirtualList : MonoBehaviour
{
  [Header("引用")] [SerializeField] private ScrollRect scrollRect;
  [SerializeField] private RectTransform content;
  [SerializeField] private VirtualListItem itemPrefab;

  [Header("设置")] [SerializeField] private ScrollType scrollType;

  [Tooltip("水平方向显示的单元格之间的间距")] [SerializeField]
  private int spaceX = 0;

  [Tooltip("垂直方向显示的单元格之间的间距")] [SerializeField]
  private int spaceY = 0;

  [Tooltip("水平方向显示的单元格数量")] [SerializeField]
  private int repeatX = 0;

  [Tooltip("垂直方向显示的单元格数量")] [SerializeField]
  private int repeatY = 0;

  private readonly List<object> _dataList = new();
  private readonly Queue<VirtualListItem> _pool = new();
  private readonly List<VirtualListItem> _visibleItems = new();
#if UNITY_EDITOR
  private readonly List<VirtualListItem> _previewItems = new();
  private Transform _previewRoot;
  private bool _validateScheduled = false;
#endif

  private int _visibleCount;
  private int _startIndex = -1;
  private int _columns = 1;
  private int _rows = 1;

  private float _viewportHeight = 0;
  private float _viewportWidth = 0;

  private readonly int _extraCount = 2; // 额外缓存数量
  private float _itemHeight = 0;
  private float _itemWidth = 0;

  private void Awake()
  {
    if (Application.isPlaying)
    {
      ClearPreviewItems();
    }

    CreateItemPrefab();
    scrollRect.onValueChanged.AddListener(OnScroll);
    scrollRect.vertical = scrollType == ScrollType.Vertical;
    scrollRect.horizontal = scrollType == ScrollType.Horizontal;
  }

  private void Start()
  {
    if (!Application.isPlaying) return;
    // Ensure layout has been calculated so viewport sizes are valid
    Canvas.ForceUpdateCanvases();

    // set content anchors/pivot once
    if (content != null)
    {
      content.anchorMin = new Vector2(0, 1);
      content.anchorMax = new Vector2(0, 1);
      content.pivot = new Vector2(0, 1);
    }

    InitRect();
  }

  private void OnValidate()
  {
    if (Application.isPlaying)
      return;

    if (scrollRect == null || content == null || itemPrefab == null)
      return;

#if UNITY_EDITOR
    if (!_validateScheduled)
    {
      _validateScheduled = true;
      EditorApplication.delayCall += ValidateDelayed;
    }
#endif
  }

#if UNITY_EDITOR
  private void ValidateDelayed()
  {
    _validateScheduled = false;
    if (this == null) return;
    if (Application.isPlaying) return;
    if (scrollRect == null || content == null || itemPrefab == null)
      return;

    ClearPreviewItems();
    Canvas.ForceUpdateCanvases();
    CreateItemPrefab();
    InitRect();
    UpdateContentLayout();
    RefreshVisible(true);
  }
#endif

  private void CreateItemPrefab()
  {
    if (itemPrefab == null)
    {
      Debug.LogError("VirtualList itemPrefab is null, Please check your list.");
      return;
    }

    RectTransform rt = itemPrefab.GetComponent<RectTransform>();
    _itemHeight = rt.sizeDelta.y;
    _itemWidth = rt.sizeDelta.x;
    itemPrefab.gameObject.SetActive(false);
  }

  private void InitRect()
  {
    if (itemPrefab == null) return;

    _viewportHeight = scrollRect.viewport.rect.height;
    _viewportWidth = scrollRect.viewport.rect.width;

    // determine grid columns/rows based on scroll type
    if (scrollType == ScrollType.Vertical)
    {
      // Vertical: columns fixed (repeatX), rows auto-calculated
      _columns = repeatX > 0 ? repeatX : 1;
      _rows = Mathf.Max(1, Mathf.FloorToInt(_viewportHeight / (_itemHeight + spaceY)));
    }
    else
    {
      // Horizontal: rows fixed (repeatY), columns auto-calculated
      _rows = repeatY > 0 ? repeatY : 1;
      _columns = Mathf.Max(1, Mathf.FloorToInt(_viewportWidth / (_itemWidth + spaceX)));
    }

    if (scrollType == ScrollType.Vertical)
    {
      int visibleRows = Mathf.CeilToInt(_viewportHeight / (_itemHeight + spaceY)) + _extraCount;
      _visibleCount = visibleRows * _columns;
    }
    else
    {
      int visibleCols = Mathf.CeilToInt(_viewportWidth / (_itemWidth + spaceX)) + _extraCount;
      _visibleCount = visibleCols * _rows;
    }

    if (Application.isPlaying)
    {
      CreateItems();
    }
#if UNITY_EDITOR
    else
    {
      CreatePreviewItems();
    }
#endif
  }

  private void CreateItems()
  {
    // adjust existing items to match the desired visible count
    while (_visibleItems.Count < _visibleCount)
    {
      VirtualListItem item = Instantiate(itemPrefab, content, false);
      if (item != null)
      {
        item.gameObject.SetActive(false);
        _visibleItems.Add(item);
      }
    }

    while (_visibleItems.Count > _visibleCount)
    {
      var item = _visibleItems[_visibleItems.Count - 1];
      _visibleItems.RemoveAt(_visibleItems.Count - 1);
      if (item != null)
      {
        if (Application.isPlaying)
        {
          Destroy(item.gameObject);
        }
        else
        {
          DestroyImmediate(item.gameObject);
        }
      }
    }
  }

#if UNITY_EDITOR
  private void EnsurePreviewRoot()
  {
    if (content == null) return;
    if (_previewRoot != null) return;

    var root = content.Find("__PreviewVirtualListItems");
    if (root == null)
    {
      var go = new GameObject("__PreviewVirtualListItems");
      go.hideFlags = HideFlags.DontSave;
      go.transform.SetParent(content, false);
      _previewRoot = go.transform;
    }
    else
    {
      _previewRoot = root;
    }
  }

  private void CreatePreviewItems()
  {
    EnsurePreviewRoot();
    if (_previewRoot == null) return;

    while (_previewItems.Count < _visibleCount)
    {
      VirtualListItem item = Instantiate(itemPrefab, _previewRoot, false);
      if (item != null)
      {
        item.gameObject.SetActive(false);
        _previewItems.Add(item);
      }
    }

    while (_previewItems.Count > _visibleCount)
    {
      var item = _previewItems[_previewItems.Count - 1];
      _previewItems.RemoveAt(_previewItems.Count - 1);
      if (item != null)
      {
        DestroyImmediate(item.gameObject);
      }
    }
  }
#endif

  private void ClearPreviewItems()
  {
#if UNITY_EDITOR
    if (_previewRoot != null)
    {
      if (Application.isPlaying)
      {
        Destroy(_previewRoot.gameObject);
      }
      else
      {
        DestroyImmediate(_previewRoot.gameObject);
      }

      _previewRoot = null;
    }

    _previewItems.Clear();
#endif
  }

  public void RefreshData<T>(List<T> datas)
  {
    _dataList.Clear();

    foreach (var data in datas)
    {
      _dataList.Add(data);
    }

    UpdateContentLayout();

    RefreshVisible(true);
  }

  private void UpdateContentLayout()
  {
    // compute content size based on grid columns/rows
    int totalItems = _dataList.Count;
    int totalRows = Mathf.CeilToInt((float)totalItems / _columns);
    int totalCols = Mathf.CeilToInt((float)totalItems / _rows);

    if (scrollType == ScrollType.Vertical)
    {
      // 垂直方向
      float height = 0f;
      if (totalRows > 0)
      {
        height = totalRows * _itemHeight + ((totalRows - 1) * spaceY);
      }

      content.sizeDelta = new Vector2(_columns * _itemWidth + ((_columns - 1) * spaceX), height);
    }
    else
    {
      // 水平方向
      float width = 0f;
      if (totalCols > 0)
      {
        width = totalCols * _itemWidth + ((totalCols - 1) * spaceX);
      }

      content.sizeDelta = new Vector2(width, _rows * _itemHeight + ((_rows - 1) * spaceY));
    }
  }

  private void OnScroll(Vector2 value)
  {
    RefreshVisible(false);
  }

  private void RefreshVisible(bool force)
  {
    if (Application.isPlaying)
    {
      Runtime_RefreshVisible(force);
    }
#if UNITY_EDITOR
    else
    {
      Editor_RefreshVisible(force);
    }
#endif
  }

  // Runtime-only refresh (clean, focused on _visibleItems)
  private void Runtime_RefreshVisible(bool force)
  {
    int newStartIndex = 0;
    if (scrollType == ScrollType.Vertical)
    {
      int startRow = Mathf.FloorToInt(Mathf.Abs(content.anchoredPosition.y) / (_itemHeight + spaceY));
      startRow = Mathf.Max(0, startRow);
      newStartIndex = startRow * _columns;
    }
    else
    {
      int startCol = Mathf.FloorToInt(Mathf.Abs(content.anchoredPosition.x) / (_itemWidth + spaceX));
      startCol = Mathf.Max(0, startCol);
      newStartIndex = startCol * _rows;
    }

    if (!force && newStartIndex == _startIndex) return;
    _startIndex = newStartIndex;

    for (int i = 0; i < _visibleItems.Count; i++)
    {
      int dataIndex = _startIndex + i;
      var item = _visibleItems[i];
      if (item == null) continue;

      if (dataIndex >= _dataList.Count)
      {
        item.gameObject.SetActive(false);
        continue;
      }

      RefreshItem(item, dataIndex);
    }
  }

#if UNITY_EDITOR
  // Editor-only preview refresh (uses _previewItems and shows layout when no data)
  private void Editor_RefreshVisible(bool force)
  {
    int newStartIndex = 0;
    if (scrollType == ScrollType.Vertical)
    {
      int startRow = Mathf.FloorToInt(Mathf.Abs(content.anchoredPosition.y) / (_itemHeight + spaceY));
      startRow = Mathf.Max(0, startRow);
      newStartIndex = startRow * _columns;
    }
    else
    {
      int startCol = Mathf.FloorToInt(Mathf.Abs(content.anchoredPosition.x) / (_itemWidth + spaceX));
      startCol = Mathf.Max(0, startCol);
      newStartIndex = startCol * _rows;
    }

    if (!force && newStartIndex == _startIndex) return;
    _startIndex = newStartIndex;

    bool previewEmpty = _dataList.Count == 0;

    for (int i = 0; i < _previewItems.Count; i++)
    {
      int dataIndex = _startIndex + i;
      var item = _previewItems[i];
      if (item == null) continue;

      if (dataIndex >= _dataList.Count)
      {
        if (previewEmpty)
        {
          item.gameObject.SetActive(true);
          item.name = "item" + dataIndex;
          UpdateItemPosition(item, dataIndex);
        }
        else
        {
          item.gameObject.SetActive(false);
        }

        continue;
      }

      // has data
      item.gameObject.SetActive(true);
      item.name = "item" + dataIndex;
      UpdateItemPosition(item, dataIndex);
      // do not call item.Refresh in preview unless data exists
      item.Refresh(dataIndex, _dataList.Count > dataIndex ? _dataList[dataIndex] : null);
    }
  }
#endif

  private void RefreshItem(VirtualListItem item, int dataIndex)
  {
    if (item == null) return;

    item.gameObject.SetActive(true);
    item.name = "item" + dataIndex;
    UpdateItemPosition(item, dataIndex);
    item.Refresh(dataIndex, _dataList[dataIndex]);
  }

  private void UpdateItemPosition(VirtualListItem item, int dataIndex)
  {
    RectTransform rt = item.transform as RectTransform;
    if (rt == null) return;

    float x, y;

    if (scrollType == ScrollType.Vertical)
    {
      // row-major: row = index / columns, col = index % columns
      int row = dataIndex / _columns;
      int col = dataIndex % _columns;
      x = col * (_itemWidth + spaceX);
      y = -row * (_itemHeight + spaceY);
    }
    else
    {
      // column-major: col = index / rows, row = index % rows
      int col = dataIndex / _rows;
      int row = dataIndex % _rows;
      x = col * (_itemWidth + spaceX);
      y = -row * (_itemHeight + spaceY);
    }

    rt.anchoredPosition = new Vector2(x, y);
  }
}