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
        // transform 是 WeaponManager 创建的 WeaponArrow 节点，因此弓箭实例位于 WeaponManager/WeaponArrow 下。
        var arrow = Instantiate(data.prefab, player.position, Quaternion.identity, transform);
        var levelData = GetLevelData();
        // false：弓箭仅在发射时锁定方向，之后沿直线飞行。
        var arrowScript = arrow.GetComponent<ArrowWeapon>();
        arrowScript.Init(enemy.transform, levelData, false);
        Destroy(arrow.gameObject, levelData.duration);
      }
    }
  }

}
