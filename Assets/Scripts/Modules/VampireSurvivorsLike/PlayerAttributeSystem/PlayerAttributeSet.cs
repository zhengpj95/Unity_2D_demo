using System.Collections.Generic;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// 保存一局战斗中的永久玩家属性增量，并负责与临时修正合并计算。
  /// 此对象由 SurvivorModel 持有；场景组件只能通过 SurvivorModule/SurvivorProxy 提供的接口访问。
  /// </summary>
  public sealed class PlayerAttributeSet
  {
    private readonly Dictionary<PlayerStat, PlayerStatModifier> _permanentModifiers =
      new Dictionary<PlayerStat, PlayerStatModifier>();

    /// <summary>读取指定属性的本局永久修正；尚未升级时返回零修正。</summary>
    public PlayerStatModifier GetPermanentModifier(PlayerStat stat)
    {
      return _permanentModifiers.TryGetValue(stat, out PlayerStatModifier modifier)
        ? modifier
        : default;
    }

    /// <summary>
    /// 将本局永久修正和调用方提供的临时修正应用到基础值。
    /// 临时修正不写入本对象，Buff 到期后无需反向修改 SurvivorModel。
    /// </summary>
    public float Calculate(PlayerStat stat, float baseValue, PlayerStatModifier temporaryModifier)
    {
      PlayerStatModifier totalModifier = GetPermanentModifier(stat).Combine(temporaryModifier);
      return totalModifier.ApplyTo(baseValue);
    }

    /// <summary>
    /// 累加一条本局永久属性升级。仅供 SurvivorProxy 调用，配置资源和场景组件不应直接写入。
    /// </summary>
    internal bool AddPermanentModifier(PlayerStat stat, float value, bool isPercent)
    {
      if (!IsSupported(stat) || float.IsNaN(value) || float.IsInfinity(value) || value == 0f)
        return false;

      PlayerStatModifier current = GetPermanentModifier(stat);
      _permanentModifiers[stat] = current.Combine(PlayerStatModifier.FromValue(value, isPercent));
      return true;
    }

    /// <summary>
    /// 限制可写入的属性，防止损坏的 ScriptableObject 枚举值进入运行时数据。
    /// 扩展 PlayerStat 时需要在这里同步登记，确保新增属性是一次显式的维护动作。
    /// </summary>
    private static bool IsSupported(PlayerStat stat)
    {
      int value = (int)stat;
      return value >= (int)PlayerStat.MoveSpeed && value <= (int)PlayerStat.TargetingRange;
    }
  }
}
