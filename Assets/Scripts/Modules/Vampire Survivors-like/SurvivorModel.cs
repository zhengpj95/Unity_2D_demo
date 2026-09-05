/// <summary>
/// 一局 Survivor 战斗的运行时数据。
/// 数据只由 SurvivorProxy 修改，Presenter 仅读取并展示。
/// </summary>
public sealed class SurvivorModel
{
  /// <summary>一局战斗的默认初始生命；后续平衡调整统一修改这里。</summary>
  public const int DefaultMaxHealth = 5;

  public int CurrentHealth { get; internal set; } = DefaultMaxHealth;
  public int MaxHealth { get; internal set; } = DefaultMaxHealth;

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
