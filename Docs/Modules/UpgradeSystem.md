# Upgrade System

## 1. 当前实现

升级系统负责玩家升级后的三选一候选生成、展示和应用。当前实现已经闭合以下流程：

```text
Gem 被拾取
    ↓
SurvivorProxy.AddExp
    ↓
累计并排队 PendingLevelUpCount
    ↓
SurvivorGameplayController 打开升级选择
    ↓
UpgradeManager 生成最多 3 个合法候选
    ↓
SurvivorSkillSelectPanelPresenter 展示
    ↓
Controller 再次校验并 Apply
    ↓
WeaponManager / Hero / VSPlayerHealth
```

主要实现文件：

```text
Assets/Scripts/Modules/Vampire Survivors-like/UpgradeSystem/UpgradeConfig.cs
Assets/Scripts/Modules/Vampire Survivors-like/UpgradeSystem/UpgradeManager.cs
Assets/Scripts/Modules/Vampire Survivors-like/WeaponSystem/WeaponManager.cs
Assets/Scripts/Modules/Vampire Survivors-like/WeaponSystem/WeaponSO.cs
Assets/Scripts/Modules/Vampire Survivors-like/WeaponSystem/WeaponLevelData.cs
Assets/Scripts/Modules/Vampire Survivors-like/SurvivorGameplayController.cs
Assets/Scripts/Modules/Vampire Survivors-like/SurvivorSkillSelectPanelPresenter.cs
```

---

## 2. 升级配置类型

当前 `UpgradeConfig.cs` 中有三类 `ScriptableObject`：

### NewWeaponUpgradeConfig

目标是一个尚未拥有的 `WeaponSO`。可用条件：

```text
WeaponManager 存在
AND WeaponSO 不为空
AND 当前没有该 WeaponSO
AND WeaponManager.WeaponCount < maxWeaponSlots
```

应用时调用 `WeaponManager.TryAddOrUpgrade(weapon)`，首次获得会创建武器运行时控制器。

### WeaponUpgradeConfig

目标是一个已经拥有的 `WeaponSO`。可用条件：

```text
WeaponManager 存在
AND 当前已经拥有该 WeaponSO
AND WeaponManager.CanUpgrade(weapon)
```

它本身不保存“升级到几级”的字段，也不单独保存伤害数值；应用时调用 `TryAddOrUpgrade`，由对应 `WeaponController.LevelUp()` 将运行时等级加 1。

### PlayerUpgradeConfig

当前支持的属性枚举：

```csharp
MoveSpeed
PickupRadius
MaxHealth
AttackRange
```

`MoveSpeed`、`PickupRadius`、`AttackRange` 应用到 `Hero.ApplyUpgrade`；`MaxHealth` 应用到 `VSPlayerHealth.ApplyMaxHealthUpgrade`。配置的 `value` 由 `isPercent` 决定是百分比还是固定值。

默认运行时候选目前只自动创建：移动速度、拾取范围、最大生命；攻击范围可以通过自定义 `PlayerUpgradeConfig` 资源加入。

---

## 3. UpgradeId 与候选去重

配置不再提供可由 Inspector 随意填写的字符串 ID。固定类型由枚举决定：

```csharp
public enum UpgradeId
{
    NewWeapon,
    WeaponLevel,
    PlayerMoveSpeed,
    PlayerPickupRadius,
    PlayerMaxHealth,
    PlayerAttackRange,
}
```

唯一键规则：

| 配置 | 唯一键示例 |
| --- | --- |
| 新武器 | `NewWeapon:WeaponArrow` |
| 武器升级 | `WeaponLevel:WeaponArrow` |
| 移动速度 | `PlayerMoveSpeed` |
| 拾取范围 | `PlayerPickupRadius` |
| 最大生命 | `PlayerMaxHealth` |
| 攻击范围 | `PlayerAttackRange` |

武器类配置会在枚举值后追加 `WeaponSO.weaponId`，避免不同武器互相去重；玩家属性按属性枚举映射。`UpgradeManager` 每轮使用 `HashSet<string>` 去重，自定义资源和运行时默认候选重复时只保留一个。

新增升级类型时：

