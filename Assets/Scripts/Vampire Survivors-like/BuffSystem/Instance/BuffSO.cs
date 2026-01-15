using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BuffSO : ScriptableObject
{
  public string buffId;
  public BuffStackType stackType;
  // 持续时间
  public float duration;
  // 最大叠加层数
  public int maxStack = 1;

  public abstract BuffInstance CreateInstance(GameObject target);
}
