# Vampire Survivors-like 模块

更新时间：2026-09-07

这是当前项目的类 Vampire Survivors 局内战斗模块。本文只记录当前代码和 `SurvivorsDemo` 场景能够确认的能力；后续规划以 [BACKLOG.md](../../../../BACKLOG.md) 为准，详细规则见 [Docs/Modules](../../../../Docs/Modules/)。

## 1. 模块边界与运行流程

模块负责一局战斗中的玩家、敌人、掉落、武器、升级、Wave 与结算流程。局内数据由 `SurvivorModel` 保存，业务修改由 `SurvivorProxy` 完成；场景组件不直接保存 UI 业务状态。

```text
EnemyDirector / EnemySpawner
    → EnemyChasing / VSEnemyHealth
    → DropItemManager（Gem / Coin，PoolManager）
    → SurvivorModule.UpdateExp / AddDropItem
    → SurvivorProxy（经验、等级、击杀、货币、生命）
    → SurvivorGameplayController
    → 三选一升级面板 / 主界面 / GameOver 面板

Hero
    → WeaponManager
    → WeaponController
    → PooledWeaponEffect（PoolManager）
    → VSEnemyHealth
```

`SurvivorGameplayController` 是局内流程编排点：经验溢出后逐次打开升级面板；升级和 GameOver 时暂停 `Time.timeScale`；重开场景前回收活跃敌人、掉落物及武器攻击对象，再重置 Model 与重载当前场景。

## 2. 目录与模块内框架大纲

```text
Vampire Survivors-like/
├── SurvivorModule.cs                    # BaseModule：注册 Proxy 与 Presenter
├── Model/                               # 模块运行时数据与数据修改入口
│   ├── SurvivorModel.cs                  # 一局生命、等级、经验、货币、游戏状态
│   └── SurvivorProxy.cs                  # BaseProxy：唯一的局内数据修改入口
├── Gameplay/                            # 局内流程编排和可复用玩法辅助
│   ├── SurvivorGameplayController.cs     # 升级、暂停、结算、重开流程编排
│   └── PointUtil.cs                      # 刷怪、扇形、圆形等二维坐标工具
├── PlayerAttributeSystem/               # 玩家属性枚举、修正结构、永久增量与计算规则
├── View/
│   ├── SurvivorMainPresenter.cs          # 主界面 Presenter
│   ├── SurvivorSkillSelectPanelPresenter.cs # 三选一 Presenter
│   └── SurvivorGameOverPresenter.cs      # GameOver Presenter
├── Entity/
│   ├── Hero.cs                           # 玩家移动、基础属性配置与最终属性消费
│   ├── EnemyDirector.cs                  # 敌人列表、刷怪、Wave、选敌、回收
│   ├── EnemySpawner.cs                   # EnemyDirector 的生成与预热协作类
│   ├── EnemyChasing.cs                   # 敌人追击与超距回收
│   ├── DropItem.cs                       # Gem/Coin 拾取及对象池状态
│   ├── DropItemManager.cs                # 掉落抽取、生成与回收
│   └── Health/
│       ├── VSPlayerHealth.cs             # 伤害上报和死亡通知
│       ├── VSEnemyHealth.cs              # 敌人生命、死亡与对象池重置
│       └── UI_HpBar.cs                   # 实体血条显示
├── WeaponSystem/
│   ├── WeaponManager.cs                  # 场景级武器槽位与控制器创建
│   ├── WeaponSO.cs / WeaponLevelData.cs  # 武器和等级配置
│   ├── WeaponController/                 # 各武器开火、选敌与等级运行时逻辑
│   └── Weapon/                           # 池化投射物、范围效果和 Saw 行为
├── UpgradeSystem/
│   ├── UpgradeManager.cs                 # 候选构建、过滤、随机抽取
│   └── UpgradeConfig.cs                  # NewWeapon / Weapon / Player 升级配置
├── WaveSystem/
│   ├── WaveTimelineConfig.cs             # 一局 Wave 顺序与持续时间配置
│   └── WaveSpawnConfig.cs                # 可复用 Wave 与敌人生成条目配置
├── Map/
│   ├── InfiniteGroundTilemap.cs          # 无限地表
│   └── SurvivorCameraFollow.cs           # 相机跟随
└── BuffSystem/                           # Buff 基础模型、Handler 与示例 SO
```

