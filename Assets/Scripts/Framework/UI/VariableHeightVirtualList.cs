using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 垂直异高虚拟列表。
/// 
/// 与 <see cref="VirtualList"/> 分开实现：VirtualList 负责固定尺寸的 vertical/horizontal/grid；
/// 本组件只处理单列垂直布局，并在渲染后缓存每个 Item 的实际高度。
/// </summary>
/// <remarks>
/// ItemTemplate 与 Content 都必须使用左上角锚点和 Pivot。
/// Item 的实际高度由渲染回调设置内容后，通过 LayoutUtility.GetPreferredHeight 测得。
/// 对包含 TMP 自动换行文本的 Item，根节点应配置 VerticalLayoutGroup + ContentSizeFitter
/// （Vertical Fit = Preferred Size），或提供可用的 ILayoutElement。
/// </remarks>
[RequireComponent(typeof(RectTransform))]
public class VariableHeightVirtualList : ScrollRect, IPointerClickHandler, IPointerDownHandler
{
  [Header("引用")]
  [Tooltip("异高 Item 模板。锚点与 Pivot 必须位于左上角。")]
  [SerializeField] private RectTransform itemTemplate;

  [Header("布局")]
  [Min(0f)]
  [SerializeField] private float spacing = 0f;
  [Min(1f)]
  [SerializeField] private float estimatedItemHeight = 80f;
  [Min(0f)]
  [SerializeField] private float bufferHeight = 200f;

  [Header("交互")]
  [Min(0f)]
  [SerializeField] private float clickThreshold = 10f;
  [Min(1f)]
  [SerializeField] private float scrollSpeed = 1000f;

  private IList _dataSource;
  private readonly Queue<RectTransform> _pool = new();
  private readonly List<RectTransform> _activeItems = new();
  private readonly List<float> _heights = new();
  private readonly List<float> _tops = new();
  private readonly List<float> _bottoms = new();
  private readonly Dictionary<RectTransform, int> _itemNameIndices = new();

  private Action<VirtualListRenderInfo> _renderHandler;
  private Action<VirtualListRenderInfo> _itemClickHandler;
  private Action<Vector2> _scrollChangedHandler;
  private Func<object, object> _selectionKeySelector;

  private int _selectedIndex = -1;
  private object _selectedKey;
  private bool _hasSelectedKey;
  private bool _isInitialized;
  private bool _isRefreshing;
  private Vector2 _pointerDownPosition;
  private Coroutine _scrollCoroutine;
  private Vector2 _lastViewportSize;
  private bool _hasViewportSize;

  private readonly VirtualListRenderInfo _renderInfo = new();

  public RectTransform ItemTemplate
  {
    get => itemTemplate;
    set
    {
      if (itemTemplate == value)
        return;

      itemTemplate = value;
      if (Application.isPlaying)
        InitializeTemplateAndRebuild();
    }
  }

  public float Spacing
  {
    get => spacing;
    set
    {
      value = Mathf.Max(0f, value);
      if (Mathf.Approximately(spacing, value))
        return;

      spacing = value;
      RebuildLayoutAndRefresh();
    }
  }

  public int Count => _dataSource?.Count ?? 0;

  public int SelectedIndex
  {
    get => _selectedIndex;
    set
    {
      if (SetSelectedIndex(value))
        RefreshVisible(true);
    }
  }

  protected override void Reset()
  {
    base.Reset();
    AutoAssignReferences();
  }

  protected override void Awake()
  {
    base.Awake();
    AutoAssignReferences();

    vertical = true;
    horizontal = false;
    onValueChanged.RemoveListener(OnScroll);
    onValueChanged.AddListener(OnScroll);

    InitializeTemplateAndRebuild();
  }

  protected override void Start()
  {
    base.Start();
    if (!Application.isPlaying)
      return;

    Canvas.ForceUpdateCanvases();
    ConfigureContentTransform();
    _isInitialized = true;
    RebuildLayoutAndRefresh();
  }

  protected override void OnDisable()
  {
    StopSmoothScroll();
    StopMovement();
    base.OnDisable();
  }

