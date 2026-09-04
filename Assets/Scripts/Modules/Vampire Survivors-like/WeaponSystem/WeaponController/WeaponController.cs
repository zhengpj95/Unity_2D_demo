using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  /**
   * 武器控制器基类
   */
  public abstract class WeaponController : MonoBehaviour
  {
    protected WeaponSO data;
    protected int level = 1;
    protected float timer;

    /// <summary>该控制器使用的只读武器配置。</summary>
    public WeaponSO WeaponData => data;
    /// <summary>当前运行时等级，升级时只修改控制器实例。</summary>
    public int CurrentLevel => level;
    /// <summary>由该武器自己的 levels 数组决定的最大等级。</summary>
    public int MaxLevel => data?.levels?.Length ?? 0;
    /// <summary>是否还存在可应用的下一等级数据。</summary>
    public bool CanLevelUp => level < MaxLevel;

    public Transform player;

    private void Start()
    {
      player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // 初始化武器
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

    // 升级武器
    /// <summary>提升一次运行时等级并返回是否成功。</summary>
    public bool LevelUp()
    {
      if (!CanLevelUp)
      {
        Debug.Log($"武器等级已达上限：{level}");
        return false;
      }
      level++;
      return true;
    }

    // 当前武器等级数据
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

    // 攻击范围
    public float GetAttackRange()
    {
      return player.GetComponent<Hero>().AttackRange;
    }
  }

}
