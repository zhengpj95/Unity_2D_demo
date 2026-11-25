# Enemy System

## 1. 当前实现

第一阶段敌人系统由 `EnemyDirector`、`EnemySpawner`、`EnemyChasing` 和框架 `PoolManager` 组成：

```text
EnemyDirector
    ↓ 按 WaveTimelineConfig 调度
EnemySpawner
    ↓ 计算出生点并从 PoolManager 取出实例
EnemyChasing
    ↓ 追击、超距回收、碰撞玩家回收
EnemyHealth
    ↓ 受伤动画、死亡、击杀统计和掉落
DropItemManager
```

当前实现文件：

```text
Assets/Scripts/Modules/VampireSurvivorsLike/Entity/Enemy/EnemyDirector.cs
Assets/Scripts/Modules/VampireSurvivorsLike/Entity/Enemy/EnemySpawner.cs
Assets/Scripts/Modules/VampireSurvivorsLike/Entity/Enemy/EnemyChasing.cs
Assets/Scripts/Modules/VampireSurvivorsLike/Entity/Enemy/EnemyHealth.cs
Assets/Scripts/Modules/VampireSurvivorsLike/Entity/Enemy/EnemyTypeConfig.cs
Assets/Scripts/Modules/VampireSurvivorsLike/Entity/Enemy/EnemyConfigurator.cs
Assets/Scripts/Framework/Pool/PoolManager.cs
Assets/Prefabs/Survivor/Enemy/EnemyCommon.prefab
```

敌人 Wave 配置的详细说明见 [WaveSystem.md](WaveSystem.md)。

---

## 2. 系统职责

### EnemyDirector

负责：

- 保存场景中的敌人列表和击杀计数。
- 解析 Player 引用并创建 `EnemySpawner`。
- 累计 `gameTime`，按 `WaveTimelineConfig` 切换当前 Wave，并驱动每个 `WaveSpawnEntry` 的独立计时器。
- 按当前 `WaveSpawnConfig.maxEnemies` 限制同时存活的敌人数量。
- 预热当前模式会使用的敌人 Prefab。
- 提供 `RecycleEnemy(GameObject)` 作为统一回收入口。
- 提供最近/随机选敌接口；`GetRandomExcluding` 支持调用方复用排除集合，让同一次多目标施放优先命中不同敌人。

`EnemyDirector` 是场景级单例，不跨场景保留。它持有当前场景的 Player、敌人容器和 Wave 运行时计时；重开时通过重载场景重新创建，避免继续引用上一局已销毁的 Player。

不负责计算具体出生位置，也不负责敌人的移动、受伤或死亡。

### EnemySpawner

负责：

- 以 Player 当前坐标为圆心计算随机出生点。
- 调用 `PoolManager.Alloc(prefab, position, rotation)` 获取敌人实例。
- 设置运行时父节点 `enemyContainer`。
- 调用 `EnemyChasing.Initialize(player, director)` 注入运行时依赖。

它只生成当前 `WaveSpawnEntry` 明确指定的 Prefab，不保存候选列表，也不负责随机选择敌人类型。

### 配置驱动敌人（可选）

`EnemyTypeConfig` 保存动画、速度、伤害、生命、碰撞体和血条位置。`EnemyCommon.prefab` 提供统一节点层级，`EnemyConfigurator.Apply` 在生成或 Editor 实例化时把配置写入实例：覆盖 `EnemyChasing` 与 `EnemyHealth` 数值、切换 `AnimFrame` 配置、设置 `CapsuleCollider2D` 和血条 Transform。池复用时每次出池都会重新应用，避免残留上一类型的数据。

`WaveSpawnEntry.enemyTypeConfig` 为空时走旧 Prefab 路径；非空时由 `EnemyDirector.enemyCommonPrefab` 提供外壳。当前 `SurvivorsDemo` 已绑定通用 Prefab，4 份 Wave 的条目也都设置了 `enemyTypeConfig`，因此实际生效的是配置驱动路径；条目上的 `enemyPrefab` 仅作为回退与对照保留。`spriteSize` 在 Simple 绘制模式下不参与显示，暂不注入实例。

