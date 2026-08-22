using System;
using System.Collections.Generic;

/**
 * 全局消息中心（支持无参与有参事件）
 * 游戏全局消息中心（字符串 Key 版）
 * 支持泛型参数
 */
public static class EventBus
{
  // 用字符串为 key 的事件表
  private static readonly Dictionary<string, Delegate> EventTable = new();

  #region --- 添加监听 ---

  public static void AddListener(string eventName, Action listener)
  {
    if (!EventTable.ContainsKey(eventName))
      EventTable[eventName] = null;

    EventTable[eventName] = (Action)EventTable[eventName] + listener;
  }

  /// <summary>
  /// 统一参数事件监听。无参 Dispatch 会传入 null，有参 Dispatch 会传入实际参数。
  /// </summary>
  public static void AddListener(string eventName, Action<object> listener)
  {
    if (!EventTable.ContainsKey(eventName))
      EventTable[eventName] = null;

    EventTable[eventName] = (Action<object>)EventTable[eventName] + listener;
  }

  public static void AddListener<T>(string eventName, Action<T> listener)
  {
    if (!EventTable.ContainsKey(eventName))
    {
      EventTable[eventName] = null;
    }

    EventTable[eventName] = (Action<T>)EventTable[eventName] + listener;
  }

  #endregion

  #region --- 移除监听 ---

  public static void RemoveListener(string eventName, Action listener)
  {
    if (EventTable.ContainsKey(eventName))
      EventTable[eventName] = (Action)EventTable[eventName] - listener;
  }

  public static void RemoveListener(string eventName, Action<object> listener)
  {
    if (EventTable.ContainsKey(eventName))
      EventTable[eventName] = (Action<object>)EventTable[eventName] - listener;
  }

  public static void RemoveListener<T>(string eventName, Action<T> listener)
  {
    if (EventTable.ContainsKey(eventName))
    {
      EventTable[eventName] = (Action<T>)EventTable[eventName] - listener;
    }
  }

  #endregion

  #region --- 派发事件 ---

  public static void Dispatch(string eventName)
  {
    if (EventTable.ContainsKey(eventName))
    {
      // Action<object> 是模块统一事件通道；命中后直接返回，避免与委托逆变匹配的 Action<T> 重复调用。
      var objectAction = EventTable[eventName] as Action<object>;
      if (objectAction != null)
      {
        objectAction.Invoke(null);
        return;
      }

      var action = EventTable[eventName] as Action;
      action?.Invoke();
    }
  }

  public static void Dispatch<T>(string eventName, T arg)
  {
    if (EventTable.ContainsKey(eventName))
    {
      // Action<object> 可因逆变关系转换为 Action<T>，必须优先处理并只调用一次。
      var objectAction = EventTable[eventName] as Action<object>;
      if (objectAction != null)
      {
        objectAction.Invoke(arg);
        return;
      }

      var action = EventTable[eventName] as Action<T>;
      action?.Invoke(arg);
    }
  }

  #endregion

  // 清空所有事件（场景切换时调用）
  public static void ClearAll()
  {
    EventTable.Clear();
  }
}
