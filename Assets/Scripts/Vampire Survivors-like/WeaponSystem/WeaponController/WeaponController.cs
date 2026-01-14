using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponController : MonoBehaviour
{
  protected WeaponSO data;
  protected int level = 1;
  protected float timer;

  public WeaponSO WeaponData => data;

  public Transform player;

  private void Start()
  {
    player = GameObject.FindGameObjectWithTag("Player").transform;
  }


  public virtual void Init(WeaponSO weaponSO)
  {
    this.data = weaponSO;
  }

  protected virtual void Update()
  {
    timer += Time.deltaTime;
    var levelData = GetLevelData();
    if (timer >= levelData.fireInterval)
    {
      timer = 0;
      Fire();
    }
  }

  protected abstract void Fire();

  public void LevelUp()
  {
    if (level >= data.levels.Length)
    {
      Debug.Log($"武器等级已达上限：{level}");
      return;
    }
    level++;
  }

  public WeaponLevelData GetLevelData()
  {
    if (data?.levels?.Length > 0)
    {
      if (level > data.levels.Length)
      {
        return data.levels[data.levels.Length - 1];
      }
      return data.levels[Mathf.Max(0, level - 1)];
    }
    return new WeaponLevelData
    {
      level = 1,
      damage = 1,
      count = 1,
      range = 1,
      fireInterval = 1,
    };
  }
}
