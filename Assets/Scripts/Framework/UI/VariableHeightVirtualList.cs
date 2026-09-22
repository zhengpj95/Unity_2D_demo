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
/// 使用方法：
/// 1. 推荐从 Hierarchy 右键 GameObject/UI/Variable Height Virtual List 创建标准层级。
/// 2. 列表根节点挂载本组件；若需要响应点击，根节点需有开启 Raycast Target 的 Graphic（通常使用透明 Image）。
/// 3. Viewport 使用 RectMask2D 裁剪；Content 只保留 RectTransform，并使用左上角锚点与 Pivot。
///    不要给 Content 添加 VerticalLayoutGroup、ContentSizeFitter 等布局组件，本组件会直接管理 Content 高度与 Item 位置。
/// 4. ItemTemplate 使用左上角锚点与 Pivot，建议放在 Content 外并保持停用；运行时本组件会自动停用模板并创建对象池实例。
/// 5. 代码控制高度：在 render 回调中对 info.itemTransform 调用 SetSizeWithCurrentAnchors(Axis.Vertical, height)。
///    每次根据数据设置绝对高度（不要累加当前高度）；对象池实例会被不同数据复用。
/// 6. 布局自适应：Item 根节点使用 VerticalLayoutGroup + ContentSizeFitter（Vertical Fit = Preferred Size），
///    列表在 render 后立即执行布局，并读取根节点实际高度。代码直接控制根高度时不要同时启用根节点的高度 Fitter。
///    只有 LayoutElement/LayoutGroup 而没有 Fitter 时，可调用 SetItemHeightResolver(info => LayoutUtility.GetPreferredHeight(info.itemTransform))。
///    混合模板也可在此回调中按数据选择测量方式；单 TMP 可使用 TMP 自身的 ContentSizeFitter 或在回调中计算高度。
/// 7. 初始化顺序通常为 SetHandlers → 可选的 SetItemTemplateSelector/SetSelectionKeySelector → RefreshData。
///    持有列表回调的对象关闭或销毁时应调用 ClearHandlers，避免列表继续持有外部对象。
///    如果选择器捕获了外部对象，也应调用 SetItemTemplateSelector(null)/SetSelectionKeySelector(null) 释放引用。
///
/// 高度来源：自定义高度回调的有效返回值优先，否则读取 render 和布局更新后的 Item 根节点实际高度。
/// 不自动扫描子 TMP 推断整个 Item 的尺寸；图片、文本、展开区域等组合内容由业务或 Item 布局组件汇总高度。
/// 异步内容加载完成后先更新数据，再调用 RefreshVisible；滚动时也会重复执行 render，不要在其中重复发起异步加载。
/// ItemTemplate 与所有备用模板都必须遵守相同的锚点、Pivot 和高度提供规则。
///
/// 阅读代码时可按以下顺序理解：
/// RefreshData（接收全部数据）→ RebuildLayoutCache（用高度算位置）→ RefreshVisible（决定哪些数据需要实例）。
/// RefreshVisible 内部：选模板/取对象池 → RenderItem（业务赋值）→ MeasureItemHeight（读取最终高度）→ 更新后续项坐标。
/// 三种“高度”不要混淆：estimatedItemHeight 只是未测量时的占位值；_heights 是排版缓存；Item.rect.height 是实际显示尺寸。
/// 所有公开操作都应在 Unity 主线程执行；索引从 0 开始，距离和高度使用 RectTransform 本地 UI 单位。
/// </remarks>
[RequireComponent(typeof(RectTransform))]
public class VariableHeightVirtualList : ScrollRect, IPointerClickHandler, IPointerDownHandler
{
  /// <summary>默认模板引用；仅用于克隆，不直接作为显示中的数据项。</summary>
  [Header("引用")]
  [Tooltip("默认异高 Item 模板。锚点与 Pivot 必须位于左上角。")]
  [SerializeField] private RectTransform itemTemplate;
  /// <summary>备用模板配置；填写此列表不会自动切换模板，还需要注册模板选择器。</summary>
  [Tooltip("可选的额外 Item 模板。通过 SetItemTemplateSelector 按数据选择模板。")]
  [SerializeField] private List<RectTransform> itemTemplates = new();

