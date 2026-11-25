using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>Saw 控制器：count 决定同时环绕的 Saw 数，range 为环绕半径，speed 为角速度。</summary>
  public class SawController : WeaponController
  {
    protected override void Fire()
    {
      WeaponLevelData levelData = GetLevelData();
      int sawCount = GetEffectCount(levelData);
      for (int i = 0; i < sawCount; i++)
      {
        // Saw 挂在 Player 下以使用本地坐标环绕；每把 Saw 使用均分的初始角度避免重叠。
        SawWeapon saw = SpawnPooledEffect<SawWeapon>(data.prefab, player.position, Quaternion.identity, player);
        if (saw == null)
          break;

        saw.Init(this, levelData, i, sawCount);
      }
    }
  }
}
