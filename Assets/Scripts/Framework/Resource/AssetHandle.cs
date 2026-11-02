using System;
using UnityEngine;

/// <summary>统一登记可由全局生命周期兜底释放的资源租约。</summary>
internal interface IAssetLease
{
  void ForceRelease();
}

/// <summary>单个资源加载结果的共享所有权计数。</summary>
internal sealed class AssetLease<T> : IAssetLease where T : UnityEngine.Object
{
  private readonly Action _release;
  private readonly Action _onReleased;
  private int _referenceCount;
  private bool _isReleased;

  public T Asset { get; private set; }
  public bool IsReleased => _isReleased;

  public AssetLease(T asset, Action release, Action onReleased)
  {
    Asset = asset;
    _release = release;
    _onReleased = onReleased;
  }

  public void Reserve()
  {
    if (_isReleased)
      throw new ObjectDisposedException(nameof(AssetLease<T>));

    _referenceCount++;
  }

  public AssetHandle<T> CreateHandle()
  {
    if (_isReleased)
      throw new ObjectDisposedException(nameof(AssetLease<T>));

    _referenceCount++;
    return new AssetHandle<T>(this);
  }

  public void ReleaseReference()
  {
    if (_isReleased) return;

    _referenceCount--;
    if (_referenceCount > 0) return;

    _isReleased = true;
    Asset = null;
    try
    {
      _release?.Invoke();
    }
    finally
    {
      _onReleased?.Invoke();
    }
  }

  public void ReleaseReservation()
  {
    ReleaseReference();
  }

  public void ForceRelease()
  {
    if (_isReleased) return;

    _referenceCount = 1;
    ReleaseReference();
  }
}

/// <summary>
/// 资源加载租约。调用方必须在不再使用资源时释放句柄；可通过 Retain 为独立生命周期创建额外所有权。
/// </summary>
/// <typeparam name="T">Unity 资源类型。</typeparam>
public sealed class AssetHandle<T> : IDisposable where T : UnityEngine.Object
{
  private AssetLease<T> _lease;

  internal AssetHandle(AssetLease<T> lease)
  {
    _lease = lease ?? throw new ArgumentNullException(nameof(lease));
  }

  /// <summary>获取已加载资源；句柄释放后再次访问会抛出异常。</summary>
  public T Asset => _lease != null && !_lease.IsReleased
    ? _lease.Asset
    : throw new ObjectDisposedException(nameof(AssetHandle<T>));

  /// <summary>指示句柄是否已释放。</summary>
  public bool IsReleased => _lease == null || _lease.IsReleased;

  /// <summary>为另一项独立生命周期保留同一资源。</summary>
  /// <returns>引用同一资源且需要单独释放的新句柄。</returns>
  public AssetHandle<T> Retain()
  {
    if (_lease == null)
      throw new ObjectDisposedException(nameof(AssetHandle<T>));

    return _lease.CreateHandle();
  }

  /// <summary>释放本句柄持有的资源引用；重复调用安全。</summary>
  public void Dispose()
  {
    AssetLease<T> lease = _lease;
    _lease = null;
    lease?.ReleaseReference();
  }
}

/// <summary>由资源后端返回的资源和底层释放操作。</summary>
public sealed class AssetLoadResult<T> where T : UnityEngine.Object
{
  /// <summary>已加载资源；未命中时为 null。</summary>
  public T Asset { get; }

  internal Action Release { get; }

  internal AssetLoadResult(T asset, Action release = null)
  {
    Asset = asset;
    Release = release;
  }
}
