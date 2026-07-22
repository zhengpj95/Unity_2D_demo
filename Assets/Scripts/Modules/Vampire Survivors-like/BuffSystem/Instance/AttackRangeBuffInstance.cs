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
  }

}