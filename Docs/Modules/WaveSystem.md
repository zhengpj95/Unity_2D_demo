# Wave System

## 1. 当前状态

WaveSystem 第一阶段已经接入 Survivor 敌人生成流程。实现目标是根据游戏运行时间切换不同的普通敌人生成规则，不包含 Boss、Elite 或特殊事件。

当前实现文件：

```text
Assets/Scripts/Modules/Vampire Survivors-like/Entity/WaveConfig.cs
Assets/Scripts/Modules/Vampire Survivors-like/Entity/EnemyDirector.cs
Assets/Scripts/Modules/Vampire Survivors-like/Entity/EnemySpawner.cs
```

项目中没有 `Docs/Systems/` 目录，本系统文档实际位于 `Docs/Modules/WaveSystem.md`。

当前场景 `SurvivorsDemo` 已经给 `EnemyDirector` 配置了 3 个示例 Wave。`EnemyDirector.waves` 为空时，系统会自动回退到原有固定频率刷怪逻辑，保证旧场景兼容。

---

## 2. 系统职责

```text
EnemyDirector
    ↓ 选择当前 Wave、驱动计时器
WaveConfig
    ↓ 提供当前时间段的静态配置
SpawnEntry
    ↓ 指定敌人 Prefab、间隔和批量数量
EnemySpawner
    ↓ 计算出生位置、从对象池取出实例
PoolManager
    ↓ 按 Prefab 复用实例
EnemyChasing
```

### EnemyDirector

负责：

- 累计游戏时间。
- 查找当前生效的 Wave。
- Wave 切换时重建 SpawnEntry 运行时状态。
- 驱动每个 SpawnEntry 的独立计时器。
- 限制场景中同时存活的敌人数量。
- 预热 Wave 使用的敌人 Prefab。

不负责：

- 计算具体出生位置。
- 创建或销毁敌人实例。
- 敌人移动、攻击和死亡逻辑。

### WaveConfig

描述一个时间区间内的静态刷怪规则。它是 `ScriptableObject`，运行时不会写入计时器或其他战斗状态。

### SpawnEntry

描述一种敌人在当前 Wave 中的生成方式。第一阶段直接引用现有敌人 Prefab，这是为了复用当前 `EnemySpawner` 和 `PoolManager`，没有新增第二套 `EnemyConfig` 或对象池系统。

### EnemySpawner

只负责：

1. 以 Player 为中心计算圆周出生位置。
2. 将 Wave 指定的 Prefab 交给 `PoolManager.Alloc`。
3. 设置父节点并向 `EnemyChasing` 注入 Player 和回收入口。

Wave 判断不能放入 EnemySpawner。

---

## 3. 配置数据

### 3.1 SpawnEntry

实际结构位于 `WaveConfig.cs`：

```csharp
[Serializable]
public sealed class SpawnEntry
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
    [SerializeField, Min(1)] private int spawnCount = 1;
}
```

字段含义：

| 字段 | 含义 |
| --- | --- |
| `enemyPrefab` | 本条目生成的敌人 Prefab，必须挂载 `EnemyChasing` |
| `spawnInterval` | 两次生成触发之间的间隔，单位为秒 |
| `spawnCount` | 每次触发生成的数量 |

`SpawnEntry.IsValid` 会检查 Prefab 不为空、间隔大于 0、数量大于 0。无效条目会输出 Warning 并跳过。

第一阶段没有 `StartDelay`、`MaxAlive`、`SpawnWeight`、`SpawnPattern` 等字段。

### 3.2 WaveConfig

实际结构：

```csharp
[CreateAssetMenu(fileName = "WaveConfig", menuName = "Survivor/Wave/Wave Config")]
public sealed class WaveConfig : ScriptableObject
{
    [SerializeField] private float startTime;
    [SerializeField] private float endTime = 60f;
    [SerializeField] private List<SpawnEntry> spawnEntries;
}
```

Wave 时间区间采用左闭右开：

```text
startTime <= gameTime < endTime
```

这样相邻 Wave 在边界时间不会同时生效。`endTime <= startTime` 时会在 Inspector 校验或运行初始化时输出 Warning。

---

## 4. Wave 运行流程

### 4.1 初始化

`EnemyDirector.Awake` 执行：

```text
解析 Player
↓
创建 EnemySpawner
↓
复制并按 StartTime 排序 Wave 引用
↓
检查时间倒置和区间重叠
↓
根据 waves 是否为空决定 Wave 模式或兼容模式
```

排序只作用于运行时列表，不修改 Inspector 中的 Wave 数组顺序。

### 4.2 Wave 模式

```text
EnemyDirector.Update()
↓
gameTime += Time.deltaTime
↓
查找 StartTime <= gameTime < EndTime 的 Wave
↓
如果 Wave 变化，重建 SpawnEntryRuntime
↓
分别推进每个 SpawnEntry 的 Timer
↓
达到 SpawnInterval 后调用 EnemySpawner
```

每个条目都有独立计时器，因此同一 Wave 中不同敌人可以使用不同的生成间隔。

进入新 Wave 时，计时器初始化为该条目的 `spawnInterval`，允许下一帧立即生成一次；之后每次触发使用：

```csharp
timer -= spawnInterval;
```

不会使用 `while` 在低帧率时一次性补刷大量敌人。计时器不会写回 `WaveConfig` 或 `SpawnEntry` 资源。

### 4.3 Wave 切换

切换时：

- 清除旧 Wave 的运行时计时器。
- 创建新 Wave 的运行时条目。
- 停止旧 Wave 的新敌人生成。
- 不清除场上已经存在的敌人。
- 已存在敌人继续追击、战斗或按距离回收。

