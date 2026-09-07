using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class ArrowController : WeaponController
  {
    protected override void Fire()
    {
      EnemyChasing enemy = EnemyDirector.Instance.GetCloseest(player.position, GetAttackRange());
      if (enemy)
      {
        var levelData = GetLevelData();
        // transform 是 WeaponManager 创建的 WeaponArrow 节点；出池后仍挂在这里便于分类和重开统一回收。
        ArrowWeapon arrow = SpawnPooledEffect<ArrowWeapon>(data.prefab, player.position, Quaternion.identity, transform);
        if (arrow == null)
          return;

        // false：弓箭仅在发射时锁定方向，之后沿直线飞行。
        arrow.Init(this, enemy.transform, levelData, false);
      }
    }
  }

}
