using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : SingletonMono<WeaponManager>
{
  private Transform player;

  [Header("Attack Setting")]
  [SerializeField] private float attackRange = 6f;
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
    EnemyChasing nearestEnemy = EnemySpawnManager.Instance.GetCloseest(player.position, attackRange);
    if (nearestEnemy != null)
    {
      Transform bullet = Instantiate(bulletWeaponPrefab, player.position, Quaternion.identity, weaponContainer);
      ArrowWeapon arrowScript = bullet.GetComponent<ArrowWeapon>();
      arrowScript.SetTarget(nearestEnemy.transform, bulletDestroyTime);
    }
  }

  private void FireLightning()
  {
    var enemy = EnemySpawnManager.Instance.GetRandom(player.position, attackRange);
    if (enemy == null) return;
    Transform lightning = Instantiate(lightningWeaponPrefab, player.position, Quaternion.identity, weaponContainer);
    BlueOvalWeapon lightningWeapon = lightning.GetComponent<BlueOvalWeapon>();
    lightningWeapon.SetTarget(enemy.transform);
  }

  private void FireBlueOval()
  {
    var enemy = EnemySpawnManager.Instance.GetRandom(player.position, attackRange);
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
    EnemyChasing nearestEnemy = EnemySpawnManager.Instance.GetCloseest(player.position, attackRange);
    if (nearestEnemy != null)
    {
      Transform arrow = Instantiate(arrowWeaponPrefab, player.position, Quaternion.identity, weaponContainer);
      ArrowWeapon arrowScript = arrow.GetComponent<ArrowWeapon>();
      arrowScript.SetTarget(nearestEnemy.transform, arrowDestroyTime);
    }
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, attackRange);
  }
}