using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 项目统一资源加载入口。迁移期间兼容 Resources 与 Addressables，调用方不得直接依赖具体后端。
/// </summary>
public sealed class AssetLoader : Singleton<AssetLoader>
{
  private readonly struct AssetKey : IEquatable<AssetKey>
  {
    private readonly string _key;
    private readonly Type _type;

    public AssetKey(string key, Type type)
    {
      _key = key;
      _type = type;
    }

    public bool Equals(AssetKey other)
    {
      return string.Equals(_key, other._key, StringComparison.Ordinal) && _type == other._type;
    }

    public override bool Equals(object obj)
    {
      return obj is AssetKey other && Equals(other);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return ((_key != null ? StringComparer.Ordinal.GetHashCode(_key) : 0) * 397) ^ (_type != null ? _type.GetHashCode() : 0);
      }
    }
  }

  private sealed class PendingAssetLoad<T> where T : UnityEngine.Object
  {
    public Task<AssetLease<T>> Task;
    public AssetLease<T> Lease;
    public int WaiterCount;
    public int Generation;
  }

  // 迁移期间优先保持 Resources 兼容；仅当 Resources 未命中时查询 Addressables。
  private readonly IAssetLoader _resourcesBackend = new ResourcesAssetLoader();
  private readonly AddressablesAssetLoader _addressablesBackend = new AddressablesAssetLoader();
  private readonly Dictionary<AssetKey, object> _pendingLoads = new Dictionary<AssetKey, object>();
  private readonly HashSet<IAssetLease> _activeLeases = new HashSet<IAssetLease>();
  private readonly List<IDisposable> _legacyHandles = new List<IDisposable>();
  private int _generation;

  private AssetLoader()
  {
  }

  /// <summary>
  /// 按资源键同步加载资源。
  /// </summary>
  /// <typeparam name="T">期望的 Unity 资源类型。</typeparam>
  /// <param name="assetKey">稳定资源键；Resources 未命中时使用相同键查询 Addressables。</param>
  /// <returns>加载到的资源；找不到或类型不匹配时返回 null。</returns>
  public T Load<T>(string assetKey) where T : UnityEngine.Object
  {
    AssetHandle<T> handle = LoadHandle<T>(assetKey);
    if (handle == null) return null;

    _legacyHandles.Add(handle);
    return handle.Asset;
  }

  /// <summary>
  /// 按资源键异步加载资源；该兼容入口会将句柄保留到全局 ReleaseAll。
  /// </summary>
  /// <typeparam name="T">期望的 Unity 资源类型。</typeparam>
  /// <param name="assetKey">稳定资源键；Resources 未命中时使用相同键查询 Addressables。</param>
  /// <returns>表示加载过程的任务；找不到或类型不匹配时结果为 null。</returns>
  public Task<T> LoadAsync<T>(string assetKey) where T : UnityEngine.Object
  {
    return LoadAndKeepLegacyAsync<T>(assetKey);
  }

  /// <summary>同步加载并取得具有独立释放时机的资源句柄。</summary>
  /// <typeparam name="T">期望的 Unity 资源类型。</typeparam>
  /// <param name="assetKey">稳定资源键。</param>
  /// <returns>加载成功时返回资源句柄，否则返回 null。</returns>
  public AssetHandle<T> LoadHandle<T>(string assetKey) where T : UnityEngine.Object
  {
    ValidateAssetKey(assetKey);
    AssetLoadResult<T> result = _resourcesBackend.Load<T>(assetKey);
    if (result.Asset == null)
      result = _addressablesBackend.Load<T>(assetKey);

    return CreateHandle(result);
  }

  /// <summary>
  /// 异步加载并取得具有独立释放时机的资源句柄；相同资源键和类型的并发请求共享一次后端加载。
  /// 必须从 Unity 主线程发起调用。
  /// </summary>
  /// <typeparam name="T">期望的 Unity 资源类型。</typeparam>
  /// <param name="assetKey">稳定资源键。</param>
  /// <returns>加载成功时返回资源句柄，否则返回 null。</returns>
  public async Task<AssetHandle<T>> LoadHandleAsync<T>(string assetKey) where T : UnityEngine.Object
  {
    ValidateAssetKey(assetKey);

    AssetKey cacheKey = new AssetKey(assetKey, typeof(T));
    if (!_pendingLoads.TryGetValue(cacheKey, out object pendingObject))
    {
      var pending = new PendingAssetLoad<T>();
      pending.Generation = _generation;
      _pendingLoads.Add(cacheKey, pending);
      pending.Task = LoadLeaseWithFallbackAsync<T>(assetKey, pending.Generation);
      pendingObject = pending;
    }

    if (pendingObject is not PendingAssetLoad<T> sharedLoad)
      throw new InvalidOperationException($"[AssetLoader] Pending resource type mismatch for key '{assetKey}'.");

    sharedLoad.WaiterCount++;
    try
    {
      AssetLease<T> lease = await sharedLoad.Task;
      if (lease == null)
        return null;
      if (sharedLoad.Generation != _generation)
        return null;

      sharedLoad.Lease = lease;
      return lease.CreateHandle();
    }
    finally
    {
      sharedLoad.WaiterCount--;
      if (sharedLoad.WaiterCount == 0)
      {
        if (sharedLoad.Lease != null)
          sharedLoad.Lease.ReleaseReservation();

        if (_pendingLoads.TryGetValue(cacheKey, out object current) && ReferenceEquals(current, sharedLoad))
          _pendingLoads.Remove(cacheKey);
      }
    }
  }

  private async Task<T> LoadAndKeepLegacyAsync<T>(string assetKey) where T : UnityEngine.Object
  {
    AssetHandle<T> handle = await LoadHandleAsync<T>(assetKey);
    if (handle != null)
    {
      _legacyHandles.Add(handle);
      return handle.Asset;
    }

    return null;
  }

  private async Task<AssetLease<T>> LoadLeaseWithFallbackAsync<T>(string assetKey, int generation)
    where T : UnityEngine.Object
  {
    AssetLoadResult<T> result = await _resourcesBackend.LoadAsync<T>(assetKey);
    if (result.Asset == null)
      result = await _addressablesBackend.LoadAsync<T>(assetKey);

    if (result.Asset == null) return null;
    if (generation != _generation)
    {
      result.Release?.Invoke();
      return null;
    }

    AssetLease<T> lease = CreateLease(result);
    lease.Reserve();
    return lease;
  }

  private AssetHandle<T> CreateHandle<T>(AssetLoadResult<T> result) where T : UnityEngine.Object
  {
    if (result.Asset == null) return null;
    return CreateLease(result).CreateHandle();
  }

  private AssetLease<T> CreateLease<T>(AssetLoadResult<T> result) where T : UnityEngine.Object
  {
    AssetLease<T> lease = null;
    lease = new AssetLease<T>(result.Asset, result.Release, () => _activeLeases.Remove(lease));
    _activeLeases.Add(lease);
    return lease;
  }

  private static void ValidateAssetKey(string assetKey)
  {
    if (string.IsNullOrWhiteSpace(assetKey))
      throw new ArgumentException("资源键不能为空。", nameof(assetKey));
  }

  /// <summary>
  /// 释放兼容加载入口遗留句柄，并强制释放所有尚存资源租约。
  /// 仅用于确认所有资源使用方已进入销毁阶段的全局生命周期末尾。
  /// </summary>
  public void ReleaseAll()
  {
    _generation++;

    foreach (IDisposable handle in _legacyHandles)
      handle.Dispose();
    _legacyHandles.Clear();

    IAssetLease[] leases = new IAssetLease[_activeLeases.Count];
    _activeLeases.CopyTo(leases);
    foreach (IAssetLease lease in leases)
      lease.ForceRelease();
    _activeLeases.Clear();
    _pendingLoads.Clear();
  }
}
