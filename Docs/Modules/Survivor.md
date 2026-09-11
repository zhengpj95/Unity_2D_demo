# Survivor 模块

## 1. 当前职责

Survivor 模块负责 Vampire Survivors-like 的局外 Home、场景进入/返回，以及一局战斗中的运行时状态、升级流程和界面刷新。当前实现以代码为准：

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
Assets/Scripts/Modules/Vampire Survivors-like/Model/SurvivorModel.cs
Assets/Scripts/Modules/Vampire Survivors-like/Model/SurvivorProxy.cs
Assets/Scripts/Modules/Vampire Survivors-like/Gameplay/SurvivorGameplayController.cs
Assets/Scripts/Modules/Vampire Survivors-like/SurvivorModule.cs
Assets/Scripts/Modules/Vampire Survivors-like/View/SurvivorHomePresenter.cs
Assets/Scripts/Modules/Vampire Survivors-like/View/SurvivorMainPresenter.cs
Assets/Scripts/Modules/Vampire Survivors-like/View/SurvivorSkillSelectPanelPresenter.cs
Assets/Scripts/Modules/Vampire Survivors-like/View/SurvivorGameOverPresenter.cs
```

---

## 2. 运行时数据

`SurvivorModel` 保存一局战斗的数据，字段包括：

- `CurrentHealth`、`MaxHealth`
- `BaseMaxHealth`、`PlayerAttributes`
- `Level`、`CurrentExp`、`PendingLevelUpCount`
- `KillCount`
- `GemCount`、`CoinCount`
- `GameState`：`Playing`、`LevelUp`、`GameOver`

`SurvivorModel.DefaultMaxHealth` 定义一局默认初始生命；`PlayerAttributes` 保存本局永久属性修正。`SurvivorProxy` 持有并修改 Model，负责伤害结算以及全部永久玩家属性升级；`VSPlayerHealth` 只负责接收伤害并上报死亡，不保存或初始化运行时生命。当前经验需求公式为：

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

## 5. 主界面、GameOver 与场景流转

当前正式入口为：登录成功后先在 `Launcher.unity` 打开 `SurvivorHomePresenter`，而不是直接加载战斗场景。Home 位于常驻 `UILauncher/UIRoot` 的 `Main` 层；显示 Home 时没有加载 Player、EnemyDirector、WeaponManager 和 Wave，因此不需要用暂停状态维持局外界面。

```text
Launcher 登录成功
    ↓
打开 SurvivorHome，隐藏登录节点
    ↓ 点击 btnStart
重置本局 Model，异步加载 SurvivorsDemo
    ↓
打开 SurvivorMain，开始战斗
```

Home 只通过 `SurvivorHomeArgs.OnStartBattle` 将按钮输入交给 `SurvivorGameplayController`，不直接加载场景或修改 `Time.timeScale`。详细职责、异常恢复和 Play Mode 验收步骤见 [SurvivorSceneFlow.md](SurvivorSceneFlow.md)。

### GameOver 与重新开始

`VSPlayerHealth` 只负责扣减生命与上报死亡；当生命降至 `0` 时，它只向 `SurvivorModule` 上报一次，由 `SurvivorGameplayController.OnPlayerDied` 编排后续流程：

```text
玩家生命归零
    ↓
关闭仍打开的升级选择面板
    ↓
GameState = GameOver，Time.timeScale = 0
    ↓
打开 GameOver 结算窗口（等级、击杀、金币）
    ↓
玩家点击“重新开始”
    ↓
回收当前活跃敌人、掉落物与武器攻击对象
    ↓
SurvivorProxy.ResetRound + Time.timeScale = 1
    ↓
重载当前场景，重置玩家、武器、敌人、掉落和 Wave 运行时状态
```

当前结算窗口使用 `Resources/Prefabs/SurvivorGameOver`，由 `SurvivorGameOverPresenter` 通过 `SurvivorGameOverView` 绑定标题、结算信息与按钮；“重新开始”只通过回调请求 Controller 重开，不直接修改战斗状态或场景。

### GameOver 返回主页

结算窗口的原“退出”按钮已改为“返回主页”。Presenter 关闭自身后只触发 `SurvivorGameOverArgs.OnReturnHome`；Controller 隐藏局内 UI、回收敌人/掉落物/武器效果，异步加载 `Launcher.unity`，重置本局 Model 并重新打开 Home。场景加载期间使用切换标记防止重复请求，并保持 `Time.timeScale = 0`，加载完成后恢复为 `1`。

---

## 6. 玩家属性、实体与拾取范围

`PlayerAttributeSystem` 是独立的玩家属性基础能力，但不创建新的 Manager：

```text
Hero / SurvivorModel 基础值
    + SurvivorModel.PlayerAttributes 永久升级
    + BuffHandler 临时修正
    → (基础值 + 固定值) × (1 + 百分比)
    → Hero / WeaponController / SurvivorProxy 消费
