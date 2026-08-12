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
      throw new ArgumentNullException(nameof(handler));
    }

    if (_handlers.ContainsKey(cmd))
    {
      throw new InvalidOperationException($"Handler already registered. Cmd: {cmd}");
    }

    Action<IMessage> wrapper =
      message =>
        {
          if (message is not T typedMessage)
          {
            throw new InvalidCastException(
                      $"Message type mismatch. " +
                      $"Cmd: {cmd}, " +
                      $"Expected: {typeof(T).Name}, " +
                      $"Actual: {message?.GetType().Name}"
                  );
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
      throw new ArgumentNullException(nameof(message));
    }

    if (!_handlers.TryGetValue(cmd, out Action<IMessage> handler))
    {
      UnityEngine.Debug.LogWarning($"Message handler not found. Cmd: {cmd}");
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