using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI控制器管理器
/// 负责管理所有UI控制器的创建、缓存和生命周期
/// </summary>
public class UIControllerManager : Singleton<UIControllerManager>
{
  /// <summary>
  /// 控制器缓存
  /// </summary>
  private readonly Dictionary<string, IController> _controllers = new Dictionary<string, IController>();

  /// <summary>
  /// 注册控制器
  /// </summary>
  /// <typeparam name="TController">控制器类型</typeparam>
  /// <typeparam name="TData">数据类型</typeparam>
  /// <param name="controller">控制器实例</param>
  /// <param name="key">键名（可选，默认为类型名）</param>
  public void Register<TController, TData>(TController controller, string key = null)
      where TController : IController<TData>
      where TData : new()
  {
    string controllerKey = key ?? typeof(TController).Name;

    if (_controllers.ContainsKey(controllerKey))
    {
      Debug.LogWarning($"[{GetType().Name}] Controller already registered: {controllerKey}");
      return;
    }

    _controllers[controllerKey] = controller;
    Debug.Log($"[{GetType().Name}] Registered controller: {controllerKey}");
  }

  /// <summary>
  /// 获取控制器
  /// </summary>
  /// <typeparam name="TController">控制器类型</typeparam>
  /// <param name="key">键名（可选）</param>
  /// <returns>控制器实例</returns>
  public TController Get<TController>(string key = null) where TController : class, IController
  {
    string controllerKey = key ?? typeof(TController).Name;
    _controllers.TryGetValue(controllerKey, out var controller);
    return controller as TController;
  }

  /// <summary>
  /// 注销控制器
  /// </summary>
  /// <param name="key">键名</param>
  public void Unregister(string key)
  {
    if (_controllers.TryGetValue(key, out var controller))
    {
      controller.Cleanup();
      _controllers.Remove(key);
      Debug.Log($"[{GetType().Name}] Unregistered controller: {key}");
    }
  }

  /// <summary>
  /// 注销控制器
  /// </summary>
  /// <typeparam name="TController">控制器类型</typeparam>
  public void Unregister<TController>() where TController : IController
  {
    Unregister(typeof(TController).Name);
  }

  /// <summary>
  /// 清理所有控制器
  /// </summary>
  public void Clear()
  {
    foreach (var kvp in _controllers)
    {
      kvp.Value.Cleanup();
    }
    _controllers.Clear();
  }

  /// <summary>
  /// 获取所有控制器
  /// </summary>
  public Dictionary<string, IController>.ValueCollection GetAllControllers()
  {
    return _controllers.Values;
  }

  /// <summary>
  /// 检查控制器是否存在
  /// </summary>
  public bool HasController(string key)
  {
    return _controllers.ContainsKey(key);
  }

  /// <summary>
  /// 检查控制器是否存在
  /// </summary>
  public bool HasController<TController>() where TController : IController
  {
    return HasController(typeof(TController).Name);
  }
}

/// <summary>
/// 控制器工厂
/// 提供便捷的控制器创建和管理方法
/// </summary>
public static class UIControllerFactory
{
  /// <summary>
  /// 创建并初始化控制器
  /// </summary>
  /// <typeparam name="TController">控制器类型</typeparam>
  /// <typeparam name="TData">数据类型</typeparam>
  /// <param name="view">视图实例</param>
  /// <param name="register">是否注册到管理器</param>
  /// <returns>控制器实例</returns>
  public static TController Create<TController, TData>(IView<TData> view, bool register = true)
      where TController : UIController<TData>, new()
      where TData : new()
  {
    var controller = new TController();
    controller.Initialize(view);

    if (register)
    {
      UIControllerManager.Instance.Register<TController, TData>(controller);
    }

    return controller;
  }

  /// <summary>
  /// 从管理器获取或创建控制器
  /// </summary>
  public static TController GetOrCreate<TController, TData>(IView<TData> view)
      where TController : UIController<TData>, new()
      where TData : new()
  {
    var controller = UIControllerManager.Instance.Get<TController>();
    if (controller == null)
    {
      controller = Create<TController, TData>(view);
    }
    return controller;
  }
}