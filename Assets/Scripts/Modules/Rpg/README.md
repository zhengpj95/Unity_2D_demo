# RPG 学习模块

该目录是 `Assets/Scenes/RPGDemo.unity` 对应的 2D RPG 原型代码，用于练习移动、近战、射击、敌人行为、生命 UI、掉落和高低层切换。它目前由场景 MonoBehaviour 和少量 Singleton 直接协作，**没有接入**项目的 `BaseModule` / Proxy / Command / Presenter 架构，也不在当前 Build Settings 中。

## 当前组成

| 目录/类型 | 当前职责 |
| --- | --- |
| `StatsManager` | 跨场景保存玩家/敌人基础数值；场景 Singleton |
| `Player/RpgPlayerMovement` | Rigidbody2D 八方向移动、朝向与击退 |
| `Player/RpgPlayerCombat` | 空格触发近战动画，动画事件调用 `DealDamage`/`FinishAttack` |
| `Player/PlayerShooter` | 鼠标朝向、移动和 Fire1 发射物的独立实验控制器 |
| `Player/RpgPlayerHealth` | 修改共享生命并通过 EventBus 刷新 UI/触发 GameOver |
| `Enemy/*` | 生成、追击、移动、近战伤害、击退与生命 |
| `Invertory_Shop/*` | `ItemSO` 与 `Loot` 的背包/掉落实验数据 |
| `ElevationEntry/Exit` | 2D 场景高低层切换触发器 |
| `UI/*` | 玩家生命和 GameOver 的场景 UI |

## 运行与输入

用 Unity 打开 `Assets/Scenes/RPGDemo.unity` 单独运行。具体生效的玩家控制器取决于场景对象绑定：

- `RpgPlayerMovement` 使用旧 Input Manager 的 `Horizontal` / `Vertical`。
- `RpgPlayerCombat` 使用 `Jump` 触发攻击。
- `PlayerShooter` 使用 `Horizontal` / `Vertical`、鼠标位置和 `Fire1`。

本模块同时安装了新 Input System 包，但这些脚本仍使用 `UnityEngine.Input`；不要仅因包已安装就改写输入链路。

## 当前边界与已知风险

- `StatsManager` 与 `EnemyManager` 是模块自己的 MonoBehaviour Singleton，不属于 `GameMgr` 管理的全局 Framework。
- `EnemyManager.Update` 当前每帧启动一个延迟 2 秒的协程，会累积大量生成检查；这是已知原型问题，不应作为正式刷怪实现参考。
- 射击对象使用 `Instantiate`，未接入 `PoolManager`。
- 多处组件依赖通过 `GetComponent` 和场景引用隐式获得，缺少启动期配置校验。
- 生命/数值保存在可变全局 `StatsManager` 中，UI 通过全局 EventBus 更新；不存在独立 Model 或存档边界。
- 目录名 `Invertory_Shop` 是现有拼写。若要更名必须连同 `.meta`、引用和序列化兼容一起处理，不能只改文件夹名。
- 当前没有自动化测试或正式 Play Mode 验收记录。

## 修改规则

小型学习实验优先保持当前场景组件风格。只有当 RPG 被确定为持续演进业务模块，并且存在清晰的跨场景状态、窗口或网络需求时，才规划迁入 Module/MVC；不要在一次 Bug 修复中混做全面架构迁移。

修改时至少检查 `RPGDemo` 中的组件引用、Animator 参数/动画事件、2D Layer/Collider、EventBus 订阅解绑和 Singleton 重复实例。若把场景加入 Build Settings 或改变项目级生命周期，再同步根 README 与 `Docs/Architecture.md`。