  protected override void OnDestroy()
  {
    onValueChanged.RemoveListener(OnScroll);
    ClearHandlers();
    base.OnDestroy();
  }

  protected override void OnRectTransformDimensionsChange()
  {
    base.OnRectTransformDimensionsChange();

    if (!Application.isPlaying || !_isInitialized || !isActiveAndEnabled || viewport == null)
      return;

    if (!_hasViewportSize || (viewport.rect.size - _lastViewportSize).sqrMagnitude > 0.0001f)
      StartCoroutine(RebuildAfterLayoutPass());
  }

  private IEnumerator RebuildAfterLayoutPass()
  {
    yield return null;

    if (_isInitialized && isActiveAndEnabled)
      RebuildLayoutAndRefresh();
  }

  /// <summary>
  /// 设置当前列表唯一的渲染、点击与滚动回调。重复调用会整体替换旧回调。
  /// </summary>
  public void SetHandlers(
    Action<VirtualListRenderInfo> renderHandler,
    Action<VirtualListRenderInfo> itemClickHandler = null,
    Action<Vector2> scrollChangedHandler = null)
  {
    _renderHandler = renderHandler;
    _itemClickHandler = itemClickHandler;
    _scrollChangedHandler = scrollChangedHandler;
  }

  public void ClearHandlers()
  {
    _renderHandler = null;
    _itemClickHandler = null;
    _scrollChangedHandler = null;
  }

  /// <summary>
  /// 设置用于数据重排后恢复选中项的稳定键。
  /// </summary>
  public void SetSelectionKeySelector(Func<object, object> selectionKeySelector)
  {
    _selectionKeySelector = selectionKeySelector;
    CacheSelectedKey();
  }

  /// <summary>
  /// 设置数据源。列表不复制数据，调用方修改数据后需再次调用本方法。
  /// </summary>
  public void RefreshData(IList datas)
  {
    _dataSource = datas;
    RestoreSelectedIndex();

    if (_isInitialized)
      RebuildLayoutAndRefresh();
  }

  public void Clear()
  {
    _dataSource = null;
    SetSelectedIndex(-1);

    if (_isInitialized)
      RebuildLayoutAndRefresh();
  }

  /// <summary>
  /// 强制重渲染可见项并重新测量其高度。
  /// 当可见项文字、图片或展开状态改变后调用。
  /// </summary>
  public void RefreshVisible()
  {
    RefreshVisible(true);
  }

  /// <summary>
  /// 将某一项的高度缓存恢复为预估值；如果该项当前可见，会在本次刷新中重新测量。
  /// </summary>
  public void InvalidateItemHeight(int index)
  {
    if (index < 0 || index >= Count || index >= _heights.Count)
      return;

    _heights[index] = estimatedItemHeight;
    RebuildLayoutCacheFrom(index);
    ClampContentPosition();
    RefreshVisible(true);
  }

  /// <summary>
  /// 直接更新某一项的已知高度，避免等待该项滚动到可见区域。
  /// </summary>
  public void SetItemHeight(int index, float height)
  {
    if (index < 0 || index >= Count || index >= _heights.Count)
      return;

    height = Mathf.Max(1f, height);
    if (Mathf.Abs(_heights[index] - height) < 0.01f)
      return;

    _heights[index] = height;
    RebuildLayoutCacheFrom(index);
    ClampContentPosition();
    RefreshVisible(true);
  }

