using System;
using System.Collections.Generic;

/// <summary>
/// 业务模块根节点，统一持有本模块的 Command、Proxy、Presenter 与事件订阅。
/// </summary>
public abstract class BaseModule
{
  private readonly Dictionary<Type, UIPresenter> _presenters = new();
  private readonly Dictionary<Type, BaseProxy> _proxies = new();
  private readonly Dictionary<Type, BaseCommand> _commands = new();
  private readonly List<Action> _eventUnregisterActions = new();

  public abstract ModuleName ModuleName { get; }
  public ModuleManager Manager { get; private set; }
  public bool IsInitialized { get; private set; }
  public bool IsRunning { get; private set; }

  internal void Initialize(ModuleManager manager)
  {
    if (IsInitialized) return;

    Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    OnInit();

    foreach (BaseProxy proxy in _proxies.Values) proxy.Initialize(this);
    IsInitialized = true;
    IsRunning = true;
  }

  internal void UpdateModule()
  {
    if (IsInitialized && IsRunning) OnUpdate();
  }

  internal void Release()
  {
    if (!IsInitialized) return;

    IsRunning = false;

    // 先解绑事件和协议，避免释放期间收到回调访问已释放对象。
    for (int i = _eventUnregisterActions.Count - 1; i >= 0; i--)
      _eventUnregisterActions[i].Invoke();
    _eventUnregisterActions.Clear();

    foreach (BaseProxy proxy in _proxies.Values) proxy.Release();
    foreach (UIPresenter presenter in _presenters.Values) UIManager.Instance.DestroyWindow(presenter);

    OnRelease();
    foreach (BaseCommand command in _commands.Values) command.SetModule(null);
    _presenters.Clear();
    _commands.Clear();
    _proxies.Clear();
    Manager = null;
    IsInitialized = false;
  }

  protected virtual void OnInit() { }
  protected virtual void OnUpdate() { }
  protected virtual void OnRelease() { }

  /// <summary>登记本模块持有的 Presenter；模块释放时自动销毁。</summary>
  protected T RegPresenter<T>(T presenter) where T : UIPresenter
  {
    if (presenter == null) throw new ArgumentNullException(nameof(presenter));
    RegisterUnique(_presenters, presenter, "Presenter");
    return presenter;
  }

  /// <summary>打开界面并登记 Presenter，使其生命周期归属当前模块。</summary>
  protected T OpenWindow<T>(string prefabPath, UILayerIndex layer, object args = null) where T : UIPresenter, new()
  {
    T presenter = UIManager.Instance.OpenWindow<T>(prefabPath, layer, args);
    return presenter == null ? null : RegPresenter(presenter);
  }

  /// <summary>登记 Proxy；模块已运行时会立即初始化该 Proxy。</summary>
  protected T RegProxy<T>(T proxy) where T : BaseProxy
  {
    if (proxy == null) throw new ArgumentNullException(nameof(proxy));
    RegisterUnique(_proxies, proxy, "Proxy");
    if (IsInitialized) proxy.Initialize(this);
    return proxy;
  }

  protected T RegProxy<T>() where T : BaseProxy, new() => RegProxy(new T());

  /// <summary>将无参数事件绑定到 Command；模块释放时自动取消监听。</summary>
  protected T RegCmd<T>(string eventName, T command) where T : BaseCommand
  {
    if (command == null) throw new ArgumentNullException(nameof(command));
    ValidateEventName(eventName);
    RegisterCommand(command);

    Action listener = () => command.Execute();
    EventBus.AddListener(eventName, listener);
    _eventUnregisterActions.Add(() => EventBus.RemoveListener(eventName, listener));
    return command;
  }

  protected T RegCmd<T>(string eventName) where T : BaseCommand, new() => RegCmd(eventName, new T());

  /// <summary>将带参数事件绑定到 Command；事件参数会作为 Execute 的 args 传入。</summary>
  protected TCommand RegCmd<TCommand, TArgs>(string eventName, TCommand command) where TCommand : BaseCommand
  {
    if (command == null) throw new ArgumentNullException(nameof(command));
    ValidateEventName(eventName);
    RegisterCommand(command);

    Action<TArgs> listener = args => command.Execute(args);
    EventBus.AddListener(eventName, listener);
    _eventUnregisterActions.Add(() => EventBus.RemoveListener(eventName, listener));
    return command;
  }

  public T GetPresenter<T>() where T : UIPresenter => _presenters.TryGetValue(typeof(T), out UIPresenter value) ? value as T : null;
  public T GetProxy<T>() where T : BaseProxy => _proxies.TryGetValue(typeof(T), out BaseProxy value) ? value as T : null;
  public T GetCommand<T>() where T : BaseCommand => _commands.TryGetValue(typeof(T), out BaseCommand value) ? value as T : null;

  private void RegisterCommand(BaseCommand command)
  {
    RegisterUnique(_commands, command, "Command");
    command.SetModule(this);
  }

  private void RegisterUnique<T>(Dictionary<Type, T> items, T item, string itemType)
  {
    Type type = item.GetType();
    if (items.ContainsKey(type))
      throw new InvalidOperationException($"[{GetType().Name}] {itemType} already registered: {type.Name}");
    items.Add(type, item);
  }

  private static void ValidateEventName(string eventName)
  {
    if (string.IsNullOrWhiteSpace(eventName))
      throw new ArgumentException("Event name cannot be null or empty.", nameof(eventName));
  }
}
