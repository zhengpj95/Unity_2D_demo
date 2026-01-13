using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletbController : WeaponController
{
  protected override void Fire()
  {
    var hero = player.GetComponent<Hero>();
    var enemy = EnemySpawnManager.Instance.GetCloseest(player.position, hero.attackRange);
    if (enemy)
    {
      var bulletb = Instantiate(data.prefab, player.position, Quaternion.identity, transform);
      var bulletbScript = bulletb.GetComponent<ArrowWeapon>();
      var levelData = GetLevelData();
      bulletbScript.Init(enemy.transform, levelData);
      Destroy(bulletb.gameObject, levelData.duration);
    }
  }
}
