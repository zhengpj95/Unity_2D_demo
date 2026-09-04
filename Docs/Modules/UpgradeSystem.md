# Upgrade System

## 1. 文档目的

本文档定义 Survivor 项目的升级三选一系统、武器成长规则以及 Upgrade 候选生成逻辑。

当前系统已经支持：

- Enemy 击杀。
- 经验掉落。
- 经验拾取。
- Player Level Up。
- Level Up 后出现三选一。
- 玩家可以获得武器。
- 再次选择已有武器时可以提升武器能力。

当前阶段需要将原有“固定三把武器”的升级逻辑改造成可扩展的 Upgrade System。

---

## 2. 当前目标

将：

```text
LevelUp
→ 固定显示 3 把武器
→ 玩家选择
→ 新武器 / 已有武器强化
```

升级为：

```text
LevelUp
↓
UpgradeManager
↓
根据 Player 当前状态生成合法候选池
↓
随机抽取最多 3 个 Upgrade
↓
LevelUpPanel 展示
↓
Player Select
↓
Upgrade Apply
```

Upgrade 不再等同于“武器”。

当前第一阶段支持：

```text
NewWeapon
WeaponUpgrade
PlayerUpgrade
```

---

## 3. Upgrade 类型

### 3.1 NewWeapon

用于获得 Player 当前尚未拥有的武器。

例如：

```text
获得 Saw
获得 BlueOrb
获得 FireBall
```

出现条件：

```text
Player 没有该 Weapon
AND
Player 还有空余 Weapon Slot
```

如果 Player 已经拥有该 Weapon：

```text
NewWeapon 不再进入候选池
```

---

### 3.2 WeaponUpgrade

用于升级 Player 已经拥有的 Weapon。

例如：

```text
Saw Lv2
Damage +20%

Saw Lv3
Cooldown -15%

Saw Lv4
Projectile Count +1

Saw Lv5
Size +20%
```

出现条件：

```text
Player 已拥有该 Weapon
AND
Weapon 尚未达到 MaxLevel
```

如果 Weapon 已满级：

```text
该 Weapon 的 WeaponUpgrade 不再进入候选池
```

---

### 3.3 PlayerUpgrade

用于提升 Player 基础属性。

第一阶段可以支持：

```text
MoveSpeed
PickupRadius
MaxHP
```

后续可以继续扩展：

```text
Armor
Luck
Cooldown
Area
Amount
Recovery
Damage
Duration
```

---

## 4. Upgrade 候选生成流程

每次 Level Up 都必须重新计算候选池。

流程：

```text
All UpgradeConfig
↓
IsAvailable(context)
↓
过滤非法 Upgrade
↓
Candidate Pool
↓
Random
↓
最多选择 3 个不同 Upgrade
↓
LevelUpPanel
```

例如：

```text
Player 没有 Saw
→ NewWeapon(Saw) 可用
→ WeaponUpgrade(Saw) 不可用

Player 已有 Saw Lv2
→ NewWeapon(Saw) 不可用
→ WeaponUpgrade(Saw Lv3) 可用

Saw MaxLevel
→ Saw 相关 Upgrade 全部不可用
```

---

## 5. UpgradeConfig

推荐使用 `ScriptableObject` 描述 Upgrade 配置。

基础结构可以类似：

```csharp
public abstract class UpgradeConfig : ScriptableObject
{
    public string id;
    public string title;
    public string description;

    public abstract bool IsAvailable(PlayerUpgradeContext context);

    public abstract void Apply(PlayerUpgradeContext context);
}
```

如果当前项目更适合数据驱动方案，也可以使用：

```text
UpgradeConfig
+
UpgradeType
+
UpgradeExecutor / Apply Dispatcher
```

项目实现应优先遵循现有工程风格。

不要为了升级系统大规模重构整个项目。

---

## 6. PlayerUpgradeContext

为了避免 UpgradeConfig 在运行时主动查找各种 Manager，可以提供统一 Context。

例如：

```csharp
public class PlayerUpgradeContext
{
    public WeaponManager WeaponManager { get; }
    public PlayerStats PlayerStats { get; }
}
```

