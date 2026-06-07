using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class SawController : WeaponController
  {
    protected override void Fire()
    {
      var saw = Instantiate(data.prefab, player.position, Quaternion.identity, player);
      var sawScript = saw.GetComponent<SawWeapon>();
      var levelData = GetLevelData();
      sawScript.Init(levelData);
      Destroy(saw.gameObject, levelData.duration);
    }
  }

}