using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponController : MonoBehaviour
{
  protected WeaponSO data;
  protected int level = 1;
  protected float timer;

  public WeaponSO WeaponData => data;

  public virtual void Init(WeaponSO weaponSO)
  {
    this.data = weaponSO;
  }

  protected virtual void Update()
  {
    timer += Time.deltaTime;
    if (timer >= data.fireInterval)
    {
      timer = 0;
      Fire();
    }
  }

  protected abstract void Fire();

  public void LevelUp()
  {
    level++;
  }
}
