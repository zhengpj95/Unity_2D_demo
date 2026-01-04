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
  [SerializeField] private float arrowDestroyTime = 10f;
  private float nextArrowTimer = 0f;

  [Header("Saw Weapon Settings")]
  [SerializeField] private Transform sawWeaponPrefab;
  [SerializeField] private float sawFireInterval = 10f;
  [SerializeField] private float sawDestroyTime = 10f;
  private float nextSawTimer = 0f;

  private float timer = 0f;

  private void Start()
  {
    player = GameObject.FindGameObjectWithTag("Player").transform;
  }

  private void Update()
  {
    timer += Time.deltaTime;

    if (timer >= nextArrowTimer)
    {
      nextArrowTimer += arrowFireInterval;
      FireArrow();
    }

    if (timer >= nextSawTimer)
    {
      nextSawTimer += sawFireInterval;
      FireSaw();
    }
  }

  private void FireSaw()
  {
    Transform saw = Instantiate(sawWeaponPrefab, player);
    SawWeapon sawWeapon = saw.GetComponent<SawWeapon>();
    sawWeapon.SetDestoryTime(sawDestroyTime);
  }

  private void FireArrow()
  {
    EnemyChasing nearestEnemy = GetNearestEnemy(player.position, attackRange, enemyLayer);
    if (nearestEnemy != null)
    {
      Transform arrow = Instantiate(arrowWeaponPrefab, player.position, Quaternion.identity);
      ArrowWeapon arrowScript = arrow.GetComponent<ArrowWeapon>();
      arrowScript.SetTarget(nearestEnemy.transform, arrowDestroyTime);
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