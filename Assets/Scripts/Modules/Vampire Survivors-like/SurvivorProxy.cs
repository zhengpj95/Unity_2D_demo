using System;
using VampireSurvivorsLike;

/// <summary>
/// Survivor 模块的数据与未来协议边界。
/// 持有一局战斗的 SurvivorModel，不直接操作 UI。
/// </summary>
public sealed class SurvivorProxy : BaseProxy
{
  private const int BaseLevelExp = 20;
  private const int LevelExpGrowth = 5;

  public SurvivorModel Model { get; private set; }

  protected override void OnInit()
  {
    ResetRound();
  }

  public void ResetRound()
  {
    Model = new SurvivorModel();
  }

  public void SetHealth(int currentHealth, int maxHealth)
  {
    Model.MaxHealth = Math.Max(0, maxHealth);
    Model.CurrentHealth = Math.Clamp(currentHealth, 0, Model.MaxHealth);
  }

  /// <summary>
  /// 增加经验并保留溢出经验；多个等级提升会进入待处理队列。
  /// </summary>
  public void AddExp(int value)
  {
    if (value <= 0)
      return;

    Model.CurrentExp += value;

    while (Model.CurrentExp >= GetRequiredExp())
    {
      Model.CurrentExp -= GetRequiredExp();
      Model.Level++;
      Model.PendingLevelUpCount++;
    }
  }

  public int GetRequiredExp()
  {
    int level = Model.Level;
    return BaseLevelExp * level + LevelExpGrowth * level * level;
  }

  public bool TryConsumePendingLevelUp()
  {
    if (Model.PendingLevelUpCount <= 0)
      return false;

    Model.PendingLevelUpCount--;
    return true;
  }

  public bool HasPendingLevelUp => Model.PendingLevelUpCount > 0;

  public void SetKillCount(int killCount)
  {
    Model.KillCount = Math.Max(0, killCount);
  }

  public void AddDropItem(DropItemType dropItemType, int count)
  {
    if (count <= 0)
      return;

    switch (dropItemType)
    {
      case DropItemType.Gem:
        Model.GemCount += count;
        break;
      case DropItemType.Coin:
        Model.CoinCount += count;
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(dropItemType), dropItemType, null);
    }
  }

  public void SetGameState(SurvivorGameState state)
  {
    Model.GameState = state;
  }
}
