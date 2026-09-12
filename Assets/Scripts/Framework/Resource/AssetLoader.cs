using System.Threading.Tasks;

/// <summary>
/// 项目统一资源加载入口。当前使用 Resources 后端，调用方不得直接使用 Resources.Load。
/// </summary>
public sealed class AssetLoader : Singleton<AssetLoader>
{
  private readonly IAssetLoader _backend = new ResourcesAssetLoader();

  private AssetLoader()
  {
  }

  /// <summary>
  /// 按资源键同步加载资源。
  /// </summary>
  /// <typeparam name="T">期望的 Unity 资源类型。</typeparam>
  /// <param name="assetKey">当前 Resources 后端中相对 Resources 目录的路径，不包含扩展名。</param>
  /// <returns>加载到的资源；找不到或类型不匹配时返回 null。</returns>
  public T Load<T>(string assetKey) where T : UnityEngine.Object
  {
    return _backend.Load<T>(assetKey);
  }

  /// <summary>
  /// 按资源键异步加载资源；必须从 Unity 主线程发起调用。
  /// </summary>
  /// <typeparam name="T">期望的 Unity 资源类型。</typeparam>
  /// <param name="assetKey">当前 Resources 后端中相对 Resources 目录的路径，不包含扩展名。</param>
  /// <returns>表示加载过程的任务；找不到或类型不匹配时结果为 null。</returns>
  public Task<T> LoadAsync<T>(string assetKey) where T : UnityEngine.Object
  {
    return _backend.LoadAsync<T>(assetKey);
  }
}
