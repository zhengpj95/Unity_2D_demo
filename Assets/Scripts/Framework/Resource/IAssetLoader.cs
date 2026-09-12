using System.Threading.Tasks;

/// <summary>
/// 资源加载抽象。调用方只传入资源键，不直接依赖 Resources、Addressables 或 AssetBundle。
/// </summary>
public interface IAssetLoader
{
  /// <summary>
  /// 按资源键加载指定类型的资源。
  /// </summary>
  /// <typeparam name="T">期望的 Unity 资源类型。</typeparam>
  /// <param name="assetKey">资源系统约定的资源键。</param>
  /// <returns>加载到的资源；找不到或类型不匹配时返回 null。</returns>
  T Load<T>(string assetKey) where T : UnityEngine.Object;

  /// <summary>
  /// 按资源键异步加载指定类型的资源。
  /// </summary>
  /// <typeparam name="T">期望的 Unity 资源类型。</typeparam>
  /// <param name="assetKey">资源系统约定的资源键。</param>
  /// <returns>表示加载过程的任务；找不到或类型不匹配时结果为 null。</returns>
  Task<T> LoadAsync<T>(string assetKey) where T : UnityEngine.Object;
}
