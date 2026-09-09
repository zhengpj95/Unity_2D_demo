# Survivor 数值平衡基线

本文是 SB-005 的首版可调参数表。它不创建第二套运行时配置；下列 Prefab、`WeaponSO`、Wave 资源和场景 Inspector 仍是唯一数值真源。调整参数时先修改对应资源，再同步本文与验收记录。

## 1. 调整原则

- 初始 Bullet 在 Wave 1 面对 `2` 点生命的 Slime 时单发击杀，避免前 60 秒因伤害不足堆积敌人。
- 武器每升一级至少提升伤害、攻击频率、数量、选敌范围、持续时间或移动速度中的一项；不允许出现等级越高核心数值反向下降。
- Rino 与 Treant 承担中后段耐久差异，Slime 继续作为基础经验来源。
- Wave、掉落权重和敌人移动速度暂不由运行时倍率覆盖，便于先建立可重复的手动验收基线。

## 2. 玩家与敌人

| 类别 | 当前基线 | 唯一配置位置 | 说明 |
| --- | --- | --- | --- |
| 玩家生命 | `5` | `SurvivorsDemo/Hero` 的 `VSPlayerHealth` 覆盖 | 正式局内初始生命；不使用测试覆盖入口。 |
| 玩家移动速度 | `2` | `Hero.prefab` 的 `Hero.baseMoveSpeed` | 高于 Slime/Rino/Treant 的追击速度。 |
| 玩家索敌范围 | `4` | `Hero.prefab` 的 `Hero.baseAttackRange` | 武器 `range` 以此值为倍率基础。 |
| 玩家拾取范围 | `0.3` | `SurvivorsDemo/Hero` 的 `basePickupRadius` 覆盖 | 以 Hero 根节点为圆心。 |
| Slime | 生命 `2`，速度 `0.5`，伤害 `1` | `Enemy_Slime.prefab` | Wave 1 基础敌人。 |
| Rino | 生命 `6`，速度 `0.8`，伤害 `2` | `Enemy_Rino.prefab` | Wave 2 起的中型敌人。 |
| Treant | 生命 `10`，速度 `1.0`，伤害 `3` | `Enemy_Treant.prefab` | Wave 3 起的高耐久敌人。 |

## 3. 武器等级基线

`damage / count / range / fireInterval / duration` 分别对应伤害、对象数量、选敌倍率（Saw 为环绕半径）、触发间隔和攻击对象存活时间。Arrow、Bulletb 的 `speed` 是飞行速度，Saw 的 `speed` 是环绕角速度；静态范围武器的 `speed=0`、`damageInterval=0` 保持为非适用字段。

| 武器 | Lv1 | Lv2 | Lv3 |
| --- | --- | --- | --- |
| Arrow | `spd 5, dmg 2, cnt 1, rng 1, int 0.8` | `6, 3, 1, 1.25, 0.7` | `7, 4, 2, 1.5, 0.6` |
| Bulletb | `spd 6, dmg 2, cnt 1, rng 1, int 0.5` | `7, 3, 2, 1.25, 0.45` | `8, 4, 2, 1.5, 0.4` |
| BlueOval | `dmg 2, cnt 1, rng 1, int 1.2` | `3, 1, 1.25, 1` | `4, 2, 1.5, 0.8` |
| Lightning | `dmg 3, cnt 1, rng 1, int 1.3` | `4, 1, 1.25, 1` | `5, 2, 1.5, 0.8` |
| Fire | `dmg 1, tick 0.7, cnt 1, rng 1, int 4, dur 2` | `2, 0.6, 1, 1.25, 3.5, 3` | `3, 0.5, 2, 1.5, 3, 3` |
| Saw | `spd 180, dmg 2, cnt 1, rad 1, int 4, dur 3` | `270, 3, 2, 1.5, 3.5, 4` | `360, 4, 2, 2, 3, 5` |

完整字段语义见 `UpgradeSystem.md` 的 SB-004 章节；不要把本表的简写当作新增字段。

## 4. Wave 与掉落

| 项目 | 当前基线 | 配置位置 | 说明 |
| --- | --- | --- | --- |
| Wave 1 | Slime：`1 / 秒` | `Wave_01_Intro.asset` | `0 ~ 60` 秒的初期压力。 |
| Wave 2 | Slime：`1.25 / 秒`；Rino：约 `0.67 / 秒` | `Wave_02_Threat.asset` | `60 ~ 120` 秒。 |
| Wave 3 | Slime：约 `3.33 / 秒`；Treant：约 `0.83 / 秒`；Rino：`0.5 / 秒` | `Wave_03_Heavy.asset` | `120` 秒后，场上上限仍由 `EnemyDirector.maxEnemies=20` 限制。 |
| 生成/回收半径 | `10 / 20` | `SurvivorsDemo/EnemyDirector` | 保证敌人在镜头外生成并在过远时回收。 |
| Gem / Coin 权重 | `80 / 10` | `SurvivorsDemo/DropItemManager` | Coin 概率约 `11.1%`，保持稀缺但可稳定出现。 |

## 5. Play Mode 验收

1. 从 `Launcher.unity` 进入 `SurvivorsDemo`，不启用 `OverridePlayerHealthForTesting` 等测试入口。
2. 前 60 秒只使用初始 Bullet，确认 Slime 能被单发击杀，敌人不会持续堆至 `maxEnemies`。
3. 升级或获得各类武器后，确认 Lv2/Lv3 的核心数值不低于前一级；Saw Lv3 必须继续转动并更频繁触发。
4. 进入 Wave 2、Wave 3，确认 Rino 与 Treant 比 Slime 更耐打但玩家可通过武器升级处理。
5. 观察至少 30 次掉落，Coin 应明显少于 Gem，且并非长期不出现。
6. 武器槽位满、已有武器满级时，连续获得经验不应让升级流程停留在无可选候选状态。

SB-005 只有在以上 Play Mode 记录完成后才能标记为验收通过。
