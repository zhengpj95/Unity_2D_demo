using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SawController : WeaponController
{
  protected override void Fire()
  {
    var saw = Instantiate(data.prefab, player.position, Quaternion.identity, player);
    Destroy(saw.gameObject, GetLevelData().duration);
  }
}
