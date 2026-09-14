using System;
using System.Collections.Generic;

/// <summary>
/// 统一事件消息。所有 EventBus 监听器都会收到该结构，EventType 表示事件类型，Data 保存可选数据。
/// </summary>
public readonly struct EventContext
{
  /// <summary>事件类型，同时也是 EventBus 的路由键。</summary>
  public string EventType { get; }

  /// <summary>事件携带的数据；无参事件时为 null。</summary>
  public object Data { get; }

  /// <summary>事件是否携带非 null 数据。</summary>
  public bool HasData => Data != null;

  internal EventContext(string eventType, object data)
  {
    EventType = eventType;
    Data = data;
  }

  /// <summary>
  /// 尝试读取指定类型的事件数据；无数据或类型不匹配时返回 false。
  /// </summary>
  public bool TryGetData<T>(out T data)
  {
    if (Data is T typedData)
    {
      data = typedData;
      return true;
    }

    data = default;
    return false;
  }

  /// <summary>
  /// 获取指定类型的事件数据；无数据或类型不匹配时抛出异常，适合参数类型固定的事件。
  /// </summary>
  public T GetData<T>()
  {
    if (TryGetData(out T data)) return data;

    string actualType = Data == null ? "null" : Data.GetType().Name;
    throw new InvalidCastException(
      $"Event '{EventType}' data type mismatch. Expected: {typeof(T).Name}, Actual: {actualType}.");
  }
}

/// <summary>
/// 游戏全局事件中心。事件统一分发 EventContext，并按 owner 管理监听生命周期。
/// </summary>
public static class EventBus
{
  private sealed class EventSubscription
  {
    public object Owner;
    public Action<EventContext> Handler;
  }

  private static readonly Dictionary<string, List<EventSubscription>> EventTable = new();

  /// <summary>
  /// 监听指定事件。监听器统一接收包含 EventType、Data 和 HasData 的 EventContext。
  /// </summary>
  public static void On(string eventName, Action<EventContext> listener, object owner)
  {
    ValidateEventName(eventName);
    ValidateOwner(owner);
    if (listener == null) throw new ArgumentNullException(nameof(listener));

    if (!EventTable.TryGetValue(eventName, out List<EventSubscription> subscriptions))
    {
      subscriptions = new List<EventSubscription>();
      EventTable.Add(eventName, subscriptions);
    }

    foreach (EventSubscription subscription in subscriptions)
    {
      if (ReferenceEquals(subscription.Owner, owner) && Equals(subscription.Handler, listener))
        return;
    }

    subscriptions.Add(new EventSubscription
    {
      Owner = owner,
      Handler = listener
    });
  }

  /// <summary>取消指定 owner 的事件监听。</summary>
  public static void Off(string eventName, Action<EventContext> listener, object owner)
  {
    ValidateOwner(owner);
    if (!EventTable.TryGetValue(eventName, out List<EventSubscription> subscriptions)) return;

    for (int i = subscriptions.Count - 1; i >= 0; i--)
    {
      EventSubscription subscription = subscriptions[i];
      if (ReferenceEquals(subscription.Owner, owner) && Equals(subscription.Handler, listener))
        subscriptions.RemoveAt(i);
    }

    if (subscriptions.Count == 0) EventTable.Remove(eventName);
  }

  /// <summary>发出不携带数据的事件。</summary>
  public static void Emit(string eventName)
  {
    Dispatch(new EventContext(eventName, null));
  }

  /// <summary>发出携带数据的事件，数据会保存在 EventContext.Data 中。</summary>
  public static void Emit<T>(string eventName, T data)
  {
    Dispatch(new EventContext(eventName, data));
  }

  /// <summary>清空所有事件监听。</summary>
  public static void OffAll()
  {
    EventTable.Clear();
  }

  /// <summary>清空指定 owner 的全部事件监听。</summary>
  public static void OffAll(object owner)
  {
    ValidateOwner(owner);

    List<string> emptyEvents = null;
    foreach (KeyValuePair<string, List<EventSubscription>> pair in EventTable)
    {
      pair.Value.RemoveAll(item => ReferenceEquals(item.Owner, owner));
      if (pair.Value.Count != 0) continue;

      emptyEvents ??= new List<string>();
      emptyEvents.Add(pair.Key);
    }

    if (emptyEvents == null) return;
    foreach (string eventName in emptyEvents) EventTable.Remove(eventName);
  }

  private static void Dispatch(EventContext message)
  {
    ValidateEventName(message.EventType);
    if (!EventTable.TryGetValue(message.EventType, out List<EventSubscription> subscriptions)) return;

    EventSubscription[] snapshot = subscriptions.ToArray();
    foreach (EventSubscription subscription in snapshot)
      subscription.Handler.Invoke(message);
  }

  private static void ValidateEventName(string eventName)
  {
    if (string.IsNullOrWhiteSpace(eventName))
      throw new ArgumentException("Event name cannot be null or empty.", nameof(eventName));
  }

  private static void ValidateOwner(object owner)
  {
    if (owner == null) throw new ArgumentNullException(nameof(owner));
  }
}
