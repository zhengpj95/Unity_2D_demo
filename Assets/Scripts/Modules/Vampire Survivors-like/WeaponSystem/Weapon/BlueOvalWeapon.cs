using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike
{
  /**
   * 炸弹武器、闪电武器
   */
  public class BlueOvalWeapon : PooledWeaponEffect
  {
    private bool initialized;
    private int damage = 1;
    private List<Transform> hitEnemies = new List<Transform>();

    /// <summary>在目标位置启动一次范围伤害效果，并由 owner 管理其对象池生命周期。</summary>
    public void Init(WeaponController owner, Transform target, WeaponLevelData data)
    {
      if (target == null || data == null)
      {
        Debug.LogWarning("[BlueOvalWeapon] Missing target or level data; effect was skipped.", this);
        PoolManager.Instance.Free(gameObject);
        return;
      }

      transform.position = target.position;
      damage = data.damage;
      initialized = true;
      BeginEffect(owner, data.duration);
    }

    private void Update()
    {
      if (!initialized) return;
      TryRecycleWhenExpired();
    }

    void OnTriggerEnter2D(Collider2D collision)
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

    // Animation Event
    public void DamageEnemy()
    {
      // 动画事件可能在对象归还对象池后延迟到达，入池对象不能继续造成伤害。
      if (!initialized || !IsActiveEffect)
        return;

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

    /// <summary>入池前清空范围内敌人缓存，避免下一次动画事件误伤旧目标。</summary>
    protected override void ResetEffectState()
    {
      initialized = false;
      damage = 1;
      hitEnemies.Clear();
    }
  }

}