本模块的 Presenter 位于 `View/`。实际的 Unity View 位于项目公共目录 `Assets/Scripts/Define/UI/`：`SurvivorMainView`、`SurvivorSkillSelectPanelView` 和 `SurvivorGameOverView`。它们通过项目 UI 框架由对应 Presenter 打开和刷新，不在本目录重复实现 View 基类。

### 依赖的项目公共框架

| 公共能力                       | 本模块使用方式                                                                           |
| ------------------------------ | ---------------------------------------------------------------------------------------- |
| `BaseModule` / `ModuleManager` | `SurvivorModule` 注册 Proxy、Presenter，并持有流程 Controller。                          |
| `BaseProxy`                    | `SurvivorProxy` 持有并修改 `SurvivorModel`，不直接操作 UI。                              |
| `UIManager` / Presenter / View | 主 HUD、三选一、GameOver 按既有 Presenter 生命周期打开、隐藏和关闭。                     |
| `SingletonMono<T>`             | `EnemyDirector`、`DropItemManager`、`WeaponManager`、`UpgradeManager` 等场景级组件使用。 |
| `PoolManager` / `IPoolable`    | 敌人、掉落物、投射物和范围特效复用；不另建武器专用池。                                   |
| `DamageController`             | `VSPlayerHealth` 调用项目现有的伤害飘字能力；该能力不由本模块维护。                      |

## 3. 当前已实现

### 局内状态、UI 与结算

- `SurvivorModel` 保存当前/最大生命、等级、经验、待处理升级次数、击杀数、Gem 数、Coin 数和 `Playing`、`LevelUp`、`GameOver` 状态。
- Gem 经验支持溢出与连续升级：一次拾取跨多个等级时，升级面板会逐轮重新生成候选。
- 主 HUD 显示局内状态；三选一升级面板显示图标、标题和描述；GameOver 面板显示等级、击杀与 Coin。
- 玩家死亡时关闭升级面板、暂停游戏、打开结算面板；重开时恢复时间并重载当前场景。

### 玩家、敌人、掉落与 Wave

- `PlayerAttributeSystem` 统一管理 `MoveSpeed`、`PickupRadius`、`MaxHealth`、`TargetingRange` 的类型与计算规则；永久增量在 `SurvivorModel`，临时增量由 `BuffHandler` 提供。
- `Hero` 支持上下左右移动、武器索敌范围和拾取范围；它只保存 Inspector 基础值并消费最终属性，拾取范围由独立 `CircleCollider2D` 触发器驱动，并有 Scene Gizmos。
- 敌人通过 `EnemyDirector` 管理活跃列表，支持追击玩家、超出回收距离时入池、死亡后入池和击杀计数。
- `EnemySpawner` 使用玩家为中心的生成半径，在相机外生成；`EnemyDirector` 支持预热敌人 Prefab。
- `WaveTimelineConfig` 保存一局的有序 `WaveTimelineEntry`，`WaveSpawnConfig` 保存可复用的 `WaveSpawnEntry` 组合；未配置有效时间轴时不会刷怪。
- 敌人死亡通过 `DropItemManager` 按权重生成 Gem 或 Coin。Gem 增加经验，Coin 只增加本局货币；两者都使用对象池。

### 武器与对象池

- 当前配置的武器为 Saw、Arrow、Bulletb、BlueOval、Lightning、Fire；`WeaponManager` 在首次获得时动态创建对应 `WeaponController` 子节点，并受 `maxWeaponSlots` 限制。
- 弓箭、子弹、蓝色爆炸、闪电、火焰和 Saw 都经 `PooledWeaponEffect` 接入 `PoolManager`。命中、`duration` 超时及 GameOver 重开前都会回收活跃攻击对象；`OnDestroy` 只清理引用，不在 Unity 场景销毁阶段重设对象池父节点。
- 直线弓箭与子弹会优先瞄准本武器尚未被飞行投射物占用的敌人；范围内没有可用目标时本次不生成投射物，避免单敌场景重复浪费。
- 出池时会清理目标、方向、命中集合、计时与初始化状态；带 Animator 的效果会重置播放进度。

