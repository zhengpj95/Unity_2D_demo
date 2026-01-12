using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningController : WeaponController
{
  protected override void Fire()
  {
    var hero = player.GetComponent<Hero>();
    var enemy = EnemySpawnManager.Instance.GetRandom(player.position, hero.attackRange);
    if (enemy)
    {
      Transform lightning = Instantiate(data.prefab, player.position, Quaternion.identity, transform);
      BlueOvalWeapon lightningWeapon = lightning.GetComponent<BlueOvalWeapon>();
      lightningWeapon.SetTarget(enemy.transform);
      Destroy(lightning.gameObject, GetLevelData().duration);
    }
  }
}
