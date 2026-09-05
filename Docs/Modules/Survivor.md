# Survivor 模块

## 1. 当前职责

Survivor 模块负责一局 Vampire Survivors-like 战斗中的运行时状态、升级流程和主界面刷新。当前实现以代码为准：

```text
SurvivorProxy
    ↓ 保存 SurvivorModel
SurvivorGameplayController
    ↓ 编排经验、升级和暂停
SurvivorModule
    ↓ 打开/刷新 Presenter
Presenter / View
    ↓ 只展示状态并回传输入
```

主要实现文件：

```text
Assets/Scripts/Modules/Vampire Survivors-like/SurvivorModel.cs
Assets/Scripts/Modules/Vampire Survivors-like/SurvivorProxy.cs
Assets/Scripts/Modules/Vampire Survivors-like/SurvivorGameplayController.cs
Assets/Scripts/Modules/Vampire Survivors-like/SurvivorModule.cs
Assets/Scripts/Modules/Vampire Survivors-like/SurvivorMainPresenter.cs
Assets/Scripts/Modules/Vampire Survivors-like/SurvivorSkillSelectPanelPresenter.cs
```

---

## 2. 运行时数据

`SurvivorModel` 保存一局战斗的数据，字段包括：

- `CurrentHealth`、`MaxHealth`
- `Level`、`CurrentExp`、`PendingLevelUpCount`
- `KillCount`
- `GemCount`、`CoinCount`
- `GameState`：`Playing`、`LevelUp`、`GameOver`

`SurvivorModel.DefaultMaxHealth` 定义一局默认初始生命；`SurvivorProxy` 持有并修改 Model，负责伤害结算和最大生命升级；`VSPlayerHealth` 只负责接收伤害并上报死亡，不保存或初始化运行时生命。当前经验需求公式为：

```text
RequiredExp(level) = 20 × level + 5 × level²
```

`AddExp` 会保留溢出经验；一次拾取如果跨过多个等级，会累加 `PendingLevelUpCount`，由 Controller 逐次处理。

Presenter 不直接保存战斗数据，也不直接修改 `SurvivorModel`。主界面由 `SurvivorMainPresenter.Refresh(model)` 使用快照刷新。

---

## 3. 经验、掉落与升级闭环

```text
Enemy 死亡
    ↓
DropItemManager 从 PoolManager 取出 Gem/Coin
    ↓
Player 的 PickupRadius 触发器拾取
    ↓
Gem：AddExperience(score)
Coin：只增加 CoinCount
    ↓
SurvivorGameplayController.OnExpCollected
    ↓
SurvivorProxy.AddExp
    ↓
OpenNextLevelUp
    ↓
三选一面板
```

当前实现中：

- `DropItem` 只响应带 `Player` 标签的触发器。
- Gem 才会调用 `AddExperience`；Coin 不会触发升级。
- 敌人死亡时由 `DropItemManager` 按场景权重抽取 Gem/Coin；`SurvivorsDemo` 当前为 Gem `80`、Coin `10`，每次击杀掉落一件，金币用于后续局外武器升级而保持稀缺。
- 掉落物通过 `PoolManager.Alloc/Free` 复用，不以 `Destroy` 作为普通拾取流程。
- `DropItem.OnAlloc/OnFree` 会重置已拾取状态。
- 掉落物没有额外的分数、刷怪加速或未落地技能进度；Gem 的 `score` 仅作为经验值使用。

---

## 4. 升级选择流程

`SurvivorGameplayController` 是升级、死亡和重开流程的唯一编排入口：

```text
OnExpCollected
    ↓
有待处理升级？
    ↓
TryConsumePendingLevelUp
    ↓
UpgradeManager.GetUpgradeOptions(3, context)
    ↓
设置 GameState = LevelUp、Time.timeScale = 0
    ↓
打开 SurvivorSkillSelectPanel
    ↓
玩家点击或 10 秒倒计时结束自动选择第一个选项
    ↓
再次校验 IsAvailable
    ↓
UpgradeConfig.Apply(context)
    ↓
仍有待处理升级则重新抽取，否则恢复 Playing
```

每一轮升级都会重新创建候选结果，不会提前缓存多轮选项。选择时会再次调用 `IsAvailable`，防止连续升级或外部状态变化导致应用失效候选。

升级弹窗使用 `Time.unscaledDeltaTime` 倒计时，因此暂停游戏后仍能在 10 秒结束时自动选择；Wave 和敌人使用的 `Time.deltaTime` 则会暂停。

Presenter 只负责显示图标、标题、描述和点击输入。隐藏弹窗时会清理候选数组与回调，避免下一轮沿用旧状态。

---

## 5. GameOver 与重开

`VSPlayerHealth` 只负责扣减生命与上报死亡；当生命降至 `0` 时，它只向 `SurvivorModule` 上报一次，由 `SurvivorGameplayController.OnPlayerDied` 编排后续流程：

```text
玩家生命归零
    ↓
关闭仍打开的升级选择面板
    ↓
GameState = GameOver，Time.timeScale = 0
    ↓
打开 GameOver 结算窗口（等级、击杀、宝石）
    ↓
玩家点击“重新开始”
    ↓
SurvivorProxy.ResetRound + Time.timeScale = 1
    ↓
重载当前场景，重置玩家、武器、敌人、掉落和 Wave 运行时状态
```

