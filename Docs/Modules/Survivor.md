# Survivor 模块设计

## 当前目标

Survivor 是一局独立的类幸存者玩法模块。当前优先完成：

```text
敌人死亡 → 获得经验 → 升级 → 选择技能 → 应用升级 → 继续战斗
```

## 开始前必读

修改 `Assets/Scripts/Modules/Vampire Survivors-like/` 前，必须阅读：

1. `AGENTS.md`
2. `Docs/Architecture.md`
3. 本文
4. `.codex/skills/unity-mvc-development/SKILL.md`
5. 目标类、`BaseModule`、`BaseProxy` 与真实调用方

## 职责边界

### SurvivorModel

保存一局 Survivor 的运行时数据，是模块的数据真源。它不继承 MonoBehaviour，不直接操作 UI 或网络。

当前数据包括血量、等级、经验、待处理升级次数、击杀数、宝石、金币和游戏状态。

### SurvivorProxy

持有并修改 `SurvivorModel`，负责一局数据初始化、数据修改和未来 Survivor 协议同步。Proxy 不直接操作 UI。

### SurvivorGameplayController

编排一局流程：经验结算、连续升级队列、暂停、技能选择结果和恢复战斗。它不保存 UI 数据。

### Presenter / View

Presenter 和 View 只负责展示与交互：

- `SurvivorMainPresenter` 展示 Model 的快照；
- `SurvivorSkillSelectPanelPresenter` 展示选项，并通过回调把选择结果交给 GameplayController；
- Presenter 不保存等级、经验、血量等玩法状态；
- Presenter 不直接调用 `WeaponManager`，也不直接修改 `Time.timeScale`。

## 数据流

```text
DropItemManager
    ↓
SurvivorModule
    ↓
SurvivorGameplayController
    ↓
SurvivorProxy
    ↓
SurvivorModel
    ↓
SurvivorMainPresenter / View
```

技能选择反向流转：

```text
SkillSelectPresenter
    ↓ 回调 WeaponSO
SurvivorGameplayController
    ↓
WeaponManager
```

当前仍保留旧的 `WeaponManager` 与 `DropItemManager` 场景组件；本次迁移只移除它们对 UI 和 UI 数据真源的职责，不重写其生成或武器创建逻辑。

## 后续演进

1. 将武器运行时等级和已拥有武器列表纳入 Model。
2. 由 `LevelUpOptionGenerator` 生成升级选项，替代 Prefab 中固定的武器数组。
3. 将敌人生成、武器创建等旧 Singleton 场景组件逐步收敛到 Survivor 模块边界。
