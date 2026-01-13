using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : WeaponController
{
  protected override void Fire()
  {
    var hero = player.GetComponent<Hero>();
    EnemyChasing enemy = EnemySpawnManager.Instance.GetCloseest(player.position, hero.attackRange);
    if (enemy)
    {
      var arrow = Instantiate(data.prefab, player.position, Quaternion.identity, transform);
      var levelData = GetLevelData();
      var arrowScript = arrow.GetComponent<ArrowWeapon>();
      arrowScript.Init(enemy.transform, levelData);
      Destroy(arrow.gameObject, levelData.duration);
    }
  }
}
