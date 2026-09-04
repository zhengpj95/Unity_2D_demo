using UnityEngine;

namespace VampireSurvivorsLike {

  [System.Serializable]
  public class WeaponLevelData
  {
    [Tooltip("武器等级")]
    public int level;
    [Tooltip("武器移动速度")]
    public float speed;
    [Tooltip("武器伤害值")]
    public int damage;
    [Tooltip("伤害间隔，范围型伤害需要用到")]
    public float damageInterval;
    [Tooltip("武器发射数量")]
    public int count;
    [Tooltip("武器攻击范围")]
    public float range;

    [Tooltip("武器触发间隔")]
    public float fireInterval;
    [Tooltip("武器持续时间")]
    // 投射物应在飞出当前相机视野后再超时销毁；当前 Size=10 使用 20 秒作为余量。
    public float duration;
  }
}
