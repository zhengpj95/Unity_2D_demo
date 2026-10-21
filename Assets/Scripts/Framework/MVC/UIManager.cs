using System;
using System.Collections.Generic;
using System.Linq;
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

  public UIManagerConfig(Transform main, Transform window, Transform model, Transform tip)
  {
    mainLayer = main;
    windowLayer = window;
    modelLayer = model;
    tipLayer = tip;
  }

  /// <summary>
  /// 验证配置是否有效
  /// </summary>
  public bool IsValid()
  {
    return mainLayer != null && windowLayer != null &&
           modelLayer != null && tipLayer != null;
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
  /// <summary>
  /// UI层级配置
  /// </summary>
  private UIManagerConfig _config;

  /// <summary>
  /// UI缓存
  /// </summary>
  private readonly Dictionary<string, GameObject> _uiCache = new Dictionary<string, GameObject>();

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
    if (!IsInitialized) return;

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
      GameObject prefab = AssetLoader.Instance.Load<GameObject>(prefabPath);
      if (prefab == null)
      {
        Debug.LogError($"[UIManager] Prefab not found: {prefabPath}");
        return;
      }

      var parent = GetLayerParent(layer);
      uiObj = UnityEngine.Object.Instantiate(prefab, parent);
      _uiCache[prefabPath] = uiObj;
      Debug.Log($"[UIManager] Created UI: {prefabPath}");
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
  private readonly Dictionary<ModuleViewKey, BasePresenter> _presenterCache = new Dictionary<ModuleViewKey, BasePresenter>();
  // 供 Presenter 自身关闭或模块释放时反查其全局身份。
  private readonly Dictionary<BasePresenter, ModuleViewKey> _presenterKeys = new Dictionary<BasePresenter, ModuleViewKey>();

  /// <summary>
  /// 按具体 Presenter 类型打开界面，并保留强类型返回值。
  /// </summary>
  internal T OpenWindow<T>(ModuleViewKey viewKey, object args = null) where T : BasePresenter, new()
  {
    BasePresenter presenter = OpenWindowInternal(viewKey, () => new T(), args, typeof(T), null, false);
    return presenter as T;
  }

  /// <summary>使用模块登记的 Presenter 工厂打开界面；调用方无需在此处再次指定 Presenter 泛型类型。</summary>
  internal BasePresenter OpenWindow(ModuleViewKey viewKey, Func<BasePresenter> presenterFactory, object args = null)
  {
    return OpenWindowInternal(viewKey, presenterFactory, args, null, null, false);
  }

  /// <summary>在指定的局部父节点下创建或显示 Presenter，供 ListTab 等复合界面使用。</summary>
  /// <param name="viewKey">Presenter 的模块和 ViewType 缓存键。</param>
  /// <param name="presenterFactory">创建 Presenter 的模块注册工厂。</param>
  /// <param name="parent">Presenter View 的局部挂载节点。</param>
  /// <param name="args">传递给 Presenter.OnOpen 的参数。</param>
  /// <returns>打开后的 Presenter；资源加载失败时返回 null。</returns>
  internal BasePresenter OpenWindowIn(ModuleViewKey viewKey, Func<BasePresenter> presenterFactory, Transform parent, object args = null)
  {
    if (parent == null) throw new ArgumentNullException(nameof(parent));
    return OpenWindowInternal(viewKey, presenterFactory, args, null, parent, true);
  }

  private BasePresenter OpenWindowInternal(
    ModuleViewKey viewKey,
    Func<BasePresenter> presenterFactory,
    object args,
    Type expectedPresenterType,
    Transform parentOverride,
    bool showCachedOnlyWhenHidden)
  {
    if (presenterFactory == null) throw new ArgumentNullException(nameof(presenterFactory));

    if (!_presenterCache.TryGetValue(viewKey, out BasePresenter presenter))
    {
      presenter = presenterFactory();
      if (presenter == null)
        throw new InvalidOperationException($"[UIManager] Presenter factory returned null for view key {viewKey}.");
      if (string.IsNullOrWhiteSpace(presenter.PrefabPath))
      {
        Debug.LogError($"[UIManager] Presenter {presenter.GetType().Name} must define a valid PrefabPath.");
        return null;
      }

      // 1. 通过统一资源入口加载 Prefab；后续切换资源后端不修改 UIManager 或 Presenter。
      GameObject prefab = AssetLoader.Instance.Load<GameObject>(presenter.PrefabPath);
      if (prefab == null)
      {
        Debug.LogError($"[UIManager] Prefab not found: {presenter.PrefabPath}");
        return null;
      }

      // 局部容器优先于全局 Layer；普通窗口仍沿用 Presenter.Layer 对应的根节点。
      Transform parent = parentOverride != null ? parentOverride : GetLayerParent(presenter.Layer);
      GameObject go = UnityEngine.Object.Instantiate(prefab, parent);

      UIView view = go.GetComponent<UIView>();

      // 2. 实例化 Presenter 并初始化
      presenter.OnInit(view);
      _presenterCache.Add(viewKey, presenter);
      _presenterKeys.Add(presenter, viewKey);
    }
    else if (expectedPresenterType != null && !expectedPresenterType.IsInstanceOfType(presenter))
    {
      throw new InvalidOperationException($"[UIManager] View key {viewKey} is bound to {presenter.GetType().Name}, not {expectedPresenterType.Name}.");
    }

    // 缓存 Presenter 可能来自全局 Layer 或另一个局部容器；按当前调用方要求重新挂载后再执行 OnOpen。
    if (parentOverride != null && presenter.View != null && presenter.View.transform.parent != parentOverride)
      presenter.View.transform.SetParent(parentOverride, false);

    // Module 缓存语义是仅从隐藏状态恢复时调用 OnOpen；直接 UIManager 打开时保留每次调用都触发 OnOpen 的旧行为。
    if (!showCachedOnlyWhenHidden || !presenter.IsVisible)
      presenter.OnOpen(args);
    return presenter;
  }

  /// <summary>
  /// 实例关闭界面：关闭指定界面（通过 BasePresenter 实例）
  /// 在 Presenter 中调用 CloseWindow(this) 可以关闭当前界面
  /// </summary>
  /// <param name="presenter"></param>
  public void CloseWindow(BasePresenter presenter)
  {
    if (presenter != null && _presenterKeys.TryGetValue(presenter, out ModuleViewKey viewKey))
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

    if (_presenterKeys.TryGetValue(presenter, out ModuleViewKey viewKey)
        && _presenterCache.TryGetValue(viewKey, out BasePresenter cachedPresenter)
        && ReferenceEquals(cachedPresenter, presenter))
    {
      if (presenter.IsVisible) presenter.OnClose();
      presenter.OnDestroy();
      RemovePresenter(viewKey, presenter);
      return;
    }

    if (presenter.IsVisible)
    {
      presenter.OnClose();
    }
    presenter.OnDestroy();
  }

  /// <summary>
  /// 核心关闭逻辑
  /// </summary>
  /// <param name="viewKey"></param>
  private void CloseWindow(ModuleViewKey viewKey)
  {
    if (_presenterCache.TryGetValue(viewKey, out BasePresenter presenter))
    {
      if (presenter.IsVisible) presenter.OnClose();
      presenter.OnDestroy();
      RemovePresenter(viewKey, presenter);
    }
  }

  private void RemovePresenter(ModuleViewKey viewKey, BasePresenter presenter)
  {
    _presenterCache.Remove(viewKey);
    _presenterKeys.Remove(presenter);
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
      presenter.OnOpen(args);
    }
  }

  /// <summary>
  /// 隐藏 Presenter（调用 OnHide，不销毁）
  /// </summary>
  public void HidePresenter(BasePresenter presenter)
  {
    if (presenter != null && presenter.IsVisible)
    {
      presenter.OnClose();
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
  internal BasePresenter GetPresenter(ModuleViewKey viewKey)
  {
    return _presenterCache.TryGetValue(viewKey, out BasePresenter presenter) ? presenter : null;
  }

  /// <summary>检查指定模块 ViewType 的 Presenter 是否存在。</summary>
  public bool HasPresenter(ModuleViewKey viewKey)
  {
    return _presenterCache.ContainsKey(viewKey);
  }

  #endregion
}
