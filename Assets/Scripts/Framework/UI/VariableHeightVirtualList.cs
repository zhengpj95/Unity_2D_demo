using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 垂直异高虚拟列表。
/// 
/// 与 <see cref="VirtualList"/> 分开实现：VirtualList 负责固定尺寸的 vertical/horizontal/grid；
/// 本组件只处理单列垂直布局，并在渲染后缓存每个 Item 的实际高度。
/// </summary>
/// <remarks>
/// 使用方法：
/// 1. 推荐从 Hierarchy 右键 GameObject/UI/Variable Height Virtual List 创建标准层级。
/// 2. 列表根节点挂载本组件；若需要响应点击，根节点需有开启 Raycast Target 的 Graphic（通常使用透明 Image）。
/// 3. Viewport 使用 RectMask2D 裁剪；Content 只保留 RectTransform，并使用左上角锚点与 Pivot。
///    不要给 Content 添加 VerticalLayoutGroup、ContentSizeFitter 等布局组件，本组件会直接管理 Content 高度与 Item 位置。
/// 4. ItemTemplate 使用左上角锚点与 Pivot，建议放在 Content 外并保持停用；运行时本组件会自动停用模板并创建对象池实例。
/// 5. 简单的单 TMP Item：限制 TMP 宽度、开启自动换行且不要限制高度，本组件会读取当前宽度下的首选文本高度。
/// 6. 包含多个控件的复杂 Item：推荐在 Item 根节点配置 VerticalLayoutGroup，让其首选高度汇总子节点；
///    也可配置 LayoutElement，并在渲染回调中设置 preferredHeight。ContentSizeFitter 只能配置在 Item 上，不能配置在 Content 上。
/// 7. 初始化顺序通常为 SetHandlers → 可选的 SetItemTemplateSelector/SetSelectionKeySelector → RefreshData。
///    持有列表回调的对象关闭或销毁时应调用 ClearHandlers，避免列表继续持有外部对象。
///
/// 高度测量优先级：Item 根节点的布局首选高度 → 简单 TMP 在限宽下的首选高度 → Item 当前 RectTransform 高度。
/// ItemTemplate 与所有备用模板都必须遵守相同的锚点、Pivot 和高度提供规则。
/// </remarks>
[RequireComponent(typeof(RectTransform))]
public class VariableHeightVirtualList : ScrollRect, IPointerClickHandler, IPointerDownHandler
{
  [Header("引用")]
  [Tooltip("默认异高 Item 模板。锚点与 Pivot 必须位于左上角。")]
  [SerializeField] private RectTransform itemTemplate;
  [Tooltip("可选的额外 Item 模板。通过 SetItemTemplateSelector 按数据选择模板。")]
  [SerializeField] private List<RectTransform> itemTemplates = new();

  [Header("布局")]
  [Min(0f)]
  [Tooltip("相邻 Item 的垂直间距，单位为 UI 像素。运行时可通过 Spacing 属性修改。")]
  [SerializeField] private float spacing = 0f;
  [Min(1f)]
  [Tooltip("尚未测量实际高度的 Item 所使用的初始高度，单位为 UI 像素。应接近常见 Item 高度以减少首次滚动时的布局跳动。")]
  [SerializeField] private float estimatedItemHeight = 80f;
  [Min(0f)]
  [Tooltip("视口上下额外保留的渲染范围，单位为 UI 像素。较大值可减少快速滚动时的显隐切换，但会增加同时激活的 Item 数量。")]
  [SerializeField] private float bufferHeight = 200f;

  [Header("交互")]
  [Min(0f)]
  [Tooltip("按下与抬起的屏幕距离不超过此值时，才认定为点击；超过则视为拖拽滚动，单位为像素。")]
  [SerializeField] private float clickThreshold = 10f;
  [Min(1f)]
  [Tooltip("调用 ScrollToIndex 并启用平滑滚动时的滚动速度，单位为 UI 像素/秒。")]
  [SerializeField] private float scrollSpeed = 1000f;

  private IList _dataSource;
  private readonly Dictionary<RectTransform, Queue<RectTransform>> _pools = new();
  private readonly Dictionary<RectTransform, RectTransform> _itemTemplateByInstance = new();
  private readonly List<RectTransform> _activeItems = new();
  private readonly List<float> _heights = new();
  private readonly List<float> _tops = new();
  private readonly List<float> _bottoms = new();
  private readonly Dictionary<RectTransform, int> _itemNameIndices = new();
  // 复用 TMP 收集缓冲，避免异高测量在滚动刷新时产生数组分配。
  private readonly List<TMP_Text> _textMeasureBuffer = new();

  private Action<VirtualListRenderInfo> _renderHandler;
  private Action<VirtualListRenderInfo> _itemClickHandler;
  private Action<Vector2> _scrollChangedHandler;
  private Func<object, object> _selectionKeySelector;
  private Func<object, int, RectTransform> _itemTemplateSelector;

