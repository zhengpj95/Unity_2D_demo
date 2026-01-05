using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueOvalWeapon : MonoBehaviour
{
  private List<Transform> hitEnemies = new List<Transform>();

  public void SetTarget(Transform target)
  {
    transform.position = target.position;
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
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
        EnemyChasing enemyChasing = enemy.GetComponent<EnemyChasing>();
        DamageController.Instance.ShowDamage(enemyChasing.Damage, enemy.position);
        DropItemManager.Instance.SpawnDropItem(enemy.position, enemyChasing.DropItemType);
        Destroy(enemy.gameObject);
      }
    }
  }
}