  /// <summary>两个相邻 Item 根节点之间的间隔；不包含 Item 内部的 Padding。</summary>
  [Header("布局")]
  [Min(0f)]
  [Tooltip("相邻 Item 的垂直间距，单位为 UI 像素。运行时可通过 Spacing 属性修改。")]
  [SerializeField] private float spacing = 0f;
  /// <summary>尚未渲染数据的占位高度，用于预先估算整个 Content 长度。</summary>
  [Min(1f)]
  [Tooltip("尚未测量实际高度的 Item 所使用的初始高度，单位为 UI 像素。应接近常见 Item 高度以减少首次滚动时的布局跳动。")]
  [SerializeField] private float estimatedItemHeight = 80f;
  /// <summary>在视口下方额外创建实例的距离；当前实现不向上方扩展缓冲范围。</summary>
  [Min(0f)]
  [Tooltip("视口下方额外保留的渲染范围，单位为 UI 本地单位。较大值可预先准备更多 Item，但会增加同时激活的实例数量。")]
  [SerializeField] private float bufferHeight = 200f;

  /// <summary>区分点击与拖拽的屏幕距离阈值；与 UI 本地尺寸不同，这里使用屏幕像素。</summary>
  [Header("交互")]
  [Min(0f)]
  [Tooltip("按下与抬起的屏幕距离不超过此值时，才认定为点击；超过则视为拖拽滚动，单位为像素。")]
  [SerializeField] private float clickThreshold = 10f;
  /// <summary>平滑滚动的参考速度，用于估算时长；时长最多 1 秒，动画采用缓入缓出而非匀速。</summary>
  [Min(1f)]
  [Tooltip("ScrollToIndex 平滑滚动的参考速度，单位为 UI 本地单位/秒；按距离估算时长并限制为最多 1 秒。")]
  [SerializeField] private float scrollSpeed = 1000f;

  // 数据源保留调用方的引用；增删或重排后必须通过 RefreshData 重建索引缓存。
  private IList _dataSource;
  // 每种模板有独立的空闲队列，保证取出的实例拥有正确的控件层级。
  private readonly Dictionary<RectTransform, Queue<RectTransform>> _pools = new();
  // 记录每个克隆实例的来源；回收、切换模板、Hierarchy 命名都依赖此映射。
  private readonly Dictionary<RectTransform, RectTransform> _itemTemplateByInstance = new();
  // 只存当前可见范围（含下方缓冲）的实例，按数据顺序排列；其下标不是全量数据索引。
  private readonly List<RectTransform> _activeItems = new();
  // 以下四个列表都按全量数据索引访问，数量与 Count 一致。
  // 每项的排版高度，可能是预估值、测量结果，或调用方通过 SetItemHeight 写入的值。
  private readonly List<float> _heights = new();
  // true 表示已有可用高度；false 时 render 从来源模板的原始高度开始，避免继承其他数据的尺寸。
  private readonly List<bool> _hasMeasuredHeights = new();
  // 从 Content 顶部向下计算的 Item 上边缘距离（正数），实际 anchoredPosition.y 使用相反数。
  private readonly List<float> _tops = new();
  // Item 下边缘距离，等于 top + height；按顺序递增，因此可二分定位可见项。
  private readonly List<float> _bottoms = new();
  // 同时记录实例上次绑定的数据索引和命名索引；用于跳过不必要的 render 和字符串拼接。
  private readonly Dictionary<RectTransform, int> _itemNameIndices = new();
  // 可选的业务高度计算器；在内容绑定与布局完成后调用，不负责修改业务数据。
  private Func<VirtualListRenderInfo, float> _itemHeightResolver;
  // 当前同步刷新已达到三轮上限，LateUpdate 需要继续补齐新出现的可见项。
  private bool _refreshPending;

