using System;
using System.Collections.Generic;
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
  private readonly List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();

  /// <inheritdoc />
  public T Load<T>(string assetKey) where T : UnityEngine.Object
  {
    ValidateAssetKey(assetKey);

    AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(assetKey);
    try
    {
      T asset = handle.WaitForCompletion();
      if (asset == null)
      {
        Addressables.Release(handle);
        return null;
      }

      _handles.Add(handle);
      return asset;
    }
    catch (Exception exception)
    {
      Debug.LogWarning($"[AddressablesAssetLoader] 同步加载失败: {assetKey}, {exception.Message}");
      Addressables.Release(handle);
      return null;
    }
  }

  /// <inheritdoc />
  public async Task<T> LoadAsync<T>(string assetKey) where T : UnityEngine.Object
  {
    ValidateAssetKey(assetKey);

    AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(assetKey);
    try
    {
      T asset = await handle.Task;
      if (asset == null)
      {
        Addressables.Release(handle);
        return null;
      }

      _handles.Add(handle);
      return asset;
    }
    catch (Exception exception)
    {
      Debug.LogWarning($"[AddressablesAssetLoader] 异步加载失败: {assetKey}, {exception.Message}");
      Addressables.Release(handle);
      return null;
    }
  }

  /// <summary>
  /// 释放当前后端持有的全部成功加载句柄。
  /// 仅在应用退出等确认不再使用所有加载资源的全局生命周期末尾调用。
  /// </summary>
  public void ReleaseAll()
  {
    for (int index = _handles.Count - 1; index >= 0; index--)
    {
      AsyncOperationHandle handle = _handles[index];
      if (handle.IsValid())
        Addressables.Release(handle);
    }

    _handles.Clear();
  }

  /// <summary>校验 Addressables 与 Resources 后端共用的资源键输入。</summary>
  private static void ValidateAssetKey(string assetKey)
  {
    if (string.IsNullOrWhiteSpace(assetKey))
      throw new ArgumentException("资源键不能为空。", nameof(assetKey));
  }
}
