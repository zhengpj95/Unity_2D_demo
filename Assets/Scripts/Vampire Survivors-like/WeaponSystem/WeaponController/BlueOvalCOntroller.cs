using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueOvalController : WeaponController
{
  protected override void Fire()
  {
    var hero = player.GetComponent<Hero>();
    var enemy = EnemySpawnManager.Instance.GetRandom(transform.position, hero.attackRange);
    if (enemy)
    {
      Transform blueOval = Instantiate(data.prefab, player.position, Quaternion.identity, transform);
      BlueOvalWeapon blueOvalWeapon = blueOval.GetComponent<BlueOvalWeapon>();
      var levelData = GetLevelData();
      blueOvalWeapon.Init(enemy.transform, levelData);
      Destroy(blueOval.gameObject, levelData.duration);
    }
  }
}