当前最小结算窗口复用通用 `AlertTipsPanel` Prefab，由 `SurvivorGameOverPresenter` 专门控制；它只显示结算数据并通过回调请求 Controller 重开，不直接修改战斗状态或场景。

### GameOver 测试开关

`SurvivorsDemo/EnemyDirector` 当前挂载 `SurvivorGameOverTestSetup`，用于人工快速验证该闭环：初始生命为 `1`，所有已配置武器伤害按 `0.25` 倍计算（当前初始 1 点伤害武器会变为 0），并禁用本局已创建的武器控制器以阻止发射。该组件会在运行时缓存并在禁用、删除或场景重载时恢复原始伤害和武器启用状态；它不会写入 `WeaponSO` 资源文件。测试完成后，在 Inspector 禁用或移除该组件并重载场景即可恢复正式数值。

---

## 6. 玩家实体与拾取范围

`Hero` 负责移动和本局玩家属性：

- `MoveSpeed`：基础值 + 升级平坦值，再乘百分比和 Buff 倍率。
- `AttackRange`：基础值 + 升级平坦值，再乘百分比和 Buff 范围增量。
- `PickupRadius`：基础值 + 升级平坦值，再乘百分比，最小为 `0.1`。

启动时 `Hero.Start` 会确保自身有一个 `CircleCollider2D`，设置为 `isTrigger = true`，半径同步为 `PickupRadius`。因此拾取范围以玩家根节点中心为圆心，不以脚步 Sprite 为中心；升级后在 `Update` 中同步半径。

`Hero.OnDrawGizmosSelected`：

- 红色线框圆：`AttackRange`
- 青色线框圆：`PickupRadius`

当前场景对 Hero Prefab 的 `basePickupRadius` 覆盖值为 `0.3`，实际效果仍应以运行时 Inspector 和 Gizmos 为准。

---

## 7. 无限地图与相机

### 地表

场景使用 `InfiniteGroundImage` 挂载的 `InfiniteGroundTilemap`，根据正交相机可见范围动态创建并复用 SpriteRenderer。默认源布局是 4×4，使用 `viewPadding` 扩展可见区域，不创建地图边界。

场景中的旧 `Grid` 当前禁用并保留作回退，不应把它当作运行时地表主实现。

### 相机

Main Camera 挂载 `SurvivorCameraFollow`：

- 优先使用 Inspector 目标。
- 目标为空时按 `Player` 标签解析。
- 在 `LateUpdate` 中跟随目标，可通过 `_smoothTime` 控制平滑。
- 不做地图范围裁剪，玩家可以在无限地表上移动。

地图显示和相机跟随属于场景表现层，不放入 `SurvivorModel` 或 `SurvivorProxy`。

---

## 8. 敌人与武器的关系

敌人生成和 Wave 调度由 `EnemyDirector` 负责，详细规则见 [EnemySystem.md](EnemySystem.md) 和 [WaveSystem.md](WaveSystem.md)。敌人实例和掉落物都通过框架 `PoolManager` 复用。

玩家武器由场景中的 `WeaponManager` 管理，详细升级规则见 [UpgradeSystem.md](UpgradeSystem.md)。

`WeaponManager.AddWeapon` 会在首次获得武器时动态创建控制器节点：

```text
WeaponManager
├── WeaponArrow
├── WeaponBulletb
├── WeaponSaw
└── ...
```

场景中不需要预先创建这些子节点。弓箭、子弹等投射物由对应控制器作为子物体创建；环绕型 Saw 直接挂在 Player 下以保持跟随。

---

## 9. 当前限制

当前已落地：

- 一局状态、经验溢出和连续升级队列。
- Gem/Coin 分离结算。
- 对象池敌人和掉落物。
- Wave 第一阶段和旧固定刷怪兼容模式。
- NewWeapon、WeaponUpgrade、PlayerUpgrade 三类候选。

当前没有：

- 独立美术样式的 GameOver Prefab、局外结算与局外成长流程。
- 被动道具、武器进化、稀有度、刷新/跳过/禁用升级。
- 完整的 Buff 结算；部分技能进度代码仍是占位。
- 复杂敌人 AI、Boss、Elite 和特殊 Wave 事件。

---

## 10. 文档同步规则

后续修改代码时，按职责同步对应文档：

| 代码变更 | 需要同步的文档 |
| --- | --- |
| Model、经验、暂停、升级弹窗、GameOver 或重开流程 | `Survivor.md`、`UpgradeSystem.md` |
| EnemyDirector、EnemySpawner、EnemyChasing、掉落回收 | `EnemySystem.md`、`WaveSystem.md`，必要时同步本文件 |
| WaveConfig、Wave 时间和 SpawnEntry | `WaveSystem.md`、`EnemySystem.md` |
| UpgradeConfig、WeaponManager、WeaponSO 等级 | `UpgradeSystem.md`，必要时同步本文件 |
| 场景层级、相机、无限地表或拾取范围 | `Survivor.md` |

文档中的“当前实现”必须以仓库代码和场景为准；计划中的功能统一放在“当前限制/后续方向”，不能写成已完成。
