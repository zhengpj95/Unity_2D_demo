using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 当前资源加载实现：使用 Unity Resources 提供同步与异步加载。
/// 后续迁移资源系统时只替换本类，不允许业务层重新直接调用 Resources。
/// </summary>
internal sealed class ResourcesAssetLoader : IAssetLoader
{
  /// <inheritdoc />
  public AssetLoadResult<T> Load<T>(string assetKey) where T : UnityEngine.Object
  {
    if (!ValidateAssetKey(assetKey))
      return new AssetLoadResult<T>(null);

    return new AssetLoadResult<T>(Resources.Load<T>(assetKey));
  }

  /// <inheritdoc />
  public Task<AssetLoadResult<T>> LoadAsync<T>(string assetKey) where T : UnityEngine.Object
  {
    if (!ValidateAssetKey(assetKey))
      return Task.FromResult(new AssetLoadResult<T>(null));

    ResourceRequest request = Resources.LoadAsync<T>(assetKey);
    if (request.isDone)
      return Task.FromResult(new AssetLoadResult<T>(request.asset as T));

    var completionSource = new TaskCompletionSource<AssetLoadResult<T>>();
    request.completed += _ => completionSource.TrySetResult(new AssetLoadResult<T>(request.asset as T));
    return completionSource.Task;
  }

  /// <summary>统一校验同步与异步接口使用的资源键，非法时记录日志并返回 false。</summary>
  private static bool ValidateAssetKey(string assetKey)
  {
    if (string.IsNullOrWhiteSpace(assetKey))
    {
      Debug.LogError("[ResourcesAssetLoader] 资源键不能为空。");
      return false;
    }

    return true;
  }
}