Upgrade 通过 Context 获取需要的运行时能力。

禁止在 Upgrade 中频繁使用：

```csharp
GameObject.Find(...)
FindObjectOfType(...)
```

---

## 7. Weapon Level System

当前武器升级逻辑不应该继续使用统一规则：

```text
再次选中 Weapon
→ Damage +
→ AttackSpeed +
```

每把武器应该拥有独立成长路线。

例如：

```text
Saw

Lv1
基础能力

Lv2
Damage +20%

Lv3
Cooldown -15%

Lv4
Projectile Count +1

Lv5
Damage +30%

Lv6
Size +20%

Lv7
Projectile Count +1

Lv8
Max Level
```

不同 Weapon 可以拥有不同的成长方向。

例如：

```text
Saw
→ Damage / Count

BlueOrb
→ Size / Duration

FireBall
→ Damage / Explosion Radius
```

---

## 8. WeaponLevelData

每把 Weapon 应该有独立等级配置。

示例：

```csharp
[Serializable]
public class WeaponLevelData
{
    public float damageMultiplier = 1f;
    public float cooldownMultiplier = 1f;

    public int additionalProjectileCount;

    public float sizeMultiplier = 1f;

    public string description;
}
```

具体字段应该根据当前 Weapon 参数结构调整。

WeaponConfig 示例：

```csharp
public class WeaponConfig : ScriptableObject
{
    public List<WeaponLevelData> levels;
}
```

Weapon Runtime 状态至少需要：

```text
WeaponId
CurrentLevel
MaxLevel
```

---

## 9. Weapon Config 与 Runtime Data 分离

`ScriptableObject` 只保存基础配置。

运行时不要修改 WeaponConfig。

错误示例：

```csharp
weaponConfig.damage += 10;
```

推荐：

```text
WeaponConfig
基础只读数据
↓
WeaponRuntimeData
运行时属性
↓
Weapon
战斗逻辑
```

例如：

```text
Base Damage = 10
Lv2 Damage Multiplier = 1.2
Runtime Damage = 12
```

这样可以避免：

- Play Mode 中配置被污染。
- 多个 Weapon 实例共享错误状态。
- 下一局游戏继承上一局数据。
- Upgrade 重复叠加产生不可控状态。

---

## 10. UpgradeManager

UpgradeManager 负责：

- 管理所有 UpgradeConfig。
- 根据当前 Player 状态过滤 Upgrade。
- 生成 Candidate Pool。
- 随机抽取 UpgradeOption。
- 应用玩家选择的 Upgrade。

建议接口：

```csharp
List<UpgradeConfig> GetUpgradeOptions(int count);
```

流程：

```text
GetUpgradeOptions(3)
↓
遍历 All UpgradeConfig
↓
IsAvailable(context)
↓
Candidate Pool
↓
Random
↓
最多返回 3 个
```

规则：

- 同一次三选一不能出现完全相同的 Upgrade。
- 如果合法候选不足 3 个，则返回实际数量。
- 不应该通过展示非法选项来强行凑满 3 个。

---

## 11. LevelUpPanel

LevelUpPanel 只负责 UI 展示与输入。

负责：

```text
显示 Icon
显示 Title
显示 Description
处理按钮点击
```

不负责：

```text
判断是不是新武器
判断 Weapon 是否满级
判断 Weapon Slot
直接修改 Weapon 属性
直接修改 PlayerStats
```

点击流程：

```text
LevelUpPanel
↓
UpgradeManager.Apply()
↓
UpgradeConfig.Apply()
↓
WeaponManager / PlayerStats
```

---

## 12. 核心规则

Upgrade System 必须满足以下规则。

### NewWeapon

```text
没有 Weapon
+
还有 Weapon Slot
→ 可以出现
```

### WeaponUpgrade

```text
已有 Weapon
+
未 MaxLevel
→ 可以出现
```

### MaxLevel

```text
Weapon MaxLevel
→ 不再出现对应 Upgrade
```

### Weapon Slot

