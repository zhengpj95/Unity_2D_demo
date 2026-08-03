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
public abstract class UIPresenter
{
  public UIView View { get; private set; }
  public bool IsVisible { get; private set; }

  // 是否需要每帧更新
  public bool NeedUpdate { get; protected set; }

  // 缓存的按钮回调（用于自动清理）
  private readonly Dictionary<Button, UnityAction> _buttonCallbacks = new();

  #region 生命周期方法

  // 初始化（加载 Prefab 后调用一次）
  public virtual void OnInit(UIView view)
  {
    View = view;
  }

  // 打开界面
  public virtual void OnOpen(object args = null)
  {
    IsVisible = true;
    if (View != null) View.gameObject.SetActive(true);
    OnShow();
  }

  // 关闭界面
  public virtual void OnClose()
  {
    OnHide();
    IsVisible = false;
    if (View != null) View.gameObject.SetActive(false);
  }

  // 销毁界面
  public virtual void OnDestroy()
  {
    // 清理所有按钮监听器
    ClearAllListeners();

    if (View != null)
    {
      UnityEngine.Object.Destroy(View.gameObject);
      View = null;
    }
  }

  // 每次从隐藏到显示时调用（例如从缓存恢复）
  public virtual void OnShow() { }

  // 每次从显示到隐藏时调用（不销毁）
  public virtual void OnHide() { }

  // 每帧更新（需要设置 NeedUpdate = true）
  public virtual void Update() { }

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
    if (_buttonCallbacks.ContainsKey(button))
    {
      button.onClick.RemoveListener(_buttonCallbacks[button]);
      _buttonCallbacks.Remove(button);
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
  /// 关闭自己
  /// </summary>
  public void Close()
  {
    UIManager.Instance.CloseWindow(this);
  }

  #endregion
}

#region 泛型版本（类型安全）

/// <summary>
/// 泛型 UIPresenter（类型安全的 View）
/// </summary>
/// <typeparam name="TView">视图类型</typeparam>
public abstract class UIPresenter<TView> : UIPresenter where TView : UIView
{
  /// <summary>
  /// 强类型视图引用
  /// </summary>
  protected TView ViewT => View as TView;

  public override void OnInit(UIView view)
  {
    base.OnInit(view);

    if (view is not TView)
    {
      Debug.LogError($"[{GetType().Name}] View type mismatch: expected {typeof(TView)}, got {view?.GetType()}");
    }
  }
}

/// <summary>
/// 泛型 UIPresenter（类型安全的 View 和参数）
/// </summary>
/// <typeparam name="TView">视图类型</typeparam>
/// <typeparam name="TArgs">参数类型（建议使用 struct）</typeparam>
public abstract class UIPresenter<TView, TArgs> : UIPresenter<TView>
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
    }
    else
    {
      if (args != null)
      {
        Debug.LogError($"[{GetType().Name}] Invalid args type: expected {typeof(TArgs)}, got {args.GetType()}");
      }
      OnOpen(default);
    }
  }

  /// <summary>
  /// 类型安全的 OnOpen 方法（子类重写）
  /// </summary>
  /// <param name="args">类型安全的参数</param>
  public virtual void OnOpen(TArgs args)
  {
    base.OnOpen(null);
  }
}

#endregion
