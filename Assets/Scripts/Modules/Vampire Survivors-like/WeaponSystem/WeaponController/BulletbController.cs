using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class BulletbController : WeaponController
  {
    protected override void Fire()
    {
      var enemy = EnemyDirector.Instance.GetCloseest(player.position, GetAttackRange());
      if (enemy)
      {
        var levelData = GetLevelData();
        // transform 是 WeaponManager 创建的 WeaponBulletb 节点；出池后仍挂在这里便于分类和重开统一回收。
        ArrowWeapon bulletb = SpawnPooledEffect<ArrowWeapon>(data.prefab, player.position, Quaternion.identity, transform);
        if (bulletb == null)
          return;

        // false：普通子弹仅在发射时锁定方向，敌人移动或回收后都不会影响弹道。
        bulletb.Init(this, enemy.transform, levelData, false);
      }
    }
  }

}
