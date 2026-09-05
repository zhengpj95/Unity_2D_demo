using System;
using UnityEngine;

/// <summary>GameOver 面板的显示参数；Presenter 只展示数据并将重开动作回传给 GameplayController。</summary>
public readonly struct SurvivorGameOverArgs
{
  public int Level { get; }
  public int KillCount { get; }
  public int CoinCount { get; }
  public Action OnRestart { get; }

  /// <summary>创建本局结算数据与玩家点击重开的回调。</summary>
  public SurvivorGameOverArgs(int level, int killCount, int coinCount, Action onRestart)
  {
    Level = level;
    KillCount = killCount;
    CoinCount = coinCount;
    OnRestart = onRestart;
  }
}

/// <summary>
/// Survivor 的最小 GameOver 结算窗口。
/// 通过 SurvivorGameOverView 展示结算数据，并将“下一轮”操作回传给 GameplayController。
/// </summary>
public sealed class SurvivorGameOverPresenter : BasePresenter<SurvivorGameOverView, SurvivorGameOverArgs>
{
  private Action _onRestart;

  public override UILayerIndex Layer => UILayerIndex.Model;
  public override string PrefabPath => "Prefabs/SurvivorGameOver";

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    if (ViewT != null)
    {
      // UIManager 当前不自动执行 UIView.InitView，专用 View 必须在绑定按钮前完成引用查找。
      ViewT.InitView();
      AddClickListener(ViewT.btnRestart, RestartGame);
      AddClickListener(ViewT.btnQuit, QuitGame);
    }
  }

  public override void OnOpen(SurvivorGameOverArgs args)
  {
    base.OnOpen(args);
    if (ViewT == null)
      return;

    _onRestart = args.OnRestart;
    if (ViewT.txtTitle != null)
      ViewT.txtTitle.text = "游戏结束";
    if (ViewT.txtInfo != null)
      ViewT.txtInfo.text = $"等级：{args.Level}\n击杀：{args.KillCount}\n金币：{args.CoinCount}";
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

  /// <summary>退出已构建的应用；Unity Editor 中仅记录提示，避免意外中断测试会话。</summary>
  private void QuitGame()
  {
#if UNITY_EDITOR
    Debug.Log("[SurvivorGameOverPresenter] 已请求退出游戏；Editor 中不会关闭 Play Mode。");
#else
    Application.Quit();
#endif
  }
}
