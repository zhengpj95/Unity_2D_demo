using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 火焰武器，持续伤害敌人
 */
public class FireWeapon : MonoBehaviour
{
  private bool initialized;
  private int damage = 1;
  private float damageInterval = 0.5f;
  private float nextDamageTime = 0f;
  private List<Transform> hitEnemies = new List<Transform>();

  public void Init(WeaponLevelData data)
  {
    damage = data.damage;
    damageInterval = data.damageInterval;
    initialized = true;
  }

  void Update()
  {
    if (!initialized) return;
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
    if (!initialized) return;
    if (hitEnemies.Count <= 0) return;
    nextDamageTime = damageInterval;
    for (int i = 0; i < hitEnemies.Count; i++)
    {
      if (i >= 0 && hitEnemies[i] != null)
      {
        var enemy = hitEnemies[i];
        VSHealth vSHealth = enemy.GetComponent<VSHealth>();
        vSHealth.TakeDamage(damage);
      }
    }
  }

  // 敌人在武器范围时，添加到敌人列表
  private void OnTriggerStay2D(Collider2D collision)
  {
    if (!initialized) return;
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
    if (!initialized) return;
    if (collision.gameObject.CompareTag("Enemy"))
    {
      Transform enemyTransform = collision.transform;
      if (hitEnemies.Contains(enemyTransform))
      {
        hitEnemies.Remove(enemyTransform);
      }
    }
  }
}
