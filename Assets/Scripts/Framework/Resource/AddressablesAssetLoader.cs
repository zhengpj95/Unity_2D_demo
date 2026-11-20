using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables 资源加载后端。
/// 该实现持有成功加载的操作句柄，并由 AssetLoader 在全局生命周期结束时统一释放。
/// </summary>
internal sealed class AddressablesAssetLoader : IAssetLoader
{
  /// <inheritdoc />
  public AssetLoadResult<T> Load<T>(string assetKey) where T : UnityEngine.Object
  {
    if (!ValidateAssetKey(assetKey))
      return new AssetLoadResult<T>(null);

    AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(assetKey);
    try
    {
      T asset = handle.WaitForCompletion();
      if (asset == null)
      {
        Addressables.Release(handle);
        return new AssetLoadResult<T>(null);
      }

      return CreateResult(handle, asset);
    }
    catch (Exception exception)
    {
      Debug.LogWarning($"[AddressablesAssetLoader] 同步加载失败: {assetKey}, {exception.Message}");
      if (handle.IsValid())
        Addressables.Release(handle);
      return new AssetLoadResult<T>(null);
    }
  }

  /// <inheritdoc />
  public async Task<AssetLoadResult<T>> LoadAsync<T>(string assetKey) where T : UnityEngine.Object
  {
    if (!ValidateAssetKey(assetKey))
      return new AssetLoadResult<T>(null);

    AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(assetKey);
    try
    {
      T asset = await handle.Task;
      if (asset == null)
      {
        Addressables.Release(handle);
        return new AssetLoadResult<T>(null);
      }

      return CreateResult(handle, asset);
    }
    catch (Exception exception)
    {
      Debug.LogWarning($"[AddressablesAssetLoader] 异步加载失败: {assetKey}, {exception.Message}");
      if (handle.IsValid())
        Addressables.Release(handle);
      return new AssetLoadResult<T>(null);
    }
  }

  private static AssetLoadResult<T> CreateResult<T>(
    AsyncOperationHandle<T> handle,
    T asset) where T : UnityEngine.Object
  {
    bool released = false;
    return new AssetLoadResult<T>(asset, () =>
    {
      if (released) return;
      released = true;
      if (handle.IsValid())
        Addressables.Release(handle);
    });
  }

  /// <summary>校验资源键输入，非法时记录日志并返回 false。</summary>
  private static bool ValidateAssetKey(string assetKey)
  {
    if (string.IsNullOrWhiteSpace(assetKey))
    {
      Debug.LogError("[AddressablesAssetLoader] 资源键不能为空。");
      return false;
    }

    return true;
  }
}
