using UnityEngine;

/// <summary>
/// 通用 MonoBehaviour 单例基类。
/// 特性：
/// - 懒加载：第一次访问 Instance 时会尝试 FindObjectOfType，找不到则自动创建 GameObject + 组件
/// - 可选跨场景保留（子类可以覆盖 PersistAcrossScenes）
/// - 应用退出/编辑器停止时保护（避免在退出阶段创建新实例）
/// - 在 Awake 中处理重复实例（销毁重复）
/// - 在 OnDestroy 中清理静态引用
/// - 处理 Editor domain reload：通过 RuntimeInitializeOnLoadMethod 清理静态字段
/// 
/// 使用：让你的类继承 MonoSingleton<YourManager>，并把生命周期函数设为 protected/virtual 以便覆盖。
/// </summary>
public abstract class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
  private static T _instance;
  private static readonly object _lock = new object();
  private static bool _isShuttingDown = false;

  /// <summary>
  /// 是否在场景切换时保留（默认 true）。子类可 override 改变行为。
  /// </summary>
  protected virtual bool PersistAcrossScenes => true;

  /// <summary>
  /// 全局访问点。若在应用退出阶段（或编辑器停止）返回 null，以避免意外创建。
  /// </summary>
  public static T Instance
  {
    get
    {
      if (_isShuttingDown) return null;

      lock (_lock)
      {
        if (_instance != null) return _instance;

        // 尝试在场景中查找已有实例
        _instance = FindObjectOfType<T>();
        if (_instance != null) return _instance;

        // 没有找到则创建一个新的 GameObject 并添加组件
        var go = new GameObject(typeof(T).Name);
        _instance = go.AddComponent<T>();

        // 如果子类希望保留跨场景，则调用 DontDestroyOnLoad
        if (_instance is SingletonMono<T> mono && mono.PersistAcrossScenes)
        {
          DontDestroyOnLoad(go);
        }

        return _instance;
      }
    }
  }

  /// <summary>
  /// Awake 处理：如果已有实例则销毁重复；否则设置实例并根据 PersistAcrossScenes 处理 DontDestroyOnLoad。
  /// 子类重写时请先调用 base.Awake();
  /// </summary>
  protected virtual void Awake()
  {
    if (_isShuttingDown)
    {
      // 应用正在退出，避免注册/创建新的单例
      Destroy(gameObject);
      return;
    }

    lock (_lock)
    {
      if (_instance == null)
      {
        _instance = this as T;
        if (PersistAcrossScenes) DontDestroyOnLoad(gameObject);
      }
      else if (_instance != this)
      {
        // 已有其他实例，销毁当前重复对象
        Destroy(gameObject);
      }
    }
  }

  /// <summary>
  /// 标记为应用正在退出，防止在此阶段通过 Instance getter 再次创建实例。
  /// </summary>
  protected virtual void OnApplicationQuit()
  {
    _isShuttingDown = true;
  }

  /// <summary>
  /// 在销毁时清理静态引用。
  /// </summary>
  protected virtual void OnDestroy()
  {
    lock (_lock)
    {
      if (_instance == this) _instance = null;
    }
  }

  /// <summary>
  /// Editor 下 Domain Reload 时会触发，此方法用于重置静态字段，避免编辑器停止 Play 后残留状态。
  /// </summary>
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void ResetStatics()
  {
    _isShuttingDown = false;
    _instance = null;
  }
}