#### 武器类型与扩展入口

当前的“武器类型”按攻击行为划分，而不是按美术名称划分；新武器应优先复用已有 Controller 或池化效果，只有攻击生命周期不同才新增类型。

| 类型             | 当前武器                            | 实际行为                                                                             | 可复用的实现                                                    |
| ---------------- | ----------------------------------- | ------------------------------------------------------------------------------------ | --------------------------------------------------------------- |
| 环绕近战型       | `WeaponSaw`                         | 攻击对象挂在 Player 下绕行；`count` 决定数量，`range` 为环绕半径，`speed` 为角速度。 | `SawController` + `SawWeapon`                                   |
| 直线投射型       | `WeaponArrow`、`WeaponBulletb`      | 发射时锁定最近的可用敌人后直线飞行；多发会优先分散目标，命中或超时回池。             | `ArrowController` / `BulletbController` + `ArrowWeapon`         |
| 目标点瞬发范围型 | `WeaponBlueOval`、`WeaponLightning` | 在随机目标位置创建一次性范围效果；`count` 可选多个不同落点，伤害由动画事件结算。     | `BlueOvalController` / `LightningController` + `BlueOvalWeapon` |
| 目标点持续范围型 | `WeaponFire`                        | 在最近目标位置生成持续伤害区域；`damageInterval` 控制同区域内敌人的结算频率。        | `FireController` + `FireWeapon`                                 |

`ArrowWeapon` 本身还支持追踪模式（`Init` 的 `shouldFollowTarget = true`），但当前 Arrow/Bulletb 均按直线投射模式配置；它属于可复用能力，不是当前已配置的独立武器类型。

新增武器时，按以下最小链路接入：

1. 创建 `WeaponSO` 和对应攻击 Prefab，配置 `weaponId`、图标、等级数组与池化效果组件。
2. 能复用现有攻击行为时，在 `WeaponManager` 的 Inspector 引用和 `GetConfiguredWeapons()` 中加入该 SO，并在 `GetWeaponType()` 为其 `weaponId` 映射对应 Controller。
3. 需要新攻击行为时，新建继承 `WeaponController` 的开火/选敌逻辑，以及继承 `PooledWeaponEffect` 的攻击对象；实现出池初始化、命中或超时回收、`ResetEffectState()` 状态清理。
4. 在 `SurvivorsDemo/UpgradeManager` 配置 NewWeapon 与 WeaponUpgrade；新增或修改字段语义时，同步更新 `Docs/Modules/UpgradeSystem.md` 与本 README。

当前 `WeaponManager` 使用显式的 Inspector 字段与 `weaponId -> Controller` 映射，尚未实现自动注册；只创建 `WeaponSO` 不会自动进入升级候选或生成对应控制器。

### 三选一升级

- `UpgradeManager` 每轮筛选并随机提供最多 3 个可用候选，使用 `UpgradeId` 去重。
- 支持 `NewWeaponUpgradeConfig`、`WeaponUpgradeConfig` 和 `PlayerUpgradeConfig`。
- 武器等级运行时保存于 `WeaponController`，原始等级数据保存在 `WeaponSO.levels`，不会在战斗中写回 ScriptableObject。
- 默认运行时玩家升级候选包括移动速度、拾取范围和最大生命；自定义升级可在 Inspector 配置图标、名称、描述及数据。

### 地图与 Buff 基础能力

- 无限地表与相机跟随已经实现，均属于场景表现层。
- `BuffSystem` 已有 `BuffSO`、`BuffInstance`、`BuffHandler`、移动速度与索敌范围 Buff 的基础堆叠/刷新/替换逻辑；Hero 缺少组件时会在 Awake 补充 `BuffHandler`，Buff 通过 `PlayerStatModifier` 参与最终属性计算。

