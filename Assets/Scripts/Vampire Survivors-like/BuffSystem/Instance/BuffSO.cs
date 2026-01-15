using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BuffSO : ScriptableObject
{
  public string buffId;
  // Buff 类型
  public BuffStackType stackType;
  // 持续时间
  public float duration;
  // 最大叠加层数
  public int maxStack = 1;
  // 是百分比(10%)还是固定值(+10)
  public bool isPercent;

  public abstract BuffInstance CreateInstance(GameObject target);
}
