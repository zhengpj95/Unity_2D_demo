using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>直线子弹控制器：count 会发射多个独立子弹，并优先分散至未被飞行子弹占用的目标。</summary>
  public class BulletbController : WeaponController
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

        ArrowWeapon bullet = SpawnPooledEffect<ArrowWeapon>(data.prefab, player.position, Quaternion.identity, transform);
        if (bullet == null)
          break;

        // 普通子弹仅在发射瞬间锁定方向，之后保持直线飞行。
        bullet.Init(this, enemy.transform, levelData, false);
      }
    }
  }
}
