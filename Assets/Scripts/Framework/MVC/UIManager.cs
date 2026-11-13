using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// UI管理器配置
/// 用于存储UI层级Transform引用
/// </summary>
public class UIManagerConfig
{
  public Transform mainLayer;
  public Transform windowLayer;
  public Transform modelLayer;
  public Transform tipLayer;
  public Transform guideLayer;

  public UIManagerConfig(Transform main, Transform window, Transform model, Transform tip, Transform guide)
  {
    mainLayer = main;
    windowLayer = window;
    modelLayer = model;
    tipLayer = tip;
    guideLayer = guide;
  }

  /// <summary>
  /// 验证配置是否有效
  /// </summary>
  public bool IsValid()
  {
    return mainLayer != null && windowLayer != null &&
           modelLayer != null && tipLayer != null && guideLayer != null;
  }
}

/// <summary>UI Presenter 生命周期事件数据，标识所属模块和界面类型。</summary>
public readonly struct PresenterLifecycleEventArgs
{
  /// <summary>Presenter 所属模块。</summary>
  public ModuleName ModuleName { get; }

  /// <summary>Presenter 在所属模块中的界面类型。</summary>
  public Enum ViewType { get; }

  /// <summary>创建包含模块和界面类型的生命周期事件数据。</summary>
  /// <param name="moduleName">Presenter 所属模块。</param>
  /// <param name="viewType">Presenter 在所属模块中的界面类型。</param>
  public PresenterLifecycleEventArgs(ModuleName moduleName, Enum viewType)
  {
    ModuleName = moduleName;
    ViewType = viewType;
  }
}

/// <summary>
/// UI管理器（纯C#实现）
/// 负责UI的显示、隐藏、层级管理和缓存
///
/// 使用方式：
/// 1. 在场景启动时调用 UIManager.Initialize(config)
/// 2. 在场景销毁时调用 UIManager.Release()
/// </summary>
public class UIManager : Singleton<UIManager>
{
  /// <summary>UIManager 内部的 Presenter 缓存键，由模块身份和模块内 ViewType 组成。</summary>
  private readonly struct PresenterKey : IEquatable<PresenterKey>
  {
    public readonly ModuleName ModuleName;
    public readonly Enum ViewType;

    public PresenterKey(ModuleName moduleName, Enum viewType)
    {
      if (moduleName == ModuleName.None)
        throw new ArgumentException("ModuleName cannot be None.", nameof(moduleName));

      ModuleName = moduleName;
      ViewType = viewType ?? throw new ArgumentNullException(nameof(viewType));
    }

    public bool Equals(PresenterKey other)
    {
      return ModuleName == other.ModuleName && object.Equals(ViewType, other.ViewType);
    }

    public override bool Equals(object obj)
    {
      return obj is PresenterKey other && Equals(other);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return ((int)ModuleName * 397) ^ (ViewType?.GetHashCode() ?? 0);
      }
    }

