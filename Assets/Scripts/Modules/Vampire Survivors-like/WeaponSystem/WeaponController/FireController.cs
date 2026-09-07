using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class FireController : WeaponController
  {
    protected override void Fire()
    {
      var enemy = EnemyDirector.Instance.GetCloseest(player.position, GetAttackRange());
      if (enemy)
      {
        var levelData = GetLevelData();
        FireWeapon weapon = SpawnPooledEffect<FireWeapon>(data.prefab, enemy.transform.position, Quaternion.identity, transform);
        if (weapon == null)
          return;

        // 旧实现遗漏 Init，导致 FireWeapon 的持续伤害逻辑不会真正启动；对象池改造时在出池后统一初始化。
        weapon.Init(this, levelData);
      }
    }
  }

}
