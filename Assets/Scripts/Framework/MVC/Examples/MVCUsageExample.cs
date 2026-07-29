using UnityEngine;

/// <summary>
/// MVC框架使用示例
/// 展示如何在项目中使用MVC框架
/// </summary>
public class MVCUsageExample : MonoBehaviour
{
  [Header("Dialog Prefab")]
  [SerializeField] private GameObject _dialogPrefab;

  private ConfirmDialogController _dialogController;
  private ConfirmDialogView _dialogView;

  private void Start()
  {
    // 示例1: 创建并使用确认弹窗
    ShowConfirmDialog();
  }

  /// <summary>
  /// 示例1: 显示确认弹窗
  /// </summary>
  private void ShowConfirmDialog()
  {
    // 1. 实例化UI Prefab
    var dialogGo = Instantiate(_dialogPrefab);
    _dialogView = dialogGo.GetComponent<ConfirmDialogView>();

    if (_dialogView == null)
    {
      Debug.LogError("DialogView component not found on prefab!");
      return;
    }

    // 2. 创建Controller
    _dialogController = new ConfirmDialogController();

    // 3. 初始化Controller（连接View）
    _dialogController.Initialize(_dialogView);

    // 4. 订阅弹窗关闭事件
    _dialogController.OnDialogClosed += OnDialogClosed;

    // 5. 显示弹窗并设置回调
    _dialogController.ShowDialog(
        "确认操作",
        "您确定要执行此操作吗？",
        () => Debug.Log("用户点击了确认"),
        () => Debug.Log("用户点击了取消")
    );
  }

  /// <summary>
  /// 弹窗关闭回调
  /// </summary>
  private void OnDialogClosed()
  {
    Debug.Log($"弹窗已关闭，结果: {_dialogController.DialogResult}");

    // 清理
    if (_dialogController != null)
    {
      _dialogController.OnDialogClosed -= OnDialogClosed;
      _dialogController.Cleanup();
    }
  }

  private void OnDestroy()
  {
    if (_dialogController != null)
    {
      _dialogController.OnDialogClosed -= OnDialogClosed;
      _dialogController.Cleanup();
    }
  }
}