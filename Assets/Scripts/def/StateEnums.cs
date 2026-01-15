

public enum EntityState
{
  Idle,
  Run,
  Jump,
  Fall,
  Attack,
  Hurt,
  Death,
}

// 掉落物品类型
public enum DropItemType
{
  Gem,
  Coin,
}

public enum BuffStackType
{
  Refresh,   // 刷新时间
  Stack,     // 叠加层数
  Replace,   // 覆盖

  Damage,
  Cooldown,
  Range,

  MoveSpeed, // 移动速度，速度叠加，时间则重置
  MoveSpeed2, // 移动速度，速度叠加，时间叠加
  MoveSpeed3, // 移动速度，多个buff叠加，每个重置时间不同
}
