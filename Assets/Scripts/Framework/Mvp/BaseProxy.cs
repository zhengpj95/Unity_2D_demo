using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>
/// 模块数据访问层基类。
/// </summary>
public abstract class BaseProxy
{
  private readonly HashSet<uint> _registeredCommands = new();

  public BaseModule Module { get; private set; }

  internal void Initialize(BaseModule module)
  {
    Module = module;
    OnInit();
  }

  internal void Release()
  {
    OnRelease();

    foreach (uint command in _registeredCommands)
    {
      NetworkMgr.Instance.UnregisterHandler(command);
    }
    _registeredCommands.Clear();

    Module = null;
  }

  /// <summary>
  /// 注册由当前 Proxy 管理生命周期的协议处理器。
  /// </summary>
  protected void RegisterHandler<T>(uint command, Action<T> handler)
    where T : IMessage<T>
  {
    NetworkMgr.Instance.RegisterHandler(command, handler);
    _registeredCommands.Add(command);
  }

  protected virtual void OnInit()
  {
  }

  protected virtual void OnRelease()
  {
  }
}