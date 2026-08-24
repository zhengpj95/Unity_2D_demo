using UnityEngine;

/// <summary>
/// UI启动器
/// 负责在场景启动时初始化UIManager
/// 将此脚本挂载到场景中的一个GameObject上
/// </summary>
public class UILauncher : MonoBehaviour
{
  private static UILauncher _instance;
  private bool _isDuplicate;

  // [Header("UI Layers")]
  [SerializeField] private Transform _mainLayer;
  [SerializeField] private Transform _windowLayer;
  [SerializeField] private Transform _modelLayer;
  [SerializeField] private Transform _tipLayer;

  private bool _dontDestroyOnLoad = true;

  private void Awake()
  {
    // UILauncher 会跨场景保留；场景重载时销毁新场景中的重复实例，
    // 避免其子节点 EventSystem 与旧场景的 EventSystem 同时存在。
    if (_instance != null && _instance != this)
    {
      _isDuplicate = true;
      Destroy(gameObject);
      return;
    }

    _instance = this;
    InitializeUIManager();
  }

  private void Start()
  {
    if (_isDuplicate) return;

    // 初始化 UI 池
    if (!PoolManager.IsCreated)
    {
      PoolManager.Instance.InitializeUIPool(GetComponentInChildren<Canvas>());
    }
  }

  private void InitializeUIManager()
  {
    var config = new UIManagerConfig(_mainLayer, _windowLayer, _modelLayer, _tipLayer);
    UIManager.Instance.Initialize(config);

    if (_dontDestroyOnLoad)
    {
      DontDestroyOnLoad(gameObject);
    }
  }

  protected void Update()
  {
    if (_isDuplicate) return;

    TimerManager.Instance.OnUpdate();
    PoolManager.Instance.OnUpdate();

    if (ModuleManager.IsCreated)
    {
      ModuleManager.Instance.Update();
    }
  }

  private void OnDestroy()
  {
    if (_instance != this)
    {
      return;
    }

    _instance = null;

    if (ModuleManager.IsCreated)
    {
      ModuleManager.Instance.ReleaseAll();
    }

    if (UIManager.IsCreated)
    {
      UIManager.Instance.Release();
    }
  }

#if UNITY_EDITOR
  /// <summary>
  /// 编辑器下自动查找层级节点（可选）
  /// </summary>
  private void Reset()
  {
    AutoFindLayers();
  }

  private void AutoFindLayers()
  {
    if (_mainLayer == null)
    {
      _mainLayer = transform.Find("MainLayer");
    }
    if (_windowLayer == null)
    {
      _windowLayer = transform.Find("WindowLayer");
    }
    if (_modelLayer == null)
    {
      _modelLayer = transform.Find("ModelLayer");
    }
    if (_tipLayer == null)
    {
      _tipLayer = transform.Find("TipLayer");
    }
  }
#endif
}
