using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class LightningController : WeaponController
  {
    protected override void Fire()
    {
      var enemy = EnemyDirector.Instance.GetRandom(player.position, GetAttackRange());
      if (enemy)
      {
        var weaponLevelData = GetLevelData();
        BlueOvalWeapon lightning = SpawnPooledEffect<BlueOvalWeapon>(data.prefab, player.position, Quaternion.identity, transform);
        if (lightning == null)
          return;

        lightning.Init(this, enemy.transform, weaponLevelData);
      }
    }
  }

}
