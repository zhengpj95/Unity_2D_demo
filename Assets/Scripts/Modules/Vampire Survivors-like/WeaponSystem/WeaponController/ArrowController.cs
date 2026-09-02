using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class ArrowController : WeaponController
  {
    protected override void Fire()
    {
      EnemyChasing enemy = EnemySpawnManager.Instance.GetCloseest(player.position, GetAttackRange());
      if (enemy)
      {
        var arrow = Instantiate(data.prefab, player.position, Quaternion.identity, transform);
        var levelData = GetLevelData();
        // 使用默认 true：弓箭在飞行过程中持续追踪目标。
        var arrowScript = arrow.GetComponent<ArrowWeapon>();
        arrowScript.Init(enemy.transform, levelData);
        Destroy(arrow.gameObject, levelData.duration);
      }
    }
  }

}