如果当前时间不处于任何 Wave，系统暂时不生成新敌人，但场上已有敌人不受影响。

---

## 5. 对象池接入

第一阶段不新增 `EnemyPoolManager`。现有 `PoolManager` 已经按 Prefab 的 InstanceID 区分对象池：

```text
SpawnEntry.enemyPrefab
↓
PoolManager.Preload(prefab, count)
↓
PoolManager.Alloc(prefab, position, rotation)
↓
EnemyChasing.Initialize(player, director)
```

Wave 模式启动时，`EnemyDirector` 会收集所有有效条目中的 Prefab，相同 Prefab 只预热一次。

敌人死亡、碰撞玩家或距离玩家超过 `despawnRadius` 时，仍然通过现有 `EnemyDirector.RecycleEnemy` 和 `PoolManager.Free` 回收，不使用 `Destroy` 作为普通生命周期。

---

## 6. EnemySpawner 接口

旧版固定刷怪接口继续保留：

```csharp
EnemyChasing Spawn(Transform player, float spawnRadius, EnemyDirector director);
```

它会从 `EnemyDirector.enemyPrefab` 列表随机选择 Prefab。

Wave 模式使用新增重载：

```csharp
EnemyChasing Spawn(
    Transform player,
    float spawnRadius,
    EnemyDirector director,
    GameObject prefab);
```

该重载仍然使用同一套出生位置计算、对象池获取、父节点设置和 `EnemyChasing.Initialize` 流程。

---

## 7. EnemyDirector 配置

场景位置：

```text
Assets/Scenes/Vampire Survivors-like/SurvivorsDemo.unity
└── EnemyDirector
    └── Waves
```

主要字段：

| 字段 | Wave 模式用途 |
| --- | --- |
| `waves` | WaveConfig 资源数组 |
| `maxEnemies` | 场景中同时存活的敌人上限 |
| `enemyContainer` | 运行时敌人父节点 |
| `player` | 生成中心和敌人追击目标；为空时按 Player 标签解析一次 |
| `spawnRadius` | 敌人相对玩家的出生半径 |
| `despawnRadius` | 敌人离开玩家后的回收半径 |
| `preloadCountPerPrefab` | 每种 Prefab 的预热数量 |

当 `waves` 不为空时，旧字段 `spawnInterval` 和 `spawnCount` 不再控制生成节奏，但仍保留在组件中用于兼容旧场景。

---

## 8. 示例配置

示例资源位于：

```text
Assets/Configs/Survivor/
├── Wave_01_Intro.asset
├── Wave_02_Threat.asset
└── Wave_03_Heavy.asset
```

当前示例：

| Wave | 时间区间 | SpawnEntry |
| --- | --- | --- |
| Wave 1 | `0 ~ 60` | `Enemy_Slime`，间隔 `1.0` 秒，数量 `1` |
| Wave 2 | `60 ~ 120` | `Enemy_Slime`，间隔 `0.8` 秒，数量 `1`；`Enemy_Rino`，间隔 `1.5` 秒，数量 `1` |
| Wave 3 | `120 ~ 999999` | `Enemy_Slime`，间隔 `0.6` 秒，数量 `2`；`Enemy_Treant`，间隔 `1.2` 秒，数量 `1`；`Enemy_Rino`，间隔 `2.0` 秒，数量 `1` |

创建新 Wave：

```text
Project 面板右键
→ Create
→ Survivor
→ Wave
→ Wave Config
```

在 `Start Time`、`End Time` 和 `Spawn Entries` 中配置 Prefab、间隔和数量，然后将资源拖入场景 `EnemyDirector` 的 `Waves` 数组。

---

## 9. 与其他系统的关系

WaveSystem 只负责产生敌人，不直接调用 UpgradeSystem：

```text
GameTime
↓
Wave 生成敌人
↓
EnemyChasing 战斗
↓
Enemy 死亡
↓
掉落经验
↓
Player Level Up
↓
UpgradeSystem
```

升级面板将游戏暂停时，`Time.deltaTime` 为 0，Wave 的 `gameTime` 和 SpawnEntry 计时器也会暂停，不会在暂停期间生成敌人。

---

## 10. 当前未实现与后续方向

当前不包含：

- `EnemyConfig` 独立数值资源。
- Boss Wave、Elite、稀有敌人。
- Spawn Weight、Enemy Budget、动态难度。
- Spawn Pattern、Formation、地图区域刷怪。
- Wave Clear Condition、Wave Reward。

如果后续需要让 Wave 同时配置敌人的移动速度、生命值和掉落规则，可以在不改变 `EnemyDirector` 调度职责的前提下，将 `SpawnEntry.enemyPrefab` 增量替换为 `EnemyConfig`；对象池仍应复用现有 `PoolManager`，不要重复创建第二套池系统。

---

## 11. 验收清单

打开 `SurvivorsDemo` 运行后检查：

1. `0 ~ 60` 秒只生成 Slime。
2. 到达 `60` 秒后，Slime 和 Rino 按各自间隔生成。
3. 到达 `120` 秒后，Slime、Treant、Rino 按 Wave 3 规则生成。
4. Wave 切换不会删除场上已有敌人。
5. 场上敌人数量不会超过 `maxEnemies`。
6. 敌人仍从 `PoolManager` 取出并回收。
7. 将场景中 `EnemyDirector` 的 `Waves` 数组清空后，旧版固定频率刷怪仍能工作。
8. 在升级选择暂停期间，Wave 时间不会推进。
9. 配置重叠或非法时间区间时 Console 出现 Warning。

本地已完成 `dotnet build Assembly-CSharp.csproj --no-restore`，0 警告、0 错误；Unity Editor 内的实际运行验证仍需在编辑器中进行。
