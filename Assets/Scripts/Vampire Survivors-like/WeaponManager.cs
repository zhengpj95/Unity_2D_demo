using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : SingletonMono<WeaponManager>
{
  private Transform player;

  [SerializeField] private float attackRange = 6f;
  [SerializeField] private LayerMask enemyLayer;

  [Header("Arrow Weapon Settings")]
  [SerializeField] private Transform arrowWeaponPrefab;
  [SerializeField] private float arrowFireInterval = 1f;
  private float arrowTimer = 0f;

  private void Start()
  {
    player = GameObject.FindGameObjectWithTag("Player").transform;
  }

  private void Update()
  {
    arrowTimer += Time.deltaTime;

    if (arrowTimer >= arrowFireInterval)
    {
      arrowTimer = 0f;
      FireArrow();
    }
  }

  private void FireArrow()
  {
    Transform arrow = Instantiate(arrowWeaponPrefab, player.position, Quaternion.identity);
    ArrowWeapon arrowScript = arrow.GetComponent<ArrowWeapon>();
    EnemyChasing nearestEnemy = GetNearestEnemy(player.position, attackRange, enemyLayer);
    if (nearestEnemy != null)
    {
      arrowScript.SetTarget(nearestEnemy.transform);
    }
  }

  public static EnemyChasing GetNearestEnemy(Vector2 center, float range, LayerMask enemyLayer)
  {
    Collider2D[] results = new Collider2D[32];

    int count = Physics2D.OverlapCircleNonAlloc(center, range, results, enemyLayer);

    EnemyChasing nearest = null;
    float minSqrDist = float.MaxValue;

    for (int i = 0; i < count; i++)
    {
      EnemyChasing enemy = results[i].GetComponent<EnemyChasing>();
      if (enemy == null) continue;

      float sqrDist = ((Vector2)enemy.transform.position - center).sqrMagnitude;

      if (sqrDist < minSqrDist)
      {
        minSqrDist = sqrDist;
        nearest = enemy;
      }
    }

    return nearest;
  }

  void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, attackRange);
  }

}