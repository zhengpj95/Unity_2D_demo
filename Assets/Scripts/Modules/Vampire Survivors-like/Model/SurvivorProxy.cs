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

  /// <summary>仅供 GameOver 测试覆盖本局生命；正式初始值直接由 SurvivorModel 定义。</summary>
  public void OverrideHealthForTesting(int maxHealth)
  {
    Model.BaseMaxHealth = Math.Max(1, maxHealth);
    Model.MaxHealth = CalculateMaxHealth();
    Model.CurrentHealth = Model.MaxHealth;
  }

  /// <summary>扣减本局当前生命，最小保留为 0。</summary>
  public void ApplyDamage(int damage)
  {
    if (damage <= 0)
      return;

    Model.CurrentHealth = Math.Max(0, Model.CurrentHealth - damage);
  }

  /// <summary>兼容旧调用入口：将最大生命升级转交统一玩家属性系统。</summary>
  public void AddMaxHealth(float value, bool isPercent)
  {
    ApplyPlayerStatUpgrade(PlayerStat.MaxHealth, value, isPercent);
  }

  /// <summary>
  /// 应用一条本局永久玩家属性升级。普通属性只记录修正值；最大生命还会同步更新当前生命。
  /// </summary>
  /// <param name="stat">要修改的统一玩家属性。</param>
  /// <param name="value">固定增量或百分比小数。</param>
  /// <param name="isPercent">true 表示百分比，false 表示固定值。</param>
  /// <returns>配置有效并成功写入时返回 true。</returns>
  public bool ApplyPlayerStatUpgrade(PlayerStat stat, float value, bool isPercent)
  {
    if (value <= 0f)
      return false;

    int previousMaxHealth = Model.MaxHealth;
    if (!Model.PlayerAttributes.AddPermanentModifier(stat, value, isPercent))
      return false;

    if (stat == PlayerStat.MaxHealth)
    {
      Model.MaxHealth = CalculateMaxHealth();
      // 永久最大生命提升同时补充新增的生命上限，保持旧玩法升级后的即时收益。
      Model.CurrentHealth = Math.Min(Model.MaxHealth,
        Model.CurrentHealth + Math.Max(0, Model.MaxHealth - previousMaxHealth));
    }

    return true;
  }

  /// <summary>读取指定玩家属性的本局永久修正，供实体层合并基础值与临时 Buff。</summary>
  public PlayerStatModifier GetPermanentPlayerStatModifier(PlayerStat stat)
  {
    return Model.PlayerAttributes.GetPermanentModifier(stat);
  }

  /// <summary>按统一公式计算基础值、永久升级和临时 Buff 合并后的最终属性。</summary>
  public float CalculatePlayerStat(PlayerStat stat, float baseValue, PlayerStatModifier temporaryModifier)
  {
    return Model.PlayerAttributes.Calculate(stat, baseValue, temporaryModifier);
  }

  private int CalculateMaxHealth()
  {
    float value = Model.PlayerAttributes.Calculate(
      PlayerStat.MaxHealth, Model.BaseMaxHealth, default);
    return Math.Max(1, (int)Math.Ceiling(value));
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
