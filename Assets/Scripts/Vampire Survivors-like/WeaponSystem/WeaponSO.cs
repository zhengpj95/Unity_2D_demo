using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon/WeaponSO")]
public class WeaponSO : ScriptableObject
{
  public string weaponId;
  public Transform prefab;
  public Sprite icon;
  public string weaponName;

  public WeaponLevelData[] levels;
}
