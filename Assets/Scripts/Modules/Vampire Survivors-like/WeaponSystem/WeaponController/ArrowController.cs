using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  /// <summary>直线弓箭武器控制器，复用直线投射物分散选敌规则避免连续集中射向同一敌人。</summary>
  public class ArrowController : WeaponController
  {
    protected override void Fire()
    {
      EnemyChasing enemy = GetClosestProjectileTarget();
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
