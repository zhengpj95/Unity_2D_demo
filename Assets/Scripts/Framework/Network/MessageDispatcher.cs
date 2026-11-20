using System;
using System.Collections.Generic;
using Google.Protobuf;

public class MessageDispatcher
{
  private readonly Dictionary<uint, Action<IMessage>> _handlers = new();


  #region Register

  /// <summary>
  /// 注册消息处理器
  /// </summary>
  public void Register<T>(uint cmd, Action<T> handler) where T : IMessage<T>
  {
    if (handler == null)
    {
      UnityEngine.Debug.LogError($"[MessageDispatcher] 注册失败：Cmd={cmd} 的 handler 为空。");
      return;
    }

    if (_handlers.ContainsKey(cmd))
    {
      UnityEngine.Debug.LogError($"[MessageDispatcher] 注册失败：Cmd={cmd} 已重复注册，已忽略本次注册。");
      return;
    }

    Action<IMessage> wrapper =
      message =>
        {
          if (message is not T typedMessage)
          {
            UnityEngine.Debug.LogError(
              $"[MessageDispatcher] 消息类型不匹配，已忽略：Cmd={cmd}, " +
              $"Expected={typeof(T).Name}, Actual={message?.GetType().Name}");
            return;
          }

          handler(typedMessage);
        };

    _handlers.Add(cmd, wrapper);
  }


  /// <summary>
  /// 注销消息处理器
  /// </summary>
  public bool Unregister(uint cmd)
  {
    return _handlers.Remove(cmd);
  }

  #endregion


  #region Dispatch

  /// <summary>
  /// 分发消息
  /// </summary>
  public void Dispatch(uint cmd, IMessage message)
  {
    if (message == null)
    {
      UnityEngine.Debug.LogError($"[MessageDispatcher] 分发失败：Cmd={cmd} 的 message 为空。");
      return;
    }

    if (!_handlers.TryGetValue(cmd, out Action<IMessage> handler))
    {
      UnityEngine.Debug.LogWarning(
          $"[NetworkMgr] 协议未注册处理逻辑：Cmd={cmd}, MessageType={message.GetType().Name}");
      return;
    }

    handler(message);
  }

  #endregion


  #region Query

  public bool Contains(uint cmd)
  {
    return _handlers.ContainsKey(cmd);
  }


  public void Clear()
  {
    _handlers.Clear();
  }

  #endregion
}
