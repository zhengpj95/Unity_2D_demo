using System;
using UnityEngine;

/// <summary>GameOver 面板的显示参数；Presenter 只展示数据并将玩家操作回传给 GameplayController。</summary>
public readonly struct SurvivorGameOverArgs
{
  public int Level { get; }
  public int KillCount { get; }
  public int CoinCount { get; }
  public Action OnRestart { get; }
  /// <summary>玩家选择返回主页时，由 Controller 执行的场景流程回调。</summary>
  public Action OnReturnHome { get; }

  /// <summary>创建本局结算数据，以及再来一局和返回主界面的回调。</summary>
  /// <param name="level">本局结束时的玩家等级。</param>
  /// <param name="killCount">本局累计击杀数。</param>
  /// <param name="coinCount">本局累计金币数。</param>
  /// <param name="onRestart">重新开始当前战斗的回调。</param>
  /// <param name="onReturnHome">返回 Launcher/Home 的回调。</param>
  public SurvivorGameOverArgs(int level, int killCount, int coinCount, Action onRestart, Action onReturnHome)
  {
    Level = level;
    KillCount = killCount;
    CoinCount = coinCount;
    OnRestart = onRestart;
    OnReturnHome = onReturnHome;
  }
}

/// <summary>
/// Survivor 的最小 GameOver 结算窗口。
/// 通过 SurvivorGameOverView 展示结算数据，并将“重新开始/返回主页”操作回传给 GameplayController。
/// </summary>
public sealed class SurvivorGameOverPresenter : BasePresenter<SurvivorGameOverView, SurvivorGameOverArgs>
{
  private Action _onRestart;
  private Action _onReturnHome;

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
      AddClickListener(ViewT.btnQuit, ReturnHome);
    }
  }

  public override void OnOpen(SurvivorGameOverArgs args)
  {
    base.OnOpen(args);
    if (ViewT == null)
      return;

    _onRestart = args.OnRestart;
    _onReturnHome = args.OnReturnHome;
    if (ViewT.txtTitle != null)
      ViewT.txtTitle.text = "游戏结束";
    if (ViewT.txtInfo != null)
      ViewT.txtInfo.text = $"等级：{args.Level}\n击杀：{args.KillCount}\n金币：{args.CoinCount}";
  }

  public override void OnClose()
  {
    _onRestart = null;
    _onReturnHome = null;
    base.OnClose();
  }

  private void RestartGame()
  {
    // 先关闭窗口再重载场景，避免场景卸载期间 Presenter 再访问已销毁的 View。
    Action onRestart = _onRestart;
    UIManager.Instance.CloseWindow(this);
    onRestart?.Invoke();
  }

  /// <summary>关闭结算窗口，并将返回主界面的请求交给 GameplayController。</summary>
  private void ReturnHome()
  {
    Action onReturnHome = _onReturnHome;
    UIManager.Instance.CloseWindow(this);
    onReturnHome?.Invoke();
  }
}
