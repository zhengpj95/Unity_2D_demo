using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>直线弓箭控制器：count 决定单次触发的投射物数，range 决定选敌范围倍率。</summary>
  public class ArrowController : WeaponController
  {
    protected override void Fire()
    {
      WeaponLevelData levelData = GetLevelData();
      int projectileCount = GetEffectCount(levelData);
      for (int i = 0; i < projectileCount; i++)
      {
        EnemyChasing enemy = GetClosestProjectileTarget(levelData);
        if (enemy == null)
          break;

        ArrowWeapon arrow = SpawnPooledEffect<ArrowWeapon>(data.prefab, player.position, Quaternion.identity, transform);
        if (arrow == null)
          break;

        // 弓箭仅在发射瞬间锁定方向，之后保持直线飞行。
        arrow.Init(this, enemy.transform, levelData, false);
      }
    }
  }
}
