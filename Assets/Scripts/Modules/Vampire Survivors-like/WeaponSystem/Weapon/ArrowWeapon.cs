using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike
{
  /**
   * 弓箭武器
   */
  public class ArrowWeapon : PooledWeaponEffect
  {
    private bool initialized;
    private float speed = 2f;
    private int damage = 1;
    private Transform target;
    // 发射瞬间选中的目标；直线投射物不跟随目标，但控制器仍用此引用避免多枚子弹重复瞄准同一敌人。
    private Transform launchTarget;
    // 投射物当前的飞行方向；直线投射物初始化后不再修改它。
    private Vector3 direction;
    // true: 每帧朝目标修正方向（追踪投射物）；false: 保持初始方向（直线投射物）。
    private bool followTarget;

    /// <summary>本次发射瞬间选中的目标，效果入池后会清空。</summary>
    public Transform LaunchTarget => launchTarget;

    /// <summary>
    /// 初始化投射物的伤害、速度及飞行方式。
    /// </summary>
    /// <param name="owner">创建本次投射物并负责统一回收的武器控制器。</param>
    /// <param name="targetTransform">
    /// 发射瞬间用于计算初始方向的目标。仅当 <paramref name="shouldFollowTarget"/> 为 true 时才会在飞行过程中持续追踪。
    /// </param>
    /// <param name="levelData">当前武器等级数据，提供飞行速度和伤害值。</param>
    /// <param name="shouldFollowTarget">
    /// true 表示追踪投射物：每帧朝目标转向；false 表示直线投射物：只在初始化时瞄准一次，之后直线飞行。
    /// </param>
    public void Init(WeaponController owner, Transform targetTransform, WeaponLevelData levelData, bool shouldFollowTarget = true)
    {
      if (levelData == null)
      {
        Debug.LogWarning("[ArrowWeapon] Missing weapon level data; projectile was recycled.", this);
        PoolManager.Instance.Free(gameObject);
        return;
      }

      speed = levelData.speed;
      damage = levelData.damage;
      followTarget = shouldFollowTarget;
      launchTarget = targetTransform;
      target = shouldFollowTarget ? targetTransform : null;

      direction = targetTransform != null
        ? targetTransform.position - transform.position
        : transform.right;
      if (direction.sqrMagnitude < Mathf.Epsilon)
      {
        direction = transform.right;
      }

      direction.Normalize();
      UpdateRotation();
      initialized = true;
      BeginEffect(owner, levelData.duration);
    }

    private void Update()
    {
      if (!initialized || TryRecycleWhenExpired()) return;
      if (followTarget && target != null)
      {
        Vector3 targetDirection = target.position - transform.position;
        if (targetDirection.sqrMagnitude >= Mathf.Epsilon)
        {
          direction = targetDirection.normalized;
          UpdateRotation();
        }
      }

      transform.position += direction * speed * Time.deltaTime;
    }

    private void UpdateRotation()
    {
      float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
      transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
      // Collider 可能在对象入池后的同一物理帧继续派发回调，只处理本次出池已完成初始化的攻击。
      if (!initialized || !IsActiveEffect) return;
      if (collision.gameObject.CompareTag("Enemy"))
      {
        VSEnemyHealth vSHealth = collision.gameObject.GetComponent<VSEnemyHealth>();
        if (vSHealth != null)
          vSHealth.TakeDamage(damage);

        // 命中后统一归还对象池，不再销毁投射物实例。
        Recycle();
      }
    }

    /// <summary>清理直线/追踪投射物的目标、方向和数值，避免下一次出池沿用旧弹道。</summary>
    protected override void ResetEffectState()
    {
      initialized = false;
      speed = 2f;
      damage = 1;
      target = null;
      launchTarget = null;
      direction = Vector3.zero;
      followTarget = false;
    }
  }

}
