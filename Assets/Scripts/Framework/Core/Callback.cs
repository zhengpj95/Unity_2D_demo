using System;
using System.Collections.Generic;

/// <summary>
/// 通用回调类 - 支持对象池、传参/无参、一次性回调
/// </summary>
public class Callback
{
  #region 对象池
  private static readonly Stack<Callback> _pool = new Stack<Callback>();

  public static Callback Create(Action callback, bool once = false)
  {
    var cb = _pool.Count > 0 ? _pool.Pop() : new Callback();
    cb._isOnce = once;
    cb.Add(callback);
    return cb;
  }

  public static Callback<T> Create<T>(Action<T> callback, bool once = false)
  {
    var cb = Callback<T>.Get();
    cb._isOnce = once;
    cb.Add(callback);
    return cb;
  }

  public static void Release(Callback callback)
  {
    if (callback == null) return;
    callback.Clear();
    _pool.Push(callback);
  }

  public static void Release<T>(Callback<T> callback)
  {
    if (callback == null) return;
    callback.Clear();
    Callback<T>.Return(callback);
  }
  #endregion

  private Action _callbacks;
  private bool _isOnce;
  private bool _isInvoking;
  private readonly List<Action> _pendingAdd = new List<Action>();
  private readonly List<Action> _pendingRemove = new List<Action>();

  public Callback Add(Action callback)
  {
    if (callback == null) return this;
    if (_isInvoking) _pendingAdd.Add(callback);
    else _callbacks += callback;
    return this;
  }

  public Callback Remove(Action callback)
  {
    if (callback == null) return this;
    if (_isInvoking) _pendingRemove.Add(callback);
    else _callbacks -= callback;
    return this;
  }

  public void Invoke()
  {
    if (_callbacks == null) return;
    _isInvoking = true;
    try { _callbacks?.Invoke(); }
    finally
    {
      _isInvoking = false;
      ProcessPending();
      if (_isOnce) Clear();
    }
  }

  private void ProcessPending()
  {
    foreach (var cb in _pendingAdd) _callbacks += cb;
    _pendingAdd.Clear();
    foreach (var cb in _pendingRemove) _callbacks -= cb;
    _pendingRemove.Clear();
  }

  public void Clear()
  {
    _callbacks = null;
    _isOnce = false;
    _pendingAdd.Clear();
    _pendingRemove.Clear();
  }
}

public class Callback<T>
{
  private static readonly Stack<Callback<T>> _pool = new Stack<Callback<T>>();

  public static Callback<T> Get()
  {
    return _pool.Count > 0 ? _pool.Pop() : new Callback<T>();
  }

  public static void Return(Callback<T> callback)
  {
    _pool.Push(callback);
  }

  private Action<T> _callbacks;
  internal bool _isOnce;
  private bool _isInvoking;
  private readonly List<Action<T>> _pendingAdd = new List<Action<T>>();
  private readonly List<Action<T>> _pendingRemove = new List<Action<T>>();

  public Callback<T> Add(Action<T> callback)
  {
    if (callback == null) return this;
    if (_isInvoking) _pendingAdd.Add(callback);
    else _callbacks += callback;
    return this;
  }

  public Callback<T> Remove(Action<T> callback)
  {
    if (callback == null) return this;
    if (_isInvoking) _pendingRemove.Add(callback);
    else _callbacks -= callback;
    return this;
  }

  public void Invoke(T arg)
  {
    if (_callbacks == null) return;
    _isInvoking = true;
    try { _callbacks?.Invoke(arg); }
    finally
    {
      _isInvoking = false;
      ProcessPending();
      if (_isOnce) Clear();
    }
  }

  private void ProcessPending()
  {
    foreach (var cb in _pendingAdd) _callbacks += cb;
    _pendingAdd.Clear();
    foreach (var cb in _pendingRemove) _callbacks -= cb;
    _pendingRemove.Clear();
  }

  public void Clear()
  {
    _callbacks = null;
    _isOnce = false;
    _pendingAdd.Clear();
    _pendingRemove.Clear();
  }
}
