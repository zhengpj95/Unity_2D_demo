# Survivor 项目 Backlog

更新时间：2026-09-07

本文基于当前仓库代码、`SurvivorsDemo` 场景以及 `Docs/Modules` 下的模块文档整理。优先级含义：

- **P0**：影响当前核心闭环、性能或重开稳定性，下一阶段优先处理。
- **P1**：完善当前玩法和可调试性。
- **P2**：扩展内容和长期成长。
- **P3**：工程化、自动化和发布准备。

## 当前已完成基线

- [x] 玩家移动、相机跟随和无限地表。
- [x] 敌人生成、追击、受伤、死亡和对象池回收。
- [x] 第一阶段 Wave 配置与固定频率刷怪兼容模式。
- [x] 敌人死亡掉落 Gem/Coin，掉落物使用对象池。
- [x] Gem 经验、Coin 货币分离结算。
- [x] 经验溢出和连续升级队列。
- [x] NewWeapon、WeaponUpgrade、PlayerUpgrade 三类三选一候选。
- [x] `UpgradeId` 枚举去重和 `WeaponSO.levels` 武器等级。
- [x] GameOver 结算窗口、暂停、回收当前实体和场景重开。
- [x] 拾取范围运行时触发器和 Scene Gizmos。
- [x] 武器投射物与范围特效使用通用对象池回收。

---

## P0：下一阶段优先实现

### SB-001 武器投射物与特效对象池

**状态：已完成（Play Mode 验收通过）**  |  **依赖：无**

`ArrowController`、`BulletbController`、`BlueOvalController`、`LightningController`、`FireController`、`SawController` 已改为经 `PoolManager.Alloc/Free` 复用攻击对象。没有新建第二套武器池；池对象的生命周期统一由 `PooledWeaponEffect` 管理。

本次实现：

- 为弓箭、子弹、闪电、火焰、蓝色爆炸和 Saw 增加 `IPoolable` 重置逻辑。
- 回收时清理目标引用、飞行方向、命中列表、计时器与初始化标记；带 Animator 的范围特效会重新绑定动画状态。
- 保留当前不同武器的挂点规则：投射物挂在对应 WeaponController，Saw 挂在 Player。
- 命中、`duration` 超时和场景重开都走同一个回收入口。
- `WeaponController` 登记本控制器创建的活跃效果；`WeaponManager` 在销毁或重开场景前统一归还它们。
- 直线子弹和弓箭优先选择未被同武器飞行投射物瞄准的范围内敌人；全部目标已被瞄准时本次不发射，避免单敌连续浪费投射物。

验收结论：已在 Unity Play Mode 正常运行并完成验收。

验收项：

1. [x] 长时间发射时 Hierarchy 和 Profiler 不再持续产生投射物实例。
2. [x] 投射物重复取出后不会继承上一次目标、方向、命中列表或状态。
3. [x] 场景重开前可安全回收武器产生的活跃对象。
4. [x] `PoolManager` 中不存在第二套武器专用池管理器。

### SB-002 核心玩法闭环 Play Mode 验收

**状态：已完成（Play Mode 验收通过）**  |  **依赖：SB-001 已完成**

使用 `SurvivorsDemo` 建立一组可重复的人工验收记录：

- Wave `0/60/120` 秒切换、重叠配置和空 Wave。
- 敌人最大数量、超距回收、碰撞玩家和死亡掉落。
- Gem 升级、Coin 不升级、一次拾取跨多个等级。
- 升级面板倒计时、候选不足 3 个、候选失效和死亡时关闭面板。
- GameOver 期间时间冻结、结算数据正确、重新开始后敌人/掉落/武器/Wave 全部重置。
- 连续重开多次后不出现重复单例、旧 UI、旧武器控制器或旧对象池实体。

验收步骤与记录模板：`Docs/Modules/PlayModeAcceptance.md`。验收产物应记录 Unity Console、关键 Inspector 配置和必要的运行截图；没有 Editor 运行记录前，不把“流程已闭合”视为最终验收完成。

验收结论：已在 Unity Play Mode 完成 Wave、敌人/掉落、经验升级、GameOver 与连续重开的核心闭环验收，Console 无 Error。


## P1：完善当前玩法

### SB-004 统一武器等级字段的实际效果

**状态：待实现**  |  **依赖：SB-001**

`WeaponLevelData` 已包含 `speed`、`damage`、`damageInterval`、`count`、`range`、`fireInterval`、`duration`，但各武器控制器并未完整消费所有字段。逐武器确认并补齐：

- `count`：多发射物或多目标数量。
- `speed`：投射物速度或环绕物转速。
- `range`：攻击范围/环绕半径。
- `damageInterval`：范围伤害间隔。
- `fireInterval`：武器触发间隔。
- `duration`：特效和投射物的有效时间。

验收标准：每个可配置字段在 Inspector 改动后都有可观察的运行时效果；没有被使用的字段要删除或在文档中明确保留原因。

### SB-005 武器攻击与玩家属性平衡

**状态：待实现**  |  **依赖：SB-004**

建立一份可调参数表，统一检查玩家攻击范围、武器伤害、敌人生命、敌人速度、Wave 刷怪速度和掉落权重。重点确认：

