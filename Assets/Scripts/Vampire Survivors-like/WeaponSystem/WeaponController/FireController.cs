using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireController : WeaponController
{
  protected override void Fire()
  {
    var hero = player.GetComponent<Hero>();
    var enemy = EnemySpawnManager.Instance.GetCloseest(player.position, hero.attackRange);
    if (enemy)
    {
      var weapon = Instantiate(data.prefab, enemy.transform.position, Quaternion.identity, transform);
      Destroy(weapon.gameObject, GetLevelData().duration);
    }
  }
}
