using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>使用 VirtualList 展示页签，并在选中时切换到对应的 Module Presenter。</summary>
[DisallowMultipleComponent]
public sealed class ListTab : MonoBehaviour
{
  [SerializeField, Tooltip("负责页签虚拟化和点击检测的 VirtualList。")]
  private VirtualList virtualList;
  [SerializeField, Tooltip("所有页签 Presenter 的共同父节点；必须位于 ListTab 宿主层级内。")]
  private Transform contentRoot;
  [SerializeField, Min(0f), Tooltip("页签切走后等待多久销毁其 Presenter；0 表示立即销毁。")]
  private float inactivePresenterDestroyDelay = 5f;

  private sealed class HostedPresenter
  {
    public BasePresenter Presenter;
    public uint DestroyTimerId;
    public ModuleName ModuleName;
    public Enum ViewType;
  }

  private List<ListTabItemData> _tabs;
  private BasePresenter _activePresenter;
  private int? _selectedTabId;
  private bool _reportedMissingItemView;
  private readonly Dictionary<string, Sprite> _iconCache = new Dictionary<string, Sprite>();
  private readonly Dictionary<int, HostedPresenter> _hostedPresenters = new Dictionary<int, HostedPresenter>();

  /// <summary>页签成功切换后触发；订阅方应在自身关闭时解除订阅。</summary>
  public event Action<ListTabItemData> TabSelected;

  /// <summary>初始化页签列表的稳定选中键，确保数据刷新后按页签 ID 恢复选中状态。</summary>
  private void Awake()
  {
    if (virtualList == null)
    {
      Debug.LogError("[ListTab] VirtualList reference is missing.", this);
      return;
    }

    // 页签 ID 在数据刷新或重排时保持同一选中项。
    virtualList.SetSelectionKeySelector(item => ((ListTabItemData)item).Id);
  }

  /// <summary>组件启用时重新绑定 VirtualList 回调，并恢复当前页签和对应 Presenter。</summary>
  private void OnEnable()
  {
    if (virtualList == null) return;

    virtualList.SetHandlers(RenderTab, OnTabClicked);
    virtualList.RefreshData(_tabs);
    RestoreSelectionAndPresenter();
  }

  /// <summary>组件禁用时解除列表回调并隐藏当前 Presenter；保留引用以便重新启用时恢复。</summary>
  private void OnDisable()
  {
    if (virtualList != null) virtualList.ClearHandlers();
    HideActivePresenter();
  }

  /// <summary>组件销毁时清除外部事件订阅，避免事件引用延长订阅方生命周期。</summary>
  private void OnDestroy()
  {
    // 宿主 View 即将销毁；先清理子 Presenter 缓存和计时器，避免 UIManager 留有失效 View。
    CloseAllHostedPresenters();
    TabSelected = null;
  }

  /// <summary>设置页签数据；未指定初始 ID 时默认选择并打开第一项，null 或空列表会清空页签。</summary>
  /// <param name="tabs">页签数据源。设置时会复制列表引用集合，后续修改需再次调用本方法。</param>
  /// <param name="initialSelectedId">可选初始页签 ID；必须存在于数据源中。</param>
  public void SetTabs(IList<ListTabItemData> tabs, int? initialSelectedId = null)
  {
    ValidateTabs(tabs);
    _tabs = tabs == null ? null : new List<ListTabItemData>(tabs);
    PruneHostedPresenters();

    if (_tabs == null || _tabs.Count == 0)
    {
      _selectedTabId = null;
      if (virtualList != null) virtualList.Clear();
      CloseAllHostedPresenters();
      return;
    }

    int selectedIndex = initialSelectedId.HasValue ? FindIndex(initialSelectedId.Value) : 0;
    if (selectedIndex < 0)
      throw new ArgumentException($"Initial tab id was not found: {initialSelectedId.Value}", nameof(initialSelectedId));

    _selectedTabId = _tabs[selectedIndex].Id;
    if (virtualList == null) return;

    virtualList.RefreshData(_tabs);
    virtualList.SelectedIndex = selectedIndex;
    if (isActiveAndEnabled) OpenTab(_tabs[selectedIndex]);
  }

