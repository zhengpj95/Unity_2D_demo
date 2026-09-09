using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>火圈控制器：count 在一次触发中选择多个不同落点，range 决定可选敌范围倍率。</summary>
  public class FireController : WeaponController
  {
    protected override void Fire()
    {
      WeaponLevelData levelData = GetLevelData();
      BeginBurstTargetSelection();
      int effectCount = GetEffectCount(levelData);
      for (int i = 0; i < effectCount; i++)
      {
        EnemyChasing enemy = GetClosestBurstTarget(levelData);
        if (enemy == null)
          break;

        FireWeapon weapon = SpawnPooledEffect<FireWeapon>(data.prefab, enemy.transform.position, Quaternion.identity, transform);
        if (weapon == null)
          break;

        weapon.Init(this, levelData);
      }
    }
  }
}
