using System.Threading.Tasks;

/// <summary>
/// 项目统一资源加载入口。迁移期间兼容 Resources 与 Addressables，调用方不得直接依赖具体后端。
/// </summary>
public sealed class AssetLoader : Singleton<AssetLoader>
{
  // 迁移期间优先保持 Resources 兼容；仅当 Resources 未命中时查询 Addressables。
  private readonly IAssetLoader _resourcesBackend = new ResourcesAssetLoader();
  private readonly AddressablesAssetLoader _addressablesBackend = new AddressablesAssetLoader();

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
    T asset = _resourcesBackend.Load<T>(assetKey);
    return asset != null ? asset : _addressablesBackend.Load<T>(assetKey);
  }

  /// <summary>
  /// 按资源键异步加载资源；必须从 Unity 主线程发起调用。
  /// </summary>
  /// <typeparam name="T">期望的 Unity 资源类型。</typeparam>
  /// <param name="assetKey">稳定资源键；Resources 未命中时使用相同键查询 Addressables。</param>
  /// <returns>表示加载过程的任务；找不到或类型不匹配时结果为 null。</returns>
  public Task<T> LoadAsync<T>(string assetKey) where T : UnityEngine.Object
  {
    return LoadWithFallbackAsync<T>(assetKey);
  }

  /// <summary>
  /// 迁移期间异步优先加载 Resources；未命中时再查询 Addressables，避免影响尚未迁移的资源。
  /// </summary>
  private async Task<T> LoadWithFallbackAsync<T>(string assetKey) where T : UnityEngine.Object
  {
    T asset = await _resourcesBackend.LoadAsync<T>(assetKey);
    return asset != null ? asset : await _addressablesBackend.LoadAsync<T>(assetKey);
  }

  /// <summary>
  /// 释放 Addressables 后端持有的全部句柄。
  /// 该入口只用于应用全局生命周期收口，业务层不直接调用。
  /// </summary>
  public void ReleaseAll()
  {
    _addressablesBackend.ReleaseAll();
  }
}