  private int _selectedIndex = -1;
  private object _selectedKey;
  private bool _hasSelectedKey;
  private bool _isInitialized;
  private bool _isRefreshing;
  private Vector2 _pointerDownPosition;
  private Coroutine _scrollCoroutine;
  private Vector2 _lastViewportSize;
  private bool _hasViewportSize;

  private VirtualListRenderInfo _renderInfo = new();

  /// <summary>
  /// 获取或设置默认 Item 模板。
  /// 运行时更换模板会停用模板并重建当前可见项；传入的模板需使用左上角锚点与 Pivot。
  /// </summary>
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

  /// <summary>
  /// 获取 Inspector 配置的备用 Item 模板列表。
  /// 列表仅供读取；实际使用哪个模板由 <see cref="SetItemTemplateSelector"/> 的选择器决定。
  /// </summary>
  public IReadOnlyList<RectTransform> ItemTemplates
  {
    get
    {
      if (itemTemplates != null)
        return itemTemplates;

      return Array.Empty<RectTransform>();
    }
  }

  /// <summary>
  /// 获取或设置相邻 Item 之间的垂直间距。
  /// 设置负值时会自动限制为 0，并在初始化完成后重建布局。
  /// </summary>
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

  /// <summary>
  /// 当前数据源中的数据项数量；未设置数据源时为 0。
  /// </summary>
  public int Count => _dataSource?.Count ?? 0;

  /// <summary>
  /// 获取或设置当前选中项的索引。
  /// 无效索引会被视为未选中（-1）；设置成功后会刷新可见项以传递新的选中状态。
  /// </summary>
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
  /// <param name="renderHandler">可见 Item 的渲染回调；参数中的 <c>itemTransform</c> 为当前实例。</param>
  /// <param name="itemClickHandler">Item 点击回调；参数中的 <c>itemTransform</c> 为 null，避免暴露可能已复用的实例。</param>
  /// <param name="scrollChangedHandler">滚动位置变化回调，参数与 <see cref="ScrollRect.onValueChanged"/> 一致。</param>
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
  /// 清除渲染、点击和滚动回调。
  /// 适用于外部持有者释放或不再需要接收列表事件的场景。
  /// </summary>
  public void ClearHandlers()
  {
    _renderHandler = null;
    _itemClickHandler = null;
    _scrollChangedHandler = null;
  }

  /// <summary>
  /// 设置按数据选择 Item 模板的回调。返回 null 时会回退到默认 ItemTemplate。
  /// 支持不同模板各自独立对象池，例如普通文本、奖励卡和系统提示。
  /// </summary>
  /// <param name="itemTemplateSelector">接收数据和索引并返回模板的选择器；传入 null 可恢复仅使用默认模板。</param>
  public void SetItemTemplateSelector(Func<object, int, RectTransform> itemTemplateSelector)
  {
    _itemTemplateSelector = itemTemplateSelector;
    RefreshVisible(true);
  }

  /// <summary>
  /// 设置用于数据重排后恢复选中项的稳定键。
  /// </summary>
  /// <param name="selectionKeySelector">从数据项提取稳定且可比较的键；传入 null 后仅按当前索引保留选中状态。</param>
  public void SetSelectionKeySelector(Func<object, object> selectionKeySelector)
  {
    _selectionKeySelector = selectionKeySelector;
    CacheSelectedKey();
  }

  /// <summary>
  /// 设置数据源。列表不复制数据，调用方修改数据后需再次调用本方法。
  /// </summary>
  /// <param name="datas">数据源；可为 null，此时列表会显示为空。</param>
  public void RefreshData(IList datas)
  {
    _dataSource = datas;
    RestoreSelectedIndex();

    if (_isInitialized)
      RebuildLayoutAndRefresh();
  }

  /// <summary>
  /// 清空数据源与选中状态，并在运行时初始化完成后回收当前可见 Item。
  /// </summary>
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
  /// <param name="index">要失效的项索引；无效索引会被忽略。</param>
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
  /// <param name="index">要更新的项索引；无效索引会被忽略。</param>
  /// <param name="height">新的高度；小于 1 的值会被限制为 1。</param>
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

  /// <summary>
  /// 将列表滚动到指定索引，并按给定方式对齐目标 Item。
  /// 未初始化、索引无效或缺少必要 RectTransform 时会直接忽略本次调用。
  /// </summary>
  /// <param name="index">目标数据项索引。</param>
  /// <param name="smooth">是否使用基于 <see cref="Time.unscaledDeltaTime"/> 的平滑滚动。</param>
  /// <param name="alignment">目标 Item 在视口中的对齐方式。</param>
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

  /// <summary>
  /// 记录按下位置，供 <see cref="OnPointerClick"/> 区分点击与拖拽滚动。
  /// </summary>
  /// <param name="eventData">Unity EventSystem 传入的指针事件数据。</param>
  public void OnPointerDown(PointerEventData eventData)
  {
    if (Application.isPlaying)
      _pointerDownPosition = eventData.position;
  }

