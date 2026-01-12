using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletbController : WeaponController
{
  private Transform player;

  private void Start()
  {
    player = GameObject.FindGameObjectWithTag("Player").transform;
  }

  protected override void Fire()
  {
    var hero = player.GetComponent<Hero>();
    var enemy = EnemySpawnManager.Instance.GetCloseest(player.position, hero.attackRange);
    if (enemy)
    {
      var bulletb = Instantiate(data.prefab, player.position, Quaternion.identity, player);
      var bulletbScript = bulletb.GetComponent<ArrowWeapon>();
      var levelData = GetLevelData();
      bulletbScript.SetTarget(enemy.transform, levelData.duration);
      Destroy(bulletb.gameObject, levelData.duration);
    }
  }
}
