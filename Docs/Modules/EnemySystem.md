# Enemy System

## 1. 当前实现

第一阶段敌人系统由 `EnemyDirector`、`EnemySpawner`、`EnemyChasing` 和框架 `PoolManager` 组成：

```text
EnemyDirector
    ↓ 固定刷怪或 Wave 调度
EnemySpawner
    ↓ 计算出生点并从 PoolManager 取出实例
EnemyChasing
    ↓ 追击、超距回收、碰撞玩家回收
VSEnemyHealth
    ↓ 受伤、死亡、击杀统计和掉落
DropItemManager
```

当前实现文件：

```text
Assets/Scripts/Modules/Vampire Survivors-like/Entity/EnemyDirector.cs
Assets/Scripts/Modules/Vampire Survivors-like/Entity/EnemySpawner.cs
Assets/Scripts/Modules/Vampire Survivors-like/Entity/EnemyChasing.cs
Assets/Scripts/Modules/Vampire Survivors-like/UI/VSEnemyHealth.cs
Assets/Scripts/Framework/Pool/PoolManager.cs
```

敌人 Wave 配置的详细说明见 [WaveSystem.md](WaveSystem.md)。

---

## 2. 系统职责

### EnemyDirector

负责：

- 保存场景中的敌人列表和击杀计数。
- 解析 Player 引用并创建 `EnemySpawner`。
- 在兼容模式下按 `spawnInterval/spawnCount` 刷怪。
- 在 Wave 模式下累计 `gameTime`、切换当前 Wave，并驱动每个 `SpawnEntry` 的独立计时器。
- 限制场景中同时存活的敌人数量 `maxEnemies`。
- 预热当前模式会使用的敌人 Prefab。
- 提供 `RecycleEnemy(GameObject)` 作为统一回收入口。

`EnemyDirector` 是场景级单例，不跨场景保留。它持有当前场景的 Player、敌人容器和 Wave 运行时计时；重开时通过重载场景重新创建，避免继续引用上一局已销毁的 Player。

不负责计算具体出生位置，也不负责敌人的移动、受伤或死亡。

### EnemySpawner

负责：

- 以 Player 当前坐标为圆心计算随机出生点。
- 调用 `PoolManager.Alloc(prefab, position, rotation)` 获取敌人实例。
- 设置运行时父节点 `enemyContainer`。
- 调用 `EnemyChasing.Initialize(player, director)` 注入运行时依赖。

它同时保留旧接口：随机选择 `EnemyDirector.enemyPrefab` 列表中的 Prefab；Wave 模式使用带 `GameObject prefab` 参数的重载。

### EnemyChasing

负责：

- 保存 Player 和 EnemyDirector 引用。
- 在 `FixedUpdate` 中通过 `Rigidbody2D.MovePosition` 追击 Player。
- 自己检查与 Player 的平方距离，超过 `DespawnSqrDistance` 时请求回收。
- 与带 `Player` 标签的物体碰撞后造成伤害并回收自身。
- 在池生命周期中重置刚体速度并注销自身。

### VSEnemyHealth

负责：

- 在 `OnAlloc` 时恢复满血。
- 受伤后刷新 `UI_HpBar` 和伤害飘字。
- 生命值归零时累计击杀、生成掉落并通过 `EnemyDirector.RecycleEnemy` 回收。

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

## 4. 两种刷怪模式

### 兼容模式

当 `EnemyDirector.waves` 为空时，沿用旧逻辑：

```text
timer += Time.deltaTime
timer >= spawnInterval
    ↓
按 spawnCount 尝试生成
```

场景中的旧字段仍然保留：

- `enemyPrefab`：随机候选 Prefab 数组。
- `spawnInterval`：刷怪间隔。
- `spawnCount`：每次尝试数量。

### Wave 模式

当 `waves` 至少包含一个资源时：