```

当前统一属性：

- `MoveSpeed`：最终移动速度，最小为 `0`。
- `PickupRadius`：最终拾取触发器半径，最小为 `0.1`。
- `TargetingRange`：武器寻找敌人的基础距离，最小为 `0.1`；它不代表武器碰撞体大小。
- `MaxHealth`：基础生命和永久修正由 `SurvivorProxy` 计算并向上取整，提升上限时同步补充新增生命。

当前已有的临时 Buff 仅覆盖 `MoveSpeed` 和 `TargetingRange`。`PickupRadius` 与 `MaxHealth` 已接入统一永久升级数据，但对应的限时 Buff 及最大生命 Buff 到期时的当前生命处理规则尚未实现，不能把它们当作已闭合能力。

`Hero` 只保留 Inspector 基础配置，不再保存永久升级字段。为兼容现有场景，`baseAttackRange` 和 `AttackRange` 旧名称暂时保留，但内部都按 `TargetingRange` 解释。Hero 在 `Awake` 中确保自身存在 `BuffHandler` 和拾取 `CircleCollider2D`；拾取半径在运行时同步为 `PickupRadius`，所以圆心是玩家根节点中心而不是脚部 Sprite。

`Hero.OnDrawGizmosSelected`：

- 红色线框圆：`TargetingRange`
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

弓箭、子弹、蓝色爆炸、闪电、火焰与 Saw 都继承 `PooledWeaponEffect`，通过框架 `PoolManager.Alloc/Free` 复用，不在普通攻击路径中 `Instantiate/Destroy`。`WeaponController` 会登记本控制器创建的活跃效果：命中或 `WeaponLevelData.duration` 超时后由效果自身归还对象池；场景重开时，`SurvivorGameplayController` 会在 `LoadScene` 前通过 `ClearActiveWeaponEffects` 统一归还，避免池对象残留旧场景引用。`WeaponManager` 和 `WeaponController` 的 `OnDestroy` 只清理引用，不会在 Unity 销毁阶段重新设定效果父节点。

每次出池会清理上一轮的目标、方向、命中列表、伤害计时与初始化状态，并重置子 Animator 的播放进度。`PoolManager` 入池时将对象移到池根节点，控制器取出后再恢复当前武器控制器或 Player 的挂点，因此武器 Prefab 无需预先挂在场景层级中。

SB-004 已将 `WeaponLevelData` 接入实际玩法：`count` 决定一次触发创建的独立攻击对象数，`range` 对定向/范围武器作为 `Hero.TargetingRange` 的选敌倍率（Saw 保持为环绕半径），`speed` 作用于投射物与 Saw，`damageInterval` 仅作用于 Fire，`fireInterval` 控制触发节奏，`duration` 控制对象池回收时机。`level` 的运行时来源始终是 `WeaponSO.levels` 数组下标，字段本身只作 Inspector 标识。多发攻击会优先分散目标；候选不足时不为凑数量重复生成定向攻击对象。

普通子弹和弓箭均为直线投射物：对应 Controller 每次开火会先收集本武器仍在飞行投射物的发射目标，并优先从未被占用的范围内敌人中选择最近者。范围内所有敌人都已经被本武器瞄准时，本次不生成投射物，等目标死亡、投射物入池或出现新的可选敌人后再发射，避免单敌场景连续浪费弹药。敌人死亡后会被 `EnemyDirector` 跳过，投射物入池后则从活跃集合注销，下一次选敌不会受旧目标影响。

---

## 9. 当前限制

当前已落地：

- 一局状态、经验溢出和连续升级队列。
- Gem/Coin 分离结算。
- 对象池敌人、掉落物、武器投射物和范围特效。
- 武器投射物与特效对象池已完成 Play Mode 验收。
- 核心玩法闭环（Wave、敌人/掉落、经验升级、GameOver 与连续重开）已完成 Play Mode 验收。
- SB-004 武器等级字段实际效果已完成 Play Mode 验收。
- 可复用 `WaveSpawnConfig` 与顺序 `WaveTimelineConfig` 刷怪调度。
- NewWeapon、WeaponUpgrade、PlayerUpgrade 三类候选。
- 独立玩家属性基础系统，以及永久升级和临时 Buff 的统一计算入口。
- SB-005 首版数值平衡基线已完成 Play Mode 验收；详细参数表见 `BalanceSystem.md`。
- Launcher 登录后进入 SurvivorHome，点击开始进入战斗；GameOver 可重新开始或返回主页。

当前没有：

- 正式的局外结算数据持久化、角色选择与局外成长流程。
- 被动道具、武器进化、稀有度、刷新/跳过/禁用升级。
- 完整的 Buff 结算；部分技能进度代码仍是占位。
- 复杂敌人 AI、Boss、Elite 和特殊 Wave 事件。

---

## 10. 文档同步规则

后续修改代码时，按职责同步对应文档：

| 代码变更 | 需要同步的文档 |
| --- | --- |
| Model、经验、暂停、升级弹窗、GameOver 或重开流程 | `Survivor.md`、`UpgradeSystem.md` |
| 登录、Home、开始战斗、返回主页或场景切换 | `SurvivorSceneFlow.md`、`Survivor.md`，必要时同步 `Architecture.md` |
| EnemyDirector、EnemySpawner、EnemyChasing、掉落回收 | `EnemySystem.md`、`WaveSystem.md`，必要时同步本文件 |
| WaveSpawnConfig、WaveTimelineConfig、Wave 时间和 WaveSpawnEntry | `WaveSystem.md`、`EnemySystem.md` |
| UpgradeConfig、WeaponManager、WeaponSO 等级、武器投射物/特效对象池 | `UpgradeSystem.md`，必要时同步本文件 |
| 场景层级、相机、无限地表或拾取范围 | `Survivor.md` |

文档中的“当前实现”必须以仓库代码和场景为准；计划中的功能统一放在“当前限制/后续方向”，不能写成已完成。
