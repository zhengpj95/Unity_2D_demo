using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 业务模块基类。一个模块集中管理自己的 Presenter、Proxy 和 Command。
/// </summary>
public abstract class BaseModule
{
  private readonly Dictionary<Type, UIPresenter> _presenters = new();
  private readonly Dictionary<Type, BaseProxy> _proxies = new();
  private readonly Dictionary<Type, BaseCommand> _commands = new();

  public abstract ModuleName ModuleName { get; }
  public ModuleManager Manager { get; private set; }
  public bool IsInitialized { get; private set; }
  public bool IsRunning { get; private set; }

  internal void Initialize(ModuleManager manager)
  {
    if (IsInitialized)
    {
      return;
    }

    Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    OnInit();

    foreach (BaseProxy proxy in _proxies.Values)
    {
      proxy.Initialize(this);
    }

    foreach (BaseCommand command in _commands.Values)
    {
      command.Initialize(this);
    }

    IsInitialized = true;
    IsRunning = true;
  }

  internal void UpdateModule()
  {
    if (IsInitialized && IsRunning)
    {
      OnUpdate();
    }
  }

  internal void Release()
  {
    if (!IsInitialized)
    {
      return;
    }

    IsRunning = false;
    OnRelease();

    foreach (UIPresenter presenter in _presenters.Values)
    {
      UIManager.Instance.DestroyWindow(presenter);
    }

    foreach (BaseCommand command in _commands.Values)
    {
      command.Release();
    }

    foreach (BaseProxy proxy in _proxies.Values)
    {
      proxy.Release();
    }

    _presenters.Clear();
    _commands.Clear();
    _proxies.Clear();
    Manager = null;
    IsInitialized = false;
  }

  protected virtual void OnInit()
  {
  }

  protected virtual void OnUpdate()
  {
  }

  protected virtual void OnRelease()
  {
  }

  protected T RegisterPresenter<T>(T presenter) where T : UIPresenter
  {
    if (presenter == null)
    {
      throw new ArgumentNullException(nameof(presenter));
    }

    RegisterUnique(_presenters, presenter, "Presenter");
    return presenter;
  }

  protected T OpenWindow<T>(string prefabPath, UILayerIndex layer, object args = null)
    where T : UIPresenter, new()
  {
    return RegisterPresenter(UIManager.Instance.OpenWindow<T>(prefabPath, layer, args));
  }

  protected T RegisterProxy<T>(T proxy) where T : BaseProxy
  {
    if (proxy == null)
    {
      throw new ArgumentNullException(nameof(proxy));
    }

    RegisterUnique(_proxies, proxy, "Proxy");
    if (IsInitialized)
    {
      proxy.Initialize(this);
    }
    return proxy;
  }

  protected T RegisterCommand<T>(T command) where T : BaseCommand
  {
    if (command == null)
    {
      throw new ArgumentNullException(nameof(command));
    }

    RegisterUnique(_commands, command, "Command");
    if (IsInitialized)
    {
      command.Initialize(this);
    }
    return command;
  }

  public T GetPresenter<T>() where T : UIPresenter
  {
    return _presenters.TryGetValue(typeof(T), out UIPresenter presenter) ? presenter as T : null;
  }

  public T GetProxy<T>() where T : BaseProxy
  {
    return _proxies.TryGetValue(typeof(T), out BaseProxy proxy) ? proxy as T : null;
  }

  public T GetCommand<T>() where T : BaseCommand
  {
    return _commands.TryGetValue(typeof(T), out BaseCommand command) ? command as T : null;
  }

  private void RegisterUnique<T>(Dictionary<Type, T> items, T item, string itemType)
  {
    Type type = item.GetType();
    if (items.ContainsKey(type))
    {
      throw new InvalidOperationException($"[{GetType().Name}] {itemType} already registered: {type.Name}");
    }

    items.Add(type, item);
  }
}