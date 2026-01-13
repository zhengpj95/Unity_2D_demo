using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : SingletonMono<WeaponManager>
{
  [Header("近战环绕型武器")]
  public WeaponSO sawSO;

  [Header("投射型武器")]
  public WeaponSO arrowSO;
  public WeaponSO bulletbSO;

  [Header("区域型武器")]
  public WeaponSO blueOvalSO;
  public WeaponSO lightningSO;
  public WeaponSO fireSO;

  private readonly List<WeaponController> weaponControllers = new List<WeaponController>();

  public bool HasWeapon(WeaponSO data)
  {
    return weaponControllers.Exists(x => x.WeaponData == data);
  }

  public void AddOrUpgrade(WeaponSO soData)
  {
    if (!soData)
    {
      Debug.LogError($"AddOrUpgrade: WeaponSO is null");
      return;
    }
    if (HasWeapon(soData))
    {
      LevelUpWeapon(soData);
    }
    else
    {
      AddWeapon(soData);
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
      "WeaponLightning" => typeof(LightningController),
      "WeaponFire" => typeof(FireController),
      "WeaponArrow" => typeof(ArrowController),
      "WeaponBlueOval" => typeof(BlueOvalController),
      _ => null,
    };
  }
}