  /// <summary>按稳定 ID 选择并打开页签；页签不存在时返回 false。</summary>
  /// <param name="tabId">目标页签 ID。</param>
  /// <returns>目标页签存在且 Presenter 打开成功时返回 true。</returns>
  public bool SelectTab(int tabId)
  {
    int index = FindIndex(tabId);
    if (index < 0) return false;

    if (virtualList != null) virtualList.SelectedIndex = index;
    if (!isActiveAndEnabled)
    {
      _selectedTabId = tabId;
      return true;
    }
    bool opened = OpenTab(_tabs[index]);
    if (!opened) RestoreVisualSelection();
    return opened;
  }

  /// <summary>将 VirtualList 当前复用的单元格绑定到页签数据、图标和选中状态。</summary>
  /// <param name="info">VirtualList 提供的索引、数据、选中索引和单元格引用。</param>
  private void RenderTab(VirtualListRenderInfo info)
  {
    if (info.itemTransform == null || info.data is not ListTabItemData data) return;

    ListTabItemView itemView = info.itemTransform.GetComponent<ListTabItemView>();
    if (itemView == null)
    {
      if (!_reportedMissingItemView)
      {
        Debug.LogError("[ListTab] VirtualList itemTemplate must have a ListTabItemView component.", this);
        _reportedMissingItemView = true;
      }
      return;
    }

    itemView.Bind(data, LoadIcon(data.IconPath), info.index == info.selectedIndex);
  }

  /// <summary>响应 VirtualList 的页签点击；切换失败时恢复此前成功打开的页签选中态。</summary>
  /// <param name="info">被点击单元格的虚拟列表回调信息。</param>
  private void OnTabClicked(VirtualListRenderInfo info)
  {
    if (info.data is ListTabItemData data)
    {
      if (!OpenTab(data)) RestoreVisualSelection();
    }
  }

  /// <summary>打开页签对应模块中的 Presenter；打开成功后再隐藏上一个 Presenter。</summary>
  /// <param name="data">目标页签数据及其模块、ViewType 和启动参数。</param>
  /// <returns>目标 Presenter 成功打开时返回 true；模块缺失或打开失败时返回 false。</returns>
  private bool OpenTab(ListTabItemData data)
  {
    if (contentRoot == null)
    {
      Debug.LogError("[ListTab] Content root is missing; child Presenters must be mounted under the host.", this);
      return false;
    }

    if (!ModuleManager.IsCreated)
    {
      Debug.LogError($"[ListTab] ModuleManager is not initialized; cannot open {data.ModuleName}.{data.ViewType}.", this);
      return false;
    }

    if (!ModuleManager.Instance.TryGetModule(data.ModuleName, out BaseModule module))
    {
      Debug.LogError($"[ListTab] Module was not found: {data.ModuleName}.", this);
      return false;
    }

    _hostedPresenters.TryGetValue(data.Id, out HostedPresenter cachedEntry);
    if (cachedEntry != null) CancelDestroy(cachedEntry);

    // 先打开目标界面；成功后再隐藏旧界面，避免目标资源加载失败时留下空白内容区。
    BasePresenter nextPresenter;
    try
    {
      nextPresenter = module.OpenWindowIn(data.ViewType, contentRoot, data.ExtraData);
    }
    catch (Exception exception)
    {
      Debug.LogError($"[ListTab] Failed to open {data.ModuleName}.{data.ViewType}: {exception}", this);
      if (cachedEntry != null) ScheduleDestroy(cachedEntry.Presenter);
      return false;
    }

    if (nextPresenter == null)
    {
      if (cachedEntry != null) ScheduleDestroy(cachedEntry.Presenter);
      return false;
    }

    _hostedPresenters[data.Id] = cachedEntry ?? new HostedPresenter();
    _hostedPresenters[data.Id].Presenter = nextPresenter;
    _hostedPresenters[data.Id].ModuleName = data.ModuleName;
    _hostedPresenters[data.Id].ViewType = data.ViewType;

    BasePresenter previousPresenter = _activePresenter;
    _activePresenter = nextPresenter;
    _selectedTabId = data.Id;
    if (previousPresenter != null && !ReferenceEquals(previousPresenter, nextPresenter))
    {
      UIManager.Instance.HidePresenter(previousPresenter);
      ScheduleDestroy(previousPresenter);
    }

    TabSelected?.Invoke(data);
    return true;
  }