- 按 `WaveConfig.StartTime` 排序运行时列表。
- 使用 `StartTime <= gameTime < EndTime` 查找当前 Wave。
- 当前 Wave 变化时重建运行时 SpawnEntry 列表，旧计时器不会带入新 Wave。
- 每个 SpawnEntry 独立计时，并使用其自己的 Prefab、间隔和批量数量。
- 场上敌人数量达到 `maxEnemies` 时不再继续增加；计时器仍保留一个触发周期，不会低帧率补刷大量敌人。

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
VSEnemyHealth.TakeDamage
    ↓
HP > 0：刷新血条
HP <= 0：
    ├── EnemyDirector.KillEnemyCount++
    ├── SurvivorModule.UpdateEnemyKillCount()
    ├── DropItemManager.SpawnDropItem()
    └── EnemyDirector.RecycleEnemy()
```

死亡掉落的具体类型和概率由 `EnemyChasing.dropItemType/dropItemProb` 决定。掉落物之后由 [Survivor.md](Survivor.md) 中的拾取流程处理。

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

`PoolManager` 以 Prefab 的 `InstanceID` 区分池，池中没有实例时会按需 `Instantiate`。当前场景 `preloadCountPerPrefab = 3`，Wave 模式会对所有 Wave 中有效条目的不同 Prefab 各预热一次。

常规敌人生命周期不直接调用 `Destroy(gameObject)`。只有不属于池的对象交给 `PoolManager.Free` 时，PoolManager 才会记录警告并销毁它。

池复用时必须重置状态：

- `VSEnemyHealth.OnAlloc` 恢复满血。
- `EnemyChasing.OnAlloc` 清零 Rigidbody2D 速度。
- `EnemyChasing.OnFree` 注销敌人。

---

## 8. 场景配置

场景位置：

```text
Assets/Scenes/Vampire Survivors-like/SurvivorsDemo.unity
└── EnemyDirector
```

当前主要字段：

| 字段 | 当前用途 |
| --- | --- |
| `enemyPrefab` | 兼容模式的随机敌人 Prefab 列表 |
| `spawnInterval` | 兼容模式刷怪间隔 |
| `spawnCount` | 兼容模式单次数量 |
| `maxEnemies` | 场景中同时存活敌人上限，当前为 20 |
| `enemyContainer` | 运行时敌人父节点 |
| `player` | 生成中心和追击目标；为空时按 Player 标签解析一次 |
| `spawnRadius` | 出生半径，当前为 10 |
| `despawnRadius` | 超距回收半径，当前为 20 |
| `preloadCountPerPrefab` | 每种 Prefab 预热数量，当前为 3 |
| `waves` | 可选的 WaveConfig 数组 |

不需要在场景中创建独立 EnemyPool 节点。`enemyContainer` 只负责运行时层级整理，不改变敌人的世界坐标。

---

## 9. 当前边界

已实现：

- 无限地图下的玩家中心刷怪。
- 固定刷怪兼容模式和第一阶段 Wave 模式。
- EnemyChasing 追击、超距回收和碰撞玩家回收。
- 受伤、死亡、击杀统计、掉落和对象池复用。

当前未实现：

- Boss、Elite、稀有度和特殊出生编队。
- NavMesh、A*、行为树、复杂避障或敌人分离。
- EnemyConfig 独立数值资源和动态难度预算。
- DOTS/ECS、Spatial Hash、分帧大规模模拟。

---

## 10. 文档同步规则

修改以下代码时要同步检查对应文档：

| 代码变更 | 文档 |
| --- | --- |
| `EnemyDirector`、`EnemySpawner`、`EnemyChasing`、`VSEnemyHealth` | 本文件、`WaveSystem.md` |
| `WaveConfig` 或 Wave 调度规则 | `WaveSystem.md`、本文件 |
| `PoolManager` 的敌人池生命周期 | 本文件、`Survivor.md` |
| 掉落类型、死亡结算或拾取规则 | `Survivor.md`，必要时本文件 |

文档中的类型名、字段名和场景参数必须与当前代码一致；未来规划只能放在“当前边界/后续方向”中。