#### 已通过 Play Mode 验收

- SB-001 武器投射物与特效对象池。
- SB-002 核心玩法闭环（Wave、敌人/掉落、经验升级、GameOver 与连续重开）。
- SB-004 武器等级字段实际效果：`WeaponLevelData` 的 `count`、`range`、`speed`、`damageInterval`、`fireInterval` 与 `duration` 已按对应武器类型生效。

## 4. 未实现或尚未闭合

以下内容不能视为当前正式玩法能力：

- Buff 尚未接入三选一升级候选和正式构筑流程；当前只是可由场景组件使用的基础能力。
- SB-005 已建立首版玩家、武器、敌人、Wave 与掉落数值基线，仍待按 `BalanceSystem.md` 完成 Play Mode 数值验收。
- 被动道具、武器进化、合成、稀有度、刷新/跳过/禁用升级尚未实现。
- Coin 仅记录在本局 `SurvivorModel` 和结算面板中，没有局外持久化、商店或局外成长。
- Boss、Elite、特殊 Wave 事件、动态难度、Wave 奖励和胜利条件尚未实现。
- 正式的数值平衡、UI 多分辨率验收、音效/特效反馈、性能监控和自动化测试尚未完成。

## 5. 场景与资源配置入口

| 目标                             | 主要配置位置                                                                  |
| -------------------------------- | ----------------------------------------------------------------------------- |
| 敌人、生成距离、Wave、预热数量   | `SurvivorsDemo/EnemyDirector` Inspector。                                     |
| 掉落权重、掉落容器               | `SurvivorsDemo/DropItemManager` Inspector。                                   |
| 武器引用与最大槽位               | `SurvivorsDemo/WeaponManager` Inspector。                                     |
| 武器数值和等级数组               | `WeaponSystem/SO/*.asset` 的 `WeaponSO.levels`。                              |
| 自定义升级、默认属性升级图标     | `SurvivorsDemo/UpgradeManager` Inspector。                                    |
| 玩家初始武器、基础移动/索敌/拾取范围 | Player 的 `Hero` 组件；永久增量保存在 `SurvivorModel.PlayerAttributes`。       |
| 玩家与敌人生命、Collider、Tag    | Player / Enemy Prefab 上的 `VSPlayerHealth`、`VSEnemyHealth` 和 2D Collider。 |

不要在运行时修改 `WeaponSO`、`WaveSpawnConfig`、`WaveTimelineConfig` 等资源文件；局内等级、Buff 与属性增量应只保存在运行时实例中。

## 6. 文档导航与同步规则

- [Survivor.md](../../../../Docs/Modules/Survivor.md)：局内主流程、对象池、拾取、GameOver 和场景约定。
- [PlayModeAcceptance.md](../../../../Docs/Modules/PlayModeAcceptance.md)：SB-002 的场景基线、正式流程验收步骤和实机记录模板。
- [EnemySystem.md](../../../../Docs/Modules/EnemySystem.md)：敌人、掉落和对象池规则。
- [UpgradeSystem.md](../../../../Docs/Modules/UpgradeSystem.md)：升级配置、`UpgradeId` 和武器等级规则。
- [WaveSystem.md](../../../../Docs/Modules/WaveSystem.md)：Wave 配置与时间区间规则。
- [BalanceSystem.md](../../../../Docs/Modules/BalanceSystem.md)：SB-005 的玩家、武器、敌人、Wave 与掉落数值基线。
- [Architecture.md](../../../../Docs/Architecture.md)：项目级 Module、UI、网络和全局生命周期事实源。
- [BACKLOG.md](../../../../BACKLOG.md)：已经确认的后续工作和验收项。

后续代码修改时，应同步更新与职责相符的模块文档；如果修改了项目级 Manager、Module 生命周期、UI 主关系或跨模块数据流，还必须检查 `Docs/Architecture.md` 是否需要更新。
