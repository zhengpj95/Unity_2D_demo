using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>GameOver 面板的显示参数；Presenter 只展示数据并将重开动作回传给 GameplayController。</summary>
public readonly struct SurvivorGameOverArgs
{
  public int Level { get; }
  public int KillCount { get; }
  public int GemCount { get; }
  public Action OnRestart { get; }

  /// <summary>创建本局结算数据与玩家点击重开的回调。</summary>
  public SurvivorGameOverArgs(int level, int killCount, int gemCount, Action onRestart)
  {
    Level = level;
    KillCount = killCount;
    GemCount = gemCount;
    OnRestart = onRestart;
  }
}

/// <summary>
/// Survivor 的最小 GameOver 结算窗口。
/// 当前复用通用单按钮提示 Prefab，避免在本次流程改动中额外修改场景或 Prefab 资源。
/// </summary>
public sealed class SurvivorGameOverPresenter : BasePresenter<AlertTipsPanelView, SurvivorGameOverArgs>
{
  private Action _onRestart;

  public override UILayerIndex Layer => UILayerIndex.Model;
  public override string PrefabPath => "Prefabs/AlertTipsPanel";

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    if (ViewT != null)
      AddClickListener(ViewT.btn_confirm, RestartGame);
  }

  public override void OnOpen(SurvivorGameOverArgs args)
  {
    base.OnOpen(args);
    if (ViewT == null)
      return;

    _onRestart = args.OnRestart;
    ViewT.txt_title.text = "游戏结束";
    ViewT.txt_desc.text = $"等级：{args.Level}\n击杀：{args.KillCount}\n宝石：{args.GemCount}";

    Text buttonText = ViewT.btn_confirm == null ? null : ViewT.btn_confirm.GetComponentInChildren<Text>();
    if (buttonText != null)
      buttonText.text = "重新开始";
  }

  public override void OnClose()
  {
    _onRestart = null;
    base.OnClose();
  }

  private void RestartGame()
  {
    // 先关闭窗口再重载场景，避免场景卸载期间 Presenter 再访问已销毁的 View。
    Action onRestart = _onRestart;
    UIManager.Instance.CloseWindow(this);
    onRestart?.Invoke();
  }
}
