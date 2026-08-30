using System;
using System.Collections.Generic;

/// <summary>
/// 带生命周期的 EventBus 使用基类。
/// 负责记录当前对象创建的订阅，并在 OffAll 时统一解绑；不维护独立事件表。
/// </summary>
public abstract class BaseEmitter
{
  private sealed class EventSubscription
  {
    public string EventName;
    public Delegate Listener;
    public Action Unsubscribe;
  }

  private readonly List<EventSubscription> _subscriptions = new();

  protected void On(string eventName, Action listener)
  {
    Register(eventName, listener, () => EventBus.On(eventName, listener), () => EventBus.Off(eventName, listener));
  }

  protected void On(string eventName, Action<object> listener)
  {
    Register(eventName, listener, () => EventBus.On(eventName, listener), () => EventBus.Off(eventName, listener));
  }

  protected void On<T>(string eventName, Action<T> listener)
  {
    Register(eventName, listener, () => EventBus.On(eventName, listener), () => EventBus.Off(eventName, listener));
  }

  protected void Emit(string eventName)
  {
    ValidateEventName(eventName);
    EventBus.Emit(eventName);
  }

  protected void Emit<T>(string eventName, T args)
  {
    ValidateEventName(eventName);
    EventBus.Emit(eventName, args);
  }

  /// <summary>解除当前对象经由 On 创建的全部事件订阅；可重复调用。</summary>
  protected void OffAll()
  {
    for (int i = _subscriptions.Count - 1; i >= 0; i--)
      _subscriptions[i].Unsubscribe();
    _subscriptions.Clear();
  }

  private void Register(string eventName, Delegate listener, Action subscribe, Action unsubscribe)
  {
    ValidateEventName(eventName);
    if (listener == null) throw new ArgumentNullException(nameof(listener));

    foreach (EventSubscription subscription in _subscriptions)
    {
      if (subscription.EventName == eventName && Equals(subscription.Listener, listener))
        return;
    }

    subscribe();
    _subscriptions.Add(new EventSubscription
    {
      EventName = eventName,
      Listener = listener,
      Unsubscribe = unsubscribe
    });
  }

  protected static void ValidateEventName(string eventName)
  {
    if (string.IsNullOrWhiteSpace(eventName))
      throw new ArgumentException("Event name cannot be null or empty.", nameof(eventName));
  }
}