1. 在 `UpgradeId` 中增加固定值。
2. 在对应 `UpgradeConfig` 子类中返回该值。
3. 如需区分目标对象，重写 `GetUniqueId()`。
4. 实现 `IsAvailable(context)` 和 `Apply(context)`。
5. 如需手动创建资源，再添加 `CreateAssetMenu`。

不要重新添加可编辑字符串 `id` 字段。

---

## 4. 候选池生成

`UpgradeManager.GetUpgradeOptions` 有两个入口：

```csharp
GetUpgradeOptions(int count)
GetUpgradeOptions(int count, PlayerUpgradeContext context)
```

无 Context 时，Manager 会按 `Player` 标签解析 `Hero` 和 `VSPlayerHealth`；Controller 会主动组装 Context，避免 `UpgradeConfig` 自己查找场景对象。

每次调用都会重新过滤：

```text
Inspector 的 upgradeConfigs
    +
运行时默认候选 runtimeConfigs
    ↓
IsAvailable(context)
    ↓
按 Id 去重
    ↓
Fisher-Yates 普通随机打乱
    ↓
返回 Mathf.Min(count, 合法候选数)
```

候选不足 3 个时返回实际数量，面板会隐藏多余卡片，不使用空配置凑数。运行时默认候选只创建一次，并在 `UpgradeManager.OnDestroy` 中销毁。

当前场景的 `UpgradeManager.upgradeConfigs` 为空，因此主要使用运行时默认候选；自定义资源仍可拖入该数组扩展内容。

---

## 5. 运行时 Context 与应用

`PlayerUpgradeContext` 当前包含：

```csharp
WeaponManager WeaponManager
Hero Hero
VSPlayerHealth PlayerHealth
```

`UpgradeConfig` 只读取配置并通过 Context 修改运行时实例，不修改 `WeaponSO` 或其他 ScriptableObject 原始数据。

玩家属性升级的实际写入位置：

| 属性 | 运行时写入 |
| --- | --- |
| 移动速度 | `Hero` 的升级平坦值/百分比字段 |
| 拾取范围 | `Hero` 的升级平坦值/百分比字段，随后同步 CircleCollider2D 半径 |
| 攻击范围 | `Hero` 的升级平坦值/百分比字段 |
| 最大生命 | `VSPlayerHealth` 的 `maxHealth/currentHealth` |

最大生命升级会同时增加当前生命；百分比值按当前最大生命计算，固定值按整数处理，最小增加 1。

---

## 6. 武器等级来源

当前武器资源类型是 `WeaponSO`，不是独立的 `WeaponConfig`：

```csharp
public class WeaponSO : ScriptableObject
{
    public string weaponId;
    public Transform prefab;
    public Sprite icon;
    public string weaponName;
    public WeaponLevelData[] levels;
}
```

每个武器的等级在 Inspector 中配置 `WeaponSO.levels` 数组。数组下标对应运行时等级：

```text
levels[0] → Lv1
levels[1] → Lv2
levels[2] → Lv3
...
```

`WeaponLevelData` 当前字段为：

```text
level
speed
damage
damageInterval
count
range
fireInterval
duration
```

`WeaponController` 保存本局 `level`，初始为 1；`MaxLevel` 等于 `WeaponSO.levels.Length`。`LevelUp()` 只增加控制器实例的等级，`GetLevelData()` 再读取对应数组项。达到数组长度后，`WeaponManager.CanUpgrade` 返回 false，相关 `WeaponUpgrade` 不再进入候选。

因此，新增或修改武器等级的正确位置是：

```text
Project 面板
→ 找到对应 WeaponSO
→ Inspector 的 Levels 数组
→ 增加元素并填写该等级的字段
```

不要在 `WeaponUpgradeConfig` 中另建一套等级数组，也不要在运行时修改 `WeaponSO.levels`。

---

## 7. WeaponManager 与武器槽位

场景中的 `WeaponManager` 当前配置六个可用 `WeaponSO` 引用：

```text
sawSO
arrowSO
bulletbSO
blueOvalSO
lightningSO
fireSO
```

`GetConfiguredWeapons()` 将这些引用提供给 `UpgradeManager`，用于创建默认 NewWeapon 和 WeaponUpgrade 候选。当前场景 `maxWeaponSlots = 3`。

`WeaponManager.AddWeapon` 在首次获得武器时动态创建：

```text
WeaponManager
└── WeaponArrow / WeaponBulletb / WeaponSaw / ...
```

