using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  /**
   * 火焰武器，持续伤害敌人
   */
  public class FireWeapon : PooledWeaponEffect
  {
    private bool initialized;
    private int damage = 1;
    private float damageInterval = 0.5f;
    private float nextDamageTime = 0f;
    private List<Transform> hitEnemies = new List<Transform>();

    /// <summary>启动一次持续伤害区域，并登记给创建它的武器控制器。</summary>
    public void Init(WeaponController owner, WeaponLevelData data)
    {
      if (data == null)
      {
        Debug.LogWarning("[FireWeapon] Missing weapon level data; effect was skipped.", this);
        PoolManager.Instance.Free(gameObject);
        return;
      }

      damage = data.damage;
      damageInterval = data.damageInterval;
      initialized = true;
      BeginEffect(owner, data.duration);
    }

    void Update()
    {
      if (!initialized || TryRecycleWhenExpired()) return;
      if (nextDamageTime > 0)
      {
        nextDamageTime -= Time.deltaTime;
      }
      else
      {
        DealDamage();
      }
    }

    private void DealDamage()
    {
      // 防御 Animator/物理回调与入池时序重叠，只有当前有效生命周期可以结算伤害。
      if (!initialized || !IsActiveEffect) return;
      if (hitEnemies.Count <= 0) return;
      nextDamageTime = damageInterval;
      for (int i = 0; i < hitEnemies.Count; i++)
      {
        if (hitEnemies[i] != null)
        {
          var enemy = hitEnemies[i];
          VSEnemyHealth vSHealth = enemy.GetComponent<VSEnemyHealth>();
          if (vSHealth != null)
            vSHealth.TakeDamage(damage);
        }
      }
    }

    // 敌人在武器范围时，添加到敌人列表
    private void OnTriggerStay2D(Collider2D collision)
    {
      if (!initialized || !IsActiveEffect) return;
      if (collision.gameObject.CompareTag("Enemy"))
      {
        Transform enemyTransform = collision.transform;
        if (!hitEnemies.Contains(enemyTransform))
        {
          hitEnemies.Add(enemyTransform);
        }
      }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
      if (!initialized || !IsActiveEffect) return;
      if (collision.gameObject.CompareTag("Enemy"))
      {
        Transform enemyTransform = collision.transform;
        if (hitEnemies.Contains(enemyTransform))
        {
          hitEnemies.Remove(enemyTransform);
        }
      }
    }

    /// <summary>清除持续伤害计时与命中集合，避免对象复用后沿用上一片火焰的状态。</summary>
    protected override void ResetEffectState()
    {
      initialized = false;
      damage = 1;
      damageInterval = 0.5f;
      nextDamageTime = 0f;
      hitEnemies.Clear();
    }
  }

}
