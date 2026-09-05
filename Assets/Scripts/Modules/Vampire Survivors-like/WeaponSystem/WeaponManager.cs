using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class WeaponManager : SingletonMono<WeaponManager>
  {
    // WeaponManager 持有当前场景 Player 创建的武器控制器，重开时必须丢弃旧 Player 引用并随场景重建。
    protected override bool PersistAcrossScenes => false;

    [Header("近战环绕型武器")]
    public WeaponSO sawSO;

    [Header("投射型武器")]
    public WeaponSO arrowSO;
    public WeaponSO bulletbSO;

    [Header("区域型武器")]
    public WeaponSO blueOvalSO;
    public WeaponSO lightningSO;
    public WeaponSO fireSO;

    [Header("Weapon Slots")]
    // 控制玩家最多同时拥有的武器数量，NewWeapon 候选会据此过滤。
    [SerializeField, Min(1)] private int maxWeaponSlots = 3;

    private readonly List<WeaponController> weaponControllers = new List<WeaponController>();

    /// <summary>当前已创建的武器控制器数量。</summary>
    public int WeaponCount => weaponControllers.Count;
    /// <summary>玩家本局允许拥有的最大武器数量。</summary>
    public int MaxWeaponSlots => maxWeaponSlots;

    /// <summary>判断指定武器是否已经创建了运行时控制器。</summary>
    public bool HasWeapon(WeaponSO data)
    {
      return weaponControllers.Exists(x => x.WeaponData == data);
    }

    /// <summary>判断指定武器能否作为新武器加入当前槽位。</summary>
    public bool CanAddWeapon(WeaponSO data)
    {
      return data != null && !HasWeapon(data) && weaponControllers.Count < maxWeaponSlots;
    }

    /// <summary>判断指定武器是否存在且尚未达到其 levels 最大等级。</summary>
    public bool CanUpgrade(WeaponSO data)
    {
      WeaponController controller = weaponControllers.Find(x => x.WeaponData == data);
      return controller != null && controller.CanLevelUp;
    }

    /// <summary>读取指定武器的运行时等级；未拥有时返回 0。</summary>
    public int GetWeaponLevel(WeaponSO data)
    {
      WeaponController controller = weaponControllers.Find(x => x.WeaponData == data);
      return controller == null ? 0 : controller.CurrentLevel;
    }

    /// <summary>返回 Inspector 中配置的全部武器资源，供 UpgradeManager 构建候选池。</summary>
    public IEnumerable<WeaponSO> GetConfiguredWeapons()
    {
      yield return sawSO;
      yield return arrowSO;
      yield return bulletbSO;
      yield return blueOvalSO;
      yield return lightningSO;
      yield return fireSO;
    }

    /// <summary>兼容旧调用方：已拥有则升级，否则尝试新增。</summary>
    public void AddOrUpgrade(WeaponSO soData)
    {
      TryAddOrUpgrade(soData);
    }

    /// <summary>执行一次新增或升级，并返回是否真正应用成功。</summary>
    public bool TryAddOrUpgrade(WeaponSO soData)
    {
      if (!soData)
      {
        Debug.LogError($"AddOrUpgrade: WeaponSO is null");
        return false;
      }
      if (HasWeapon(soData))
      {
        return LevelUpWeapon(soData);
      }

      if (!CanAddWeapon(soData))
      {
        Debug.LogWarning($"Cannot add weapon '{soData.weaponId}': weapon slots are full.");
        return false;
      }

      return AddWeapon(soData);
    }

    private bool LevelUpWeapon(WeaponSO data)
    {
      WeaponController controller = weaponControllers.Find(x => x.WeaponData == data);
      return controller != null && controller.LevelUp();
    }

    private bool AddWeapon(WeaponSO data)
    {
      var weaponObj = new GameObject(data.weaponId);
      // 这里决定运行时武器控制器的父节点：WeaponManager/WeaponArrow、WeaponManager/WeaponBulletb 等。
      // 场景中只需要保留 WeaponManager，具体武器节点会在首次获得武器时动态创建到这里。
      weaponObj.transform.SetParent(transform);

      var weapon = weaponObj.AddComponent(GetWeaponType(data)) as WeaponController;
      if (weapon != null)
      {
        weapon.Init(data);
        weaponControllers.Add(weapon);
        return true;
      }

      Destroy(weaponObj);
      return false;
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

}