### EnemyChasing

负责：

- 保存 Player 和 EnemyDirector 引用。
- 在 `FixedUpdate` 中通过 `Rigidbody2D.MovePosition` 追击 Player。
- 自己检查与 Player 的平方距离，超过 `DespawnSqrDistance` 时请求回收。
- 与带 `Player` 标签的物体碰撞后造成伤害并回收自身。
- 不再保存单个敌人的掉落类型或概率；死亡掉落统一由 `DropItemManager` 的场景权重决定。
- 在池生命周期中重置刚体速度并注销自身。

### EnemyHealth

负责：

- 在 `OnAlloc` 时恢复满血。
- 受伤后刷新 `HpBarDisplay` 和伤害飘字。
- 对配置了 `AnimFrame` 的敌人播放一次 `Hit`，完成后恢复循环的 `Move`；旧敌人未配置该组件时保持原行为。
- 当前 `Enemy_01/02/03`、`Enemy_Rino`、`Enemy_Slime` 和 `Enemy_Treant` 均使用 `AnimFrame`（拖入 `SpriteRenderer`）；Treant 没有 `Hit` 素材，受伤时继续保持 `Move`。
- 生命值归零时累计击杀、生成掉落并通过 `EnemyDirector.RecycleEnemy` 回收。
- 武器高频调用的最近/随机选敌直接遍历已注册敌人；随机选择使用蓄水池抽样，不创建候选列表或按距离排序。
- `ClearActiveEnemies` 在重开前回收当前活跃敌人，避免旧回合实体进入下一局。

---

## 3. 生成逻辑

出生位置基于 Player 当前坐标，而不是固定世界坐标：

```csharp
Vector2 direction = Random.insideUnitCircle;
if (direction.sqrMagnitude < Mathf.Epsilon)
    direction = Vector2.right;

Vector2 spawnPosition =
    (Vector2)player.position + direction.normalized * spawnRadius;
```

这样可以适配无限地表和玩家持续移动。当前 `SurvivorsDemo` 场景配置为：

```text
spawnRadius = 10
```

`EnemySpawner` 不决定何时刷怪；时间节奏由 `EnemyDirector` 决定。

---

## 4. Wave 刷怪

`EnemyDirector` 从 `WaveTimelineConfig` 的第一个条目开始，按各段 `duration` 自动计算连续的起止时间：

- `WaveSpawnConfig` 描述可复用的敌人上限和 `WaveSpawnEntry` 组合，不保存时间。
- `WaveTimelineEntry` 引用一个 `WaveSpawnConfig`，并保存本次使用的持续时间或无限标记。
- 当前 Wave 变化时重建运行时 WaveSpawnEntry 列表，旧计时器不会带入新 Wave。
- 每个 WaveSpawnEntry 独立计时，并使用其自己的 Prefab、间隔和批量数量。
- 场上敌人数量达到当前 Wave 的 `maxEnemies` 时不再继续增加；各条目仍按原间隔计时，不积累待补刷数量。
- 未配置有效时间轴时不生成敌人，并在 Console 输出 Warning。

详细配置和示例资源见 [WaveSystem.md](WaveSystem.md)。

---

## 5. 移动与回收

敌人在 `FixedUpdate` 中使用 Rigidbody2D 追击：

```text
player.position - enemy.position
    ↓ normalized
Rigidbody2D.MovePosition
```

当前场景配置：

```text
despawnRadius = 20
```

必须满足：

```text
despawnRadius > spawnRadius
```

敌人自己使用平方距离比较，避免每帧调用平方根。超距、碰撞玩家和死亡最终都进入同一个 `EnemyDirector.RecycleEnemy` → `PoolManager.Free` 流程；碰撞玩家和超距回收不会计入击杀或生成掉落。

---

