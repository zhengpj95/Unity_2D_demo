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
  public T Load<T>(string assetKey) where T : UnityEngine.Object
  {
    ValidateAssetKey(assetKey);

    return Resources.Load<T>(assetKey);
  }

  /// <inheritdoc />
  public Task<T> LoadAsync<T>(string assetKey) where T : UnityEngine.Object
  {
    ValidateAssetKey(assetKey);

    ResourceRequest request = Resources.LoadAsync<T>(assetKey);
    if (request.isDone)
      return Task.FromResult(request.asset as T);

    var completionSource = new TaskCompletionSource<T>();
    request.completed += _ => completionSource.TrySetResult(request.asset as T);
    return completionSource.Task;
  }

  /// <summary>统一校验同步与异步接口使用的资源键。</summary>
  private static void ValidateAssetKey(string assetKey)
  {
    if (string.IsNullOrWhiteSpace(assetKey))
      throw new ArgumentException("资源键不能为空。", nameof(assetKey));
  }
}
