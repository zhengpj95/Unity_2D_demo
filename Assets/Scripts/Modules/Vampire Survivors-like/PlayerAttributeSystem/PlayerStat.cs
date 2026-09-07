namespace VampireSurvivorsLike
{
  /// <summary>
  /// 玩家属性的统一类型。UpgradeSystem、BuffSystem 和实体表现层都依赖该定义，
  /// 但属性运行时数据仍由 SurvivorModel 保存、SurvivorProxy 修改。
  /// 枚举数值与旧 PlayerUpgradeStat 保持一致，避免已有 PlayerUpgradeConfig 资源丢失配置。
  /// </summary>
  public enum PlayerStat
  {
    MoveSpeed = 0,
    PickupRadius = 1,
    MaxHealth = 2,
    TargetingRange = 3,
  }
}
