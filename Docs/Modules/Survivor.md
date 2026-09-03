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
