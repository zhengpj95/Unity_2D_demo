# Enemy System

## 1. 文档目的

本文档定义 Survivor 项目第一阶段敌人系统的职责划分、运行流程、生命周期与扩展方向。

当前敌人系统的目标不是实现复杂 AI，而是建立一个稳定、可扩展、适合大量敌人场景的基础框架。

当前阶段核心闭环：

```text
EnemyDirector
    ↓
按固定节奏请求生成敌人

EnemySpawner
    ↓
在 Player 周围计算随机出生位置

EnemyPool
    ↓
获取并复用 Enemy 实例

Enemy
    ↓
持续朝 Player 移动

距离 Player 过远
    ↓
回收到 EnemyPool
```

---

## 2. 当前目标

第一阶段敌人系统需要满足以下能力：

- Enemy 可以持续生成。
- Enemy 出生位置始终围绕 Player 当前坐标计算。
- Player 在无限地图中移动时，敌人生成逻辑仍然有效。
- Enemy 会持续向 Player 移动。
- Enemy 距离 Player 过远时自动回收。
- Enemy 通过对象池复用，避免频繁 `Instantiate` / `Destroy`。
- 系统职责清晰，后续可以继续扩展 Wave、Elite、Boss、特殊 AI 等功能。

---

## 3. 系统结构

```text
EnemyDirector
      ↓
EnemySpawner
      ↓
EnemyPool
      ↓
Enemy
```

依赖方向必须保持单向。

### EnemyDirector

负责：

- 控制敌人的整体生成节奏。
- 维护生成间隔。
- 控制每次生成数量。
- 调用 `EnemySpawner` 请求生成敌人。

不负责：

- 计算具体出生位置。
- 创建 Enemy GameObject。
- 管理 Enemy 生命周期。
- 处理 Enemy 移动。
- 处理 Enemy AI。

---

### EnemySpawner

负责：

- 根据 Player 当前坐标计算出生位置。
- 从 `EnemyPool` 获取 Enemy。
- 将 Enemy 放置到正确位置。
- 初始化 Enemy 运行时依赖。

不负责：

- `Instantiate` Enemy。
- `Destroy` Enemy。
- 管理 Enemy 对象池容量。
- 控制敌人生成节奏。

---

### EnemyPool

负责：

- 创建 Enemy 实例。
- 复用 Enemy 实例。
- 回收 Enemy。
- 在对象池不足时按需扩容。

建议基础接口：

```csharp
Enemy Get();
void Release(Enemy enemy);
```

或者：

```csharp
Enemy Spawn();
void Despawn(Enemy enemy);
```

---

### Enemy

第一阶段只负责：

- 初始化。
- 朝 Player 移动。
- 检查与 Player 的距离。
- 在超过回收距离时请求回收。

不负责：

- 查找 Player。
- 查找 EnemyDirector。
- 生成其他 Enemy。
- 控制 Wave。
- 管理对象池。

---

## 4. Enemy 生成逻辑

Enemy 的出生位置必须基于 Player 当前坐标计算。

基础算法：

```csharp
Vector2 direction = Random.insideUnitCircle.normalized;

Vector2 spawnPosition =
    (Vector2)player.position +
    direction * spawnRadius;
```

其中：

```text
spawnRadius
```

用于控制 Enemy 与 Player 的出生距离。

建议初始值：

```text
Spawn Radius = 15
```

Enemy 不应该依赖固定地图出生点。

原因：

- 当前地图为无限地图。
- Player 可以持续移动。
- 固定世界坐标会导致敌人生成区域脱离玩家。

---

## 5. Enemy 移动逻辑

第一阶段 Enemy 使用最简单的 Seek Player 行为。

```csharp
Vector2 currentPosition = transform.position;

Vector2 direction =
    ((Vector2)player.position - currentPosition).normalized;

transform.position +=
    (Vector3)(direction * moveSpeed * Time.deltaTime);
```

当前阶段不使用：

- NavMesh
- A*
- 行为树
- Patrol
- Search
- Alert
- 复杂 Chase 状态
- Enemy 与 Enemy 的复杂避障

---

## 6. Enemy 自动回收

无限地图情况下，旧 Enemy 不能永久存在。

因此 Enemy 需要维护：

```text
despawnDistance
```

当 Enemy 与 Player 的距离超过该值时：

```text
Enemy
↓
EnemyPool.Release()
```

建议：

```text
Spawn Radius = 15
Despawn Distance = 25 ~ 30
```

必须保证：

```text
despawnDistance > spawnRadius
```

否则可能出现 Enemy 刚出生就被回收的问题。

推荐使用平方距离比较：

