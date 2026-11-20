using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// UI 视图基类 (挂载在 UI Prefab 上)
public abstract class UIView : MonoBehaviour
{
  // 用于组件引用的绑定（也可以在 Inspector 中拖拽）
  public virtual void InitView()
  {
  }
}

// UI 控制器/逻辑基类 (纯 C# 类)
public abstract class BasePresenter : BaseEmitter
{
  public UIView View { get; private set; }
  public bool IsVisible { get; private set; }
  private bool _isDestroyed;

  /// <summary>本 Presenter 所属的业务模块；由 UIManager 在创建时注入，用于访问本模块 Proxy。</summary>
  public BaseModule Module { get; private set; }

  /// <summary>由 UIManager 注入所属模块；Presenter 不自行查找和初始化其他模块。</summary>
  internal void SetModule(BaseModule module)
  {
    Module = module;
  }

  /// <summary>界面所属 UI 层级；默认窗口层，特殊界面由具体 Presenter 重写。</summary>
  public virtual UILayerIndex Layer => UILayerIndex.Window;
  /// <summary>Resources 下的界面 Prefab 路径；注册到模块的 Presenter 必须重写。</summary>
  public virtual string PrefabPath => string.Empty;

  // 是否需要每帧更新
  public bool NeedUpdate { get; protected set; }

  // 按钮回调跟随 View 存亡；隐藏时仍需保留，以便缓存界面再次显示后继续响应。
  private readonly Dictionary<Button, UnityAction> _buttonCallbacks = new();

  #region 生命周期方法

  // 初始化（加载 Prefab 后调用一次）
  public virtual void OnInit(UIView view)
  {
    if (_isDestroyed)
    {
      Debug.LogError($"[{GetType().Name}] Destroyed Presenter cannot be initialized again.");
      return;
    }
    if (view == null)
    {
      Debug.LogError($"[{GetType().Name}] View component is required.");
      return;
    }
    if (View != null)
    {
      Debug.LogError($"[{GetType().Name}] Presenter has already been initialized.");
      return;
    }

    View = view;
  }

  // 打开界面
  public virtual void OnOpen(object args = null)
  {
    if (!TryEnsureInitialized()) return;
    IsVisible = true;
    View.gameObject.SetActive(true);
    OnShow();
  }

  // 关闭界面
  public virtual void OnClose()
  {
    if (_isDestroyed || !IsVisible) return;

    OffAll();
    OnHide();
    IsVisible = false;
    if (View != null) View.gameObject.SetActive(false);
  }

  // 销毁界面
  public virtual void OnDestroy()
  {
    if (_isDestroyed) return;

    if (IsVisible)
      OnClose();
    OffAll();
    ClearAllListeners();

    if (View != null)
    {
      UnityEngine.Object.Destroy(View.gameObject);
      View = null;
    }

    IsVisible = false;
    _isDestroyed = true;
  }

  // 每次从隐藏到显示时调用（例如从缓存恢复）
  public virtual void OnShow() { }

  // 每次从显示到隐藏时调用（不销毁）
  public virtual void OnHide() { }

  // 每帧更新（需要设置 NeedUpdate = true）
  public virtual void Update() { }

  /// <summary>检查 Presenter 是否已绑定有效 View，避免生命周期错误延迟成空引用异常。</summary>
  protected void EnsureInitialized()
  {
    TryEnsureInitialized();
  }

  /// <summary>验证 Presenter 是否绑定有效 View；无效时记录错误并返回 false。</summary>
  protected bool TryEnsureInitialized()
  {
    if (_isDestroyed || View == null)
    {
      Debug.LogError($"[{GetType().Name}] Presenter must be initialized and alive before use.");
      return false;
    }

    return true;
  }

  #endregion

  #region 工具方法

  /// <summary>
  /// 安全添加按钮点击监听器（自动管理生命周期）
  /// </summary>
  /// <param name="button">按钮组件</param>
  /// <param name="callback">点击回调</param>
  protected void AddClickListener(Button button, UnityAction callback)
  {
    if (button == null)
    {
      Debug.LogWarning($"[{GetType().Name}] Button is null, cannot add listener");
      return;
    }

    if (callback == null)
    {
      Debug.LogWarning($"[{GetType().Name}] Callback is null, cannot add listener");
      return;
    }

    // 如果按钮已注册，先移除旧的
    if (_buttonCallbacks.TryGetValue(button, out UnityAction oldCallback))
    {
      button.onClick.RemoveListener(oldCallback);
    }

    _buttonCallbacks[button] = callback;
    button.onClick.AddListener(callback);
  }

