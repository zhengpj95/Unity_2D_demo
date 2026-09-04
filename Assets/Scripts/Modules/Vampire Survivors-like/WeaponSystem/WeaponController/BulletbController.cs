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
        // transform 是 WeaponManager 创建的 WeaponBulletb 节点，因此子弹实例位于 WeaponManager/WeaponBulletb 下。
        var bulletb = Instantiate(data.prefab, player.position, Quaternion.identity, transform);
        var bulletbScript = bulletb.GetComponent<ArrowWeapon>();
        var levelData = GetLevelData();
        // false：普通子弹仅在发射时锁定方向，敌人移动或回收后都不会影响弹道。
        bulletbScript.Init(enemy.transform, levelData, false);
        Destroy(bulletb.gameObject, levelData.duration);
      }
    }
  }

}
