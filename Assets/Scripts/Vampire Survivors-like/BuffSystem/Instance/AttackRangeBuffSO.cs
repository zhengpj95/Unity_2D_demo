using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Buff/AttackRangeBuffSO")]
public class AttackRangeBuffSO : BuffSO
{
  [Tooltip("攻击范围增加率【0.2f 表示增加20%】"), Range(0.05f, 0.5f)]
  public float rangeRate = 0.2f;

  public override BuffInstance CreateInstance()
  {
    return new AttackRangeBuffInstance(this);
  }
}
