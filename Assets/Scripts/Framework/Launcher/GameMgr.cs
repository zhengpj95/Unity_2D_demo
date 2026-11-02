using UnityEngine;

/// <summary>
/// 游戏全局启动入口，负责基础服务、业务模块和网络生命周期编排。
/// </summary>
public sealed class GameMgr : MonoBehaviour
{
  private static GameMgr _instance;
  private bool _isDuplicate;
  private bool _isApplicationQuitting;

  // 开发阶段可关闭 Socket，使未启动本地服务端时也能完整运行客户端流程。
  private static readonly bool EnableSocketConnection = true;
  private const string ServerUrl = "ws://localhost:3000";

  private void Awake()
  {
    if (_instance != null && _instance != this)
    {
      _isDuplicate = true;
      Destroy(gameObject);
      return;
    }

    _instance = this;
    DontDestroyOnLoad(gameObject);

    InitializeModules();
  }

  private async void Start()
  {
    if (_isDuplicate) return;

    if (!EnableSocketConnection)
    {
      Debug.Log("[GameMgr] Socket connection is disabled for development.");
      return;
    }

    await NetworkMgr.Instance.Connect(ServerUrl);
  }

  private void Update()
  {
    if (_isDuplicate) return;

    TimerManager.Instance.OnUpdate();
    PoolManager.Instance.OnUpdate();

    if (ModuleManager.IsCreated)
      ModuleManager.Instance.Update();
  }

  private void OnDestroy()
  {
    if (_instance != this) return;

    ModuleManager.Instance.ReleaseAll();

    // 先销毁 UI 与池缓存，再释放资源租约，避免实例仍引用 Addressables Prefab。
    if (UIManager.IsCreated)
      UIManager.Instance.Release();
    if (PoolManager.IsCreated)
      PoolManager.Instance.ClearAll();

    // 正常销毁仍立即释放本地网络资源；退出 Player 时由 OnApplicationQuit 先完成关闭握手。
    if (NetworkMgr.IsCreated && !_isApplicationQuitting)
      NetworkMgr.Instance.Dispose();

    // GameMgr 是资源租约的全局兜底边界；正常调用方应先按自身生命周期释放句柄。
    if (AssetLoader.IsCreated)
      AssetLoader.Instance.ReleaseAll();

    _instance = null;
  }

  /// <summary>
  /// 退出 Player 或停止 Editor Play Mode 时主动完成 WebSocket Close 握手，再释放本地引用。
  /// Unity 不会等待普通 OnDestroy 中的异步任务，因此退出关闭必须在 OnApplicationQuit 发起。
  /// </summary>
  private async void OnApplicationQuit()
  {
    if (_instance != this || _isApplicationQuitting) return;

    _isApplicationQuitting = true;
    if (!NetworkMgr.IsCreated) return;

    NetworkMgr networkManager = NetworkMgr.Instance;
    try
    {
      await networkManager.Close();
    }
    catch (System.Exception exception)
    {
      Debug.LogError($"[GameMgr] Failed to close WebSocket while quitting. Error={exception}");
    }
    finally
    {
      networkManager.Dispose();
    }
  }

  private static void InitializeModules()
  {
    ModuleManager.Instance.PushModules<MiscModule>();
    ModuleManager.Instance.PushModules<LoginModule>();
    ModuleManager.Instance.PushModules<SurvivorModule>();
    ModuleManager.Instance.InitializeAll();
  }
}
