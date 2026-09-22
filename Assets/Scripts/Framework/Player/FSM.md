# FSM 状态机说明

`Assets/Scripts/Framework/Player/FSM/` 是一套学习型角色状态机：`StateMachine` 持有当前 `BaseState`，现有状态包括 `IdleState`、`RunState` 和 `AttackState`。它目前不是 `GameMgr` 驱动的项目级 Service，也没有被所有玩法统一采用。

## 状态与移动动画

不要只用 `Input.GetKeyDown` 决定持续移动状态。按下后立即松开会产生短暂的 `Idle → Run → Idle`，状态和实际速度容易不一致。持续移动应读取轴输入或 Rigidbody2D 的实际速度：

```csharp
float moveX = Input.GetAxisRaw("Horizontal");
rigidbody2D.velocity = new Vector2(moveX * moveSpeed, rigidbody2D.velocity.y);

float speed = rigidbody2D.velocity.magnitude;
if (speed > 0.1f)
{
  // 进入或保持移动状态，并更新朝向。
}
```

另一种常见边界是：FSM 只管理 Attack、Skill、Hit、Dead 等会阻断行为的状态，Idle/Walk/Run 由 Animator Blend Tree 根据速度连续切换：

```csharp
animator.SetFloat(SpeedHash, currentSpeed);
```

```text
速度参数 → Blend Tree → Idle / Walk / Run 平滑过渡
```

选择哪种方式取决于玩法是否需要让“移动”承担独立的进入/退出规则。若只有动画差异，交给 Blend Tree 更简单；若移动状态会改变输入、碰撞、技能或网络同步，则保留显式 FSM 状态更清晰。

## 扩展约定

- 状态切换只通过 `StateMachine`，不要由状态对象直接修改外部的当前状态字段。
- `Enter` 负责进入时的一次性设置，`Update`/`FixedUpdate` 负责持续逻辑，`Exit` 恢复本状态修改的内容。
- 缓存 Character、Rigidbody2D、Animator 等高频依赖，不在逐帧路径重复 `GetComponent`。
- 动画事件、协程或异步回调在离开状态后必须失效，避免旧状态继续触发攻击。
- 该 FSM 是可选基础能力；Survivor、RPG、FrogAdventure 等现有玩法未必使用它，不要强行统一改造。
