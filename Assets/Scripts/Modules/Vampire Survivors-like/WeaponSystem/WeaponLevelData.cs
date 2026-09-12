using UnityEngine;

namespace VampireSurvivorsLike
{

  [System.Serializable]
  /// <summary>
  /// 单个武器等级的只读配置。运行时等级以 WeaponSO.levels 的数组下标为准，
  /// level 仅用于 Inspector 标识，避免配置编号影响既有存档与升级流程。
  /// </summary>
  public class WeaponLevelData
  {
    [Tooltip("Inspector 显示用的等级标识；实际等级由 WeaponSO.levels 的数组下标决定")]
    public int level;
    [Tooltip("Arrow、Bulletb 的飞行速度，以及 Saw 的环绕角速度；静态范围效果填 0")]
    public float speed;
    [Tooltip("武器伤害值")]
    public int damage;
    [Tooltip("持续范围伤害间隔；目前仅 Fire 使用，单次命中或碰撞型效果填 0")]
    public float damageInterval;
    [Tooltip("每次触发创建的独立攻击对象数量；0 兼容旧配置并按 1 处理")]
    public int count;
    [Tooltip("Arrow、Bulletb、BlueOval、Lightning、Fire 的选敌范围倍率；Saw 为实际环绕半径；0 按旧配置处理")]
    public float range;

    [Tooltip("武器触发间隔，运行时最小按 0.01 秒处理以避免无间隔重复触发")]
    public float fireInterval;
    [Tooltip("攻击对象存活时间；到期后归还对象池")]
    // 投射物应在飞出当前相机视野后再超时销毁；当前 Size=10 使用 20 秒作为余量。
    public float duration;
  }
}
