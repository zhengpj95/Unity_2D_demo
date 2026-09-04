# Survivor 模块

Survivor 模块是当前项目的 Vampire Survivors-like 玩法实现。运行时状态由 `SurvivorModel` 保存，并通过 `SurvivorProxy` 对外提供受控访问。

## 运行时数据

`SurvivorModel` 负责保存一局战斗内的状态：

- 当前生命、最大生命
- 当前经验、等级、下一级所需经验
- 击杀数
- 当前游戏状态
- 升级时待展示的技能选项

`SurvivorProxy` 是模块内访问运行时数据的入口。Presenter 只订阅 Proxy 的事件并刷新 UI；它不再直接保存战斗数据或修改 `Time.timeScale`。

## 战斗控制

`SurvivorGameplayController` 负责战斗状态切换：

- 开始战斗
- 进入升级选择
- 选择技能后恢复战斗
- 进入游戏结束

UI 的暂停、恢复和技能选择通过 Controller 协调，避免多个 Manager 同时修改游戏状态。

## 地图第一阶段：无限图片地表

Survivor 场景使用现有的 `vs-ground-seamless-2048-v1.png` 图片切片作为地表，而不是继续使用 Unity Tilemap。

- 场景根节点 `InfiniteGroundImage` 挂载 `InfiniteGroundTilemap`。
- 该组件会根据 Main Camera 周围的可见区域循环复用 4×4 地表 Sprite，不创建地图边界。
- 原有 `Grid` 对象已在场景中禁用，保留它只是为了方便回退；本阶段不删除。
- Main Camera 挂载 `SurvivorCameraFollow`，自动查找带 `Player` 标签的 Hero，并在 X/Y 两个方向平滑跟随。
- Hero 本身没有移动范围裁剪，因此玩家可以无限向任意方向移动。
- 地表不挂 Collider；后续树、岩石等障碍物应各自使用 `Collider2D`，保持全项目只使用 2D 物理。

地图显示与相机跟随属于场景表现层，不应放入 `SurvivorModel` 或 `SurvivorProxy`。

## 无限地图敌人生命周期

- `EnemyDirector` 作为生成节奏控制者，持有玩家引用并按固定间隔向 `EnemySpawner` 请求批量生成；首选在 Inspector 中绑定 Hero，仅为兼容旧场景在启动阶段按 Player 标签解析一次。
- `EnemySpawner` 根据 Player 的当前位置计算圆周出生点，从框架 `PoolManager` 取出敌人，并在每次复用时向 `EnemyChasing` 注入 Player 与回收入口。
- `EnemyChasing` 负责追击及自身距离检测；超出回收半径、死亡或碰撞玩家时都通过同一入口归还对象池，只有死亡回收会累计击杀并生成掉落。`EnemyChasing` 在池生命周期中重置刚体速度，`VSEnemyHealth` 在每次取出时恢复满血，避免复用上一轮状态。
- `DropItemManager` 在启动时预热 Gem/Coin，掉落物使用 `PoolManager` 取出与归还；`DropItem` 通过 `IPoolable` 重置拾取状态。Gem 会增加经验，Coin 只增加金币数量。

## 三选一升级系统

- UpgradeManager 在每次升级时重新生成候选池，先调用每个 UpgradeConfig.IsAvailable 过滤，再随机返回最多 3 个不重复选项；连续升级不会复用上一轮候选。
- 当前内置三类配置：NewWeaponUpgradeConfig、WeaponUpgradeConfig、PlayerUpgradeConfig。未在 Inspector 配置资源时，管理器会根据 WeaponManager 当前已有的武器配置和基础玩家属性自动生成默认候选。
- 候选 ID 使用 `UpgradeId` 枚举生成：武器候选会追加目标 `weaponId`，玩家属性候选会按 `PlayerUpgradeStat` 映射，配置资源不再暴露可重复填写的字符串 ID。
- 默认玩家属性候选是在场景 `UpgradeManager` 上运行时创建的，图标配置位于该组件的“默认玩家属性升级图标”三个字段；自定义 `PlayerUpgradeConfig` 资源则在其继承的 `Icon` 字段中配置。
- NewWeapon 只有在玩家未拥有该武器且仍有空余武器槽时可用；WeaponUpgrade 只有在已拥有且未达到 WeaponSO.levels 最大等级时可用；玩家属性升级当前支持移动速度、拾取范围和最大生命值。
- 升级面板只展示 UpgradeConfig 的标题、描述和图标，选择结果回传 SurvivorGameplayController，由配置应用到运行时对象，不修改 WeaponSO 或其他 ScriptableObject 的原始数据。

## 武器运行时层级

- `WeaponManager.AddWeapon` 中的 `weaponObj.transform.SetParent(transform)` 决定武器控制器挂在 `WeaponManager` 下，因此运行时会生成 `WeaponManager/WeaponArrow`、`WeaponManager/WeaponBulletb` 等节点；场景里不需要预先创建这些子节点。
- `ArrowController`、`BulletbController` 等控制器在 `Instantiate(..., transform)` 中把投射物挂到对应的武器控制器节点下，便于按武器分类查看和统一清理。环绕型 `SawController` 的投射物例外地挂在玩家节点下，以保持其跟随玩家的行为。