  public void ScrollToIndex(
    int index,
    bool smooth = false,
    VirtualListScrollAlignment alignment = VirtualListScrollAlignment.Start)
  {
    if (!_isInitialized || index < 0 || index >= Count || viewport == null || content == null)
      return;

    StopSmoothScroll();
    StopMovement();

    float currentOffset = Mathf.Clamp(content.anchoredPosition.y, 0f, GetMaxScrollOffset());
    float targetOffset = GetScrollOffset(
      _tops[index],
      _heights[index],
      viewport.rect.height,
      currentOffset,
      alignment);
    targetOffset = Mathf.Clamp(targetOffset, 0f, GetMaxScrollOffset());

    if (Mathf.Abs(targetOffset - currentOffset) < 0.01f)
    {
      RefreshVisible(true);
      return;
    }

    if (smooth)
    {
      float duration = Mathf.Clamp01(Mathf.Abs(targetOffset - currentOffset) / scrollSpeed);
      _scrollCoroutine = StartCoroutine(SmoothScrollTo(targetOffset, duration));
      return;
    }

    SetScrollOffset(targetOffset);
    RefreshVisible(true);
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if (Application.isPlaying)
      _pointerDownPosition = eventData.position;
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    if (!Application.isPlaying || Count == 0 || content == null)
      return;

    if (Vector2.Distance(eventData.position, _pointerDownPosition) > clickThreshold)
      return;

    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
          content,
          eventData.position,
          eventData.pressEventCamera,
          out var localPoint))
      return;

    int index = FindItemAtOffset(-localPoint.y);
    if (index < 0)
      return;

    SetSelectedIndex(index);
    _renderInfo.index = index;
    _renderInfo.data = _dataSource[index];
    _renderInfo.selectedIndex = _selectedIndex;
    _renderInfo.itemTransform = null;
    _itemClickHandler?.Invoke(_renderInfo);
    RefreshVisible(true);
  }

  private void AutoAssignReferences()
  {
    if (viewport == null)
      viewport = transform.Find("Viewport") as RectTransform;

    if (content == null)
    {
      if (viewport != null)
        content = viewport.Find("Content") as RectTransform;

      if (content == null)
        content = transform.Find("Content") as RectTransform;
    }

    if (itemTemplate == null)
    {
      if (content != null)
        itemTemplate = content.Find("render") as RectTransform;

      if (itemTemplate == null)
        itemTemplate = transform.Find("render") as RectTransform;
    }
  }

  private void InitializeTemplateAndRebuild()
  {
    if (itemTemplate == null)
      return;

    if (Application.isPlaying)
      itemTemplate.gameObject.SetActive(false);

    if (_isInitialized)
      RebuildLayoutAndRefresh();
  }

  private void ConfigureContentTransform()
  {
    if (content == null)
      return;

    content.anchorMin = new Vector2(0f, 1f);
    content.anchorMax = new Vector2(0f, 1f);
    content.pivot = new Vector2(0f, 1f);
  }

  private void RebuildLayoutAndRefresh()
  {
    if (!_isInitialized || viewport == null || content == null || itemTemplate == null)
      return;

    ConfigureContentTransform();
    RebuildLayoutCache();
    ClampContentPosition();
    RefreshVisible(true);
    _lastViewportSize = viewport.rect.size;
    _hasViewportSize = true;
  }

  private void RebuildLayoutCache()
  {
    int count = Count;

    while (_heights.Count < count)
      _heights.Add(estimatedItemHeight);
    while (_heights.Count > count)
      _heights.RemoveAt(_heights.Count - 1);

    while (_tops.Count < count)
      _tops.Add(0f);
    while (_tops.Count > count)
      _tops.RemoveAt(_tops.Count - 1);

    while (_bottoms.Count < count)
      _bottoms.Add(0f);
    while (_bottoms.Count > count)
      _bottoms.RemoveAt(_bottoms.Count - 1);

    RebuildLayoutCacheFrom(0);
  }

  private void RebuildLayoutCacheFrom(int startIndex)
  {
    if (content == null)
      return;

    if (Count == 0)
    {
      content.sizeDelta = new Vector2(content.sizeDelta.x, 0f);
      return;
    }

    startIndex = Mathf.Clamp(startIndex, 0, Count - 1);
    float top = startIndex == 0 ? 0f : _bottoms[startIndex - 1] + spacing;

    for (int i = startIndex; i < Count; i++)
    {
      float height = Mathf.Max(1f, _heights[i]);
      _heights[i] = height;
      _tops[i] = top;
      _bottoms[i] = top + height;
      top = _bottoms[i] + spacing;
    }

    float contentHeight = Mathf.Max(0f, top - spacing);
    content.sizeDelta = new Vector2(viewport.rect.width, contentHeight);
  }

  private void OnScroll(Vector2 value)
  {
    RefreshVisible(false);
    _scrollChangedHandler?.Invoke(value);
  }

  private void RefreshVisible(bool force)
  {
    if (!_isInitialized || _isRefreshing || viewport == null || content == null || itemTemplate == null)
      return;

    _isRefreshing = true;
    try
    {
      // 一次渲染可能测出新的高度；随后最多再做两次布局收敛，覆盖换行文本和边界项。
      for (int pass = 0; pass < 3; pass++)
      {
        GetVisibleRange(out int startIndex, out int endIndex);
        int requiredCount = Mathf.Max(0, endIndex - startIndex + 1);
        EnsureActiveItemCount(requiredCount);

        int firstHeightChangedIndex = -1;
        for (int i = 0; i < requiredCount; i++)
        {
          int dataIndex = startIndex + i;
          if (RenderItem(_activeItems[i], dataIndex) && firstHeightChangedIndex < 0)
            firstHeightChangedIndex = dataIndex;
        }

        for (int i = requiredCount; i < _activeItems.Count; i++)
          _activeItems[i].gameObject.SetActive(false);

        if (firstHeightChangedIndex < 0)
          break;

        RebuildLayoutCacheFrom(firstHeightChangedIndex);
        ClampContentPosition();
        force = true;
      }
    }
    finally
    {
      _isRefreshing = false;
    }
  }

  private void GetVisibleRange(out int startIndex, out int endIndex)
  {
    startIndex = 0;
    endIndex = -1;
    if (Count == 0)
      return;

    float offset = Mathf.Clamp(content.anchoredPosition.y, 0f, GetMaxScrollOffset());
    float visibleEnd = offset + viewport.rect.height + bufferHeight;
    startIndex = FindFirstIndexWithBottomAfter(offset);

    int index = startIndex;
    while (index < Count && _tops[index] < visibleEnd)
      index++;

    endIndex = index - 1;
  }

  private void EnsureActiveItemCount(int count)
  {
    while (_activeItems.Count < count)
    {
      RectTransform item = GetPooledItem();
      if (item == null)
        break;

      item.gameObject.SetActive(false);
      _activeItems.Add(item);
    }

    while (_activeItems.Count > count)
    {
      int last = _activeItems.Count - 1;
      ReleaseItem(_activeItems[last]);
      _activeItems.RemoveAt(last);
    }
  }

  private RectTransform GetPooledItem()
  {
    while (_pool.Count > 0)
    {
      RectTransform pooled = _pool.Dequeue();
      if (pooled == null)
        continue;

      pooled.SetParent(content, false);
      return pooled;
    }

    return Instantiate(itemTemplate, content, false);
  }

  private void ReleaseItem(RectTransform item)
  {
    if (item == null)
      return;

    item.gameObject.SetActive(false);
    item.SetParent(content, false);
    _pool.Enqueue(item);
  }

  private bool RenderItem(RectTransform item, int dataIndex)
  {
    item.gameObject.SetActive(true);
    item.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewport.rect.width);
    item.anchoredPosition = new Vector2(0f, -_tops[dataIndex]);
    SetItemName(item, dataIndex);

    _renderInfo.index = dataIndex;
    _renderInfo.data = _dataSource[dataIndex];
    _renderInfo.selectedIndex = _selectedIndex;
    _renderInfo.itemTransform = item;
    _renderHandler?.Invoke(_renderInfo);

    LayoutRebuilder.ForceRebuildLayoutImmediate(item);
    float measuredHeight = MeasureItemHeight(item);
    if (Mathf.Abs(_heights[dataIndex] - measuredHeight) < 0.01f)
      return false;

    _heights[dataIndex] = measuredHeight;
    return true;
  }

  private static float MeasureItemHeight(RectTransform item)
  {
    float preferred = LayoutUtility.GetPreferredHeight(item);
    if (preferred > 0f)
      return preferred;

    return Mathf.Max(1f, item.rect.height);
  }

  private void SetItemName(RectTransform item, int index)
  {
    if (_itemNameIndices.TryGetValue(item, out int previous) && previous == index)
      return;

    item.name = "item" + index;
    _itemNameIndices[item] = index;
  }

  private int FindFirstIndexWithBottomAfter(float offset)
  {
    int low = 0;
    int high = Count - 1;

    while (low <= high)
    {
      int mid = low + (high - low) / 2;
      if (_bottoms[mid] <= offset)
        low = mid + 1;
      else
        high = mid - 1;
    }

    return Mathf.Clamp(low, 0, Count - 1);
  }

  private int FindItemAtOffset(float offset)
  {
    if (offset < 0f || Count == 0)
      return -1;

    int index = FindFirstIndexWithBottomAfter(offset);
    return offset >= _tops[index] && offset <= _bottoms[index] ? index : -1;
  }

  private float GetMaxScrollOffset()
  {
    return Mathf.Max(0f, content.rect.height - viewport.rect.height);
  }

  private void ClampContentPosition()
  {
    if (content == null || viewport == null)
      return;

    var position = content.anchoredPosition;
    position.y = Mathf.Clamp(position.y, 0f, GetMaxScrollOffset());
    content.anchoredPosition = position;
  }

  private void SetScrollOffset(float offset)
  {
    var position = content.anchoredPosition;
    position.y = offset;
    content.anchoredPosition = position;
  }

  private bool SetSelectedIndex(int index)
  {
    int validIndex = index >= 0 && index < Count ? index : -1;
    if (_selectedIndex == validIndex)
      return false;

    _selectedIndex = validIndex;
    CacheSelectedKey();
    return true;
  }

  private void CacheSelectedKey()
  {
    _selectedKey = null;
    _hasSelectedKey = false;

    if (_selectionKeySelector == null || _dataSource == null || _selectedIndex < 0 || _selectedIndex >= Count)
      return;

    object key = _selectionKeySelector(_dataSource[_selectedIndex]);
    if (key == null)
      return;

    _selectedKey = key;
    _hasSelectedKey = true;
  }

  private void RestoreSelectedIndex()
  {
    if (_selectionKeySelector != null && _hasSelectedKey)
    {
      for (int i = 0; i < Count; i++)
      {
        if (Equals(_selectedKey, _selectionKeySelector(_dataSource[i])))
        {
          _selectedIndex = i;
          return;
        }
      }

      _selectedIndex = -1;
      _selectedKey = null;
      _hasSelectedKey = false;
      return;
    }

    if (_selectedIndex >= Count)
      SetSelectedIndex(-1);
  }

  private static float GetScrollOffset(
    float itemStart,
    float itemHeight,
    float viewportHeight,
    float currentOffset,
    VirtualListScrollAlignment alignment)
  {
    switch (alignment)
    {
      case VirtualListScrollAlignment.Start:
        return itemStart;
      case VirtualListScrollAlignment.Center:
        return itemStart + itemHeight * 0.5f - viewportHeight * 0.5f;
      case VirtualListScrollAlignment.End:
        return itemStart + itemHeight - viewportHeight;
      default:
        float itemEnd = itemStart + itemHeight;
        float viewportEnd = currentOffset + viewportHeight;
        if (itemStart < currentOffset)
          return itemStart;
        if (itemEnd > viewportEnd)
          return itemEnd - viewportHeight;
        return currentOffset;
    }
  }

  private IEnumerator SmoothScrollTo(float targetOffset, float duration)
  {
    float startOffset = content.anchoredPosition.y;
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(elapsed / duration);
      t = t * t * (3f - 2f * t);
      SetScrollOffset(Mathf.Lerp(startOffset, targetOffset, t));
      RefreshVisible(false);
      yield return null;
    }

    SetScrollOffset(targetOffset);
    RefreshVisible(true);
    _scrollCoroutine = null;
  }

  private void StopSmoothScroll()
  {
    if (_scrollCoroutine == null)
      return;

    StopCoroutine(_scrollCoroutine);
    _scrollCoroutine = null;
  }
}
