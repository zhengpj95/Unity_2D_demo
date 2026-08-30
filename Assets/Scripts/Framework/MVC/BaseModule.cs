using System;
using System.Collections.Generic;

/// <summary>
/// 业务模块根节点，统一持有本模块的 Command、Proxy、Presenter 与事件订阅。
/// </summary>
public abstract class BaseModule : BaseEmitter
{
  private sealed class PresenterDefinition
  {
    public Type PresenterType;
  }

  private readonly Dictionary<Enum, PresenterDefinition> _presenterDefinitions = new();
  private readonly Dictionary<Type, BaseProxy> _proxies = new();
  private readonly Dictionary<Type, BaseCommand> _commands = new();

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
    OffAll();

    foreach (BaseProxy proxy in _proxies.Values) proxy.Release();
    foreach (Enum viewType in _presenterDefinitions.Keys)
    {
      BasePresenter presenter = UIManager.Instance.GetPresenter(new ModuleViewKey(ModuleName, viewType));
      if (presenter != null) UIManager.Instance.DestroyWindow(presenter);
    }

    OnRelease();
    foreach (BaseCommand command in _commands.Values) command.Release();
    _presenterDefinitions.Clear();
    _commands.Clear();
    _proxies.Clear();
    Manager = null;
    IsInitialized = false;
  }

  protected virtual void OnInit() { }
  protected virtual void OnUpdate() { }
  protected virtual void OnRelease() { }

  /// <summary>登记 ViewType 与 Presenter 的一一对应关系，打开时才实例化 Presenter。</summary>
  protected void RegPresenter<T>(Enum viewType) where T : BasePresenter, new()
  {
    if (viewType == null) throw new ArgumentNullException(nameof(viewType));
    if (_presenterDefinitions.ContainsKey(viewType))
      throw new InvalidOperationException($"[{GetType().Name}] ViewType already registered: {viewType}");
    if (HasPresenterType(typeof(T)))
      throw new InvalidOperationException($"[{GetType().Name}] Presenter already registered: {typeof(T).Name}");

    _presenterDefinitions.Add(viewType, new PresenterDefinition
    {
      PresenterType = typeof(T)
    });
  }

  /// <summary>按模块 ViewType 打开界面；首次打开时根据 RegPresenter 映射实例化。</summary>
  protected T OpenWindow<T>(Enum viewType, object args = null) where T : BasePresenter, new()
  {
    if (viewType == null) throw new ArgumentNullException(nameof(viewType));
    if (!_presenterDefinitions.TryGetValue(viewType, out PresenterDefinition definition))
      throw new InvalidOperationException($"[{GetType().Name}] ViewType is not registered: {viewType}");
    if (definition.PresenterType != typeof(T))
      throw new InvalidOperationException($"ViewType {viewType} is bound to {definition.PresenterType.Name}, not {typeof(T).Name}.");

    ModuleViewKey viewKey = new(ModuleName, viewType);
    BasePresenter cached = UIManager.Instance.GetPresenter(viewKey);
    if (cached != null)
    {
      UIManager.Instance.ShowPresenter(cached, args);
      if (cached is T cachedPresenter) return cachedPresenter;
      throw new InvalidOperationException($"ViewType {viewType} is bound to {cached.GetType().Name}, not {typeof(T).Name}.");
    }

    T presenter = UIManager.Instance.OpenWindow<T>(viewKey, args);
    if (presenter == null) return null;
    return presenter;
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

  /// <summary>将事件绑定到 Command；参数由 Dispatch 在触发时传入，模块释放时自动取消监听。</summary>
  protected T RegCmd<T>(string eventName) where T : BaseCommand, new()
  {
    ValidateEventName(eventName);
    T command = new T();
    RegisterCommand(command);

    // 统一使用 Action<object>，这样无参和有参 Dispatch 都能进入同一个 Command。
    Action<object> listener = args => command.Execute(args);
    On(eventName, listener);
    return command;
  }

  public T GetProxy<T>() where T : BaseProxy => _proxies.TryGetValue(typeof(T), out BaseProxy value) ? value as T : null;
  public T GetCommand<T>() where T : BaseCommand => _commands.TryGetValue(typeof(T), out BaseCommand value) ? value as T : null;

  /// <summary>按模块 ViewType 获取已实例化的 Presenter；未打开时返回 null。</summary>
  public BasePresenter GetPresenter(Enum viewType)
  {
    if (viewType == null) throw new ArgumentNullException(nameof(viewType));
    ModuleViewKey viewKey = new(ModuleName, viewType);
    return UIManager.Instance.GetPresenter(viewKey);
  }

  private bool HasPresenterType(Type presenterType)
  {
    foreach (PresenterDefinition definition in _presenterDefinitions.Values)
      if (definition.PresenterType == presenterType) return true;
    return false;
  }

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

}
