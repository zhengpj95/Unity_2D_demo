using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>蓝爆控制器：count 决定本次随机落点的数量，range 决定随机选敌范围倍率。</summary>
  public class BlueOvalController : WeaponController
  {
    protected override void Fire()
    {
      WeaponLevelData levelData = GetLevelData();
      BeginBurstTargetSelection();
      int effectCount = GetEffectCount(levelData);
      for (int i = 0; i < effectCount; i++)
      {
        EnemyChasing enemy = GetRandomBurstTarget(levelData);
        if (enemy == null)
          break;

        BlueOvalWeapon blueOval = SpawnPooledEffect<BlueOvalWeapon>(data.prefab, player.position, Quaternion.identity, transform);
        if (blueOval == null)
          break;

        blueOval.Init(this, enemy.transform, levelData);
      }
    }
  }
}
