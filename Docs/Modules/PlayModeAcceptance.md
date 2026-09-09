# Survivor 核心玩法闭环 Play Mode 验收

## 1. 目的与状态

本文记录 `BACKLOG.md` 中 SB-002 的 Unity Play Mode 验收步骤与实际证据。它不是自动化测试，也不能由代码静态检查替代。

当前状态：**已完成（Unity Play Mode 验收通过）**。

通过标准：所有必测项通过；Unity Console 没有新增 Error；每个失败项都已记录复现步骤、Console 信息和后续处理项。

## 2. 场景与基线配置

从 `Assets/Scenes/Launcher.unity` 进入并通过既有入口加载 `SurvivorsDemo`，以确保 `GameMgr` 已初始化 SurvivorModule 和 UI；不要直接以 `SurvivorsDemo` 作为唯一启动场景执行完整验收。场景内基线为：

| 对象              | 验收基线                                                              |
| ----------------- | --------------------------------------------------------------------- |
| `EnemyDirector`   | `maxEnemies=20`、`spawnRadius=10`、`despawnRadius=20`、3 个 Wave 配置 |
| `DropItemManager` | Gem 权重 `80`、Coin 权重 `10`、每种预热 `8` 个                        |
| `WeaponManager`   | `maxWeaponSlots=3`                                                    |
| Hero              | 场景覆盖 `basePickupRadius=0.3`                                       |

验收不使用专用 GameOver 测试开关。死亡、结算和重开必须在正式玩法配置下，由敌人对玩家造成伤害后自然触发；不得通过运行时代码修改 `WeaponSO`、Hero Prefab 或其他资源。

## 3. 验收步骤

### A. Wave、敌人与对象池

1. 以正常配置进入 Play Mode，记录 `EnemyDirector.GameTime` 在 0、60、120 秒附近的 `CurrentWaveNumber`，确认旧 Wave 停止新生成、场上旧敌人仍能继续追击。
2. 用临时复制的 Wave 资源构造重叠区间和空 `Spawn Entries` 区间，确认 Console 分别出现重叠 Warning，且空区间不生成新敌人。验证后撤销临时资源/场景修改。
3. 持续运行至敌人数达到 `maxEnemies`，确认不再增加；将玩家移离一批敌人超过 `despawnRadius`，确认它们从 `Enemies` 容器移除并由对象池复用。
4. 让敌人与玩家碰撞，确认玩家受伤、敌人回收；击杀敌人，确认敌人死亡后生成一份 Gem 或 Coin。

### B. 掉落、经验与升级

1. 拾取 Gem，确认宝石数量和经验增加，达到阈值后弹出升级面板。
2. 拾取 Coin，确认金币数量增加、经验和等级不变化。
3. 连续拾取足够 Gem 跨越多个等级，逐轮选择升级，确认每轮都重新抽取候选且最终恢复 `Playing`。
4. 在候选不足 3 个、候选失效和倒计时归零时，确认多余卡片隐藏、无效候选仅 Warning 并恢复游戏、倒计时使用未缩放时间自动选择有效项。
5. 升级面板显示时触发死亡，确认面板关闭且之后不会再触发其倒计时或回调。

### C. GameOver 与连续重开

1. 保持正式场景配置进入 Play Mode，让敌人多次碰撞玩家直至生命归零。
2. 确认时间冻结、GameOver 面板显示的等级/击杀/金币与主界面最后快照一致。
3. 点击“重新开始”，确认敌人、掉落物、武器攻击对象和 Wave 运行时计时全部重置，`Time.timeScale` 恢复为 1。
4. 重复步骤 1–3 至少三次，确认没有重复 Singleton、旧 UI、旧武器控制器或旧池对象残留；Console 不出现 Error。
5. 再次进入 Play Mode，确认正常武器和正式 5 点初始生命均按场景配置初始化。

## 4. 本次执行记录

| 日期     | Unity 版本    | 执行人 | Console Error | 结果    | 截图/说明 |
| -------- | ------------- | ------ | ------------- | ------- | --------- |
| 2026.9.7 | 2022.3.62f2c1 | ZPJ    | 无            | success | 无        |

> 本次验收已通过：正式玩法配置下的 Wave、敌人/掉落、经验升级、GameOver 与连续重开流程均正常，Unity Console 无 Error。
