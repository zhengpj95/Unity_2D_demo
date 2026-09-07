using VampireSurvivorsLike;

/// <summary>
/// 一局 Survivor 战斗的运行时数据。
/// 数据只由 SurvivorProxy 修改，Presenter 仅读取并展示。
/// </summary>
public sealed class SurvivorModel
{
  /// <summary>一局战斗的默认初始生命；后续平衡调整统一修改这里。</summary>
  public const int DefaultMaxHealth = 5;

  /// <summary>最大生命的基础值；永久属性修正始终基于该值重新计算。</summary>
  public int BaseMaxHealth { get; internal set; } = DefaultMaxHealth;
  public int CurrentHealth { get; internal set; } = DefaultMaxHealth;
  public int MaxHealth { get; internal set; } = DefaultMaxHealth;

  /// <summary>本局玩家永久属性增量；只允许 SurvivorProxy 修改。</summary>
  public PlayerAttributeSet PlayerAttributes { get; } = new PlayerAttributeSet();

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
