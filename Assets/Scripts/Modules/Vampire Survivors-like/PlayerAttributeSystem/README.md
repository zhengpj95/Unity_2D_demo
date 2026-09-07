# Player Attribute System

该目录提供 Survivor 局内玩家属性的统一定义与计算规则。它不是独立 Manager，也不保存第二份玩家状态；永久属性数据由 `SurvivorModel` 持有、由 `SurvivorProxy` 修改。

## 1. 当前属性

| `PlayerStat`     | 含义                   | 基础值来源                       | 最终使用方                         |
| ---------------- | ---------------------- | -------------------------------- | ---------------------------------- |
| `MoveSpeed`      | 玩家移动速度           | `Hero.baseMoveSpeed`             | `Hero` 的 Rigidbody2D 速度         |
| `PickupRadius`   | 掉落物拾取半径         | `Hero.basePickupRadius`          | Player 的拾取 CircleCollider2D     |
| `MaxHealth`      | 玩家最大生命           | `SurvivorModel.DefaultMaxHealth` | `SurvivorProxy`、主界面和 GameOver |
| `TargetingRange` | 武器寻找目标的基础距离 | `Hero.baseAttackRange`           | `WeaponController` 选敌            |

`baseAttackRange` 为兼容已有场景序列化暂时保留旧字段名；运行时语义已经是 `TargetingRange`。它不等于改变武器碰撞体大小的 `WeaponArea`。

## 2. 数据来源与公式

```text
Hero / SurvivorModel 中的基础值
        ↓
PlayerAttributeSet 中的本局永久升级
        ↓
BuffHandler 提供的临时修正
        ↓
(基础值 + 固定值总和) × (1 + 百分比总和)
        ↓
Hero / WeaponController / SurvivorProxy 使用最终值
```

- 三选一产生的永久增量写入 `SurvivorModel.PlayerAttributes`，重开时随 `SurvivorModel` 重建。
- Buff 只在计算时提供临时 `PlayerStatModifier`，不会写入 Model；Buff 到期后自然退出最终计算。
- 当前已有临时 Buff 只接入 `MoveSpeed` 和 `TargetingRange`；`PickupRadius` 与 `MaxHealth` 已具备统一枚举和永久升级能力，但尚无对应的限时 Buff 类型。
- `PlayerStatModifier` 不限制负数，方便后续实现减速等 Debuff；最终最小值由属性使用方决定。
- 最大生命使用同一公式，但由 `SurvivorProxy` 取整并同步当前生命，避免场景组件持有第二份生命数据。

## 3. 模块职责

| 类型                 | 职责                                                   |
| -------------------- | ------------------------------------------------------ |
| `PlayerStat`         | Upgrade、Buff、Hero 共用的稳定属性枚举。               |
| `PlayerStatModifier` | 表示固定值和百分比修正，负责无状态的合并与计算。       |
| `PlayerAttributeSet` | 保存本局永久修正，不持有场景对象。                     |
| `SurvivorProxy`      | 唯一的永久属性写入口，负责最大生命的特殊同步。         |
| `BuffHandler`        | 汇总有持续时间的临时修正。                             |
| `Hero`               | 保存场景基础配置并消费最终属性，不再保存永久升级字段。 |
| `UpgradeSystem`      | 选择并提交升级，不拥有玩家属性数据。                   |

## 4. 扩展新属性

1. 在 `PlayerStat` 末尾增加枚举值；不要修改已有数值，避免旧 `PlayerUpgradeConfig` 资源错位。
2. 在 `PlayerAttributeSet.IsSupported` 中登记新属性。
3. 明确基础值的唯一来源与最终消费方，并使用 `SurvivorModule.CalculatePlayerStat` 合并永久/临时修正。
4. 如果需要三选一升级，在 `PlayerUpgradeConfig` 中补充 `UpgradeId`、标题和描述映射。
5. 如果需要临时 Buff，让对应 `BuffInstance.GetStatModifier` 返回该属性的修正。
6. 为属性定义合理的最小值、取整方式和 Play Mode 验收步骤，并同步模块文档。

`WeaponArea`、伤害、冷却、投射物数量等尚未加入本系统；只有字段存在且所有实际消费方闭合后，才能列为已实现属性。
