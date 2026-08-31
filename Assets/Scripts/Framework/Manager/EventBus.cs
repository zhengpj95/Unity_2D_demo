using System;
using System.Collections.Generic;
using System.Linq;

/**
 * 全局消息中心（支持无参与有参事件）
 * 游戏全局消息中心（字符串 Key 版）
 * 支持泛型参数
 *
 * 设计目标：
 * 1. 同一个事件可以被多个界面 / 对象同时监听；
 * 2. 同一个 owner 只允许注册一次同一个回调；
 * 3. 解绑时按 owner 精确清理，不会误删其他界面的监听。
 *
 * 团队约定：
 * - 统一使用 On(eventName, listener, owner) / Off(eventName, listener, owner)
 * - owner 表示订阅归属对象，通常为 this、Presenter、Module 或 UI 实例
 * - 同一事件名允许多个 owner 监听；同一 owner + 同一 listener 仅注册一次
 * - 销毁或关闭时必须调用 Off(eventName, listener, this) 或 OffAll(this)
 * - 禁止再写 On(eventName, listener) / Off(eventName, listener) 这类无 owner 版本
 *
 * 用法示例：
 * EventBus.On("UPDATE_HP", RefreshHp, this);
 * EventBus.Off("UPDATE_HP", RefreshHp, this);
 * EventBus.Emit("UPDATE_HP");
 *
 * 复制模板：
 * // 事件名 + owner + callback，必须在生命周期结束前 OffAll(this)
 * EventBus.On("EVENT_NAME", OnEvent, this);
 * EventBus.Off("EVENT_NAME", OnEvent, this);
 */
public static class EventBus
{
  private sealed class EventSubscription
  {
    public object Owner;
    public Delegate Handler;
  }

  private sealed class EventChannel
  {
    public Type ArgumentType;
    public readonly List<EventSubscription> Subscriptions = new();
  }

  private static readonly Type NoArgumentType = typeof(void);

  // 用字符串为 key 的事件表
  private static readonly Dictionary<string, EventChannel> EventTable = new();

  #region --- 添加监听 ---

  // 主 API：强制要求提供 owner，确保同一事件可以被多个对象同时监听。
  public static void On(string eventName, Action listener, object owner)
  {
    Register(eventName, owner, listener, NoArgumentType);
  }

  public static void On<T>(string eventName, Action<T> listener, object owner)
  {
    // Action<object> 是 Command 使用的通用监听器：它不定义事件签名。
    Register(eventName, owner, listener, GetArgumentType<T>());
  }

  #endregion

  #region --- 移除监听 ---

  public static void Off(string eventName, Action listener, object owner)
  {
    Unregister(eventName, owner, listener);
  }

  public static void Off<T>(string eventName, Action<T> listener, object owner)
  {
    Unregister(eventName, owner, listener);
  }

  #endregion

  #region --- 派发事件 ---

  public static void Emit(string eventName)
  {
    if (!EventTable.TryGetValue(eventName, out var channel))
      return;

    ValidateSignature(eventName, channel, NoArgumentType);

    var snapshot = channel.Subscriptions.ToArray();
    foreach (var subscription in snapshot)
    {
      switch (subscription.Handler)
      {
        case Action action:
          action.Invoke();
          break;

        case Action<object> objectAction:
          objectAction.Invoke(null);
          break;
      }
    }
  }

  public static void Emit<T>(string eventName, T arg)
  {
    if (!EventTable.TryGetValue(eventName, out var channel))
      return;

    ValidateSignature(eventName, channel, typeof(T));

    var snapshot = channel.Subscriptions.ToArray();
    foreach (var subscription in snapshot)
    {
      switch (subscription.Handler)
      {
        case Action<T> action:
          action.Invoke(arg);
          break;

        case Action<object> objectAction:
          objectAction.Invoke(arg);
          break;
      }
    }
  }

  #endregion

  // 清空所有事件（场景切换时调用）
  public static void OffAll()
  {
    EventTable.Clear();
  }

  public static void OffAll(object owner)
  {
    ValidateOwner(owner);

    foreach (var eventName in EventTable.Keys.ToList())
    {
      var channel = EventTable[eventName];
      channel.Subscriptions.RemoveAll(item => ReferenceEquals(item.Owner, owner));

      if (channel.Subscriptions.Count == 0)
        EventTable.Remove(eventName);
    }
  }

  private static void Register(string eventName, object owner, Delegate listener, Type argumentType)
  {
    if (string.IsNullOrWhiteSpace(eventName))
      throw new ArgumentException("Event name cannot be null or empty.", nameof(eventName));

    ValidateOwner(owner);

    if (listener == null)
      throw new ArgumentNullException(nameof(listener));

    if (!EventTable.TryGetValue(eventName, out var channel))
    {
      channel = new EventChannel();
      EventTable[eventName] = channel;
    }

    ValidateSignature(eventName, channel, argumentType);

    foreach (var subscription in channel.Subscriptions)
    {
      if (ReferenceEquals(subscription.Owner, owner) && Equals(subscription.Handler, listener))
        return;
    }

    channel.Subscriptions.Add(new EventSubscription
    {
      Owner = owner,
      Handler = listener,
    });
  }

  private static void Unregister(string eventName, object owner, Delegate listener)
  {
    ValidateOwner(owner);

    if (!EventTable.TryGetValue(eventName, out var channel))
      return;

    for (int i = channel.Subscriptions.Count - 1; i >= 0; i--)
    {
      var subscription = channel.Subscriptions[i];
      if (ReferenceEquals(subscription.Owner, owner) && Equals(subscription.Handler, listener))
      {
        channel.Subscriptions.RemoveAt(i);
      }
    }

    if (channel.Subscriptions.Count == 0)
      EventTable.Remove(eventName);
  }

  private static void ValidateSignature(string eventName, EventChannel channel, Type argumentType)
  {
    if (argumentType == null)
      return;

    if (channel.ArgumentType == null)
    {
      channel.ArgumentType = argumentType;
      return;
    }

    if (channel.ArgumentType != argumentType)
      throw new InvalidOperationException($"Event '{eventName}' expects {DescribeSignature(channel.ArgumentType)}, but received {DescribeSignature(argumentType)}.");
  }

  private static string DescribeSignature(Type argumentType)
  {
    return argumentType == NoArgumentType ? "no arguments" : $"an argument of type {argumentType.Name}";
  }

  private static Type GetArgumentType<T>()
  {
    return typeof(T) == typeof(object) ? null : typeof(T);
  }

  private static void ValidateOwner(object owner)
  {
    if (owner == null)
      throw new ArgumentNullException(nameof(owner));
  }
}