  // 业务负责填充运行时 Item 的内容、状态和代码控制的高度。
  private Action<VirtualListRenderInfo> _renderHandler;
  // 点击时只传数据与索引，不将可被复用的 Item 实例交给点击处理者。
  private Action<VirtualListRenderInfo> _itemClickHandler;
  // 转发 ScrollRect 的归一化滚动位置。
  private Action<Vector2> _scrollChangedHandler;
  // 提取稳定业务标识，使数据重排后仍可选中同一条数据。
  private Func<object, object> _selectionKeySelector;
  // 根据数据和索引选择模板；返回 null 代表使用默认模板。
  private Func<object, int, RectTransform> _itemTemplateSelector;

  // 当前选中的数据索引；-1 代表无选中项。
  private int _selectedIndex = -1;
  // 上次选中数据的稳定键，用于 RefreshData 后查找新索引。
  private object _selectedKey;
  // 选择器不存在或返回 null 时为 false，此时仅按索引保留选择。
  private bool _hasSelectedKey;
  // Start 完成初始 Canvas 布局后才允许执行可见项计算。
  private bool _isInitialized;
  // 防止修改 Content 尺寸/位置触发滚动事件后，递归进入刷新流程。
  private bool _isRefreshing;
  // 按下时的屏幕坐标，与点击位置比较以过滤拖拽。
  private Vector2 _pointerDownPosition;
  // 当前 ScrollToIndex 平滑滚动任务；新跳转或停用时取消。
  private Coroutine _scrollCoroutine;
  // 上次完整布局使用的视口尺寸，用于识别宽度/高度变化。
  private Vector2 _lastViewportSize;
  // 区分“还没记录视口尺寸”和“尺寸恰好为零”。
  private bool _hasViewportSize;

  // 可变结构体工作区；每次填满后按值传给回调，不代表一个持久 Item 对象。
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

  /// <summary>编辑器添加/重置组件时，按约定名称尝试补齐引用；已绑定字段保持原值。</summary>
  protected override void Reset()
  {
    base.Reset();
    AutoAssignReferences();
  }

  /// <summary>建立引用、限定单列垂直滚动并注册内部滚动监听；实际布局延后到 Start。</summary>
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

  /// <summary>等待初始 Canvas 尺寸可用，再根据数据建立高度缓存和第一批可见实例。</summary>
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

  /// <summary>停止本组件的滚动动画、惯性和待续刷新；保留数据与对象池，供后续重新显示。</summary>
  protected override void OnDisable()
  {
    _refreshPending = false;
    StopSmoothScroll();
    StopMovement();
    base.OnDisable();
  }

  /// <summary>大幅缩短 Item 后可能需要更多实例填满视口；将剩余收敛工作推迟一帧，避免无限同步循环。</summary>
  protected override void LateUpdate()
  {
    base.LateUpdate();
    if (_refreshPending && isActiveAndEnabled)
    {
      _refreshPending = false;
      RefreshVisible(false);
    }
  }

  /// <summary>解除内部滚动监听并释放业务回调引用；子 Item 随所属 GameObject 层级销毁。</summary>
  protected override void OnDestroy()
  {
    onValueChanged.RemoveListener(OnScroll);
    ClearHandlers();
    base.OnDestroy();
  }

  /// <summary>检测视口尺寸变化，延后一帧重排，避开父级布局尚未完成的中间尺寸。</summary>
  protected override void OnRectTransformDimensionsChange()
  {
    base.OnRectTransformDimensionsChange();

    if (!Application.isPlaying || !_isInitialized || !isActiveAndEnabled || viewport == null)
      return;

    if (!_hasViewportSize || (viewport.rect.size - _lastViewportSize).sqrMagnitude > 0.0001f)
      StartCoroutine(RebuildAfterLayoutPass());
  }

  /// <summary>等待下一帧后检查组件仍可用，再按新的视口尺寸刷新。</summary>
  /// <returns>等待一帧的协程迭代器。</returns>
  private IEnumerator RebuildAfterLayoutPass()
  {
    yield return null;

    if (_isInitialized && isActiveAndEnabled)
      RebuildLayoutAndRefresh();
  }

