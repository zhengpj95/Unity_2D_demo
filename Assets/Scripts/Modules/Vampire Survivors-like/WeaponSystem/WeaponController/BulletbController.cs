using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  /// <summary>
  /// 直线子弹武器控制器。
  /// 复用直线投射物分散选敌规则，减少同一目标被连续重复射击；敌人不足时才允许重复目标。
  /// </summary>
  public class BulletbController : WeaponController
  {
    protected override void Fire()
    {
      EnemyChasing enemy = GetClosestProjectileTarget();
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
