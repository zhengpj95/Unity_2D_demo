using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 业务模块根节点，统一持有本模块的 Command、Proxy、Presenter 与事件订阅。
/// </summary>
public abstract class BaseModule : BaseEmitter
{
  // 保存模块中 ViewType 与 Presenter 类型的登记信息；这里只记录类型，不保存 Presenter 实例。
  // 语法：private 表示仅本类可访问，sealed 表示不可被继承，class 表示声明一个引用类型。
  private sealed class PresenterDefinition
  {
    // Type 表示 .NET 类型信息；这里通常保存 typeof(T) 的结果，例如 typeof(MainMenuPresenter)。
    public Type PresenterType;
    // 保存已通过泛型约束校验的构造工厂，非泛型打开接口无需反射创建 Presenter。
    public Func<BasePresenter> Factory;
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
      PresenterType = typeof(T),
      Factory = () => new T()
    });
  }

  /// <summary>按本模块已登记的 ViewType 打开界面；未登记时抛出异常，资源加载失败时返回 null。</summary>
  /// <param name="viewType">本模块注册的界面类型。</param>
  /// <param name="args">传入 Presenter.OnOpen 的运行时参数，可为 null。</param>
  /// <returns>打开或显示后的 Presenter；Prefab 加载失败时返回 null。</returns>
  public BasePresenter OpenWindow(Enum viewType, object args = null)
  {
    if (viewType == null) throw new ArgumentNullException(nameof(viewType));
    if (!_presenterDefinitions.TryGetValue(viewType, out PresenterDefinition definition))
      throw new InvalidOperationException($"[{GetType().Name}] ViewType is not registered: {viewType}");

    ModuleViewKey viewKey = new(ModuleName, viewType);
    BasePresenter cached = UIManager.Instance.GetPresenter(viewKey);
    if (cached != null)
    {
      if (cached.GetType() != definition.PresenterType)
        throw new InvalidOperationException($"ViewType {viewType} is bound to {definition.PresenterType.Name}, but cached {cached.GetType().Name}.");
      UIManager.Instance.ShowPresenter(cached, args);
      return cached;
    }

    return UIManager.Instance.OpenWindow(viewKey, definition.Factory, args);
  }

  /// <summary>在指定局部容器中打开本模块已登记的界面，用于复合界面的子 Presenter。</summary>
  /// <param name="viewType">本模块注册的界面类型。</param>
  /// <param name="parent">Presenter View 的挂载节点，不能为 null。</param>
  /// <param name="args">传入 Presenter.OnOpen 的运行时参数，可为 null。</param>
  /// <returns>打开或显示后的 Presenter；Prefab 加载失败时返回 null。</returns>
  public BasePresenter OpenWindowIn(Enum viewType, Transform parent, object args = null)
  {
    if (viewType == null) throw new ArgumentNullException(nameof(viewType));
    if (parent == null) throw new ArgumentNullException(nameof(parent));
    if (!_presenterDefinitions.TryGetValue(viewType, out PresenterDefinition definition))
      throw new InvalidOperationException($"[{GetType().Name}] ViewType is not registered: {viewType}");

    ModuleViewKey viewKey = new(ModuleName, viewType);
    BasePresenter presenter = UIManager.Instance.OpenWindowIn(viewKey, definition.Factory, parent, args);
    if (presenter != null && presenter.GetType() != definition.PresenterType)
      throw new InvalidOperationException($"ViewType {viewType} is bound to {definition.PresenterType.Name}, but UIManager returned {presenter.GetType().Name}.");
    return presenter;
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
  protected T RegCmd<T>(int eventId) where T : BaseCommand, new()
  {
    ValidateEventId(eventId);
    T command = new T();
    RegisterCommand(command);

    // 所有事件统一携带 EventContext，Command 可同时读取事件类型和可选数据。
    Action<EventContext> listener = command.Execute;
    On(eventId, listener);
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