  /// <summary>
  /// 设置当前列表唯一的渲染、点击与滚动回调。重复调用会整体替换旧回调。
  /// </summary>
  /// <param name="renderHandler">内容绑定回调，可直接修改 itemTransform 高度；按数据写入完整状态，勿累加尺寸或在回调内更换数据源。</param>
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
  /// 清除渲染、点击、滚动和自定义高度回调；模板与选中键选择器由对应 Set 方法单独清理。
  /// 适用于外部持有者释放或不再需要接收列表事件的场景。
  /// </summary>
  public void ClearHandlers()
  {
    _renderHandler = null;
    _itemClickHandler = null;
    _scrollChangedHandler = null;
    _itemHeightResolver = null;
  }

  /// <summary>
  /// 设置自定义高度计算器，用于布局首选高度、图文组合或不同模板混合的测量。
  /// 在 render 和 Item 布局更新后执行；返回有限正数时作为最终高度，其他值回退到根节点实际高度。
  /// 计算器应只测量，不修改数据源或触发列表刷新；不需要立即刷新，下一次 RefreshData/RefreshVisible 时生效。
  /// </summary>
  /// <param name="heightResolver">接收当前数据、索引及运行时实例的计算器；null 恢复读取根节点实际高度。</param>
  public void SetItemHeightResolver(Func<VirtualListRenderInfo, float> heightResolver)
  {
    _itemHeightResolver = heightResolver;
  }

  /// <summary>
  /// 设置按数据选择 Item 模板的回调。返回 null 时会回退到默认 ItemTemplate。
  /// 支持不同模板各自独立对象池，例如普通文本、奖励卡和系统提示。
  /// </summary>
  /// <param name="itemTemplateSelector">接收数据和索引并返回模板的选择器；传入 null 可恢复仅使用默认模板。</param>
  public void SetItemTemplateSelector(Func<object, int, RectTransform> itemTemplateSelector)
  {
    _itemTemplateSelector = itemTemplateSelector;
    // 同一数据现在可能对应不同模板，首次渲染应以新模板尺寸为起点。
    for (int i = 0; i < _hasMeasuredHeights.Count; i++)
      _hasMeasuredHeights[i] = false;
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
    // 数据替换/重排后旧索引的高度不再可信，可见项将在 render 后重新测量。
    _heights.Clear();
    _hasMeasuredHeights.Clear();

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
  /// 应先更新数据，再刷新；仅修改实例而未更新数据，会在下一次 render 时被业务赋值覆盖。
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
    _hasMeasuredHeights[index] = false;
    RebuildLayoutCacheFrom(index);
    ClampContentPosition();
    RefreshVisible(true);
  }

  /// <summary>
  /// 直接更新某一项的已知高度，避免等待该项滚动到可见区域。
  /// 此值是布局缓存；可见时仍以 render/高度计算器的结果为准。建议先更新数据，再调用本方法。
  /// </summary>
  /// <param name="index">要更新的项索引；无效索引会被忽略。</param>
  /// <param name="height">新的高度；小于 1 的值会被限制为 1，NaN/Infinity 会记录警告并忽略。</param>
  public void SetItemHeight(int index, float height)
  {
    if (index < 0 || index >= Count || index >= _heights.Count)
      return;

    if (float.IsNaN(height) || float.IsInfinity(height))
    {
      Debug.LogWarning($"[VariableHeightVirtualList] SetItemHeight 参数无效，index={index}，height={height}。", this);
      return;
    }

    _hasMeasuredHeights[index] = true;
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

    // Content 以左上角为原点：向下的本地 y 为负，缓存距离向下为正。
    int index = FindItemAtOffset(-localPoint.y);
    if (index < 0)
      return;

    SetSelectedIndex(index);
    _renderInfo.index = index;
    _renderInfo.data = _dataSource[index];
    _renderInfo.selectedIndex = _selectedIndex;
    _renderInfo.itemTransform = null;
    _itemClickHandler?.Invoke(_renderInfo);
    // 点击回调可能改变展开状态；回调结束后统一刷新，将新数据转换成尺寸与位置。
    RefreshVisible(true);
  }

