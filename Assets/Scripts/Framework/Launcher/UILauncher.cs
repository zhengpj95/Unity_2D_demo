using UnityEngine;

/// <summary>
/// UI启动器
/// 负责在场景启动时初始化UIManager
/// 将此脚本挂载到场景中的一个GameObject上
/// </summary>
public class UILauncher : MonoBehaviour
{
  // [Header("UI Layers")]
  [SerializeField] private Transform _mainLayer;
  [SerializeField] private Transform _windowLayer;
  [SerializeField] private Transform _modelLayer;
  [SerializeField] private Transform _tipLayer;

  private bool _dontDestroyOnLoad = true;

  private void Awake()
  {
    InitializeUIManager();
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
    // TimerManager.Instance.OnUpdate();
  }

  private void OnDestroy()
  {
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