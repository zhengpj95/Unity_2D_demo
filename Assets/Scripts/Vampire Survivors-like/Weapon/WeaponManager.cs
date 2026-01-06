using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : SingletonMono<WeaponManager>
{
  private Transform player;

  [Header("Attack Setting")]
  [SerializeField] private float attackRange = 6f;
  [SerializeField] private LayerMask enemyLayer;
  [SerializeField] private Transform weaponContainer;

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

  [Header("Blue Oval Weapon Settings")]
  [SerializeField] private Transform blueOvalWeaponPrefab;
  [SerializeField] private float blueOvalFireInterval = 5f;
  private float nextBlueOvalTimer = 0f;

  [Header("Lightning Weapon Settings")]
  [SerializeField] private Transform lightningWeaponPrefab;
  [SerializeField] private float lightningFireInterval = 5f;
  private float nextLightningTimer = 0f;

  [Header("Bullet Weapon Settings")]
  [SerializeField] private Transform bulletWeaponPrefab;
  [SerializeField] private float bulletFireInterval = 0.8f;
  [SerializeField] private float bulletDestroyTime = 10f;
  private float nextBulletTimer = 0f;

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

    if (timer >= nextBlueOvalTimer)
    {
      nextBlueOvalTimer += blueOvalFireInterval;
      FireBlueOval();
    }

    if (timer >= nextLightningTimer)
    {
      nextLightningTimer += lightningFireInterval;
      FireLightning();
    }

    if (timer >= nextBulletTimer)
    {
      nextBulletTimer += bulletFireInterval;
      FireBullet();
    }
  }

  private void FireBullet()
  {
    EnemyChasing nearestEnemy = GetNearestEnemy(player.position, attackRange, enemyLayer);
    if (nearestEnemy != null)
    {
      Transform bullet = Instantiate(bulletWeaponPrefab, player.position, Quaternion.identity, weaponContainer);
      ArrowWeapon arrowScript = bullet.GetComponent<ArrowWeapon>();
      arrowScript.SetTarget(nearestEnemy.transform, bulletDestroyTime);
    }
  }

  private void FireLightning()
  {
    var enemy = GetRandomEnemy(player.position, attackRange, enemyLayer);
    if (enemy == null) return;
    Transform lightning = Instantiate(lightningWeaponPrefab, player.position, Quaternion.identity, weaponContainer);
    BlueOvalWeapon lightningWeapon = lightning.GetComponent<BlueOvalWeapon>();
    lightningWeapon.SetTarget(enemy.transform);
  }

  private void FireBlueOval()
  {
    var enemy = GetRandomEnemy(player.position, attackRange, enemyLayer);
    if (enemy == null) return;
    Transform blueOval = Instantiate(blueOvalWeaponPrefab, player.position, Quaternion.identity, weaponContainer);
    BlueOvalWeapon blueOvalWeapon = blueOval.GetComponent<BlueOvalWeapon>();
    blueOvalWeapon.SetTarget(enemy.transform);
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
      Transform arrow = Instantiate(arrowWeaponPrefab, player.position, Quaternion.identity, weaponContainer);
      ArrowWeapon arrowScript = arrow.GetComponent<ArrowWeapon>();
      arrowScript.SetTarget(nearestEnemy.transform, arrowDestroyTime);
    }
  }

  public EnemyChasing GetRandomEnemy(Vector2 center, float range, LayerMask enemyLayer)
  {
    Collider2D[] results = new Collider2D[32];

    int count = Physics2D.OverlapCircleNonAlloc(center, range, results, enemyLayer);

    if (count > 0)
    {
      int randomIndex = Random.Range(0, count);
      EnemyChasing enemy = results[randomIndex].GetComponent<EnemyChasing>();
      return enemy;
    }

    return null;
  }

  public EnemyChasing GetNearestEnemy(Vector2 center, float range, LayerMask enemyLayer)
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

  public List<EnemyChasing> GetEnemiesSortedByDistance(Vector2 center, float range, LayerMask enemyLayer)
  {
    Collider2D[] results = new Collider2D[32];
    int count = Physics2D.OverlapCircleNonAlloc(center, range, results, enemyLayer);

    List<EnemyChasing> enemies = new List<EnemyChasing>(count);
    for (int i = 0; i < count; i++)
    {
      EnemyChasing e = results[i].GetComponent<EnemyChasing>();
      if (e == null) continue;
      enemies.Add(e);
    }

    enemies.Sort((a, b) =>
    {
      float da = ((Vector2)a.transform.position - center).sqrMagnitude;
      float db = ((Vector2)b.transform.position - center).sqrMagnitude;
      return da.CompareTo(db);
    });

    return enemies;
  }

  void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, attackRange);
  }
}