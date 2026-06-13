# FSM 状态机

## 处理 Run 状态

```c++
if (Input.GetKeyDown(KeyCode.W) {
  // Run
}
```

在处理Run状态时候，假设我们监听是W键按下，那么如果我们按下后马上松开，那么就会有Idle->Run的一瞬间的情况。

解决方案：1. 监听移动

```c++
float moveX = Input.GetAxisRaw("Horizontal");
rb.velocity = new Vector2(moveX * 5f, rb.velocity.y);
float speed = rb.velocity.magnitude;
if (speed > 0.1f) {
  // Run
  // change direction
}
```

解决方案：2. FSM不切合Run，交由Animator处理

很多项目甚至：
FSM不切换Run；FSM不切换Idle

只切换：
Attack, Skill, Dead, Hit

移动动画全部交给Animator。
```c++
animator.SetFloat(
    Speed,
    currentSpeed
);
```

Animator:
```text
BlendTree

0 = Idle

0.5 = Walk

1 = Run
```
结果：
```text
速度变化
↓
BlendTree平滑过渡
↓
Idle ⇄ Run
```
根本不会闪。
