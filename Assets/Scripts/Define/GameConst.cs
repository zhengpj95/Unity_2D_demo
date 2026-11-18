/// <summary>
/// UI层级索引
/// </summary>
public enum UILayerIndex
{
  Main = 0,
  Window = 1,
  Model = 2,
  Tip = 3,
  Guide = 4,
}

/**
 * 虚拟列表布局类型
 */
public enum LayoutType
{
  Vertical,
  Horizontal,
}

/**
 * 实体状态
 */
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

/**
 * 掉落物品类型
 */
public enum DropItemType
{
  Gem,
  Coin,
}

/**
 * Buff叠加类型
 */
public enum BuffStackType
{
  Refresh,   // 刷新时间
  Stack,     // 叠加层数，一个效果*层数
  Replace,   // 覆盖
}
