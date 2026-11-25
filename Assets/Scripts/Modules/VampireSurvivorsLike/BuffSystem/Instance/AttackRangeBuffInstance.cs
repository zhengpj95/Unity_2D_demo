using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class AttackRangeBuffInstance : BuffInstance
  {
    private float rate;
    public float rangeMultiplier = 0f;

    public AttackRangeBuffInstance(AttackRangeBuffSO data) : base(data)
    {
      rate = data.rangeRate;
    }

    public override void OnAdd()
    {
      rangeMultiplier += rate;
    }

    public override void OnRemove()
    {
      rangeMultiplier -= rate * stack;
    }

    /// <summary>旧攻击范围 Buff 在新属性系统中作为武器索敌范围百分比修正。</summary>
    public override PlayerStatModifier GetStatModifier(PlayerStat stat)
    {
      return stat == PlayerStat.TargetingRange
        ? new PlayerStatModifier(0f, rangeMultiplier)
        : default;
    }
  }

}
