using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 虚拟列表单元格的渲染与点击回调参数。
/// </summary>
/// <remarks>
/// 点击回调只传递数据与索引，<see cref="itemTransform"/> 可能为 null。
/// </remarks>
public struct VirtualListRenderInfo
{
  public int index;
  public object data;
  public int selectedIndex;
  public RectTransform itemTransform;
}

/// <summary>
/// VirtualList 的目标项滚动对齐方式。
/// </summary>
public enum VirtualListScrollAlignment
{
  /// <summary>目标项已完整可见时不移动，否则移动到最近可见位置。</summary>
  Nearest,
  /// <summary>垂直列表顶部对齐；水平列表左侧对齐。</summary>
  Start,
  /// <summary>主轴居中对齐。</summary>
  Center,
  /// <summary>垂直列表底部对齐；水平列表右侧对齐。</summary>
  End
}

/**
 * 虚拟列表，支持 vertical, horizontal, grid。
 * 布局约束：运行时会将 Content 固定为左上锚点与 Pivot；ItemTemplate 必须使用固定尺寸及左上锚点与 Pivot。
 * 当前实现不支持右到左、底部起始或反向滚动布局。
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
[RequireComponent(typeof(RectTransform))]
public class VirtualList : ScrollRect, IPointerClickHandler, IPointerDownHandler
{
  [Header("引用")]
  [Tooltip("固定尺寸模板，锚点与 Pivot 必须位于左上角。")]
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

  public RectTransform ItemTemplate
  {
    get => itemTemplate;
    set
    {
      if (itemTemplate == value) return;
      itemTemplate = value;
      ApplyLayoutSettings(true);
    }
  }

  public int SpaceX
  {
    get => spaceX;
    set
    {
      if (spaceX == value) return;
      spaceX = value;
      ApplyLayoutSettings(true);
    }
  }

  public int SpaceY
  {
    get => spaceY;
    set
    {
      if (spaceY == value) return;
      spaceY = value;
      ApplyLayoutSettings(true);
    }
  }

  public int RepeatX
  {
    get => repeatX;
    set
    {
      if (repeatX == value) return;
      repeatX = value;
      ApplyLayoutSettings(true);
    }
  }

  public int RepeatY
  {
    get => repeatY;
    set
    {
      if (repeatY == value) return;
      repeatY = value;
      ApplyLayoutSettings(true);
    }
  }

  private readonly List<object> _dataList = new();
  private readonly Queue<RectTransform> _pool = new();
  private readonly List<RectTransform> _visibleItems = new();
  // 仅在单元格复用到新数据索引时改名，避免选中态刷新重复创建字符串。
  private readonly Dictionary<RectTransform, int> _itemNameIndices = new();
#if UNITY_EDITOR
  private readonly List<RectTransform> _previewItems = new();
  private Transform _previewRoot;
  private bool _validateScheduled = false;
#endif

  // 每类回调仅保留一个处理者，通过 SetHandlers 原子替换，避免多个对象争夺列表所有权。
  private Action<VirtualListRenderInfo> _renderHandler;
  private Action<VirtualListRenderInfo> _itemClickHandler;
  private Action<Vector2> _scrollChangedHandler;

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
  /// 获取当前数据数量。
  /// </summary>
  public int Count => _dataList.Count;

  /// <summary>
  /// 统一设置当前列表的唯一回调处理者。重复调用会整体替换此前设置的全部回调。
  /// </summary>
  /// <param name="renderHandler">单元格渲染回调；不需要时传入 null。</param>
  /// <param name="itemClickHandler">单元格点击回调；不需要时传入 null。</param>
  /// <param name="scrollChangedHandler">滚动位置变化回调；不需要时传入 null。</param>
  public void SetHandlers(
    Action<VirtualListRenderInfo> renderHandler,
    Action<VirtualListRenderInfo> itemClickHandler = null,
    Action<Vector2> scrollChangedHandler = null)
  {
    _renderHandler = renderHandler;
    _itemClickHandler = itemClickHandler;
    _scrollChangedHandler = scrollChangedHandler;
  }

  /// <summary>
  /// 清空当前唯一处理者的全部回调。列表缓存时，绑定方应在关闭或销毁时调用。
  /// </summary>
  public void ClearHandlers()
  {
    _renderHandler = null;
    _itemClickHandler = null;
    _scrollChangedHandler = null;
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
  // Start 完成布局测量后才允许按 Viewport 尺寸重建，避免初始化阶段拿到零尺寸。
  private bool _isInitialized;
  // 上次实际参与布局计算的 Viewport 尺寸，用于过滤无效的尺寸变化通知。
  private Vector2 _lastViewportSize;
  private bool _hasViewportSize;
  // 同帧内合并多次 RectTransform 通知，等待 UI 布局稳定后再重建。
  private Coroutine _deferredLayoutRebuildCoroutine;

  private float _viewportHeight = 0;
  private float _viewportWidth = 0;

  private readonly int _bufferCnt = 2; // 额外缓存行或列数
  private float _itemHeight = 0;
  private float _itemWidth = 0;
  // 缓存的VirtualListRenderInfo，避免重复new
  private VirtualListRenderInfo _cachedRenderInfo = new VirtualListRenderInfo();
  // 当前进行的平滑滚动协程引用
  private Coroutine _scrollCoroutine = null;
  // 滚动速度（像素/秒），用于计算平滑滚动的持续时间
  private float _scrollSpeed = 1000f;
  private bool _hasLoggedInvalidLayout;

  /// <summary>
  /// 在编辑器中，Reset 会在组件添加到 GameObject 时被调用。用于自动分配引用。
  /// 官方API，编辑器专用回调，只执行一次。
  /// 它最适合做两件事：
  ///   自动获取和绑定组件引用（如 GetComponent、GetComponentInChildren）。
  ///   初始化序列化字段的默认值。
  /// </summary>
  /// <remarks>
  /// 不要把运行时初始化逻辑放在 Reset() 中，因为它在发布后的游戏（包括微信小游戏）中不会执行。运行时初始化应放在 Awake()、OnEnable() 或 Start() 中。
  /// </remarks>
#if UNITY_EDITOR
  protected override void Reset()
  {
    base.Reset();
    AutoAssignReferences();
  }
#endif

  private void AutoAssignReferences()
  {
    if (viewport == null)
    {
      var viewportTransform = transform.Find("Viewport");
      if (viewportTransform != null)
        viewport = viewportTransform as RectTransform;
    }

    if (content == null)
    {
      if (viewport != null)
      {
        var contentTransform = viewport.Find("Content");
        if (contentTransform != null)
          content = contentTransform as RectTransform;
      }

      if (content == null)
      {
        var direct = transform.Find("Content");
        if (direct != null)
          content = direct as RectTransform;
      }
    }

    // 先从Viewport下找render，再从Content下找render，最后直接在VirtualList下找render
    if (itemTemplate == null)
    {
      if (viewport != null)
      {
        var renderTransform = viewport.Find("render");
        if (renderTransform != null)
          itemTemplate = renderTransform as RectTransform;
      }

      if (itemTemplate == null && content != null)
      {
        var renderTransform = content.Find("render");
        if (renderTransform != null)
          itemTemplate = renderTransform as RectTransform;
      }

      if (itemTemplate == null)
      {
        var direct = transform.Find("render");
        if (direct != null)
          itemTemplate = direct as RectTransform;
      }
    }
  }

  protected override void Awake()
  {
    base.Awake();
    AutoAssignReferences();

    if (Application.isPlaying)
    {
      ClearPreviewItems();
    }

    InitItemTemplate();
    onValueChanged.RemoveListener(OnScroll);
    onValueChanged.AddListener(OnScroll);
    vertical = IsVertical;
    horizontal = !IsVertical;
  }

  protected override void Start()
  {
    base.Start();
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

    _isInitialized = true;
    RebuildLayoutAndRefresh();
  }

  /// <summary>
  /// 组件禁用时停止平滑滚动，避免关闭界面后协程继续修改 Content 位置。
  /// </summary>
  protected override void OnDisable()
  {
    StopSmoothScroll();
    StopDeferredLayoutRebuild();
    StopMovement();
    base.OnDisable();
  }

  /// <summary>
  /// Viewport 尺寸变化时重新计算可见单元格数量，适配分辨率、横竖屏和动态布局变化。
  /// </summary>
  protected override void OnRectTransformDimensionsChange()
  {
    base.OnRectTransformDimensionsChange();

    if (Application.isPlaying && _isInitialized)
      ScheduleLayoutRebuild();
  }

  /// <summary>
  /// 在编辑器中，OnValidate 会在 Inspector 面板中修改属性时被频繁调用。为了避免重复执行昂贵的操作，可以使用标志位或延迟调用机制来优化性能。
  /// </summary>
  /// <remarks>
  /// （OnValidate 属性发生变化就执行）
  /// </remarks>
#if UNITY_EDITOR
  protected override void OnValidate()
  {
    base.OnValidate();

    if (Application.isPlaying)
    {
      // Inspector 在运行时修改模板或布局字段时同样重建，不能复用旧模板创建的单元格。
      ApplyLayoutSettings(true);
      return;
    }

    AutoAssignReferences();

    if (content == null || itemTemplate == null)
      return;

#if UNITY_EDITOR
    if (!_validateScheduled)
    {
      _validateScheduled = true;
      EditorApplication.delayCall += ValidateDelayed;
    }
#endif
  }
#endif

#if UNITY_EDITOR
  private void ValidateDelayed()
  {
    _validateScheduled = false;
    if (this == null) return;
    if (Application.isPlaying) return;
    if (content == null || itemTemplate == null)
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
    if (Application.isPlaying)
    {
      itemTemplate.gameObject.SetActive(false);
    }
  }

  private void InitRect()
  {
    if (itemTemplate == null || viewport == null) return;
    if (!ValidateLayoutMetrics()) return;

    _viewportHeight = viewport.rect.height;
    _viewportWidth = viewport.rect.width;

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

  /// <summary>
  /// 销毁当前模板创建的可见项和缓存项。模板或布局参数变更后必须重建，避免复用旧结构。
  /// </summary>
  private void DestroyRuntimeItems()
  {
    foreach (var item in _visibleItems)
    {
      if (item != null)
        Destroy(item.gameObject);
    }

    _visibleItems.Clear();

    while (_pool.Count > 0)
    {
      var item = _pool.Dequeue();
      if (item != null)
        Destroy(item.gameObject);
    }

    _itemNameIndices.Clear();
    _startIndex = -1;
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

    if (datas != null)
    {
      foreach (var data in datas)
      {
        _dataList.Add(data);
      }
    }

    // 数据缩短或清空后，旧的选中索引不再有效时必须清除。
    if (_selectedIndex >= _dataList.Count)
      _selectedIndex = -1;

    // Start 前允许缓存数据；待完成 Viewport 测量后由 Start 统一创建并刷新单元格。
    if (_isInitialized)
      RebuildLayoutAndRefresh();
  }

  /// <summary>
  /// 清空列表数据、选中状态与当前显示内容。
  /// </summary>
  public void Clear()
  {
    _dataList.Clear();
    _selectedIndex = -1;

    if (_isInitialized)
      RebuildLayoutAndRefresh();
  }

  /// <summary>
  /// 强制重新渲染当前可见单元格，不改变数据、布局或滚动位置。
  /// </summary>
  public void RefreshVisible()
  {
    if (_isInitialized)
      RefreshVisible(true);
  }

  /// <summary>
  /// 根据当前 Viewport 和数据重建虚拟项，并将滚动位置限制在新的 Content 范围内。
  /// </summary>
  private void RebuildLayoutAndRefresh()
  {
    if (content == null || viewport == null || itemTemplate == null)
      return;

    if (!ValidateLayoutMetrics())
    {
      SetVisibleItemsActive(false);
      return;
    }

    InitRect();
    UpdateContentLayout();
    ClampContentPosition();
    RefreshVisible(true);
    CacheViewportSize();
  }

  /// <summary>
  /// 将尺寸变化合并到下一帧处理，避免布局系统在同一帧多次驱动时重复创建和渲染虚拟项。
  /// </summary>
  private void ScheduleLayoutRebuild()
  {
    if (_deferredLayoutRebuildCoroutine != null)
      return;

    _deferredLayoutRebuildCoroutine = StartCoroutine(RebuildLayoutAfterLayoutPass());
  }

  /// <summary>
  /// 等待当前 UI 布局完成后，只在 Viewport 实际尺寸变化时重建。
  /// </summary>
  private System.Collections.IEnumerator RebuildLayoutAfterLayoutPass()
  {
    yield return null;
    _deferredLayoutRebuildCoroutine = null;

    if (!_isInitialized || !HasViewportSizeChanged())
      yield break;

    RebuildLayoutAndRefresh();
  }

  /// <summary>
  /// 记录当前 Viewport 尺寸，作为后续尺寸变化过滤基准。
  /// </summary>
  private void CacheViewportSize()
  {
    if (viewport == null)
      return;

    _lastViewportSize = viewport.rect.size;
    _hasViewportSize = true;
  }

  /// <summary>
  /// 判断 Viewport 尺寸是否真实变化，忽略布局系统产生的重复通知。
  /// </summary>
  private bool HasViewportSizeChanged()
  {
    if (viewport == null)
      return false;

    return !_hasViewportSize || (viewport.rect.size - _lastViewportSize).sqrMagnitude > 0.0001f;
  }

  /// <summary>
  /// 校验单元格及其间距的步长，避免布局和点击计算发生除零或负步长。
  /// </summary>
  private bool ValidateLayoutMetrics()
  {
    bool isValid = _itemWidth > 0f && _itemHeight > 0f &&
                   _itemWidth + spaceX > 0f && _itemHeight + spaceY > 0f;
    if (isValid)
    {
      _hasLoggedInvalidLayout = false;
      return true;
    }

    if (!_hasLoggedInvalidLayout)
    {
      Debug.LogError("VirtualList layout is invalid: item size and item size plus spacing must be greater than zero.", this);
      _hasLoggedInvalidLayout = true;
    }

    return false;
  }

  /// <summary>
  /// 隐藏可见单元格，在非法布局参数下避免继续显示旧位置的内容。
  /// </summary>
  private void SetVisibleItemsActive(bool isActive)
  {
    foreach (var item in _visibleItems)
    {
      if (item != null)
        item.gameObject.SetActive(isActive);
    }
  }

  /// <summary>
  /// 数据量或 Viewport 改变后，限制 Content 锚点位置，避免停留在已不存在的列表区域。
  /// </summary>
  private void ClampContentPosition()
  {
    var anchoredPosition = content.anchoredPosition;

    if (IsVertical)
    {
      float maxY = Mathf.Max(0f, content.rect.height - _viewportHeight);
      anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, 0f, maxY);
    }
    else
    {
      float maxX = Mathf.Max(0f, content.rect.width - _viewportWidth);
      anchoredPosition.x = -Mathf.Clamp(-anchoredPosition.x, 0f, maxX);
    }

    content.anchoredPosition = anchoredPosition;
  }

  private void ApplyLayoutSettings(bool recreateRuntimeItems = false)
  {
    // 先清理旧实例：模板被置空时也不能让旧单元格继续显示。
    if (Application.isPlaying && recreateRuntimeItems && _isInitialized)
      DestroyRuntimeItems();

    if (content == null || itemTemplate == null) return;

#if UNITY_EDITOR
    if (!Application.isPlaying)
    {
      ClearPreviewItems();
    }
#endif

    if (!Application.isPlaying)
    {
      Canvas.ForceUpdateCanvases();
      InitRect();
      UpdateContentLayout();
      RefreshVisible(true);
      return;
    }

    InitItemTemplate();
    if (!_isInitialized)
      return;

    RebuildLayoutAndRefresh();
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
    _scrollChangedHandler?.Invoke(value);
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
    if (!ValidateLayoutMetrics())
      return;

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
    SetItemName(item, dataIndex);
    UpdateItemPosition(item, dataIndex);

    // 复用缓存的实例，避免重复new
    _cachedRenderInfo.index = dataIndex;
    _cachedRenderInfo.data = _dataList[dataIndex];
    _cachedRenderInfo.selectedIndex = _selectedIndex;
    _cachedRenderInfo.itemTransform = item;
    _renderHandler?.Invoke(_cachedRenderInfo);
  }

  /// <summary>
  /// 保持 item 名称可读；同一对象仍对应原索引时不重复创建名称字符串。
  /// </summary>
  private void SetItemName(RectTransform item, int dataIndex)
  {
    if (_itemNameIndices.TryGetValue(item, out int previousIndex) && previousIndex == dataIndex)
      return;

    item.name = "item" + dataIndex;
    _itemNameIndices[item] = dataIndex;
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
  /// 4. 调用单一点击处理回调
  /// </summary>
  public void OnPointerClick(PointerEventData eventData)
  {
    if (!Application.isPlaying)
      return;

    if (content == null)
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

      // 复用缓存的实例，避免重复new
      _cachedRenderInfo.index = clickedIndex;
      _cachedRenderInfo.data = _dataList[clickedIndex];
      _cachedRenderInfo.selectedIndex = _selectedIndex;
      _cachedRenderInfo.itemTransform = null;
      _itemClickHandler?.Invoke(_cachedRenderInfo);

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
    if (!ValidateLayoutMetrics())
      return -1;

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

      // 同时排除水平和垂直间距，避免点击空隙时误选相邻 item。
      float xInCell = xInGrid % (_itemWidth + spaceX);
      if (xInCell > _itemWidth || yInGrid > _itemHeight)
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

      // 同时排除水平和垂直间距，避免点击空隙时误选相邻 item。
      float yInCell = yInGrid % (_itemHeight + spaceY);
      if (xInGrid > _itemWidth || yInCell > _itemHeight)
        return -1;

      int col = lineIndex;
      return col * _rows + row;
    }
  }

  /// <remarks>
  /// 注意：调用此方法时，布局测量应已更新，以确保滚动位置计算正确。
  /// 若有问题，请在调用前使用 Canvas.ForceUpdateCanvases() 强制更新布局
  /// 或修改VirtualList的布局设置后，确保布局已更新再调用此方法。
  /// </remarks>
  /// <summary>
  /// 将指定项滚动到目标对齐位置。
  /// </summary>
  /// <param name="index">目标项索引。</param>
  /// <param name="smooth">是否平滑滚动。</param>
  /// <param name="alignment">垂直列表对应顶部、居中、底部；水平列表对应左侧、居中、右侧。</param>
  public void ScrollToIndex(int index, bool smooth = false, VirtualListScrollAlignment alignment = VirtualListScrollAlignment.Nearest)
  {
    if (content == null || itemTemplate == null) return;
    if (_dataList == null || index < 0 || index >= _dataList.Count) return;
    if (!ValidateLayoutMetrics()) return;

    StopSmoothScroll();
    StopMovement();

    Vector2 targetAnchored = content.anchoredPosition;
    float distance = 0f;

    if (IsVertical)
    {
      int row = index / _columns;
      float itemStart = row * (_itemHeight + spaceY);
      float target = GetScrollOffset(itemStart, _itemHeight, _viewportHeight, content.anchoredPosition.y, alignment);
      float max = Mathf.Max(0f, content.rect.height - _viewportHeight);
      float clamped = Mathf.Clamp(target, 0f, max);
      targetAnchored.y = clamped;
      distance = Mathf.Abs(targetAnchored.y - content.anchoredPosition.y);
    }
    else
    {
      int col = index / _rows;
      float itemStart = col * (_itemWidth + spaceX);
      float target = GetScrollOffset(itemStart, _itemWidth, _viewportWidth, -content.anchoredPosition.x, alignment);
      float max = Mathf.Max(0f, content.rect.width - _viewportWidth);
      float clamped = Mathf.Clamp(target, 0f, max);
      targetAnchored.x = -clamped;
      distance = Mathf.Abs(targetAnchored.x - content.anchoredPosition.x);
    }

    if (distance <= 0f)
    {
      RefreshVisible(true);
      return;
    }

    if (smooth)
    {
      float actualDuration = Mathf.Clamp01(distance / Mathf.Max(_scrollSpeed, 1f));
      _scrollCoroutine = StartCoroutine(SmoothScrollToAnchored(targetAnchored, actualDuration));
    }
    else
    {
      content.anchoredPosition = targetAnchored;
      RefreshVisible(true);
    }
  }

  /// <summary>
  /// 按对齐方式计算主轴滚动偏移；Nearest 在目标项已完整可见时保留当前位置。
  /// </summary>
  private static float GetScrollOffset(float itemStart, float itemSize, float viewportSize, float currentOffset, VirtualListScrollAlignment alignment)
  {
    switch (alignment)
    {
      case VirtualListScrollAlignment.Start:
        return itemStart;
      case VirtualListScrollAlignment.Center:
        return itemStart + itemSize * 0.5f - viewportSize * 0.5f;
      case VirtualListScrollAlignment.End:
        return itemStart + itemSize - viewportSize;
      default:
        float itemEnd = itemStart + itemSize;
        float viewportEnd = currentOffset + viewportSize;
        if (itemStart < currentOffset)
          return itemStart;
        if (itemEnd > viewportEnd)
          return itemEnd - viewportSize;
        return currentOffset;
    }
  }

  /// <summary>
  /// 平滑滚动到目标位置的协程
  /// </summary>
  /// <param name="targetAnchored">目标锚点位置</param>
  /// <param name="duration">滚动持续时间</param>
  /// <returns></returns>
  private System.Collections.IEnumerator SmoothScrollToAnchored(Vector2 targetAnchored, float duration)
  {
    Vector2 start = content.anchoredPosition;
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(elapsed / duration);
      // smoothstep easing
      t = t * t * (3f - 2f * t);
      content.anchoredPosition = Vector2.Lerp(start, targetAnchored, t);
      RefreshVisible(false);
      yield return null;
    }

    content.anchoredPosition = targetAnchored;
    RefreshVisible(true);
    _scrollCoroutine = null;
  }

  /// <summary>
  /// 停止正在执行的平滑滚动协程；可重复调用，供新滚动请求和生命周期收口复用。
  /// </summary>
  private void StopSmoothScroll()
  {
    if (_scrollCoroutine == null)
      return;

    StopCoroutine(_scrollCoroutine);
    _scrollCoroutine = null;
  }

  /// <summary>
  /// 停止等待布局完成的延迟重建协程，避免组件禁用后继续访问 UI 引用。
  /// </summary>
  private void StopDeferredLayoutRebuild()
  {
    if (_deferredLayoutRebuildCoroutine == null)
      return;

    StopCoroutine(_deferredLayoutRebuildCoroutine);
    _deferredLayoutRebuildCoroutine = null;
  }
}
