using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewWeaponManager : SingletonMono<NewWeaponManager>
{
  public WeaponSO bulletbPrefab;
  public WeaponSO sawPrefab;
  public WeaponSO arrowPrefab;
  public WeaponSO firePrefab;
  public WeaponSO buleOvalPrefab;
  public WeaponSO lightningPrefab;

  private readonly List<WeaponController> weaponControllers = new List<WeaponController>();

  public bool HasWeapon(WeaponSO data)
  {
    return weaponControllers.Exists(x => x.WeaponData == data);
  }

  public void AddOrUpgrade(WeaponSO data)
  {
    if (HasWeapon(data))
    {
      LevelUpWeapon(data);
    }
    else
    {
      AddWeapon(data);
    }
  }

  private void LevelUpWeapon(WeaponSO data)
  {
    weaponControllers.Find(x => x.WeaponData == data).LevelUp();
  }

  private void AddWeapon(WeaponSO data)
  {
    var weaponObj = new GameObject(data.weaponId);
    weaponObj.transform.SetParent(transform);

    var weapon = weaponObj.AddComponent(GetWeaponType(data)) as WeaponController;
    if (weapon != null)
    {
      weapon.Init(data);
      weaponControllers.Add(weapon);
    }
  }

  private System.Type GetWeaponType(WeaponSO data)
  {
    return data.weaponId switch
    {
      "WeaponSaw" => typeof(SawController),
      "WeaponBulletb" => typeof(BulletbController),
      _ => null,
    };
  }
}
