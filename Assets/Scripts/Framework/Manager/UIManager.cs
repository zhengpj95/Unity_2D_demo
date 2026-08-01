using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI层级索引
/// </summary>
public enum UILayerIndex
{
  Main = 0,
  Window = 1,
  Model = 2,
  Tip = 3,
}

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

  public UIManagerConfig() { }

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
  /// 初始化UI管理器（便捷方法）
  /// </summary>
  public void Initialize(Transform main, Transform window, Transform model, Transform tip)
  {
    Initialize(new UIManagerConfig(main, window, model, tip));
  }

  /// <summary>
  /// 更新配置（用于场景切换）
  /// </summary>
  /// <param name="config">新配置</param>
  public void UpdateConfig(UIManagerConfig config)
  {
    if (config == null || !config.IsValid())
    {
      Debug.LogError("[UIManager] Invalid config");
      return;
    }

    _config = config;
    Debug.Log("[UIManager] Config updated");
  }

  /// <summary>
  /// 释放资源
  /// </summary>
  public void Release()
  {
    if (!IsInitialized) return;

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
  /// <param name="prefabPath">Prefab路径（Resources下的相对路径）</param>
  /// <param name="layer">UI层级</param>
  public void ShowUI(string prefabPath, UILayerIndex layer)
  {
    if (!CheckInitialized()) return;

    if (!_uiCache.TryGetValue(prefabPath, out GameObject uiObj) || uiObj == null)
    {
      var prefab = Resources.Load<GameObject>(prefabPath);
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

  #region UI查询与管理

  /// <summary>
  /// 获取UI GameObject实例
  /// </summary>
  /// <param name="prefabPath">Prefab路径</param>
  /// <returns>GameObject实例，不存在返回null</returns>
  public GameObject GetUIObject(string prefabPath)
  {
    _uiCache.TryGetValue(prefabPath, out GameObject uiObj);
    return uiObj;
  }

  /// <summary>
  /// 获取UI上的组件
  /// </summary>
  /// <typeparam name="T">组件类型</typeparam>
  /// <param name="prefabPath">Prefab路径</param>
  /// <returns>组件实例</returns>
  public T GetUIComponent<T>(string prefabPath) where T : Component
  {
    var go = GetUIObject(prefabPath);
    return go?.GetComponent<T>();
  }

  /// <summary>
  /// 检查UI是否存在
  /// </summary>
  /// <param name="prefabPath">Prefab路径</param>
  /// <returns>是否存在</returns>
  public bool HasUI(string prefabPath)
  {
    return _uiCache.ContainsKey(prefabPath);
  }

  /// <summary>
  /// 销毁指定UI
  /// </summary>
  /// <param name="prefabPath">Prefab路径</param>
  public void DestroyUI(string prefabPath)
  {
    HideUI(prefabPath, true);
  }

  /// <summary>
  /// 销毁所有UI
  /// </summary>
  public void DestroyAllUI()
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

  /// <summary>
  /// 获取所有缓存的UI
  /// </summary>
  /// <returns>UI路径集合</returns>
  public IEnumerable<string> GetAllCachedUI()
  {
    return _uiCache.Keys;
  }

  /// <summary>
  /// 获取缓存UI数量
  /// </summary>
  public int CachedUICount => _uiCache.Count;

  #endregion

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

  #region UIPresenter处理
  // 保存所有已实例化的 Presenter
  private readonly Dictionary<Type, UIPresenter> _presenterCache = new Dictionary<Type, UIPresenter>();
  // 界面打开栈 (主要用于 PopUp 层管理)
  private readonly Stack<UIPresenter> _uiStack = new Stack<UIPresenter>();

  /// <summary>
  /// 打开界面
  /// </summary>
  public T OpenWindow<T>(string prefabPath, UILayerIndex layer, object args = null) where T : UIPresenter, new()
  {
    Type type = typeof(T);
    if (!_presenterCache.TryGetValue(type, out UIPresenter presenter))
    {
      // 1. 模拟异步/同步加载 Prefab (实际项目中可替换为 Addressables / AssetBundle)
      GameObject prefab = Resources.Load<GameObject>(prefabPath);
      if (prefab == null)
      {
        Debug.LogError($"[UIManager] Prefab not found: {prefabPath}");
        return null;
      }

      Transform parent = GetLayerParent(layer);
      GameObject go = UnityEngine.Object.Instantiate(prefab, parent);

      UIView view = go.GetComponent<UIView>();

      // 2. 实例化 Presenter 并初始化
      presenter = new T();
      presenter.OnInit(view);
      _presenterCache.Add(type, presenter);
    }

    // 3. 入栈控制（如果是 PopUp 类型的弹窗，可以压栈管理）
    if (layer == UILayerIndex.Model)
    {
      _uiStack.Push(presenter);
    }

    // 4. 调用 Open 生命周期
    presenter.OnOpen(args);
    return (T)presenter;
  }

  /// <summary>
  /// 泛型关闭界面：UIManager.Instance.CloseWindow<ConfirmPresenter>();
  /// </summary>
  public void CloseWindow<T>() where T : UIPresenter
  {
    CloseWindow(typeof(T));
  }

  /// <summary>
  /// 实例关闭界面：关闭指定界面（通过 UIPresenter 实例）
  /// 在 Presenter 中调用 CloseWindow(this) 可以关闭当前界面
  /// </summary>
  /// <param name="presenter"></param>
  public void CloseWindow(UIPresenter presenter)
  {
    if (presenter != null && _presenterCache.ContainsKey(presenter.GetType()))
    {
      CloseWindow(presenter.GetType());
    }
  }

  /// <summary>
  /// 核心关闭逻辑
  /// </summary>
  /// <param name="presenterType"></param>
  public void CloseWindow(Type presenterType)
  {
    if (_presenterCache.TryGetValue(presenterType, out UIPresenter presenter))
    {
      if (presenter.IsVisible)
      {
        presenter.OnClose();
        presenter.OnDestroy(); // TODO 延迟关闭处理，或定时检测关闭
        _presenterCache.Remove(presenterType);
      }
    }
  }

  /// <summary>
  /// 出栈 (关闭最顶层的弹窗)
  /// </summary>
  public void PopWindow()
  {
    if (_uiStack.Count > 0)
    {
      UIPresenter topPresenter = _uiStack.Pop();
      topPresenter.OnClose();
    }
  }

  #endregion
}