using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 虚拟列表项信息结构体
/// </summary>
/// <remarks>注意：点击回调中 itemTransform 可能为 null</remarks>
public struct VirtualListRenderInfo
{
  public int index;
  public object data;
  public int selectedIndex;
  public RectTransform itemTransform;
}

/**
 * 虚拟列表，支持 vertical, horizontal, grid
 * 
 * 点击处理采用矩形区域点击判断方式：
 * - 在VirtualList上统一处理点击，计算是否击中某个item
 * - 优点：内存高效、性能优秀、与虚拟列表完美配合
 * - 缺点：需要自己实现点击计算和坐标转换
 * 
 * 防滑动误触：
 * - 记录按下和释放的屏幕位置
 * - 如果偏移超过阈值，认为是滑动而不是点击
 */
[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform), typeof(ScrollRect))]
public class VirtualList : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
  [Header("引用")][SerializeField] private ScrollRect scrollRect;
  [SerializeField] private RectTransform content;
  [SerializeField] private RectTransform itemTemplate;

  [Header("设置")]
  [Tooltip("布局类型：Vertical垂直、Horizontal水平")]
  [SerializeField] private LayoutType layoutType;

  [Tooltip("水平方向显示的单元格之间的间距")]
  [SerializeField]
  private int spaceX = 0;

  [Tooltip("垂直方向显示的单元格之间的间距")]
  [SerializeField]
  private int spaceY = 0;

  [Tooltip("水平方向显示的单元格数量")]
  [SerializeField]
  private int repeatX = 0;

  [Tooltip("垂直方向显示的单元格数量")]
  [SerializeField]
  private int repeatY = 0;

  public int SpaceX
  {
    get => spaceX;
    set
    {
      if (spaceX == value) return;
      spaceX = value;
      ApplyLayoutSettings();
    }
  }

  public int SpaceY
  {
    get => spaceY;
    set
    {
      if (spaceY == value) return;
      spaceY = value;
      ApplyLayoutSettings();
    }
  }

  public int RepeatX
  {
    get => repeatX;
    set
    {
      if (repeatX == value) return;
      repeatX = value;
      ApplyLayoutSettings();
    }
  }

  public int RepeatY
  {
    get => repeatY;
    set
    {
      if (repeatY == value) return;
      repeatY = value;
      ApplyLayoutSettings();
    }
  }

  private readonly List<object> _dataList = new();
  private readonly Queue<RectTransform> _pool = new();
  private readonly List<RectTransform> _visibleItems = new();
#if UNITY_EDITOR
  private readonly List<RectTransform> _previewItems = new();
  private Transform _previewRoot;
  private bool _validateScheduled = false;
#endif

  /// <summary>
  /// item 渲染回调：(索引, 数据, RectTransform, 是否被选中)
  /// </summary>
  public System.Action<VirtualListRenderInfo> renderHandler;

  /// <summary>
  /// item 点击回调：(索引, 数据)
  /// </summary>
  public System.Action<VirtualListRenderInfo> onItemClick;

  /// <summary>
  /// 当前选中的item索引，-1表示未选中
  /// </summary>
  private int _selectedIndex = -1;

  /// <summary>
  /// 获取当前选中的索引
  /// </summary>
  public int SelectedIndex
  {
    get => _selectedIndex;
    set
    {
      if (_selectedIndex == value)
        return;

      _selectedIndex = value;
      // 选中状态改变时，重新刷新可见区域
      RefreshVisible(true);
    }
  }

  /// <summary>
  /// 是否是垂直布局
  /// </summary>
  private bool IsVertical => layoutType == LayoutType.Vertical;

  /// <summary>
  /// 点击阈值：超过这个距离就认为是滑动而不是点击（单位：像素）
  /// </summary>
  private float clickThreshold = 10f;

  /// <summary>
  /// 记录按下时的屏幕位置，用于判断是否是滑动
  /// </summary>
  private Vector2 _pointerDownPosition = Vector2.zero;

  private int _visibleCount;
  private int _startIndex = -1;
  private int _columns = 1;
  private int _rows = 1;

  private float _viewportHeight = 0;
  private float _viewportWidth = 0;

  private readonly int _bufferCnt = 2; // 额外缓存行或列数
  private float _itemHeight = 0;
  private float _itemWidth = 0;
  // 缓存的VirtualListRenderInfo，避免重复new
  private VirtualListRenderInfo _cachedRenderInfo = new VirtualListRenderInfo();

  private void Awake()
  {
    if (Application.isPlaying)
    {
      ClearPreviewItems();
    }

    InitItemTemplate();
    scrollRect.onValueChanged.AddListener(OnScroll);
    scrollRect.vertical = IsVertical;
    scrollRect.horizontal = !IsVertical;
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

    if (scrollRect == null || content == null || itemTemplate == null)
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
    if (scrollRect == null || content == null || itemTemplate == null)
      return;

    ClearPreviewItems();
    Canvas.ForceUpdateCanvases();
    InitItemTemplate();
    InitRect();
    UpdateContentLayout();
    RefreshVisible(true);
  }