  /// <summary>列表启用时恢复选中页签，并重新显示其缓存 Presenter。</summary>
  private void RestoreSelectionAndPresenter()
  {
    if (_tabs == null || _tabs.Count == 0) return;

    int index = _selectedTabId.HasValue ? FindIndex(_selectedTabId.Value) : 0;
    if (index < 0) index = 0;
    _selectedTabId = _tabs[index].Id;
    virtualList.SelectedIndex = index;
    OpenTab(_tabs[index]);
  }

  /// <summary>宿主暂时禁用时隐藏当前子 Presenter，但保留缓存和当前页签记录。</summary>
  private void HideActivePresenter()
  {
    if (_activePresenter != null && UIManager.IsCreated)
      UIManager.Instance.HidePresenter(_activePresenter);
  }

  /// <summary>为已隐藏的子 Presenter 启动延迟销毁；找不到宿主记录时立即关闭以避免缓存泄漏。</summary>
  /// <param name="presenter">刚切离的 Presenter。</param>
  private void ScheduleDestroy(BasePresenter presenter)
  {
    int tabId = -1;
    HostedPresenter matchedEntry = null;
    foreach (KeyValuePair<int, HostedPresenter> pair in _hostedPresenters)
    {
      if (!ReferenceEquals(pair.Value.Presenter, presenter)) continue;
      tabId = pair.Key;
      matchedEntry = pair.Value;
      break;
    }

    if (matchedEntry == null)
    {
      presenter.Close();
      return;
    }

    CancelDestroy(matchedEntry);
    if (inactivePresenterDestroyDelay <= 0f)
    {
      CloseHostedPresenter(tabId, presenter);
      return;
    }

    matchedEntry.DestroyTimerId = TimerManager.Instance.AddTimer(
      inactivePresenterDestroyDelay,
      () => DestroyInactivePresenter(tabId, presenter));
  }

  /// <summary>延迟时间到达时销毁仍处于非活动状态的 Presenter。</summary>
  /// <param name="tabId">Presenter 所属的页签 ID。</param>
  /// <param name="presenter">启动计时的 Presenter，用于确认缓存记录未被替换。</param>
  private void DestroyInactivePresenter(int tabId, BasePresenter presenter)
  {
    if (!_hostedPresenters.TryGetValue(tabId, out HostedPresenter entry)
        || !ReferenceEquals(entry.Presenter, presenter))
      return;

    entry.DestroyTimerId = 0;
    if (ReferenceEquals(_activePresenter, presenter)) return;
    CloseHostedPresenter(tabId, presenter);
  }

  /// <summary>取消指定子 Presenter 尚未到期的销毁计时。</summary>
  /// <param name="entry">保存 Presenter 与计时器 ID 的宿主记录。</param>
  private static void CancelDestroy(HostedPresenter entry)
  {
    if (entry.DestroyTimerId == 0) return;
    if (TimerManager.IsCreated) TimerManager.Instance.Cancel(entry.DestroyTimerId);
    entry.DestroyTimerId = 0;
  }

  /// <summary>关闭并移除一个由当前 ListTab 创建或复用的子 Presenter。</summary>
  /// <param name="tabId">子 Presenter 所属页签 ID。</param>
  /// <param name="presenter">要关闭的 Presenter。</param>
  private void CloseHostedPresenter(int tabId, BasePresenter presenter)
  {
    if (_hostedPresenters.TryGetValue(tabId, out HostedPresenter entry)
        && ReferenceEquals(entry.Presenter, presenter))
    {
      CancelDestroy(entry);
      _hostedPresenters.Remove(tabId);
    }

    if (ReferenceEquals(_activePresenter, presenter))
    {
      _activePresenter = null;
      _selectedTabId = null;
    }

    if (presenter != null) presenter.Close();
  }

  /// <summary>关闭当前页签集合中已移除或目标已变更的 Presenter。</summary>
  private void PruneHostedPresenters()
  {
    if (_hostedPresenters.Count == 0) return;

    List<int> removedTabIds = new List<int>();
    foreach (KeyValuePair<int, HostedPresenter> pair in _hostedPresenters)
    {
      ListTabItemData tab = FindTab(pair.Key);
      if (tab == null
          || tab.ModuleName != pair.Value.ModuleName
          || !Equals(tab.ViewType, pair.Value.ViewType))
        removedTabIds.Add(pair.Key);
    }

    for (int i = 0; i < removedTabIds.Count; i++)
    {
      int tabId = removedTabIds[i];
      if (_hostedPresenters.TryGetValue(tabId, out HostedPresenter entry))
        CloseHostedPresenter(tabId, entry.Presenter);
    }
  }

