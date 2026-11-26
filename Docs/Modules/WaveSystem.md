# Wave System

## 1. 当前实现

WaveSystem 通过“可复用刷怪组合 + 单局时间轴”驱动普通敌人生成：

```text
EnemyManager
    ↓ 读取并调度
WaveTimelineConfig
    ↓ 包含有序条目
WaveTimelineEntry
    ↓ 引用刷怪配置并设置持续时间
WaveSpawnConfig
    ↓ 包含一条或多条
WaveSpawnEntry
    ↓ 指定敌人类型配置、间隔和单次数量
EnemySpawner → 通用 EnemyCommon Prefab + EnemyTypeConfig → PoolManager → EnemyChaser
```

相关代码：

```text
Assets/Scripts/Modules/VampireSurvivorsLike/WaveSystem/WaveSpawnConfig.cs
Assets/Scripts/Modules/VampireSurvivorsLike/WaveSystem/WaveTimelineConfig.cs
Assets/Scripts/Modules/VampireSurvivorsLike/Entity/Enemy/EnemyManager.cs
Assets/Scripts/Modules/VampireSurvivorsLike/Entity/Enemy/EnemySpawner.cs
```

`EnemyManager` 不再保存每种敌人的 Prefab、间隔或数量；这些规则全部来自 `WaveSpawnEntry`。所有敌人均从 `EnemyManager.enemyCommonPrefab` 生成，并由 `EnemyTypeConfig` 注入外形与数值。未配置有效 `WaveTimelineConfig` 时会输出警告，本局不生成敌人。

## 2. 配置职责

### WaveSpawnEntry

一条 `WaveSpawnEntry` 描述一种敌人的生成规则：

| 字段 | 含义 |
| --- | --- |
| `enemyTypeConfig` | 必填敌人类型配置；生成时统一从 `EnemyManager.enemyCommonPrefab` 创建实例并注入数值 |
| `spawnInterval` | 两次生成触发的间隔，单位为秒 |
| `spawnCount` | 每次触发生成的数量 |

`enemyTypeConfig` 为空、间隔不大于 0 或数量不大于 0 时，条目无效并在运行时被跳过。当前 4 份示例 Wave 共 9 个条目均已配置 `EnemyTypeConfig`。唯一的生成方式见 [EnemySystem.md](EnemySystem.md) 第 10 节。

### WaveSpawnConfig

`WaveSpawnConfig` 保存本波同时存活的敌人上限 `maxEnemies` 和 `spawnEntries`，表示一组可以复用的刷怪强度配置，不保存开始或结束时间。同一份 `WaveSpawnConfig` 可以被不同时间轴引用，也可以在同一时间轴中重复使用。`maxEnemies` 必须大于 0，否则引用它的时间轴条目无效。

创建路径：

```text
Project → Create → Survivor → Wave → Wave Spawn Config
```

### WaveTimelineConfig

`WaveTimelineConfig` 表示一局游戏的有序波次。每个 `WaveTimelineEntry` 包含：

| 字段 | 含义 |
| --- | --- |
| `waveSpawnConfig` | 可复用的 `WaveSpawnConfig` |
| `duration` | 此段持续时间；非无限段必须大于 0 |
| `isInfinite` | 此段是否持续到本局结束 |

创建路径：

```text
Project → Create → Survivor → Wave → Wave Timeline
```

时间轴从 0 秒开始按列表顺序累加 `duration`，因此无需手工维护 `startTime/endTime`，也不会产生重叠或空档。无限段之后的配置不会执行，所以无限段应放在最后。

## 3. 运行流程

`EnemyManager.Awake` 创建 `EnemySpawner`，读取时间轴并生成只读运行时调度表：

```text
累计 gameTime
↓
根据累计持续时间查找当前 Wave
↓
Wave 变化时重建 WaveSpawnRuntime
↓
分别推进每条 WaveSpawnEntry 的计时器
↓
到达间隔且未超过当前 Wave 的 maxEnemies 时调用 EnemySpawner
```

每个 `WaveSpawnEntry` 使用独立计时器。进入新 Wave 时允许下一帧立即触发一次；每帧最多处理一次该条目的生成触发，避免低帧率时集中补刷。切换 Wave 只停止旧规则继续生成，不清除场上已有敌人。达到当前 Wave 的 `maxEnemies` 时，本次触发不生成并继续按原间隔推进，不积累待补刷数量。