## 6. 受伤、死亡和掉落

```text
投射物/武器命中
    ↓
EnemyHealth.TakeDamage
    ↓
HP > 0：刷新血条
HP <= 0：
    ├── EnemyDirector.KillEnemyCount++
    ├── SurvivorModule.UpdateEnemyKillCount()
    ├── DropItemManager.SpawnDropItem()
    └── EnemyDirector.RecycleEnemy()
```

死亡掉落由场景 `DropItemManager` 的 `gemDropWeight/coinDropWeight` 统一抽取；当前 `SurvivorsDemo` 配置为 `80:10`，每次击杀必定掉落一件，金币作为局外武器升级资源保持稀缺。Gem 和 Coin 的显示动画由 `AnimFrame` 驱动，不再依赖各自的 Animator Controller；拾取流程见 [Survivor.md](Survivor.md)。

---

## 7. 对象池规则

项目没有独立的 `EnemyPool` 或 `EnemyPoolManager`；敌人直接使用框架 `PoolManager`：

```text
EnemyDirector.Start
    ↓ Preload(prefab, preloadCountPerPrefab)
EnemySpawner
    ↓ Alloc(prefab, position, rotation)
EnemyChasing.Initialize
    ↓
RecycleEnemy
    ↓ Free(enemy)
```

`PoolManager` 以 Prefab 的 `InstanceID` 区分池，池中没有实例时会按需 `Instantiate`。当前场景 `preloadCountPerPrefab = 3`，时间轴会对所有 Wave 中有效条目的不同 Prefab 各预热一次。

常规敌人生命周期不直接调用 `Destroy(gameObject)`。只有不属于池的对象交给 `PoolManager.Free` 时，PoolManager 才会记录警告并销毁它。

池复用时必须重置状态：

- `EnemyHealth.OnAlloc` 恢复满血。
- `EnemyHealth.OnAlloc` 将 `AnimFrame` 恢复为 `Move`，入池时停止动画并清理上一轮完成回调。
- `EnemyChasing.OnAlloc` 清零 Rigidbody2D 速度。
- `EnemyChasing.OnFree` 注销敌人。

---

## 8. 场景配置

场景位置：

```text
Assets/Scenes/SurvivorsDemo.unity
└── EnemyDirector
```

当前主要字段：

| 字段                    | 当前用途                                                           |
| ----------------------- | ------------------------------------------------------------------ |
| `enemyContainer`        | 运行时敌人父节点                                                   |
| `player`                | 生成中心和追击目标；为空时按 Player 标签解析一次                   |
| `spawnRadius`           | 出生半径，当前为 10                                                |
| `despawnRadius`         | 超距回收半径，当前为 20                                            |
| `preloadCountPerPrefab` | 每种 Prefab 预热数量，当前为 3                                     |
| `waveTimeline`          | 本局唯一的 Wave 时间轴；当前引用 `WaveTimeline_Default`            |
| `enemyCommonPrefab`     | 配置驱动路径使用的通用敌人 Prefab；当前已引用 `EnemyCommon.prefab` |

不需要在场景中创建独立 EnemyPool 节点。`enemyContainer` 只负责运行时层级整理，不改变敌人的世界坐标。

---

## 9. 当前边界

已实现：

- 无限地图下的玩家中心刷怪。
- 可复用 `WaveSpawnConfig` 与顺序 `WaveTimelineConfig` 调度。
- EnemyChasing 追击、超距回收和碰撞玩家回收。
- 受伤、死亡、击杀统计、掉落和对象池复用。

当前未实现：

- Boss、Elite、稀有度和特殊出生编队。
- NavMesh、A*、行为树、复杂避障或敌人分离。
- 动态难度预算；两种敌人生成方式尚未收敛为一条，旧 Prefab 路径保留作回退，详见第 10 节。
- DOTS/ECS、Spatial Hash、分帧大规模模拟。

---

## 10. 两种敌人生成方式（当前并存，待收敛）

