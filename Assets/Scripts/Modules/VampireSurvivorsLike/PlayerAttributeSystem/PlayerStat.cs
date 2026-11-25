using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// 玩家属性的统一类型，供升级、Buff 和显示配置引用。
  /// 0～3 保留现有序列化编号；新增项仅定义标识，尚未接入属性计算或玩法。
  /// 属性数据仍由 SurvivorModel 保存、SurvivorProxy 修改。
  /// 不包含当前生命、等级、经验值和货币余额等运行时状态。
  /// 百分比增量使用 PlayerStatModifier 表达，不另建同属性的百分比枚举。
  /// 后续只追加编号，不重排已有数值；新增属性需明确基础值、边界和最终消费方。
  /// </summary>
  public enum PlayerStat
  {
    /// <summary>移动速度：世界单位/秒；现有属性。</summary>
    [InspectorName("移动速度 / MoveSpeed")]
    MoveSpeed = 0,

    /// <summary>拾取半径：世界单位；现有属性。</summary>
    [InspectorName("拾取半径 / PickupRadius")]
    PickupRadius = 1,

    /// <summary>最大生命：生命点数；现有属性，不等于当前生命。</summary>
    [InspectorName("最大生命 / MaxHealth")]
    MaxHealth = 2,

    /// <summary>索敌范围：世界单位；现有属性，不等于攻击碰撞范围。</summary>
    [InspectorName("索敌范围 / TargetingRange")]
    TargetingRange = 3,

    /// <summary>伤害：伤害点数；预留，玩家对武器的统一伤害属性。</summary>
    [InspectorName("伤害 / Damage")]
    Damage = 4,

    /// <summary>暴击率：比例，0.25 表示 25%；预留。</summary>
    [InspectorName("暴击率 / CriticalChance")]
    CriticalChance = 5,

    /// <summary>暴击伤害倍率：倍率，1.5 表示总伤害为基础伤害的 150%；预留。</summary>
    [InspectorName("暴击伤害倍率 / CriticalDamageMultiplier")]
    CriticalDamageMultiplier = 6,

    /// <summary>冷却缩减：比例，0.2 表示缩短 20%；预留，接入时定义上限。</summary>
    [InspectorName("冷却缩减 / CooldownReduction")]
    CooldownReduction = 7,

    /// <summary>投射物数量：每次攻击的投射物个数，最终按整数处理；预留。</summary>
    [InspectorName("投射物数量 / ProjectileCount")]
    ProjectileCount = 8,

    /// <summary>投射物速度：世界单位/秒，不用于环绕角速度；预留。</summary>
    [InspectorName("投射物速度 / ProjectileSpeed")]
    ProjectileSpeed = 9,

    /// <summary>攻击范围倍率：倍率，1 表示原始范围；预留，具体半径或尺寸缩放由武器定义。</summary>
    [InspectorName("攻击范围倍率 / WeaponArea")]
    WeaponArea = 10,

    /// <summary>效果持续时间：秒；预留，用于攻击效果持续时间。</summary>
    [InspectorName("效果持续时间 / EffectDuration")]
    EffectDuration = 11,

    /// <summary>穿透次数：额外可穿透目标数，0 表示不穿透；预留。</summary>
    [InspectorName("穿透次数 / PierceCount")]
    PierceCount = 12,

    /// <summary>击退强度：预留，具体单位由击退实现定义。</summary>
    [InspectorName("击退强度 / KnockbackForce")]
    KnockbackForce = 13,

    /// <summary>护甲：护甲点数；预留，减伤公式后续定义。</summary>
    [InspectorName("护甲 / Armor")]
    Armor = 14,

    /// <summary>伤害减免：比例，0.1 表示减伤 10%；预留，与护甲的结算顺序后续定义。</summary>
    [InspectorName("伤害减免 / DamageReduction")]
    DamageReduction = 15,

    /// <summary>闪避率：比例，0.1 表示 10%；预留。</summary>
    [InspectorName("闪避率 / DodgeChance")]
    DodgeChance = 16,

    /// <summary>生命恢复：每秒恢复的生命点数；预留。</summary>
    [InspectorName("生命恢复 / HealthRegeneration")]
    HealthRegeneration = 17,

    /// <summary>生命偷取：比例，0.05 表示按有效伤害的 5% 恢复；预留。</summary>
    [InspectorName("生命偷取 / LifeSteal")]
    LifeSteal = 18,

    /// <summary>经验获取倍率：倍率，1 表示基础经验，1.2 表示 120%；预留，不是当前经验。</summary>
    [InspectorName("经验获取倍率 / ExperienceGainMultiplier")]
    ExperienceGainMultiplier = 19,

    /// <summary>金币获取倍率：倍率，1 表示基础金币，1.2 表示 120%；预留，不是钱包余额。</summary>
    [InspectorName("金币获取倍率 / CoinGainMultiplier")]
    CoinGainMultiplier = 20,

  }
}