- 初始武器在前 60 秒可以稳定击杀敌人。
- 玩家升级不会因 `maxWeaponSlots` 或满级武器导致长期无有效候选。
- Coin 保持稀缺但能在一局内稳定产生。
- GameOver 测试参数不会混入正式平衡。

### SB-006 UI 自适应与视觉比例校准

**状态：待实现**  |  **依赖：SB-002**

当前 UI 以固定参考分辨率 `720×1280` 为基础，需要在实际窗口和目标比例下验收：

- CanvasScaler 的参考分辨率、Match 参数和安全区域。
- 血条、经验条、金币/宝石计数、三选一和 GameOver 面板的边距与缩放。
- 玩家、敌人、掉落物与相机 Size 的相对视觉比例。
- 不通过整体放大 Canvas 修复单个组件尺寸问题；优先调整 Prefab 组件和布局约束。

### SB-007 敌人和掉落物运行时监控

**状态：待实现**  |  **依赖：SB-002**

增加开发期可开关的统计信息：活跃敌人、活跃掉落物、各对象池数量、当前 Wave、GameTime、玩家等级和武器等级。统计不能在正式高频路径中产生不必要的 GC。

---

## P2：玩法内容扩展

### SB-008 Wave 难度和事件扩展

**状态：规划**  |  **依赖：SB-005**

在第一阶段 Wave 稳定后再增加：

- `EnemyConfig` 或等价的敌人属性资源。
- 敌人预算、动态难度、Spawn Weight。
- Elite/Boss Wave、特殊出生规则和 Wave 奖励。
- Wave 完成条件和胜利状态。

继续复用现有 `PoolManager`，不要创建第二套敌人池系统。

### SB-009 Coin 局外成长与持久化

**状态：规划**  |  **依赖：SB-002**

当前 Coin 只存在于本局 `SurvivorModel` 并显示在 GameOver 结算中。后续实现：

- 局内 Coin 结算到局外数据的边界。
- 简单的本地持久化。
- 局外武器/被动升级入口和消费规则。
- 新一局读取局外成长，但不污染 `WeaponSO` 原始资源。

### SB-010 被动升级、武器进化和构筑系统

**状态：规划**  |  **依赖：SB-004、SB-009**

按小步增量实现被动属性、武器进化、合成条件、稀有度和构筑协同；每个新升级类型都必须复用 `UpgradeConfig`、`UpgradeId` 和 `PlayerUpgradeContext` 的扩展规则。

### SB-011 音效、特效和反馈

**状态：规划**  |  **依赖：SB-002**

补充受击、死亡、拾取、升级、Wave 切换、GameOver 和重开的反馈，并验证暂停、对象池复用和场景重载不会残留音效或特效。

---

## P3：工程化和发布准备

### SB-012 Survivor 自动化测试

**状态：规划**  |  **依赖：SB-002**

优先为不依赖 Unity 场景的逻辑增加 Edit Mode 测试：

- 经验升级公式和溢出队列。
- `UpgradeId` 去重和候选过滤。
- Wave 左闭右开区间、重叠警告和兼容模式。
- 对象池回收后的状态重置。

Play Mode 测试再覆盖 UI、碰撞、场景重开和 Time.timeScale。

### SB-013 场景和资源配置校验

**状态：规划**  |  **依赖：SB-002**

增加 Editor 校验或启动期检查：

- Wave 的时间区间、敌人 Prefab 和 `EnemyChasing` 组件。
- WeaponSO 的 `weaponId`、Prefab、等级数组和控制器映射。
- Upgrade 图标、升级资源和 GameOver Prefab 引用。
- Player/Enemy 标签、Collider2D、Rigidbody2D 和对象池组件。

### SB-014 性能基线与构建验证

**状态：规划**  |  **依赖：SB-001、SB-007**

记录目标设备上的敌人数量、投射物数量、GC Alloc、对象池命中率和长时间运行稳定性；形成可重复的 Development Build 验证流程。

---

## 推荐执行顺序

```text
SB-004 武器等级字段落地
    ↓
SB-005 数值平衡
    ↓
SB-006 UI 与视觉比例
    ↓
SB-007 运行时监控
    ↓
SB-008 Wave 扩展 / SB-009 局外 Coin / SB-011 反馈
    ↓
SB-012～SB-014 工程化与发布
```

SB-001 与 SB-002 已完成 Play Mode 验收。下一步执行 **SB-004 武器等级字段的实际效果统一**，使各武器完整消费 `WeaponLevelData` 中的等级字段。

## 变更同步规则

每完成一个 backlog 条目，必须同时更新：

1. 对应的 `Docs/Modules/*.md`，把条目标记为已实现或调整为新的边界。
2. 如果改变 Module、Manager、Presenter、对象池或跨模块调用关系，检查 `Docs/Architecture.md`。
3. 如果新增序列化字段、Prefab、Scene 或 ScriptableObject，补充 Inspector 路径、默认值和验收步骤。
4. 代码中的新增公共 API、关键状态转换和对象池生命周期必须补充中文备注。

Backlog 只记录已经从代码/文档确认过的下一步，不把聊天中的设想直接写成已实现功能。
