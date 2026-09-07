namespace VampireSurvivorsLike
{
  /// <summary>
  /// 一组玩家属性修正值。固定值与百分比值分开累计，最终统一按
  /// (基础值 + 固定值总和) × (1 + 百分比总和) 计算。
  /// </summary>
  public readonly struct PlayerStatModifier
  {
    /// <summary>直接加在基础值上的固定增量。</summary>
    public float Flat { get; }

    /// <summary>以小数表示的百分比增量，例如 0.2 表示增加 20%。</summary>
    public float Percent { get; }

    /// <summary>创建一组明确的固定值和百分比修正。</summary>
    public PlayerStatModifier(float flat, float percent)
    {
      Flat = flat;
      Percent = percent;
    }

    /// <summary>根据配置中的 isPercent 将单个数值转换为统一修正结构。</summary>
    public static PlayerStatModifier FromValue(float value, bool isPercent)
    {
      return isPercent
        ? new PlayerStatModifier(0f, value)
        : new PlayerStatModifier(value, 0f);
    }

    /// <summary>合并来自永久升级、Buff 或其他来源的修正，不改变任一来源对象。</summary>
    public PlayerStatModifier Combine(PlayerStatModifier other)
    {
      return new PlayerStatModifier(Flat + other.Flat, Percent + other.Percent);
    }

    /// <summary>将当前修正应用到指定基础值；各属性的最小值限制由最终使用方决定。</summary>
    public float ApplyTo(float baseValue)
    {
      return (baseValue + Flat) * (1f + Percent);
    }
  }
}
