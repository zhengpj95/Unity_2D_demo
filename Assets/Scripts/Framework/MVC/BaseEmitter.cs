using System;
using System.Collections.Generic;

/// <summary>
/// 带生命周期的 EventBus 使用基类，统一记录并清理由当前对象创建的事件订阅。
/// </summary>
public abstract class BaseEmitter
{
  private sealed class EventSubscription
  {
    public string EventName;
    public Action<EventContext> Listener;
  }

  private readonly List<EventSubscription> _subscriptions = new();

  /// <summary>监听事件；所有监听器统一接收 EventContext。</summary>
  protected void On(string eventName, Action<EventContext> listener)
  {
    ValidateEventName(eventName);
    if (listener == null) throw new ArgumentNullException(nameof(listener));

    foreach (EventSubscription subscription in _subscriptions)
    {
      if (subscription.EventName == eventName && Equals(subscription.Listener, listener))
        return;
    }

    EventBus.On(eventName, listener, this);
    _subscriptions.Add(new EventSubscription
    {
      EventName = eventName,
      Listener = listener
    });
  }

  /// <summary>发出不携带数据的事件。</summary>
  protected void Emit(string eventName)
  {
    ValidateEventName(eventName);
    EventBus.Emit(eventName);
  }

  /// <summary>发出携带数据的事件。</summary>
  protected void Emit<T>(string eventName, T data)
  {
    ValidateEventName(eventName);
    EventBus.Emit(eventName, data);
  }

  /// <summary>解除当前对象通过 On 创建的全部事件订阅；可重复调用。</summary>
  protected void OffAll()
  {
    for (int i = _subscriptions.Count - 1; i >= 0; i--)
    {
      EventSubscription subscription = _subscriptions[i];
      EventBus.Off(subscription.EventName, subscription.Listener, this);
    }

    _subscriptions.Clear();
    EventBus.OffAll(this);
  }

  protected static void ValidateEventName(string eventName)
  {
    if (string.IsNullOrWhiteSpace(eventName))
      throw new ArgumentException("Event name cannot be null or empty.", nameof(eventName));
  }
}