  /// <summary>仅为缺失引用按 Viewport/Content/render 名称查找子节点；自定义命名应在 Inspector 手动绑定。</summary>
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

  /// <summary>模板变化后使已测高度失效，隐藏作为克隆来源的模板，并在初始化完成时重排。</summary>
  private void InitializeTemplateAndRebuild()
  {
    if (itemTemplate == null)
      return;

    for (int i = 0; i < _hasMeasuredHeights.Count; i++)
      _hasMeasuredHeights[i] = false;

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

  /// <summary>将 Content 固定为左上角锚点/Pivot，使高度累计、点击换算与 Item 定位使用同一坐标系。</summary>
  private void ConfigureContentTransform()
  {
    if (content == null)
      return;

    content.anchorMin = new Vector2(0f, 1f);
    content.anchorMax = new Vector2(0f, 1f);
    content.pivot = new Vector2(0f, 1f);
  }

  /// <summary>完整布局入口：检查引用、重建缓存、限制滚动范围，再重新绑定可见项。</summary>
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

  /// <summary>让四组全量索引缓存与数据数量一致；新增项先用预估高度，已有项保留缓存。</summary>
  private void RebuildLayoutCache()
  {
    int count = Count;

    while (_heights.Count < count)
      _heights.Add(estimatedItemHeight);
    while (_heights.Count > count)
      _heights.RemoveAt(_heights.Count - 1);
    while (_hasMeasuredHeights.Count < count)
      _hasMeasuredHeights.Add(false);
    while (_hasMeasuredHeights.Count > count)
      _hasMeasuredHeights.RemoveAt(_hasMeasuredHeights.Count - 1);

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

  /// <summary>从指定项起累加高度与间距，并更新 Content 的总高度；前面的项不受影响。</summary>
  /// <param name="startIndex">最早发生高度变化的索引，会限制到有效范围；空列表直接将 Content 高度清零。</param>
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

    // 最后一项底部不需要再加一个间距，所以减掉循环末尾多计入的 spacing。
    float contentHeight = Mathf.Max(0f, top - spacing);
    content.sizeDelta = new Vector2(viewport.rect.width, contentHeight);
  }

  /// <summary>ScrollRect 位置变化时更新进入/离开视口的数据，并转发滚动通知。</summary>
  /// <param name="value">ScrollRect 的归一化位置，不是 Content 的本地距离。</param>
  private void OnScroll(Vector2 value)
  {
    RefreshVisible(false);
    _scrollChangedHandler?.Invoke(value);
  }

  /// <summary>
  /// 虚拟化主循环：选取索引范围、准备实例、绑定内容并测量，随后用新高度重新排列。
  /// 高度变化可能暴露更多数据，因此最多同步处理三轮，剩余工作交给 LateUpdate。
  /// </summary>
  /// <param name="force">true 强制首轮重绑所有可见数据；false 仅绑定新索引/新实例，复用已有显示结果。</param>
  private void RefreshVisible(bool force)
  {
    if (!_isInitialized || _isRefreshing || viewport == null || content == null || itemTemplate == null)
      return;

    _isRefreshing = true;
    _refreshPending = false;
    try
    {
      // 一次渲染可能测出新的高度；随后最多再做两次布局收敛，覆盖换行文本和边界项。
      for (int pass = 0; pass < 3; pass++)
      {
        GetVisibleRange(out int startIndex, out int endIndex);
        int requiredCount = Mathf.Max(0, endIndex - startIndex + 1);
        EnsureActiveItemCount(startIndex, requiredCount);

        // 范围按索引递增遍历，记录第一个变化项即可确定需要重算的整段后续坐标。
        int firstHeightChangedIndex = -1;
        for (int i = 0; i < requiredCount; i++)
        {
          int dataIndex = startIndex + i;
          RectTransform item = _activeItems[i];
          // 重排只移动已绑定的实例；滚动进入新数据或显式刷新时才重新绑定内容，避免一帧反复调用 render。
          bool needsRender = force || !_itemNameIndices.TryGetValue(item, out int boundIndex) || boundIndex != dataIndex;
          if (needsRender && RenderItem(item, dataIndex) && firstHeightChangedIndex < 0)
            firstHeightChangedIndex = dataIndex;
          item.anchoredPosition = new Vector2(0f, -_tops[dataIndex]);
        }

        if (firstHeightChangedIndex < 0)
          break;

        RebuildLayoutCacheFrom(firstHeightChangedIndex);
        ClampContentPosition();
        // 每轮都立即同步所有坐标；即便达到本帧迭代上限，也不能让后续项停留在旧坐标。
        for (int i = 0; i < requiredCount; i++)
          _activeItems[i].anchoredPosition = new Vector2(0f, -_tops[startIndex + i]);

        if (pass == 2)
          _refreshPending = true;
        force = false;
      }
    }
    finally
    {
      // 即便业务回调抛出异常，也要解除重入保护，避免此列表永久无法刷新。
      _isRefreshing = false;
    }
  }

  /// <summary>根据缓存计算需要实例化的数据范围；包含部分露出的首项及视口下方缓冲。</summary>
  /// <param name="startIndex">首项索引（包含）。</param>
  /// <param name="endIndex">末项索引（包含）；空列表为 -1，代表无需实例。</param>
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

  /// <summary>使显示槽位数和每个槽位的模板匹配当前范围；模板不同时先归还旧实例再取新实例。</summary>
  /// <param name="startIndex">第一个槽位对应的全量数据索引。</param>
  /// <param name="count">需要的槽位数量；为 0 时归还所有显示实例。</param>
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

  /// <summary>调用业务模板选择器并应用默认模板回退规则。</summary>
  /// <param name="data">当前数据项。</param>
  /// <param name="index">当前数据索引。</param>
  /// <returns>选择器返回的有效模板，或默认 ItemTemplate。</returns>
  private RectTransform GetItemTemplate(object data, int index)
  {
    RectTransform selectedTemplate = _itemTemplateSelector?.Invoke(data, index);
    return selectedTemplate != null ? selectedTemplate : itemTemplate;
  }

  /// <summary>从指定模板的队列获取实例；池为空则克隆模板。激活与内容绑定由 RenderItem 完成。</summary>
  /// <param name="template">克隆来源模板。</param>
  /// <returns>Content 下的运行时实例；模板为 null 时返回 null。</returns>
  private RectTransform GetPooledItem(RectTransform template)
  {
    if (template == null)
      return null;

    if (_pools.TryGetValue(template, out Queue<RectTransform> pool))
    {
      while (pool.Count > 0)
      {
        RectTransform pooled = pool.Dequeue();
        // Unity 对象可能被外部销毁；跳过队列中已经失效的引用。
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

  /// <summary>隐藏实例、清除旧绑定索引并归还来源模板的队列；保留实例和组件供下次复用。</summary>
  /// <param name="item">待回收实例；null 会被忽略，来源失效的实例只隐藏而不入池。</param>
  private void ReleaseItem(RectTransform item)
  {
    if (item == null)
      return;

    item.gameObject.SetActive(false);
    _itemNameIndices.Remove(item);
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

  /// <summary>
  /// 单个实例的显示流水线：设宽度和初始高度 → 业务 render → Unity 布局 → 高度计算器 → 尺寸与缓存同步。
  /// 本方法只报告高度是否变化，后续 Item 的位置由 RefreshVisible 统一重算。
  /// </summary>
  /// <param name="item">已经选择好模板的运行时实例，不是模板资源。</param>
  /// <param name="dataIndex">要绑定的全量数据索引。</param>
  /// <returns>高度与原缓存相差至少 0.01 时为 true，表示需要重排后续项。</returns>
  private bool RenderItem(RectTransform item, int dataIndex)
  {
    item.gameObject.SetActive(true);
    item.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewport.rect.width);
    // 缓存仅作为渲染输入；render 中设置的新高度不会再被子 TMP/布局首选值覆盖。
    float initialHeight = _hasMeasuredHeights[dataIndex]
      ? _heights[dataIndex]
      : _itemTemplateByInstance[item].rect.height;
    item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, initialHeight);
    item.anchoredPosition = new Vector2(0f, -_tops[dataIndex]);
    SetItemName(item, dataIndex);

    _renderInfo.index = dataIndex;
    _renderInfo.data = _dataSource[dataIndex];
    _renderInfo.selectedIndex = _selectedIndex;
    _renderInfo.itemTransform = item;
    _renderHandler?.Invoke(_renderInfo);

    // 业务刚修改的文本/子节点/布局参数需要先生效，之后才能读取根节点最终高度。
    LayoutRebuilder.ForceRebuildLayoutImmediate(item);
    float measuredHeight = MeasureItemHeight(_renderInfo);
    // 自定义测量器可能只返回高度；仅在实际尺寸不一致时再次布局，让子控件适配新高度。
    if (!Mathf.Approximately(item.rect.height, measuredHeight))
    {
      item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, measuredHeight);
      LayoutRebuilder.ForceRebuildLayoutImmediate(item);
    }
    bool heightChanged = Mathf.Abs(_heights[dataIndex] - measuredHeight) >= 0.01f;
    _heights[dataIndex] = measuredHeight;
    _hasMeasuredHeights[dataIndex] = true;
    return heightChanged;
  }

  /// <summary>
  /// 高度由业务计算器或根节点实际尺寸提供，避免把内部某个文本/图片的首选尺寸误当成整个 Item 的高度。
  /// </summary>
  /// <param name="info">已完成业务 render 和第一次布局的实例及其数据。</param>
  /// <returns>用于排版的高度，至少为 1；无效结果按根节点高度、预估高度的顺序回退。</returns>
  private float MeasureItemHeight(VirtualListRenderInfo info)
  {
    float height = _itemHeightResolver != null ? _itemHeightResolver(info) : 0f;
    if (float.IsNaN(height) || float.IsInfinity(height) || height <= 0f)
      height = info.itemTransform.rect.height;

    // 防止错误尺寸污染后续所有索引的累计高度与二分查找。
    if (float.IsNaN(height) || float.IsInfinity(height) || height <= 0f)
    {
      Debug.LogWarning($"[VariableHeightVirtualList] Item 高度无效，index={info.index}，height={height}，使用预估高度。", this);
      height = estimatedItemHeight;
    }
    return Mathf.Max(1f, height);
  }

  /// <summary>将 Hierarchy 名称更新为“数据索引 + 来源模板”，同时记录该实例已绑定的索引。</summary>
  /// <param name="item">运行时实例。</param>
  /// <param name="index">当前数据索引。</param>
  private void SetItemName(RectTransform item, int index)
  {
    // 同一实例的来源模板固定；索引未变时无需重复拼接名称，避免滚动过程中产生字符串分配。
    if (_itemNameIndices.TryGetValue(item, out int previous) && previous == index)
      return;

    string templateName = "UnknownTemplate";
    if (_itemTemplateByInstance.TryGetValue(item, out RectTransform template) && template != null)
      templateName = template.name;

    // 在 Play Mode 的 Hierarchy 中同时显示数据索引与来源模板，便于检查多模板选择是否正确。
    string itemName = $"item{index} [{templateName}]";
    item.name = itemName;
    _itemNameIndices[item] = index;
  }

  /// <summary>在递增的下边缘缓存中二分查找第一个尚未完全越过指定距离的 Item。</summary>
  /// <param name="offset">从 Content 顶部向下的距离；调用方需保证 Count 大于 0。</param>
  /// <returns>限制在有效数据范围内的候选索引；是否命中项本体还需要检查上下边缘。</returns>
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

  /// <summary>根据垂直距离查找点击项；落在间距、顶部外或尾部外时不算命中。</summary>
  /// <param name="offset">从 Content 顶部向下的本地距离。</param>
  /// <returns>命中的数据索引；未命中为 -1。</returns>
  private int FindItemAtOffset(float offset)
  {
    if (offset < 0f || Count == 0)
      return -1;

    int index = FindFirstIndexWithBottomAfter(offset);
    return offset >= _tops[index] && offset <= _bottoms[index] ? index : -1;
  }

  /// <summary>计算滚动到最底部的偏移；内容不足一屏时返回 0。</summary>
  /// <returns>Content 高度减去视口高度后的非负距离。</returns>
  private float GetMaxScrollOffset()
  {
    return Mathf.Max(0f, content.rect.height - viewport.rect.height);
  }

  /// <summary>高度缩短或视口变大后，将 Content 拉回有效范围，避免停留在内容之外。</summary>
  private void ClampContentPosition()
  {
    if (content == null || viewport == null)
      return;

    var position = content.anchoredPosition;
    position.y = Mathf.Clamp(position.y, 0f, GetMaxScrollOffset());
    content.anchoredPosition = position;
  }

  /// <summary>只写入 Content 的垂直位置；不限制范围也不主动 render，由调用方负责。</summary>
  /// <param name="offset">向下滚动的本地距离，对应 Content 正的 anchoredPosition.y。</param>
  private void SetScrollOffset(float offset)
  {
    var position = content.anchoredPosition;
    position.y = offset;
    content.anchoredPosition = position;
  }

  /// <summary>校验并保存选择，同时缓存稳定键；不主动刷新，供属性赋值和点击流程分别调用。</summary>
  /// <param name="index">候选索引；越界统一转换为 -1。</param>
  /// <returns>选择确实发生变化时返回 true。</returns>
  private bool SetSelectedIndex(int index)
  {
    int validIndex = index >= 0 && index < Count ? index : -1;
    if (_selectedIndex == validIndex)
      return false;

    _selectedIndex = validIndex;
    CacheSelectedKey();
    return true;
  }

  /// <summary>保存当前选中数据的稳定标识；未选中、未配置选择器或键为 null 时清空缓存。</summary>
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

  /// <summary>更换数据源后优先按稳定键寻找第一条相等数据；找不到则取消选择，无键时保留合法索引。</summary>
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

  /// <summary>按对齐方式求目标滚动距离；只计算几何关系，最终边界限制由 ScrollToIndex 处理。</summary>
  /// <param name="itemStart">目标项顶部距 Content 顶部的距离。</param>
  /// <param name="itemHeight">目标项当前缓存高度，未测量时可能为预估值。</param>
  /// <param name="viewportHeight">视口高度。</param>
  /// <param name="currentOffset">当前滚动距离。</param>
  /// <param name="alignment">顶部、中心、底部或最近可见位置。</param>
  /// <returns>尚未限制到 Content 边界的目标距离。</returns>
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

  /// <summary>用非缩放时间插值滚动，每帧更新可见范围，结束后强制重新绑定可见内容。</summary>
  /// <param name="targetOffset">调用时已经算好并限制范围的目标距离；动画中不重新追踪目标索引。</param>
  /// <param name="duration">持续秒数；不大于 0 时直接落到终点。</param>
  /// <returns>跨帧执行的滚动协程。</returns>
  private IEnumerator SmoothScrollTo(float targetOffset, float duration)
  {
    float startOffset = content.anchoredPosition.y;
    float elapsed = 0f;

    while (elapsed < duration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(elapsed / duration);
      // Smoothstep 曲线让起步和停止更平缓，不是匀速移动。
      t = t * t * (3f - 2f * t);
      SetScrollOffset(Mathf.Lerp(startOffset, targetOffset, t));
      RefreshVisible(false);
      yield return null;
    }

    SetScrollOffset(targetOffset);
    RefreshVisible(true);
    _scrollCoroutine = null;
  }

  /// <summary>取消尚未完成的程序化滚动并清空句柄；不处理 ScrollRect 惯性，惯性由 StopMovement 停止。</summary>
  private void StopSmoothScroll()
  {
    if (_scrollCoroutine == null)
      return;

    StopCoroutine(_scrollCoroutine);
    _scrollCoroutine = null;
  }
}