```text
Weapon Slot 已满
→ 所有 NewWeapon 都不可用
```

### PlayerUpgrade

```text
根据自身 IsAvailable 规则判断
```

---

## 13. 示例

假设：

```text
Weapon Slots = 3

已有：
Saw Lv2
FireBall Lv1

未拥有：
BlueOrb
```

当前 Candidate Pool 可能为：

```text
Saw Lv3
FireBall Lv2
获得 BlueOrb
MoveSpeed +10%
PickupRadius +20%
MaxHP +20
```

随机抽取：

```text
Saw Lv3
获得 BlueOrb
MoveSpeed +10%
```

如果 Player 选择：

```text
Saw Lv3
```

则：

```text
Saw Lv2
↓
Saw Lv3
↓
应用该等级配置
```

下一次 Level Up 时重新计算 Candidate Pool。

---

## 14. 连续升级

系统必须兼容一次获得大量经验导致连续升级。

例如：

```text
pendingLevelUpCount = 3
```

处理流程：

```text
第 1 次三选一
↓
Player Select
↓
Apply Upgrade
↓
重新计算 Candidate Pool

第 2 次三选一
↓
Player Select
↓
Apply Upgrade
↓
重新计算 Candidate Pool

第 3 次三选一
↓
Player Select
↓
Apply Upgrade
↓
结束
```

不能提前一次性生成 3 轮候选。

因为前一次选择会改变：

- Weapon 数量。
- Weapon Level。
- Weapon Slot。
- Player Stats。
- Upgrade 可用条件。

---

## 15. 当前随机规则

第一阶段只使用：

```text
合法候选池
+
普通随机
```

暂时不实现 Weight。

但是 UpgradeConfig 结构需要保留未来加入权重的可能。

未来可能扩展：

```text
NewWeapon Weight = 100
WeaponUpgrade Weight = 120
PlayerUpgrade Weight = 80
RareUpgrade Weight = 20
```

---

## 16. 当前阶段暂不实现

暂时不要加入：

- Weapon Evolution
- Weapon Combination
- Passive Item Combination
- Rarity
- Luck
- Upgrade Weight
- Refresh
- Skip
- Ban
- Upgrade Tree
- Complex Prerequisite
- Legendary Upgrade
- Build Synergy

这些功能应在基础 Upgrade System 稳定后再继续扩展。

---

## 17. 后续扩展方向

推荐演进路线：

```text
NewWeapon / WeaponUpgrade / PlayerUpgrade
↓
Weapon 独立 Level Growth
↓
Upgrade Weight
↓
Rarity
↓
Luck
↓
Refresh / Skip / Ban
↓
Passive Item
↓
Weapon Evolution
↓
Build Synergy
```

---

## 18. 验收标准

升级系统完成后必须满足：

1. 三选一不再固定显示 3 把 Weapon。
2. 可以出现 NewWeapon。
3. 可以出现 WeaponUpgrade。
4. 可以出现 PlayerUpgrade。
5. 未拥有 Weapon 时可以出现对应 NewWeapon。
6. 获得 Weapon 后，不再出现该 Weapon 的 NewWeapon。
7. 已拥有且未满级的 Weapon 可以继续出现 WeaponUpgrade。
8. Weapon MaxLevel 后不再出现对应 WeaponUpgrade。
9. Weapon Slot 满后不再出现任何 NewWeapon。
10. 同一次三选一不会出现完全相同的 Upgrade。
11. 不同 Weapon 可以拥有不同 Level Growth。
12. WeaponConfig 不会被运行时修改。
13. 连续 Level Up 时每次都会重新计算 Candidate Pool。
14. LevelUpPanel 不包含具体 Upgrade 业务逻辑。
15. 新增 Weapon 或 PlayerUpgrade 时不需要修改大量现有逻辑。

---

## 19. 设计原则

Upgrade System 应遵循：

> UI 只展示，Config 描述规则，Manager 负责调度，Runtime Data 保存实际状态。

升级系统的核心目标不是简单增加数值，而是让玩家每次 Level Up 都基于当前 Build 获得有意义的成长选择。
