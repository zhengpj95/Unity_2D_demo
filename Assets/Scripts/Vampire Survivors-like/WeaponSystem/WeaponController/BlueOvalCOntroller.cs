using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueOvalController : WeaponController
{
  protected override void Fire()
  {
    var enemy = EnemySpawnManager.Instance.GetRandom(transform.position, GetAttackRange());
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
