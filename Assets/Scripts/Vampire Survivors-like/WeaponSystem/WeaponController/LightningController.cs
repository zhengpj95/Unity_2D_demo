using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningController : WeaponController
{
  protected override void Fire()
  {
    var enemy = EnemySpawnManager.Instance.GetRandom(player.position, GetAttackRange());
    if (enemy)
    {
      Transform lightning = Instantiate(data.prefab, player.position, Quaternion.identity, transform);
      BlueOvalWeapon lightningWeapon = lightning.GetComponent<BlueOvalWeapon>();
      var weaponLevelData = GetLevelData();
      lightningWeapon.Init(enemy.transform, weaponLevelData);
      Destroy(lightning.gameObject, weaponLevelData.duration);
    }
  }
}
