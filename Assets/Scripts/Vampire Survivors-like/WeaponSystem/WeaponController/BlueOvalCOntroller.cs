using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueOvalCOntroller : WeaponController
{
  protected override void Fire()
  {
    var hero = player.GetComponent<Hero>();
    var enemy = EnemySpawnManager.Instance.GetRandom(transform.position, hero.attackRange);
    if (enemy)
    {
      Transform blueOval = Instantiate(data.prefab, player.position, Quaternion.identity, transform);
      BlueOvalWeapon blueOvalWeapon = blueOval.GetComponent<BlueOvalWeapon>();
      blueOvalWeapon.SetTarget(enemy.transform);
      Destroy(blueOval.gameObject, GetLevelData().duration);
    }
  }
}