  /// <summary>关闭所有子 Presenter 并取消延迟销毁计时，供数据清空和宿主销毁时调用。</summary>
  private void CloseAllHostedPresenters()
  {
    if (_hostedPresenters.Count == 0)
    {
      _activePresenter = null;
      _selectedTabId = null;
      return;
    }

    List<KeyValuePair<int, HostedPresenter>> entries = new List<KeyValuePair<int, HostedPresenter>>(_hostedPresenters);
    for (int i = 0; i < entries.Count; i++)
      CloseHostedPresenter(entries[i].Key, entries[i].Value.Presenter);

    _hostedPresenters.Clear();
    _activePresenter = null;
    _selectedTabId = null;
  }

  /// <summary>根据稳定页签 ID 查找新数据集中的页签定义。</summary>
  /// <param name="tabId">待查找的页签 ID。</param>
  /// <returns>找到时返回页签数据；不存在时返回 null。</returns>
  private ListTabItemData FindTab(int tabId)
  {
    if (_tabs == null) return null;
    for (int i = 0; i < _tabs.Count; i++)
      if (_tabs[i].Id == tabId) return _tabs[i];
    return null;
  }

  /// <summary>将虚拟列表选中索引恢复到当前已成功打开的页签。</summary>
  private void RestoreVisualSelection()
  {
    if (virtualList == null || !_selectedTabId.HasValue) return;
    int activeIndex = FindIndex(_selectedTabId.Value);
    virtualList.SelectedIndex = activeIndex;
  }

  /// <summary>按资源键同步加载页签图标，并缓存结果以避免虚拟单元格重复加载。</summary>
  /// <param name="iconPath">Sprite 资源键；为空时不执行加载。</param>
  /// <returns>加载到的 Sprite；资源缺失或路径为空时返回 null。</returns>
  private Sprite LoadIcon(string iconPath)
  {
    if (string.IsNullOrWhiteSpace(iconPath)) return null;
    if (_iconCache.TryGetValue(iconPath, out Sprite cachedIcon)) return cachedIcon;

    Sprite icon = AssetLoader.Instance.Load<Sprite>(iconPath);
    _iconCache.Add(iconPath, icon);
    if (icon == null)
      Debug.LogWarning($"[ListTab] Icon resource was not found: {iconPath}.", this);
    return icon;
  }

  /// <summary>查找页签稳定 ID 在当前数据集中的索引。</summary>
  /// <param name="tabId">要查找的页签 ID。</param>
  /// <returns>找到时返回零起始索引；未找到时返回 -1。</returns>
  private int FindIndex(int tabId)
  {
    if (_tabs == null) return -1;
    for (int i = 0; i < _tabs.Count; i++)
      if (_tabs[i].Id == tabId) return i;
    return -1;
  }

  /// <summary>检查页签集合中的空项和重复 ID，保证虚拟列表稳定键唯一。</summary>
  /// <param name="tabs">待验证的页签集合；null 表示清空列表。</param>
  /// <exception cref="ArgumentException">集合含空元素或重复页签 ID 时抛出。</exception>
  private static void ValidateTabs(IList<ListTabItemData> tabs)
  {
    if (tabs == null) return;

    HashSet<int> ids = new HashSet<int>();
    HashSet<ModuleViewKey> viewKeys = new HashSet<ModuleViewKey>();
    for (int i = 0; i < tabs.Count; i++)
    {
      ListTabItemData tab = tabs[i];
      if (tab == null) throw new ArgumentException($"Tab data at index {i} is null.", nameof(tabs));
      if (!ids.Add(tab.Id)) throw new ArgumentException($"Duplicate tab id: {tab.Id}.", nameof(tabs));
      if (!viewKeys.Add(new ModuleViewKey(tab.ModuleName, tab.ViewType)))
        throw new ArgumentException($"Presenter target is assigned to more than one tab: {tab.ModuleName}.{tab.ViewType}.", nameof(tabs));
    }
  }
}