游戏暂停时 `Time.deltaTime` 为 0，时间轴和生成计时器都会暂停。

## 4. 对象池与边界

`EnemySpawner` 只使用 `EnemyManager.enemyCommonPrefab`，并要求调用方提供 `EnemyTypeConfig`。它负责：

1. 以 Player 为圆心计算出生位置。
2. 从 `PoolManager` 取得通用 Prefab 实例。
3. 调用 `EnemyConfigUtil.Apply` 覆盖数值、动画、碰撞体和血条；注入失败时回收实例。
4. 设置 `enemyContainer` 父节点。
5. 调用 `EnemyChaser.Initialize(player, director)`。

`EnemyManager` 启动时预热唯一的 `EnemyCommon` Prefab；所有 Wave 和敌人类型共享同一个对象池预热结果。敌人死亡、碰撞玩家或超出 `despawnRadius` 时仍通过 `EnemyManager.RecycleEnemy` 和 `PoolManager.Free` 回收。

## 5. 当前示例

默认配置位于：

```text
Assets/Configs/Survivor/
├── WaveTimeline_Default.asset
├── Wave_01_Intro.asset
├── Wave_02_Threat.asset
├── Wave_03_Heavy.asset
└── Wave_04_Ultimate.asset
```

| 时间轴段 | 持续时间 | 存活上限 | WaveSpawnEntry |
| --- | --- | --- | --- |
| Wave 1 | 30 秒 | 20 | EnemyType01：间隔 1.0 秒，数量 1 |
| Wave 2 | 30 秒 | 30 | EnemyType02：0.8 秒/1；EnemyType03：1.5 秒/1 |
| Wave 3 | 30 秒 | 40 | Rino：0.6 秒/2；Slime：1.2 秒/1；Treant：2.0 秒/1 |
| Wave 4 | 无限 | 80 | EnemyType01：0.6 秒/3；EnemyType02：1.2 秒/3；EnemyType03：1.8 秒/2 |

场景 `SurvivorsDemo/EnemyManager` 需要引用 `WaveTimeline_Default` 和 `EnemyCommon.prefab`。敌人上限完全由当前 `WaveSpawnConfig` 决定；生成半径、回收半径和预热数量仍配置在 `EnemyManager`。

## 6. 当前未实现

- Boss、Elite 和特殊事件。
- Spawn Weight、Enemy Budget 和动态难度。
- Spawn Pattern、Formation 和地图区域刷怪。
- Wave Clear Condition 和 Wave Reward。
- 独立 `EnemyConfig` 数值资源。

后续若扩展敌人数值配置，应保持 `EnemyManager` 负责时间调度、`EnemySpawner` 负责实例生成，继续复用现有 `PoolManager`。

## 7. 验收清单

1. 0～30 秒仅生成 EnemyType01。
2. 30～60 秒按各自间隔生成 EnemyType02 和 EnemyType03。
3. 60～90 秒生成 Rino、Slime 和 Treant。
4. 90 秒后持续按各自间隔生成 EnemyType01、EnemyType02 和 EnemyType03。
5. Wave 切换不删除场上已有敌人。
6. 四个 Wave 的场上敌人上限依次为 20、30、40、80。
7. 敌人仍从 `PoolManager` 取出并回收。
8. 暂停期间时间轴和生成计时器不推进。
9. 缺少 Timeline、存在无效条目或无限段不在末尾时，Console 给出 Warning。

Unity Editor 内的实际运行效果需要在编辑器中确认。

## 8. 文档同步规则

| 代码变更 | 需要同步的文档 |
| --- | --- |
| `WaveSpawnConfig`、`WaveTimelineConfig`、`WaveSpawnEntry` 或示例数值 | 本文件、`EnemySystem.md`、`BalanceSystem.md` |
| `EnemyManager`、`EnemySpawner`、`EnemyChaser` | 本文件、`EnemySystem.md`，必要时 `Survivor.md` |
| 敌人对象池生命周期 | 本文件、`EnemySystem.md`、`Survivor.md` |
