/// <summary>
/// 一局 Survivor 战斗的运行时数据。
/// 数据只由 SurvivorProxy 修改，Presenter 仅读取并展示。
/// </summary>
public sealed class SurvivorModel
{
  public int CurrentHealth { get; internal set; }
  public int MaxHealth { get; internal set; }

  public int Level { get; internal set; } = 1;
  public int CurrentExp { get; internal set; }
  public int PendingLevelUpCount { get; internal set; }

  public int KillCount { get; internal set; }
  public int GemCount { get; internal set; }
  public int CoinCount { get; internal set; }

  public SurvivorGameState GameState { get; internal set; } = SurvivorGameState.Playing;
}

/// <summary>Survivor 一局战斗的主状态。</summary>
public enum SurvivorGameState
{
  Playing,
  LevelUp,
  GameOver,
}
