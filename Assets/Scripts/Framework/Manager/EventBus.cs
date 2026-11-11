using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 统一事件消息。所有 EventBus 监听器都会收到该结构，EventId 表示事件类型，Data 保存可选数据。
/// </summary>
public readonly struct EventContext
{
  /// <summary>事件 ID，同时也是 EventBus 的路由键。</summary>
  public int EventId { get; }

  /// <summary>事件携带的数据；无参事件时为 null。</summary>
  public object Data { get; }

  /// <summary>事件是否携带非 null 数据。</summary>
  public bool HasData => Data != null;

  internal EventContext(int eventId, object data)
  {
    EventId = eventId;
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
  /// 获取指定类型的事件数据；无数据或类型不匹配时记录错误日志并返回类型默认值，适合参数类型固定的事件。
  /// </summary>
  public T GetData<T>()
  {
    if (TryGetData(out T data)) return data;

    // 类型不匹配属于调用方约定错误：记录事件 ID 与期望/实际类型，返回默认值而不中断事件派发。
    string actualType = Data == null ? "null" : Data.GetType().Name;
    Debug.LogError($"[EventBus] 事件 {EventId} 的数据类型不匹配，期望 {typeof(T).Name}，实际 {actualType}，返回默认值。");
    return default;
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

  private static readonly Dictionary<int, List<EventSubscription>> EventTable = new();

  /// <summary>
  /// 监听指定事件。监听器统一接收包含 EventId、Data 和 HasData 的 EventContext。
  /// </summary>
  public static void On(int eventId, Action<EventContext> listener, object owner)
  {
    // 校验失败只记录日志并忽略本次操作，避免抛异常中断调用方流程或留下无法清理的订阅。
    if (!ValidateEventId(eventId)) return;
    if (!ValidateOwner(owner)) return;
    if (listener == null)
    {
      Debug.LogError($"[EventBus] 注册事件 {eventId} 失败：监听器不能为空，本次注册已忽略。");
      return;
    }

    if (!EventTable.TryGetValue(eventId, out List<EventSubscription> subscriptions))
    {
      subscriptions = new List<EventSubscription>();
      EventTable.Add(eventId, subscriptions);
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
  public static void Off(int eventId, Action<EventContext> listener, object owner)
  {
    if (!ValidateEventId(eventId)) return;
    if (!ValidateOwner(owner)) return;
    if (!EventTable.TryGetValue(eventId, out List<EventSubscription> subscriptions)) return;

    for (int i = subscriptions.Count - 1; i >= 0; i--)
    {
      EventSubscription subscription = subscriptions[i];
      if (ReferenceEquals(subscription.Owner, owner) && Equals(subscription.Handler, listener))
        subscriptions.RemoveAt(i);
    }

    if (subscriptions.Count == 0) EventTable.Remove(eventId);
  }

  /// <summary>发出不携带数据的事件。</summary>
  public static void Emit(int eventId)
  {
    Dispatch(new EventContext(eventId, null));
  }

  /// <summary>发出携带数据的事件，数据会保存在 EventContext.Data 中。</summary>
  public static void Emit<T>(int eventId, T data)
  {
    Dispatch(new EventContext(eventId, data));
  }

  /// <summary>清空所有事件监听。</summary>
  public static void OffAll()
  {
    EventTable.Clear();
  }

  /// <summary>清空指定 owner 的全部事件监听。</summary>
  public static void OffAll(object owner)
  {
    if (!ValidateOwner(owner)) return;

    List<int> emptyEventIds = null;
    foreach (KeyValuePair<int, List<EventSubscription>> pair in EventTable)
    {
      pair.Value.RemoveAll(item => ReferenceEquals(item.Owner, owner));
      if (pair.Value.Count != 0) continue;

      emptyEventIds ??= new List<int>();
      emptyEventIds.Add(pair.Key);
    }

    if (emptyEventIds == null) return;
    foreach (int eventId in emptyEventIds) EventTable.Remove(eventId);
  }

  /// <summary>同步派发事件；单个监听器异常只记录日志，不影响同事件的其他订阅者。</summary>
  private static void Dispatch(EventContext message)
  {
    if (!ValidateEventId(message.EventId)) return;
    if (!EventTable.TryGetValue(message.EventId, out List<EventSubscription> subscriptions)) return;

    EventSubscription[] snapshot = subscriptions.ToArray();
    foreach (EventSubscription subscription in snapshot)
    {
      Action<EventContext> handler = subscription.Handler;
      if (handler == null)
      {
        Debug.LogError($"[EventBus] 派发事件 {message.EventId} 时发现空监听器，Owner={GetOwnerName(subscription.Owner)}。");
        continue;
      }

      try
      {
        handler.Invoke(message);
      }
      catch (Exception exception)
      {
        // 监听器可能来自已销毁对象或业务代码异常；隔离后继续派发，日志记录归属便于定位泄漏。
        Debug.LogError($"[EventBus] 派发事件 {message.EventId} 的监听器异常，Owner={GetOwnerName(subscription.Owner)}，Error={exception}");
      }
    }
  }

  /// <summary>校验事件 ID；非法时记录日志并返回 false，由调用方终止本次操作。</summary>
  private static bool ValidateEventId(int eventId)
  {
    if (eventId > 0) return true;

    Debug.LogError($"[EventBus] 非法事件 ID：{eventId}，事件 ID 必须大于 0，本次操作已忽略。");
    return false;
  }

  /// <summary>校验订阅归属；owner 为空时记录日志并返回 false，避免产生无法按 owner 清理的订阅。</summary>
  private static bool ValidateOwner(object owner)
  {
    if (owner != null) return true;

    Debug.LogError("[EventBus] 事件订阅归属 owner 不能为空，本次操作已忽略。");
    return false;
  }

  /// <summary>取订阅归属的类型名；避免对已销毁的 Unity 对象调用 ToString() 触发二次异常。</summary>
  private static string GetOwnerName(object owner)
  {
    return owner == null ? "null" : owner.GetType().Name;
  }
}