代码同时保留两条路径，由 `WaveSpawnEntry.enemyTypeConfig` 是否为空决定实际走哪条：

| 对比项   | 方式 A：独立 Prefab（旧）                    | 方式 B：通用 Prefab + SO（新）                                                               |
| -------- | -------------------------------------------- | -------------------------------------------------------------------------------------------- |
| 配置入口 | `WaveSpawnEntry.enemyPrefab`                 | `WaveSpawnEntry.enemyTypeConfig` + `EnemyDirector.enemyCommonPrefab`                         |
| 数据来源 | 每种敌人 Prefab 的序列化字段                 | `EnemyTypeConfig`（数值/碰撞体/血条）+ `AnimFrameConfig`（动画）                             |
| 预制体   | `Enemy_01/02/03/Rino/Slime/Treant` 各一份    | 统一 `EnemyCommon.prefab`                                                                    |
| 数值修改 | 打开对应 Prefab 改字段                       | 改 SO 资源；多个 Wave 共享同一类型配置                                                       |
| 出池处理 | 只跑 `OnAlloc`，使用 Prefab 固定值           | `EnemyConfigurator.Apply` 每次出池覆盖数值、动画、碰撞体、血条                               |
| 对象池键 | 每种 Prefab 各一个池                         | 通用 Prefab 共用一个池，按配置切换表现                                                       |
| 当前状态 | Wave 条目仍保留引用，但被 B 覆盖，实际不生效 | 场景已绑定 `EnemyCommon.prefab`，4 份 Wave 共 9 个条目已设置 `enemyTypeConfig`，当前实际生效 |

优先级：条目同时填写两个字段时，方式 B 优先；`enemyPrefab` 只在 `enemyTypeConfig` 为空时作为回退。Editor 的"实例化选中配置到场景"只用于验证方式 B，不是第三种生成方式。

### 后续收敛建议（未执行）

推荐保留方式 B：新增敌人只需建一份 `EnemyTypeConfig` 并复用 `AnimFrameConfig`，不再复制整套 Prefab；数值、碰撞体和血条集中修改，避免 Prefab 与 SO 两份数据漂移。代价是旧 Prefab 及其直连动画引用会废弃，且需要先确认配置注入后的视觉与数值和旧 Prefab 一致。

收敛步骤：

1. Play Mode 验收 4 个 Wave 的敌人外观、碰撞体、速度、伤害、血量与旧 Prefab 基线一致。
2. 从 `WaveSpawnEntry` 移除 `enemyPrefab` 和 `IsValid` / `ResolveSpawnPrefab` 的旧分支，并从 Wave 资源清理该字段。
3. 删除 6 个旧敌人 Prefab 及其 `.meta`，保留 `EnemyCommon.prefab` 和类型配置。
4. 同步更新本文件、`WaveSystem.md` 与场景引用说明。

若决定保留方式 A，则反向移除 `EnemyTypeConfig`、`EnemyConfigurator`、`enemyCommonPrefab`、Editor 实例化入口及新增的 6 份类型配置。当前不执行删除，因为收敛前必须完成 Play Mode 验收。

---

## 11. 文档同步规则

修改以下代码时要同步检查对应文档：

| 代码变更                                                               | 文档                                        |
| ---------------------------------------------------------------------- | ------------------------------------------- |
| `EnemyDirector`、`EnemySpawner`、`EnemyChasing`、`EnemyHealth`       | 本文件、`WaveSystem.md`                     |
| `WaveSpawnConfig`、`WaveTimelineConfig`、敌人生命/速度或 Wave 调度规则 | `WaveSystem.md`、`BalanceSystem.md`、本文件 |
| `PoolManager` 的敌人池生命周期                                         | 本文件、`Survivor.md`                       |
| 掉落类型、死亡结算或拾取规则                                           | `Survivor.md`，必要时本文件                 |

文档中的类型名、字段名和场景参数必须与当前代码一致；未来规划只能放在“当前边界/后续方向”中。
