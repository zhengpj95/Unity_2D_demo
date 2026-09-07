using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class BlueOvalController : WeaponController
  {
    protected override void Fire()
    {
      var enemy = EnemyDirector.Instance.GetRandom(transform.position, GetAttackRange());
      if (enemy)
      {
        var levelData = GetLevelData();
        BlueOvalWeapon blueOval = SpawnPooledEffect<BlueOvalWeapon>(data.prefab, player.position, Quaternion.identity, transform);
        if (blueOval == null)
          return;

        blueOval.Init(this, enemy.transform, levelData);
      }
    }
  }

}