#endif

  private void InitItemTemplate()
  {
    if (itemTemplate == null)
    {
      Debug.LogError("VirtualList itemTemplate is null, Please check your list.");
      return;
    }

    _itemHeight = itemTemplate.sizeDelta.y;
    _itemWidth = itemTemplate.sizeDelta.x;
    itemTemplate.gameObject.SetActive(false);
  }

  private void InitRect()
  {
    if (itemTemplate == null) return;

    _viewportHeight = scrollRect.viewport.rect.height;
    _viewportWidth = scrollRect.viewport.rect.width;

    // determine grid columns/rows based on scroll type
    if (IsVertical)
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

    if (IsVertical)
    {
      int visibleRows = Mathf.CeilToInt(_viewportHeight / (_itemHeight + spaceY)) + _bufferCnt;
      _visibleCount = visibleRows * _columns;
    }
    else
    {
      int visibleCols = Mathf.CeilToInt(_viewportWidth / (_itemWidth + spaceX)) + _bufferCnt;
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
      RectTransform item = GetPooledItem();
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
      ReleaseItemToPool(item);
    }
  }

  private RectTransform GetPooledItem()
  {
    while (_pool.Count > 0)
    {
      var pooled = _pool.Dequeue();
      if (pooled != null)
      {
        pooled.transform.SetParent(content, false);
        return pooled;
      }
    }

    var item = Instantiate(itemTemplate, content, false);
    if (item != null)
    {
      item.gameObject.SetActive(false);
    }

    return item;
  }

  private void ReleaseItemToPool(RectTransform item)
  {
    if (item == null) return;

    item.gameObject.SetActive(false);
    item.transform.SetParent(content, false);
    _pool.Enqueue(item);
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
      RectTransform item = Instantiate(itemTemplate, _previewRoot, false);
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

  private void ApplyLayoutSettings()
  {
    if (content == null || scrollRect == null || itemTemplate == null) return;

#if UNITY_EDITOR
    if (!Application.isPlaying)
    {
      ClearPreviewItems();
    }
#endif

    if (!Application.isPlaying)
    {
      Canvas.ForceUpdateCanvases();
    }

    InitRect();
    UpdateContentLayout();
    RefreshVisible(true);
  }

  private void UpdateContentLayout()
  {
    // compute content size based on grid columns/rows
    int totalItems = _dataList.Count;
    int totalRows = Mathf.CeilToInt((float)totalItems / _columns);
    int totalCols = Mathf.CeilToInt((float)totalItems / _rows);

    if (IsVertical)
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
    if (IsVertical)
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
    if (IsVertical)
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

      RefreshItem(item, dataIndex);
    }
  }
