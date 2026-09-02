using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  /**
   * 弓箭武器
   */
  public class ArrowWeapon : MonoBehaviour
  {
    private bool initialized;
    private float speed = 2f;
    private int damage = 1;
    private Transform target;
    // 投射物当前的飞行方向；直线投射物初始化后不再修改它。
    private Vector3 direction;
    // true: 每帧朝目标修正方向（追踪投射物）；false: 保持初始方向（直线投射物）。
    private bool followTarget;

    /// <summary>
    /// 初始化投射物的伤害、速度及飞行方式。
    /// </summary>
    /// <param name="targetTransform">
    /// 发射瞬间用于计算初始方向的目标。仅当 <paramref name="shouldFollowTarget"/> 为 true 时才会在飞行过程中持续追踪。
    /// </param>
    /// <param name="levelData">当前武器等级数据，提供飞行速度和伤害值。</param>
    /// <param name="shouldFollowTarget">
    /// true 表示追踪投射物：每帧朝目标转向；false 表示直线投射物：只在初始化时瞄准一次，之后直线飞行。
    /// </param>
    public void Init(Transform targetTransform, WeaponLevelData levelData, bool shouldFollowTarget = true)
    {
      speed = levelData.speed;
      damage = levelData.damage;
      followTarget = shouldFollowTarget;
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
    }

    private void Update()
    {
      if (!initialized) return;
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
      if (!initialized) return;
      if (collision.gameObject.CompareTag("Enemy"))
      {
        VSEnemyHealth vSHealth = collision.gameObject.GetComponent<VSEnemyHealth>();
        vSHealth.TakeDamage(damage);
        Destroy(gameObject); // 销毁弓箭
      }
    }
  }

}
