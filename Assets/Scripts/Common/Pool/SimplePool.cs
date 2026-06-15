using System;
using System.Collections.Generic;

/// <summary>
/// 纯 C# 对象的简单对象池
/// </summary>
/// <typeparam name="T">对象类型，必须有无参构造函数</typeparam>
public class SimplePool<T> where T : new()
{
    private readonly Stack<T> _pool = new Stack<T>();
    private readonly Action<T> _onAlloc;
    private readonly Action<T> _onFree;

    public SimplePool(int initialSize = 0, Action<T> onAlloc = null, Action<T> onFree = null)
    {
        _onAlloc = onAlloc;
        _onFree = onFree;

        for (int i = 0; i < initialSize; i++)
        {
            _pool.Push(new T());
        }
    }

    /// <summary>
    /// 从池中获取对象
    /// </summary>
    public T Alloc()
    {
        T item = _pool.Count > 0 ? _pool.Pop() : new T();
        _onAlloc?.Invoke(item);
        return item;
    }

    /// <summary>
    /// 将对象回收到池中
    /// </summary>
    public void Free(T item)
    {
        if (item == null) return;
        _onFree?.Invoke(item);
        _pool.Push(item);
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    public void Clear()
    {
        _pool.Clear();
    }

    /// <summary>
    /// 当前池中可用对象的数量
    /// </summary>
    public int Count => _pool.Count;
}
