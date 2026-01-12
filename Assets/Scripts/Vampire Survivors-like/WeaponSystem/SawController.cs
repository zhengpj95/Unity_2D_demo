using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SawController : WeaponController
{
  private Transform player;

  private void Start()
  {
    player = GameObject.FindGameObjectWithTag("Player").transform;
  }

  protected override void Fire()
  {
    var saw = Instantiate(data.prefab, player.position, Quaternion.identity, player);
    Destroy(saw.gameObject, GetLevelData().duration);
  }
}
