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
    // 当前控制器创建且尚未回收的攻击对象；用于场景重开和控制器销毁时统一归还对象池。
    private readonly List<PooledWeaponEffect> _activeEffects = new List<PooledWeaponEffect>();
    // 直线投射物选敌时复用的已占用目标集合；每个武器控制器实例独立持有，避免不同武器互相阻塞。
    private readonly HashSet<Transform> _occupiedProjectileTargets = new HashSet<Transform>();

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

    /// <summary>
    /// 为直线弓箭或子弹选择目标：优先选择当前未被本控制器其他飞行投射物瞄准的最近敌人。
    /// 范围内所有敌人均已被瞄准时返回 null，本次不生成投射物，避免单敌场景浪费多枚直线攻击。
    /// </summary>
    protected EnemyChasing GetClosestProjectileTarget()
    {
      _occupiedProjectileTargets.Clear();
      for (int i = 0; i < _activeEffects.Count; i++)
      {
        ArrowWeapon projectile = _activeEffects[i] as ArrowWeapon;
        if (projectile != null && projectile.LaunchTarget != null)
          _occupiedProjectileTargets.Add(projectile.LaunchTarget);
      }

      float attackRange = GetAttackRange();
      EnemyChasing enemy = EnemyDirector.Instance.GetClosestExcluding(
        player.position, attackRange, _occupiedProjectileTargets);
      return enemy;
    }

    /// <summary>
    /// 从通用对象池取出一种武器攻击对象，并恢复原有的运行时父节点关系。
    /// </summary>
    /// <typeparam name="T">Prefab 根节点上应存在的池化武器效果组件类型。</typeparam>
    /// <param name="prefab">WeaponSO 中配置的攻击对象 Prefab。</param>
    /// <param name="position">攻击对象的世界坐标。</param>
    /// <param name="rotation">攻击对象的初始世界旋转。</param>
    /// <param name="parent">出池后使用的运行时父节点；为 null 时保持无父节点。</param>
    protected T SpawnPooledEffect<T>(Transform prefab, Vector3 position, Quaternion rotation, Transform parent)
      where T : PooledWeaponEffect
    {
      if (prefab == null)
      {
        Debug.LogWarning($"[WeaponController] '{data?.weaponId}' 缺少攻击对象 Prefab。", this);
        return null;
      }

      GameObject effectObject = PoolManager.Instance.Alloc(prefab.gameObject, position, rotation);
      if (effectObject == null)
        return null;

      if (parent != null)
        effectObject.transform.SetParent(parent, true);

      T effect = effectObject.GetComponent<T>();
      if (effect != null)
        return effect;

      Debug.LogError($"[WeaponController] Prefab '{prefab.name}' 缺少 {typeof(T).Name} 组件。", prefab);
      PoolManager.Instance.Free(effectObject);
      return null;
    }

    /// <summary>登记一个刚完成 Init 的攻击对象，供回收路径统一管理。</summary>
    public void RegisterEffect(PooledWeaponEffect effect)
    {
      if (effect != null && !_activeEffects.Contains(effect))
        _activeEffects.Add(effect);
    }

    /// <summary>攻击对象入池时解除登记，避免控制器持有已失效的效果引用。</summary>
    public void UnregisterEffect(PooledWeaponEffect effect)
    {
      _activeEffects.Remove(effect);
    }

    /// <summary>
    /// 回收当前武器创建的全部活跃攻击对象。
    /// 场景重开前调用，确保对象被移动到持久化 PoolRoot，而不是随旧场景销毁。
    /// </summary>
    public void RecycleActiveEffects()
    {
      for (int i = _activeEffects.Count - 1; i >= 0; i--)
      {
        PooledWeaponEffect effect = _activeEffects[i];
        if (effect != null)
          effect.Recycle();
      }

      _activeEffects.Clear();
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

    protected virtual void OnDestroy()
    {
      // WeaponManager 因场景重载被销毁时的兜底，防止仍挂在控制器下的攻击对象随场景丢失。
      RecycleActiveEffects();
    }
  }

}
