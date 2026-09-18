
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MiscModule : BaseModule
{
  private NetworkMgr _networkMgr;

  public override ModuleName ModuleName => ModuleName.Misc;

  protected override void OnInit()
  {
    RegPresenter<AlertTipsPanelPresenter>(MiscViewType.AlertTips);
    RegCmd<OpenAlertTipsCmd>(EventDefine.MISC_OPEN_ALERT);

    // 网络层只报告失败状态；通用提示与重试入口属于 Misc 业务模块。
    _networkMgr = NetworkMgr.Instance;
    _networkMgr.ConnectionFailed += OnConnectionFailed;
  }

  protected override void OnRelease()
  {
    if (_networkMgr != null)
      _networkMgr.ConnectionFailed -= OnConnectionFailed;

    _networkMgr = null;
  }

  public AlertTipsPanelPresenter OpenAlert(AlertTipsPanelArgs args)
  {
    return OpenWindow<AlertTipsPanelPresenter>(MiscViewType.AlertTips, args);
  }

  /// <summary>网络重试耗尽时显示通用提示；具体 UI 决策不放入 NetworkMgr。</summary>
  private void OnConnectionFailed(NetworkConnectionFailure failure)
  {
    Debug.LogWarning($"[MiscModule] Network connection failed after {failure.AttemptCount} attempts. Reason: {failure.Reason}");
    EventBus.Emit(EventDefine.MISC_OPEN_ALERT, new AlertTipsPanelArgs("连接失败", "网络连接失败，请刷新游戏后重试。", ReloadCurrentScene));
  }

  /// <summary>提示确认后重载当前场景，重新初始化业务与连接流程。</summary>
  private static void ReloadCurrentScene()
  {
    Scene activeScene = SceneManager.GetActiveScene();
    if (activeScene.buildIndex >= 0)
      SceneManager.LoadScene(activeScene.buildIndex);
  }
}
