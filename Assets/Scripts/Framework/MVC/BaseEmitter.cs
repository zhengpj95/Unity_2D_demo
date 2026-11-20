using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 带生命周期的 EventBus 使用基类，统一记录并清理由当前对象创建的事件订阅。
/// </summary>
public abstract class BaseEmitter
{
  // 按事件 ID 分组记录订阅，同一事件下去重用 HashSet 去重；On 的去重从 O(n) 降为 O(1)。
  // 该结构只保存"本对象订阅过什么"，真正的订阅表由 EventBus 按事件 ID 持有。
  private readonly Dictionary<int, HashSet<Action<EventContext>>> _subscriptions = new();

  /// <summary>监听事件；所有监听器统一接收 EventContext。</summary>
  protected void On(int eventId, Action<EventContext> listener)
  {
    if (!TryValidateEventId(eventId)) return;
    if (listener == null)
    {
      Debug.LogError($"[{GetType().Name}] Cannot subscribe with a null listener.");
      return;
    }

    if (!_subscriptions.TryGetValue(eventId, out HashSet<Action<EventContext>> listeners))
    {
      listeners = new HashSet<Action<EventContext>>();
      _subscriptions.Add(eventId, listeners);
    }

    // HashSet.Add 返回 false 表示相同委托已存在，跳过重复注册，避免 EventBus 再做一次线性去重。
    if (!listeners.Add(listener)) return;

    EventBus.On(eventId, listener, this);
  }

  /// <summary>发出不携带数据的事件。</summary>
  protected void Emit(int eventId)
  {
    if (!TryValidateEventId(eventId)) return;
    EventBus.Emit(eventId);
  }

  /// <summary>发出携带数据的事件。</summary>
  protected void Emit<T>(int eventId, T data)
  {
    if (!TryValidateEventId(eventId)) return;
    EventBus.Emit(eventId, data);
  }

  /// <summary>
  /// 解除当前对象通过 On 创建的全部事件订阅；可重复调用。
  /// 直接按 owner 清理 EventBus，无需逐个 Off：后者与 OffAll(owner) 重复，且会在遍历时修改 EventBus 订阅表。
  /// </summary>
  protected void OffAll()
  {
    _subscriptions.Clear();
    EventBus.OffAll(this);
  }

  protected static void ValidateEventId(int eventId)
  {
    TryValidateEventId(eventId);
  }

  /// <summary>验证事件编号；无效时记录错误并返回 false。</summary>
  protected static bool TryValidateEventId(int eventId)
  {
    if (eventId <= 0)
    {
      Debug.LogError($"[BaseEmitter] Event ID must be greater than zero; got {eventId}.");
      return false;
    }

    return true;
  }
}