```csharp
Vector2 delta =
    (Vector2)player.position -
    (Vector2)transform.position;

if (delta.sqrMagnitude >
    despawnDistance * despawnDistance)
{
    Despawn();
}
```

这样可以避免不必要的平方根计算。

---

## 7. Enemy 生命周期

完整生命周期：

```text
EnemyDirector
    ↓
Spawn Request

EnemySpawner
    ↓
Calculate Spawn Position

EnemyPool.Get()
    ↓
Enemy.Initialize()

Enemy Active
    ↓
Move Towards Player

Enemy 受到攻击
    ↓
TakeDamage()

HP <= 0
    ↓
Die()

或者：

Distance > Despawn Distance
    ↓
Despawn()

最终：
EnemyPool.Release()
```

Enemy 死亡与超距回收最终都应该进入对象池。

普通 Enemy 生命周期不要使用：

```csharp
Destroy(gameObject);
```

---

## 8. 对象池规则

对象池建议支持：

```text
enemyPrefab
initialCapacity
```

初始化阶段可以提前创建一定数量 Enemy。

例如：

```text
Initial Capacity = 50
```

当对象池为空时允许扩容。

Enemy 回收：

```csharp
enemy.gameObject.SetActive(false);
```

Enemy 重新使用：

```csharp
enemy.gameObject.SetActive(true);
```

需要确保 Enemy 被重新获取时，之前的运行时状态得到正确重置，例如：

- HP
- 移动速度
- 临时 Buff
- Knockback
- 状态标记
- 事件订阅

---

## 9. 依赖规则

推荐依赖：

```text
EnemyDirector
      ↓
EnemySpawner
      ↓
EnemyPool
      ↓
Enemy
```

EnemySpawner 额外依赖：

```text
Player Transform
```

Enemy 运行时依赖：

```text
Player Transform
EnemyPool / Despawn Callback
```

禁止形成类似：

```text
Enemy
↓
EnemyDirector
↓
Enemy
```

这样的循环依赖。

同时应尽量避免：

```csharp
GameObject.Find(...)
FindObjectOfType(...)
```

Player 与 Pool 等依赖应从上层初始化时传入。

---

## 10. Inspector 参数建议

### EnemyDirector

```text
spawnInterval
spawnCount
EnemySpawner
```

### EnemySpawner

```text
Player Transform
spawnRadius
EnemyPool
```

### EnemyPool

```text
enemyPrefab
initialCapacity
poolRoot（可选）
```

### Enemy

```text
moveSpeed
despawnDistance
```

---

## 11. 当前阶段暂不实现

第一阶段不要加入：

- Wave 时间表
- Boss
- Elite
- Enemy Rarity
- Enemy Separation
- 行为树
- 复杂 AI
- 路径寻找
- DOTS / ECS
- Job System
- 大规模分帧更新
- 敌人进化
- 特殊出生规则

这些功能在基础 Enemy Loop 稳定之后再继续扩展。

---

## 12. 后续扩展方向

后续系统可以按照以下顺序演进：

```text
基础 Enemy Loop
    ↓
EnemyConfig
    ↓
Wave / SpawnSchedule
    ↓
Enemy Type
    ↓
Elite / Boss
    ↓
特殊移动模式
    ↓
Enemy Separation
    ↓
Spatial Hash
    ↓
大规模 Enemy Simulation 优化
```

EnemyDirector 后续会逐渐承担“战斗导演”的职责：

- 当前战斗时间。
- 当前 Wave。
- 当前敌人组合。
- 刷怪频率。
- 刷怪密度。
- Boss 事件。
- Elite 事件。
- 特殊刷怪事件。

---

## 13. 验收标准

当前 Enemy System 完成后必须满足：

1. 游戏开始后能够持续生成 Enemy。
2. Enemy 从 Player 周围一定距离生成。
3. Player 移动后，新 Enemy 仍基于 Player 当前坐标生成。
4. Enemy 会持续朝 Player 移动。
5. Enemy 离 Player 过远后自动回收。
6. Enemy 死亡后回收到对象池。
7. Enemy 不通过 `Destroy` 完成普通生命周期。
8. 已回收 Enemy 可以被再次复用。
9. 长时间移动后 Hierarchy 中 Enemy 数量不会无限增长。
10. EnemyDirector、EnemySpawner、EnemyPool、Enemy 职责保持清晰。
11. 当前代码不包含超出第一阶段需求的复杂系统。

---

## 14. 设计原则

本系统遵循以下原则：

> 单个 Enemy 尽可能简单，全局节奏交给 EnemyDirector。

Survivor 类型游戏需要支持大量 Enemy，因此系统设计重点不是让每个 Enemy 拥有复杂 AI，而是让整个 Enemy Simulation 足够轻量、稳定并且易于批量优化。
