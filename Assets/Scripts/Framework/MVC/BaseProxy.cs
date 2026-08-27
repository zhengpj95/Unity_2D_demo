using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>
/// 模块的数据与协议访问基类。每个 Proxy 独立管理自己注册的网络协议。
/// </summary>
public abstract class BaseProxy
{
  private readonly HashSet<uint> _registeredCommands = new();

  public BaseModule Module { get; private set; }
  public bool IsInitialized { get; private set; }

  internal void Initialize(BaseModule module)
  {
    if (IsInitialized) return;
    Module = module ?? throw new ArgumentNullException(nameof(module));
    OnInit();
    IsInitialized = true;
  }

  internal void Release()
  {
    if (!IsInitialized) return;

    // 先停止协议回调，避免释放业务数据时再次收到网络消息。
    foreach (uint command in _registeredCommands)
      NetworkMgr.Instance.UnregisterHandler(command);
    _registeredCommands.Clear();

    OnRelease();
    Module = null;
    IsInitialized = false;
  }

  /// <summary>注册由当前 Proxy 管理生命周期的协议处理器。</summary>
  protected void RegisterHandler<T>(uint command, Action<T> handler) where T : IMessage<T>
  {
    if (handler == null)
      throw new ArgumentNullException(nameof(handler));
    if (!_registeredCommands.Add(command))
      throw new InvalidOperationException($"[{GetType().Name}] Handler already registered: {command}");
    NetworkMgr.Instance.RegisterHandler(command, handler);
  }

  protected virtual void OnInit() { }
  protected virtual void OnRelease() { }
}
