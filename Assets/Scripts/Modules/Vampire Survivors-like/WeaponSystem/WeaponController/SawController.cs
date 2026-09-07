using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class SawController : WeaponController
  {
    protected override void Fire()
    {
      var levelData = GetLevelData();
      // Saw 保持挂在 Player 下，才能沿用原有的本地坐标环绕行为。
      SawWeapon saw = SpawnPooledEffect<SawWeapon>(data.prefab, player.position, Quaternion.identity, player);
      if (saw == null)
        return;

      saw.Init(this, levelData);
    }
  }

}
