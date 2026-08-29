using UnityEngine;

/// <summary>
/// 游戏全局启动入口，负责基础服务、业务模块和网络生命周期编排。
/// </summary>
public sealed class GameMgr : MonoBehaviour
{
  private static GameMgr _instance;
  private bool _isDuplicate;

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

    if (NetworkMgr.IsCreated)
      NetworkMgr.Instance.Dispose();

    _instance = null;
  }

  private static void InitializeModules()
  {
    ModuleManager.Instance.PushModules<MiscModule>();
    ModuleManager.Instance.PushModules<LoginModule>();
    ModuleManager.Instance.PushModules<SurvivorModule>();
    ModuleManager.Instance.InitializeAll();
  }
}