  /// <summary>
  /// 移除指定按钮的点击监听器
  /// </summary>
  /// <param name="button">按钮组件</param>
  protected void RemoveClickListener(Button button)
  {
    if (button != null && _buttonCallbacks.TryGetValue(button, out var callback))
    {
      button.onClick.RemoveListener(callback);
      _buttonCallbacks.Remove(button);
    }
  }

  /// <summary>
  /// 清理所有按钮监听器（在 OnDestroy 中自动调用）
  /// </summary>
  protected void ClearAllListeners()
  {
    foreach (var kvp in _buttonCallbacks)
    {
      if (kvp.Key != null)
      {
        kvp.Key.onClick.RemoveListener(kvp.Value);
      }
    }
    _buttonCallbacks.Clear();
  }

  #endregion

  #region 公共方法

  /// <summary>
  /// 获取本模块登记的指定 Proxy；Presenter 未绑定模块或 Proxy 未登记时返回 null。
  /// </summary>
  /// <typeparam name="T">要获取的 Proxy 类型。</typeparam>
  /// <returns>本模块的 Proxy 实例，未找到时返回 null。</returns>
  public T GetProxy<T>() where T : BaseProxy
  {
    if (Module == null)
    {
      Debug.LogError($"[{GetType().Name}] 未绑定所属模块，无法获取 {typeof(T).Name}。");
      return null;
    }

    return Module.GetProxy<T>();
  }

  /// <summary>
  /// 获取其他模块登记的指定 Proxy；用于跨模块只读数据访问，不缓存返回引用。
  /// </summary>
  /// <param name="moduleName">目标模块名。</param>
  /// <returns>目标模块的 Proxy 实例，模块未注册或未初始化时返回 null。</returns>
  public T GetProxy<T>(ModuleName moduleName) where T : BaseProxy
  {
    if (moduleName == ModuleName.None)
    {
      Debug.LogError($"[{GetType().Name}] 目标模块名为 None，无法获取 {typeof(T).Name}。");
      return null;
    }

    if (!ModuleManager.Instance.TryGetModule(moduleName, out BaseModule targetModule) || targetModule == null)
    {
      Debug.LogError($"[{GetType().Name}] 目标模块 {moduleName} 未注册，无法获取 {typeof(T).Name}。");
      return null;
    }

    return targetModule.GetProxy<T>();
  }

  /// <summary>
  /// 关闭自己
  /// </summary>
  public void Close()
  {
    if (_isDestroyed) return;
    UIManager.Instance.CloseWindow(this);
  }

  #endregion
}

#region 泛型版本（类型安全）

/// <summary>
/// 泛型 BasePresenter（类型安全的 View）
/// </summary>
/// <typeparam name="TView">视图类型</typeparam>
public abstract class BasePresenter<TView> : BasePresenter where TView : UIView
{
  /// <summary>
  /// 强类型视图引用
  /// </summary>
  protected TView ViewT
  {
    get
    {
      if (!TryEnsureInitialized()) return null;
      if (View is TView typedView) return typedView;
      Debug.LogError($"[{GetType().Name}] Bound View must be {typeof(TView).Name}, got {View.GetType().Name}.");
      return null;
    }
  }

  public override void OnInit(UIView view)
  {
    if (view is not TView)
    {
      Debug.LogError($"[{GetType().Name}] View type mismatch: expected {typeof(TView)}, got {view?.GetType().Name ?? "null"}.");
      return;
    }

    base.OnInit(view);
  }
}

/// <summary>
/// 泛型 BasePresenter（类型安全的 View 和参数）
/// </summary>
/// <typeparam name="TView">视图类型</typeparam>
/// <typeparam name="TArgs">参数类型（建议使用 struct）</typeparam>
public abstract class BasePresenter<TView, TArgs> : BasePresenter<TView>
    where TView : UIView
    where TArgs : struct
{
  /// <summary>
  /// 封装 object 版本，提供类型安全的参数
  /// </summary>
  public sealed override void OnOpen(object args)
  {
    if (args is TArgs typedArgs)
    {
      OnOpen(typedArgs);
      return;
    }

    Debug.LogError(
      $"[{GetType().Name}] Open arguments must be {typeof(TArgs).Name}; got {args?.GetType().Name ?? "null"}.");
  }

  /// <summary>
  /// 类型安全的 OnOpen 方法（子类重写）
  /// </summary>
  /// <param name="args">类型安全的参数</param>
  public virtual void OnOpen(TArgs args)
  {
    if (!TryEnsureInitialized()) return;
    base.OnOpen(null);
  }
}

#endregion