  /// <summary>
  /// 处理列表区域点击：命中数据项后更新选中索引并触发点击回调。
  /// 超过 <c>clickThreshold</c> 的移动会被视为滚动，不会触发点击。
  /// </summary>
  /// <param name="eventData">Unity EventSystem 传入的指针事件数据。</param>
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
    {
      itemTemplate.gameObject.SetActive(false);
      foreach (RectTransform template in itemTemplates)
      {
        if (template != null)
          template.gameObject.SetActive(false);
      }
    }

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
        EnsureActiveItemCount(startIndex, requiredCount);

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

  private void EnsureActiveItemCount(int startIndex, int count)
  {
    while (_activeItems.Count < count)
      _activeItems.Add(null);

    for (int i = 0; i < count; i++)
    {
      int dataIndex = startIndex + i;
      RectTransform expectedTemplate = GetItemTemplate(_dataSource[dataIndex], dataIndex);
      RectTransform item = _activeItems[i];

      if (item != null && _itemTemplateByInstance.TryGetValue(item, out RectTransform currentTemplate) &&
          currentTemplate == expectedTemplate)
        continue;

      ReleaseItem(item);
      _activeItems[i] = GetPooledItem(expectedTemplate);
    }

    while (_activeItems.Count > count)
    {
      int last = _activeItems.Count - 1;
      ReleaseItem(_activeItems[last]);
      _activeItems.RemoveAt(last);
    }
  }

  private RectTransform GetItemTemplate(object data, int index)
  {
    RectTransform selectedTemplate = _itemTemplateSelector?.Invoke(data, index);
    return selectedTemplate != null ? selectedTemplate : itemTemplate;
  }

  private RectTransform GetPooledItem(RectTransform template)
  {
    if (template == null)
      return null;

    if (_pools.TryGetValue(template, out Queue<RectTransform> pool))
    {
      while (pool.Count > 0)
      {
        RectTransform pooled = pool.Dequeue();
        if (pooled == null)
          continue;

        pooled.SetParent(content, false);
        return pooled;
      }
    }

    RectTransform item = Instantiate(template, content, false);
    _itemTemplateByInstance[item] = template;
    return item;
  }

  private void ReleaseItem(RectTransform item)
  {
    if (item == null)
      return;

    item.gameObject.SetActive(false);
    item.SetParent(content, false);

    if (!_itemTemplateByInstance.TryGetValue(item, out RectTransform template) || template == null)
      return;

    if (!_pools.TryGetValue(template, out Queue<RectTransform> pool))
    {
      pool = new Queue<RectTransform>();
      _pools.Add(template, pool);
    }

    pool.Enqueue(item);
  }

  private bool RenderItem(RectTransform item, int dataIndex)
  {
    item.gameObject.SetActive(true);
    item.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewport.rect.width);
    // 先应用缓存高度，确保对象池复用的 Item 不会保留上一条数据的视觉高度。
    item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _heights[dataIndex]);
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
    // LayoutElement 只提供首选高度，不会自行修改 RectTransform；这里同步视觉高度以匹配缓存位置。
    item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, measuredHeight);
    return true;
  }

  /// <summary>
  /// 测量 Item 的实际高度。
  /// 优先采用根节点布局组件的首选高度；纯 TMP 子节点未通过 LayoutGroup 上传高度时，
  /// 回退到 TMP 在当前限宽下的首选高度，支持仅限制宽度的自动换行文本模板。
  /// </summary>
  private float MeasureItemHeight(RectTransform item)
  {
    float preferred = LayoutUtility.GetPreferredHeight(item);
    if (preferred > 0f)
      return preferred;

    float textPreferred = MeasureTextPreferredHeight(item);
    if (textPreferred > 0f)
      return textPreferred;

    return Mathf.Max(1f, item.rect.height);
  }

  /// <summary>
  /// 计算 Item 内所有 TMP 文本在当前宽度约束下所需的最大高度。
  /// 根节点已配置 LayoutGroup、ContentSizeFitter 或 LayoutElement 时不会进入此分支。
  /// </summary>
  private float MeasureTextPreferredHeight(RectTransform item)
  {
    _textMeasureBuffer.Clear();
    item.GetComponentsInChildren<TMP_Text>(true, _textMeasureBuffer);

    float preferredHeight = 0f;
    foreach (TMP_Text text in _textMeasureBuffer)
    {
      if (text == null)
        continue;

      float width = text.rectTransform.rect.width;
      if (width <= 0f)
        width = item.rect.width;
      if (width <= 0f)
        continue;

      preferredHeight = Mathf.Max(preferredHeight, text.GetPreferredValues(width, 0f).y);
    }

    return preferredHeight;
  }

  private void SetItemName(RectTransform item, int index)
  {
    string templateName = "UnknownTemplate";
    if (_itemTemplateByInstance.TryGetValue(item, out RectTransform template) && template != null)
      templateName = template.name;

    // 在 Play Mode 的 Hierarchy 中同时显示数据索引与来源模板，便于检查多模板选择是否正确。
    string itemName = $"item{index} [{templateName}]";
    if (_itemNameIndices.TryGetValue(item, out int previous) && previous == index && item.name == itemName)
      return;

    item.name = itemName;
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
