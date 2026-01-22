using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 炸弹武器、闪电武器
 */
public class BlueOvalWeapon : MonoBehaviour
{
  private bool initialized;
  private int damage = 1;
  private List<Transform> hitEnemies = new List<Transform>();

  public void Init(Transform target, WeaponLevelData data)
  {
    transform.position = target.position;
    damage = data.damage;
    initialized = true;
  }

  void OnTriggerEnter2D(Collider2D collision)
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

  // Animation Event
  public void DamageEnemy()
  {
    for (int i = 0; i < hitEnemies.Count; i++)
    {
      if (i >= 0 && hitEnemies[i] != null)
      {
        var enemy = hitEnemies[i];
        VSEnemyHealth vSHealth = enemy.GetComponent<VSEnemyHealth>();
        vSHealth.TakeDamage(damage);
      }
    }
  }
}
