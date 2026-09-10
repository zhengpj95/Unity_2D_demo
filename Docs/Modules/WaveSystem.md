# Wave System

## 1. 当前实现

WaveSystem 通过“可复用刷怪组合 + 单局时间轴”驱动普通敌人生成：

```text
EnemyDirector
    ↓ 读取并调度
WaveTimelineConfig
    ↓ 包含有序条目
WaveTimelineEntry
    ↓ 引用刷怪配置并设置持续时间
WaveSpawnConfig
    ↓ 包含一条或多条
WaveSpawnEntry
    ↓ 指定 Prefab、间隔和单次数量
EnemySpawner → PoolManager → EnemyChasing
```

相关代码：

```text
Assets/Scripts/Modules/Vampire Survivors-like/WaveSystem/WaveSpawnConfig.cs
Assets/Scripts/Modules/Vampire Survivors-like/WaveSystem/WaveTimelineConfig.cs
Assets/Scripts/Modules/Vampire Survivors-like/Entity/EnemyDirector.cs
Assets/Scripts/Modules/Vampire Survivors-like/Entity/EnemySpawner.cs
```

`EnemyDirector` 不再保留 `enemyPrefab`、`spawnInterval`、`spawnCount` 固定刷怪逻辑。未配置有效 `WaveTimelineConfig` 时会输出警告，本局不生成敌人。

## 2. 配置职责

### WaveSpawnEntry

一条 `WaveSpawnEntry` 描述一种敌人的生成规则：

| 字段 | 含义 |
| --- | --- |
| `enemyPrefab` | 要生成的敌人 Prefab，必须挂载 `EnemyChasing` |
| `spawnInterval` | 两次生成触发的间隔，单位为秒 |
| `spawnCount` | 每次触发生成的数量 |

Prefab 为空、间隔不大于 0 或数量不大于 0 时，条目无效并在运行时被跳过。

### WaveSpawnConfig

`WaveSpawnConfig` 只保存 `spawnEntries`，表示一组可以复用的刷怪组合，不保存开始或结束时间。同一份 `WaveSpawnConfig` 可以被不同时间轴引用，也可以在同一时间轴中重复使用。

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

`EnemyDirector.Awake` 创建 `EnemySpawner`，读取时间轴并生成只读运行时调度表：

```text
累计 gameTime
↓
根据累计持续时间查找当前 Wave
↓
Wave 变化时重建 WaveSpawnRuntime
↓
分别推进每条 WaveSpawnEntry 的计时器
↓
到达间隔且未超过 maxEnemies 时调用 EnemySpawner
```

每个 `WaveSpawnEntry` 使用独立计时器。进入新 Wave 时允许下一帧立即触发一次；每帧最多处理一次该条目的生成触发，避免低帧率时集中补刷。切换 Wave 只停止旧规则继续生成，不清除场上已有敌人。

游戏暂停时 `Time.deltaTime` 为 0，时间轴和生成计时器都会暂停。

## 4. 对象池与边界

`EnemySpawner` 只接受当前条目明确指定的 Prefab，负责：

1. 以 Player 为圆心计算出生位置。
2. 从 `PoolManager` 取得 Prefab 实例。
3. 设置 `enemyContainer` 父节点。
4. 调用 `EnemyChasing.Initialize(player, director)`。

`EnemyDirector` 会收集时间轴内所有有效 Prefab并去重预热。敌人死亡、碰撞玩家或超出 `despawnRadius` 时仍通过 `EnemyDirector.RecycleEnemy` 和 `PoolManager.Free` 回收。

## 5. 当前示例

默认配置位于：

```text
Assets/Configs/Survivor/
├── WaveTimeline_Default.asset
├── Wave_01_Intro.asset
├── Wave_02_Threat.asset
└── Wave_03_Heavy.asset
```

| 时间轴段 | 持续时间 | WaveSpawnEntry |
| --- | --- | --- |
| Wave 1 | 60 秒 | Slime：间隔 1.0 秒，数量 1 |
| Wave 2 | 60 秒 | Slime：0.8 秒/1；Rino：1.5 秒/1 |
| Wave 3 | 无限 | Slime：0.6 秒/2；Treant：1.2 秒/1；Rino：2.0 秒/1 |

场景 `SurvivorsDemo/EnemyDirector` 只需引用 `WaveTimeline_Default`。场景级上限、生成半径、回收半径和预热数量仍配置在 `EnemyDirector`。

## 6. 当前未实现

- Boss、Elite 和特殊事件。
- Spawn Weight、Enemy Budget 和动态难度。
- Spawn Pattern、Formation 和地图区域刷怪。
- Wave Clear Condition 和 Wave Reward。
- 独立 `EnemyConfig` 数值资源。

后续若扩展敌人数值配置，应保持 `EnemyDirector` 负责时间调度、`EnemySpawner` 负责实例生成，继续复用现有 `PoolManager`。

## 7. 验收清单

1. 0～60 秒仅生成 Slime。
2. 60～120 秒按各自间隔生成 Slime 和 Rino。
3. 120 秒后持续按第三条 Wave 生成 Slime、Treant 和 Rino。
4. Wave 切换不删除场上已有敌人。
5. 场上敌人不超过 `maxEnemies`。
6. 敌人仍从 `PoolManager` 取出并回收。
7. 暂停期间时间轴和生成计时器不推进。
8. 缺少 Timeline、存在无效条目或无限段不在末尾时，Console 给出 Warning。

Unity Editor 内的实际运行效果需要在编辑器中确认。

## 8. 文档同步规则

| 代码变更 | 需要同步的文档 |
| --- | --- |
| `WaveSpawnConfig`、`WaveTimelineConfig`、`WaveSpawnEntry` 或示例数值 | 本文件、`EnemySystem.md`、`BalanceSystem.md` |
| `EnemyDirector`、`EnemySpawner`、`EnemyChasing` | 本文件、`EnemySystem.md`，必要时 `Survivor.md` |
| 敌人对象池生命周期 | 本文件、`EnemySystem.md`、`Survivor.md` |
