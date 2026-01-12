using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : WeaponController
{
  protected override void Fire()
  {
    var hero = player.GetComponent<Hero>();
    EnemyChasing enemy = EnemySpawnManager.Instance.GetCloseest(transform.position, hero.attackRange);
    if (enemy)
    {
      var arrow = Instantiate(data.prefab, transform.position, Quaternion.identity, transform);
      var levelData = GetLevelData();
      arrow.GetComponent<ArrowWeapon>().SetTarget(enemy.transform);
      Destroy(arrow.gameObject, levelData.duration);
    }
  }
}