父节点由 `weaponObj.transform.SetParent(transform)` 决定，场景不需要预先创建这些子节点。控制器类型由 `weaponId` 映射；新增武器时必须同时保证 `weaponId` 在 `GetWeaponType` 中有对应控制器，否则无法创建。

---

## 8. 三选一面板与连续升级

`SurvivorSkillSelectPanelPresenter` 只负责：

- 显示 `UpgradeConfig.Icon`、标题和描述。
- 隐藏不足 3 个的卡片。
- 响应按钮点击。
- 使用 `Time.unscaledDeltaTime` 运行 10 秒倒计时，超时自动选择第一个有效选项。

实际业务由 `SurvivorGameplayController` 编排：

```text
TryConsumePendingLevelUp
    ↓
GetUpgradeOptions(3, context)
    ↓
GameState = LevelUp，Time.timeScale = 0
    ↓
选择候选
    ↓
再次 IsAvailable 校验
    ↓
Apply(context)
    ↓
有 PendingLevelUpCount：下一轮重新抽取
没有：GameState = Playing，Time.timeScale = 1
```

选择回调在面板关闭前缓存候选对象，关闭时清理候选和回调，避免 `OnClose` 清空数据后出现空引用。

如果候选已失效，Controller 会记录 Warning 并恢复游戏；如果合法候选数量为 0，也会跳过弹窗并恢复游戏。

---

## 9. 默认图标与自定义资源

默认玩家属性候选不是 Project 资源，而是 `UpgradeManager` 运行时创建的 ScriptableObject。图标配置在场景 `UpgradeManager` 组件上：

```text
默认玩家属性升级图标
├── Move Speed Upgrade Icon
├── Pickup Radius Upgrade Icon
└── Max Health Upgrade Icon
```

当前场景已经配置这三个字段。自定义 `PlayerUpgradeConfig` 资源则在资源自身的 `Icon` 字段配置。

Project 面板创建自定义配置的菜单：

```text
Create
└── Survivor
    └── Upgrade
        ├── New Weapon
        ├── Weapon Level
        └── Player Stat
```

---

## 10. 当前未实现

当前没有：

- Weapon Evolution、Weapon Combination。
- 被动道具系统。
- Rarity、Luck、Weight、Refresh、Skip、Ban。
- 复杂前置条件、升级树、Build Synergy。
- 完整 GameOver 和局外成长流程。

`UpgradeId` 目前只用于固定类型和候选去重，不代表已经实现稀有度或权重系统。

---

## 11. 验收清单

在 `SurvivorsDemo` 中运行并获得经验后检查：

1. 候选来自当前合法配置，不固定显示三把武器。
2. 未拥有武器且仍有槽位时可出现 NewWeapon。
3. 获得武器后，该武器的 NewWeapon 不再出现。
4. 已拥有且未满级的武器可出现 WeaponUpgrade。
5. `WeaponSO.levels` 达到最大等级后，相关 WeaponUpgrade 消失。
6. `maxWeaponSlots` 满时，所有 NewWeapon 都不可用。
7. 同一轮不会出现相同 `Id` 的候选。
8. 选择玩家属性后，Hero 或 VSPlayerHealth 的运行时属性立即更新。
9. 选择升级时游戏暂停，倒计时仍能工作。
10. 连续升级每一轮都会重新生成候选。
11. 面板候选不足 3 个时多余卡片隐藏。
12. 运行时升级不会修改 WeaponSO 或其他配置资源。

---

## 12. 文档同步规则

后续代码修改按以下关系同步：

| 代码变更 | 需要同步的文档 |
| --- | --- |
| `UpgradeConfig`、`UpgradeManager`、`UpgradeId` | 本文件、`Survivor.md` |
| `WeaponManager`、`WeaponSO`、`WeaponLevelData`、WeaponController | 本文件、`Survivor.md` |
| `SurvivorGameplayController`、升级面板、经验队列 | 本文件、`Survivor.md` |
| `DropItem`、Gem/Coin 结算 | `Survivor.md`、必要时 `EnemySystem.md` |
| 敌人生成和 Wave | `EnemySystem.md`、`WaveSystem.md` |

字段、菜单路径、场景引用和示例参数变化后，应同时更新本文档中的代码片段、配置说明和验收清单。规划功能只能放在“当前未实现”中。