#endif

  private void RefreshItem(RectTransform item, int dataIndex)
  {
    if (item == null) return;

    item.gameObject.SetActive(true);
    item.name = "item" + dataIndex;
    UpdateItemPosition(item, dataIndex);

    // 复用缓存的实例，避免重复new
    _cachedRenderInfo.index = dataIndex;
    _cachedRenderInfo.data = _dataList[dataIndex];
    _cachedRenderInfo.selectedIndex = _selectedIndex;
    _cachedRenderInfo.itemTransform = item;
    renderHandler?.Invoke(_cachedRenderInfo);
  }

  private void UpdateItemPosition(RectTransform itemTransform, int dataIndex)
  {
    if (itemTransform == null) return;

    float x, y;

    if (IsVertical)
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

    itemTransform.anchoredPosition = new Vector2(x, y);
  }

  /// <summary>
  /// 处理点击事件
  /// 采用矩形区域点击判断方式：
  /// 1. 获取点击位置（世界坐标）
  /// 2. 转换为content相对坐标
  /// 3. 计算点击落在哪个item
  /// 4. 调用onItemClick回调
  /// </summary>
  public void OnPointerClick(PointerEventData eventData)
  {
    if (!Application.isPlaying)
      return;

    if (scrollRect == null || content == null)
      return;

    if (_dataList.Count == 0)
      return;

    // 检查是否是滑动操作而不是点击
    float dragDistance = Vector2.Distance(eventData.position, _pointerDownPosition);
    if (dragDistance > clickThreshold)
    {
      // 超过阈值，认为是滑动，不触发点击
      return;
    }

    // 将屏幕坐标转换为content本地坐标
    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
      content,
      eventData.position,
      eventData.pressEventCamera,
      out Vector2 localPoint))
    {
      return;
    }

    // 根据滚动方向计算点击的item索引
    int clickedIndex = GetItemIndexFromClickPosition(localPoint);

    if (clickedIndex >= 0 && clickedIndex < _dataList.Count)
    {
      // 更新选中索引
      _selectedIndex = clickedIndex;

      // // 查找对应的item
      // RectTransform itemTransform = null;
      // for (int i = 0; i < _visibleItems.Count; i++)
      // {
      //   int dataIndex = _startIndex + i;
      //   if (dataIndex == clickedIndex && _visibleItems[i] != null)
      //   {
      //     itemTransform = _visibleItems[i];
      //     break;
      //   }
      // }

      // 复用缓存的实例，避免重复new
      _cachedRenderInfo.index = clickedIndex;
      _cachedRenderInfo.data = _dataList[clickedIndex];
      _cachedRenderInfo.selectedIndex = _selectedIndex;
      _cachedRenderInfo.itemTransform = null;
      onItemClick?.Invoke(_cachedRenderInfo);

      // 重新刷新可见区域，以更新选中状态的显示
      RefreshVisible(true);
    }
  }

  /// <summary>
  /// 处理按下事件，记录初始按下位置
  /// </summary>
  public void OnPointerDown(PointerEventData eventData)
  {
    if (!Application.isPlaying)
      return;

    _pointerDownPosition = eventData.position;
  }

  /// <summary>
  /// 根据点击位置计算item的索引
  /// </summary>
  private int GetItemIndexFromClickPosition(Vector2 localPoint)
  {
    // 计算点击相对于顶部/左侧的距离
    float clickDistance = IsVertical
      ? -localPoint.y  // 垂直滚动：计算距离顶部的距离
      : localPoint.x;  // 水平滚动：计算距离左侧的距离

    if (clickDistance < 0)
      return -1;

    // 计算在第几行或第几列
    float itemSizeWithSpace = IsVertical ? (_itemHeight + spaceY) : (_itemWidth + spaceX);

    int lineIndex = Mathf.FloorToInt(clickDistance / itemSizeWithSpace);

    if (IsVertical)
    {
      // 垂直滚动：计算点击在grid中的行列位置
      float xInGrid = localPoint.x;
      float yInGrid = clickDistance % itemSizeWithSpace;

      if (xInGrid < 0 || yInGrid < 0)
        return -1;

      // 计算点击在哪一列
      int col = Mathf.FloorToInt(xInGrid / (_itemWidth + spaceX));
      if (col < 0 || col >= _columns || xInGrid > _columns * _itemWidth + (_columns - 1) * spaceX)
        return -1;

      // 检查是否在item内（不在间距区域）
      if (yInGrid > _itemHeight)
        return -1;

      int row = lineIndex;
      return row * _columns + col;
    }
    else
    {
      // 水平滚动：计算点击在grid中的行列位置
      float xInGrid = clickDistance % itemSizeWithSpace;
      float yInGrid = -localPoint.y;

      if (xInGrid < 0 || yInGrid < 0)
        return -1;

      // 计算点击在哪一行
      int row = Mathf.FloorToInt(yInGrid / (_itemHeight + spaceY));
      if (row < 0 || row >= _rows || yInGrid > _rows * _itemHeight + (_rows - 1) * spaceY)
        return -1;

      // 检查是否在item内（不在间距区域）
      if (xInGrid > _itemWidth)
        return -1;

      int col = lineIndex;
      return col * _rows + row;
    }
  }
}