    public override string ToString()
    {
      return $"{ModuleName}.{ViewType}";
    }
  }

  private sealed class PendingPresenterOpen
  {
    public bool IsCancelled;
    public Task<BasePresenter> Task;
  }

  /// <summary>
  /// UI层级配置
  /// </summary>
  private UIManagerConfig _config;

  /// <summary>
  /// UI缓存
  /// </summary>
  private readonly Dictionary<string, GameObject> _uiCache = new Dictionary<string, GameObject>();
  private readonly Dictionary<string, AssetHandle<GameObject>> _uiAssetHandles =
    new Dictionary<string, AssetHandle<GameObject>>();

  /// <summary>
  /// 是否已初始化
  /// </summary>
  public bool IsInitialized { get; private set; }

  /// <summary>
  /// 受保护构造函数（单例模式）
  /// </summary>
  protected UIManager() { }

  #region 初始化与释放

  /// <summary>
  /// 初始化UI管理器
  /// </summary>
  /// <param name="config">UI层级配置</param>
  public void Initialize(UIManagerConfig config)
  {
    if (IsInitialized)
    {
      Debug.LogWarning("[UIManager] Already initialized");
      return;
    }

    if (config == null || !config.IsValid())
    {
      Debug.LogError("[UIManager] Invalid config");
      return;
    }

    _config = config;
    IsInitialized = true;
    Debug.Log("[UIManager] Initialized");
  }

  /// <summary>
  /// 释放资源
  /// </summary>
  public void Release()
  {
    if (!IsInitialized && _pendingPresenterOpens.Count == 0) return;

    foreach (PresenterKey viewKey in _pendingPresenterOpens.Keys.ToArray())
      CancelPendingOpen(viewKey);

    foreach (BasePresenter presenter in _presenterCache.Values.ToArray())
      DestroyWindow(presenter);

    DestroyAllUI();
    _config = null;
    IsInitialized = false;
    Debug.Log("[UIManager] Released");
  }

  #endregion

  #region UI显示与隐藏

  /// <summary>
  /// 显示UI
  /// </summary>
  /// <param name="prefabPath">由 AssetLoader 解析的 Prefab 资源键。</param>
  /// <param name="layer">UI层级</param>
  [Obsolete("ShowUI 使用旧式 Prefab 路径缓存；新界面请通过 Module 的 OpenWindow 接口打开 Presenter。")]
  public void ShowUI(string prefabPath, UILayerIndex layer)
  {
    if (!CheckInitialized()) return;

    if (!_uiCache.TryGetValue(prefabPath, out GameObject uiObj) || uiObj == null)
    {
      if (_uiCache.ContainsKey(prefabPath))
      {
        _uiCache.Remove(prefabPath);
        ReleaseLegacyUIHandle(prefabPath);
      }

      AssetHandle<GameObject> prefabHandle = AssetLoader.Instance.LoadHandle<GameObject>(prefabPath);
      if (prefabHandle == null)
      {
        Debug.LogError($"[UIManager] Prefab not found: {prefabPath}");
        return;
      }

      try
      {
        var parent = GetLayerParent(layer);
        uiObj = UnityEngine.Object.Instantiate(prefabHandle.Asset, parent);
        _uiCache[prefabPath] = uiObj;
        _uiAssetHandles[prefabPath] = prefabHandle;
        Debug.Log($"[UIManager] Created UI: {prefabPath}");
      }
      catch
      {
        prefabHandle.Dispose();
        throw;
      }
    }

    uiObj.SetActive(true);
    uiObj.transform.SetAsLastSibling();
  }

  /// <summary>
  /// 隐藏UI
  /// </summary>
  /// <param name="prefabPath">Prefab路径</param>
  /// <param name="isDestroy">是否销毁GameObject</param>
  [Obsolete("HideUI 使用旧式 Prefab 路径缓存；Presenter 界面请使用 HidePresenter 或 CloseWindow。")]
  public void HideUI(string prefabPath, bool isDestroy = false)
  {
    if (!CheckInitialized()) return;

    if (_uiCache.TryGetValue(prefabPath, out GameObject ui))
    {
      if (isDestroy)
      {
        UnityEngine.Object.Destroy(ui);
        _uiCache.Remove(prefabPath);
        ReleaseLegacyUIHandle(prefabPath);
      }
      else
      {
        ui.SetActive(false);
      }
    }
  }

  #endregion

  /// <summary>销毁所有旧式路径缓存 UI；仅在 UIManager 释放时调用。</summary>
  private void DestroyAllUI()
  {
    foreach (var kvp in _uiCache)
    {
      if (kvp.Value != null)
      {
        UnityEngine.Object.Destroy(kvp.Value);
      }
    }
    _uiCache.Clear();
    foreach (AssetHandle<GameObject> handle in _uiAssetHandles.Values)
      handle.Dispose();
    _uiAssetHandles.Clear();
  }

  private void ReleaseLegacyUIHandle(string prefabPath)
  {
    if (!_uiAssetHandles.TryGetValue(prefabPath, out AssetHandle<GameObject> handle))
      return;

    _uiAssetHandles.Remove(prefabPath);
    handle.Dispose();
  }

  #region 辅助方法

  /// <summary>
  /// 获取层级父节点
  /// </summary>
  private Transform GetLayerParent(UILayerIndex layer)
  {
    return layer switch
    {
      UILayerIndex.Main => _config.mainLayer,
      UILayerIndex.Window => _config.windowLayer,
      UILayerIndex.Model => _config.modelLayer,
      UILayerIndex.Tip => _config.tipLayer,
      UILayerIndex.Guide => _config.guideLayer,
      _ => _config.mainLayer
    };
  }

  /// <summary>
  /// 检查是否已初始化
  /// </summary>
  private bool CheckInitialized()
  {
    if (!IsInitialized)
    {
      Debug.LogWarning("[UIManager] Not initialized, call Initialize() first");
      return false;
    }
    return true;
  }

  #endregion

  #region BasePresenter处理
  // Presenter 的全局身份由 ModuleName + ViewType 决定，不能仅按 Presenter 类型缓存。
  private readonly Dictionary<PresenterKey, BasePresenter> _presenterCache = new Dictionary<PresenterKey, BasePresenter>();
  // 供 Presenter 自身关闭或模块释放时反查其全局身份。
  private readonly Dictionary<BasePresenter, PresenterKey> _presenterKeys = new Dictionary<BasePresenter, PresenterKey>();
  private readonly Dictionary<BasePresenter, AssetHandle<GameObject>> _presenterAssetHandles =
    new Dictionary<BasePresenter, AssetHandle<GameObject>>();
  private readonly Dictionary<PresenterKey, PendingPresenterOpen> _pendingPresenterOpens =
    new Dictionary<PresenterKey, PendingPresenterOpen>();

  /// <summary>统一打开模块 Presenter；内部复用缓存或异步加载 Prefab。</summary>
  /// <param name="moduleName">Presenter 所属模块。</param>
  /// <param name="viewType">模块内的界面类型。</param>
  /// <param name="presenterFactory">创建 Presenter 的模块注册工厂。</param>
  /// <param name="expectedPresenterType">该 ViewType 登记的 Presenter 类型。</param>
  /// <param name="args">传递给 Presenter.OnOpen 的参数。</param>
  /// <returns>打开后的 Presenter；资源加载失败时返回 null。</returns>
  internal Task<BasePresenter> OpenWindow(
    ModuleName moduleName,
    Enum viewType,
    Func<BasePresenter> presenterFactory,
    Type expectedPresenterType,
    object args = null)
  {
    PresenterKey viewKey = new PresenterKey(moduleName, viewType);
    return OpenWindowInternal(viewKey, presenterFactory, expectedPresenterType, args, null);
  }

  /// <summary>在指定的局部父节点下统一打开 Presenter，供 ListTab 等复合界面使用。</summary>
  /// <param name="moduleName">Presenter 所属模块。</param>
  /// <param name="viewType">模块内的界面类型。</param>
  /// <param name="presenterFactory">创建 Presenter 的模块注册工厂。</param>
  /// <param name="expectedPresenterType">该 ViewType 登记的 Presenter 类型。</param>
  /// <param name="parent">Presenter View 的局部挂载节点。</param>
  /// <param name="args">传递给 Presenter.OnOpen 的参数。</param>
  /// <returns>打开后的 Presenter；资源加载失败时返回 null。</returns>
  internal Task<BasePresenter> OpenWindowIn(
    ModuleName moduleName,
    Enum viewType,
    Func<BasePresenter> presenterFactory,
    Type expectedPresenterType,
    Transform parent,
    object args = null)
  {
    if (parent == null) throw new ArgumentNullException(nameof(parent));
    PresenterKey viewKey = new PresenterKey(moduleName, viewType);
    return OpenWindowInternal(viewKey, presenterFactory, expectedPresenterType, args, parent);
  }

  /// <summary>取消指定界面尚未完成的打开请求；资源加载完成后会释放句柄且不创建 View。</summary>
  internal void CancelPendingOpen(ModuleName moduleName, Enum viewType)
  {
    CancelPendingOpen(new PresenterKey(moduleName, viewType));
  }

  private void CancelPendingOpen(PresenterKey viewKey)
  {
    if (!_pendingPresenterOpens.TryGetValue(viewKey, out PendingPresenterOpen pending))
      return;

    pending.IsCancelled = true;
    _pendingPresenterOpens.Remove(viewKey);
  }

  private Task<BasePresenter> OpenWindowInternal(
    PresenterKey viewKey,
    Func<BasePresenter> presenterFactory,
    Type expectedPresenterType,
    object args,
    Transform parentOverride)
  {
    if (presenterFactory == null) throw new ArgumentNullException(nameof(presenterFactory));
    bool hasParentOverride = parentOverride != null;

    if (_presenterCache.TryGetValue(viewKey, out BasePresenter cachedPresenter))
    {
      if (!expectedPresenterType.IsInstanceOfType(cachedPresenter))
        throw new InvalidOperationException($"[UIManager] View key {viewKey} is bound to {cachedPresenter.GetType().Name}, not {expectedPresenterType.Name}.");
      if (parentOverride != null && cachedPresenter.View != null && cachedPresenter.View.transform.parent != parentOverride)
        cachedPresenter.View.transform.SetParent(parentOverride, false);
      if (!cachedPresenter.IsVisible)
        OpenPresenter(viewKey, cachedPresenter, args);
      return Task.FromResult(cachedPresenter);
    }

    if (_pendingPresenterOpens.TryGetValue(viewKey, out PendingPresenterOpen pending))
      return pending.Task;

    pending = new PendingPresenterOpen();
    _pendingPresenterOpens.Add(viewKey, pending);
    pending.Task = CreatePresenterAsync(
      viewKey,
      presenterFactory,
      expectedPresenterType,
      args,
      parentOverride,
      hasParentOverride,
      pending);
    return pending.Task;
  }

  private async Task<BasePresenter> CreatePresenterAsync(
    PresenterKey viewKey,
    Func<BasePresenter> presenterFactory,
    Type expectedPresenterType,
    object args,
    Transform parentOverride,
    bool hasParentOverride,
    PendingPresenterOpen pending)
  {
    AssetHandle<GameObject> prefabHandle = null;
    BasePresenter presenter = null;
    GameObject viewObject = null;
    bool isCached = false;
    try
    {
      presenter = presenterFactory();
      if (presenter == null)
        throw new InvalidOperationException($"[UIManager] Presenter factory returned null for view key {viewKey}.");
      if (!expectedPresenterType.IsInstanceOfType(presenter))
        throw new InvalidOperationException($"[UIManager] Presenter factory for {viewKey} returned {presenter.GetType().Name}, not {expectedPresenterType.Name}.");
      if (string.IsNullOrWhiteSpace(presenter.PrefabPath))
      {
        Debug.LogError($"[UIManager] Presenter {presenter.GetType().Name} must define a valid PrefabPath.");
        return null;
      }

      prefabHandle = await AssetLoader.Instance.LoadHandleAsync<GameObject>(presenter.PrefabPath);
      if (pending.IsCancelled || !IsInitialized)
        return null;
      if (hasParentOverride && parentOverride == null)
        return null;
      if (prefabHandle == null)
      {
        Debug.LogError($"[UIManager] Prefab not found: {presenter.PrefabPath}");
        return null;
      }

      if (_presenterCache.TryGetValue(viewKey, out BasePresenter existingPresenter))
      {
        prefabHandle.Dispose();
        prefabHandle = null;
        if (!expectedPresenterType.IsInstanceOfType(existingPresenter))
          throw new InvalidOperationException($"[UIManager] View key {viewKey} is bound to {existingPresenter.GetType().Name}, not {expectedPresenterType.Name}.");
        if (parentOverride != null && existingPresenter.View != null && existingPresenter.View.transform.parent != parentOverride)
          existingPresenter.View.transform.SetParent(parentOverride, false);
        if (!existingPresenter.IsVisible)
          OpenPresenter(viewKey, existingPresenter, args);
        return existingPresenter;
      }

      Transform parent = parentOverride != null ? parentOverride : GetLayerParent(presenter.Layer);
      viewObject = UnityEngine.Object.Instantiate(prefabHandle.Asset, parent);
      presenter.OnInit(viewObject.GetComponent<UIView>());

      _presenterCache.Add(viewKey, presenter);
      _presenterKeys.Add(presenter, viewKey);
      _presenterAssetHandles.Add(presenter, prefabHandle);
      prefabHandle = null;
      isCached = true;

      if (!presenter.IsVisible)
        OpenPresenter(viewKey, presenter, args);
      return presenter;
    }
    catch
    {
      if (isCached && presenter != null)
      {
        if (presenter.IsVisible) presenter.OnClose();
        presenter.OnDestroy();
        RemovePresenter(viewKey, presenter);
      }
      else if (viewObject != null)
      {
        UnityEngine.Object.Destroy(viewObject);
      }
      throw;
    }
    finally
    {
      prefabHandle?.Dispose();
      if (_pendingPresenterOpens.TryGetValue(viewKey, out PendingPresenterOpen current)
          && ReferenceEquals(current, pending))
        _pendingPresenterOpens.Remove(viewKey);
    }
  }

  /// <summary>
  /// 实例关闭界面：关闭指定界面（通过 BasePresenter 实例）
  /// 在 Presenter 中调用 CloseWindow(this) 可以关闭当前界面
  /// </summary>
  /// <param name="presenter"></param>
  public void CloseWindow(BasePresenter presenter)
  {
    if (presenter != null && _presenterKeys.TryGetValue(presenter, out PresenterKey viewKey))
    {
      CloseWindow(viewKey);
    }
  }

  /// <summary>
  /// 销毁指定 Presenter，无论当前是否可见。
  /// </summary>
  internal void DestroyWindow(BasePresenter presenter)
  {
    if (presenter == null)
    {
      return;
    }

    if (_presenterKeys.TryGetValue(presenter, out PresenterKey viewKey)
        && _presenterCache.TryGetValue(viewKey, out BasePresenter cachedPresenter)
        && ReferenceEquals(cachedPresenter, presenter))
    {
      ClosePresenter(viewKey, presenter);
      presenter.OnDestroy();
      RemovePresenter(viewKey, presenter);
      return;
    }

    if (presenter.IsVisible)
    {
      presenter.OnClose();
      Debug.Log($"关闭界面：{presenter.GetType().Name}, m:unknown, v:unknown");
    }
    presenter.OnDestroy();
  }

  /// <summary>
  /// 核心关闭逻辑
  /// </summary>
  /// <param name="viewKey"></param>
  private void CloseWindow(PresenterKey viewKey)
  {
    if (_presenterCache.TryGetValue(viewKey, out BasePresenter presenter))
    {
      ClosePresenter(viewKey, presenter);
      presenter.OnDestroy();
      RemovePresenter(viewKey, presenter);
    }
  }

  private void OpenPresenter(PresenterKey viewKey, BasePresenter presenter, object args)
  {
    presenter.OnOpen(args);
    Debug.Log($"打开界面：{presenter.GetType().Name}, m:{(int)viewKey.ModuleName}, v:{Convert.ToInt32(viewKey.ViewType)}");
    EventBus.Emit(
      EventDefine.UI_PRESENTER_OPENED,
      new PresenterLifecycleEventArgs(viewKey.ModuleName, viewKey.ViewType));
  }

  private void ClosePresenter(PresenterKey viewKey, BasePresenter presenter)
  {
    if (!presenter.IsVisible)
      return;

    presenter.OnClose();
    Debug.Log($"关闭界面：{presenter.GetType().Name}, m:{(int)viewKey.ModuleName}, v:{Convert.ToInt32(viewKey.ViewType)}");
    EventBus.Emit(
      EventDefine.UI_PRESENTER_CLOSED,
      new PresenterLifecycleEventArgs(viewKey.ModuleName, viewKey.ViewType));
  }

  private void RemovePresenter(PresenterKey viewKey, BasePresenter presenter)
  {
    _presenterCache.Remove(viewKey);
    _presenterKeys.Remove(presenter);
    if (_presenterAssetHandles.TryGetValue(presenter, out AssetHandle<GameObject> handle))
    {
      _presenterAssetHandles.Remove(presenter);
      handle.Dispose();
    }
  }

  #endregion

  #region Presenter 生命周期管理

  /// <summary>
  /// 显示已缓存的 Presenter（调用 OnShow）
  /// </summary>
  public void ShowPresenter(BasePresenter presenter, object args = null)
  {
    if (presenter != null && !presenter.IsVisible)
    {
      if (_presenterKeys.TryGetValue(presenter, out PresenterKey viewKey))
        OpenPresenter(viewKey, presenter, args);
      else
      {
        presenter.OnOpen(args);
        Debug.Log($"打开界面：{presenter.GetType().Name}, m:unknown, v:unknown");
      }
    }
  }

  /// <summary>
  /// 隐藏 Presenter（调用 OnHide，不销毁）
  /// </summary>
  public void HidePresenter(BasePresenter presenter)
  {
    if (presenter != null && presenter.IsVisible)
    {
      if (_presenterKeys.TryGetValue(presenter, out PresenterKey viewKey))
        ClosePresenter(viewKey, presenter);
      else
      {
        presenter.OnClose();
        Debug.Log($"关闭界面：{presenter.GetType().Name}, m:unknown, v:unknown");
      }
    }
  }

  /// <summary>
  /// 获取所有活跃的 Presenter
  /// </summary>
  public IEnumerable<BasePresenter> GetAllActivePresenters()
  {
    return _presenterCache.Values.Where(p => p.IsVisible);
  }

  /// <summary>
  /// 获取所有缓存的 Presenter
  /// </summary>
  public IEnumerable<BasePresenter> GetAllCachedPresenters()
  {
    return _presenterCache.Values;
  }

  /// <summary>供 BaseModule 按模块 ViewType 查询已创建的 Presenter。</summary>
  internal BasePresenter GetPresenter(ModuleName moduleName, Enum viewType)
  {
    PresenterKey viewKey = new PresenterKey(moduleName, viewType);
    return _presenterCache.TryGetValue(viewKey, out BasePresenter presenter) ? presenter : null;
  }

  /// <summary>检查指定模块 ViewType 的 Presenter 是否存在。</summary>
  internal bool HasPresenter(ModuleName moduleName, Enum viewType)
  {
    PresenterKey viewKey = new PresenterKey(moduleName, viewType);
    return _presenterCache.ContainsKey(viewKey);
  }

  #endregion
}
