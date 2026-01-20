using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * buff处理类，需要挂载在需要处理buff的物体上
 */
public class BuffHandler : MonoBehaviour
{
  public List<BuffInstance> buffs = new List<BuffInstance>();

  public void AddBuff(BuffSO data)
  {

    var exist = buffs.Find(b => b.Data.GetType() == data.GetType());
    if (exist != null)
    {
      Debug.Log($"buff已存在，叠加buff效果：{data.name}");
      HandleStack(exist, data);
      return;
    }

    Debug.Log($"添加buff：{data.name}");
    var instance = data.CreateInstance();
    instance.Init(gameObject, 1);
    instance.OnAdd();
    buffs.Add(instance);
  }

  private void HandleStack(BuffInstance exist, BuffSO data)
  {
    switch (data.stackType)
    {
      case BuffStackType.MoveSpeed:
        // 时间重置，效果叠加
        if (exist.stack < data.maxStack)
        {
          exist.stack = Mathf.Min(exist.stack + 1, data.maxStack);
          exist.OnAdd();
          exist.RefreshDuration();
        }
        break;
      case BuffStackType.Range:
        // 多个buff叠加
        if (exist.stack < data.maxStack)
        {
          var instance = data.CreateInstance();
          instance.Init(gameObject, 1);
          instance.OnAdd();
          buffs.Add(instance);// 多个同buff存在
        }
        break;
      default:
        Debug.LogWarning($"未处理的buff类型：{data.stackType}");
        break;
    }
  }

  private void Update()
  {
    // 遍历所有buff，更新时间
    for (int i = buffs.Count - 1; i >= 0; i--)
    {
      var buff = buffs[i];
      buff.Update(Time.deltaTime);
      if (buff.IsExpired)
      {
        buff.OnRemove();
        buffs.RemoveAt(i);
      }
    }
  }

  // 获取移动速度
  public float GetMoveSpeedMultiplier()
  {
    float moveSpeedMultiplier = 0f;
    foreach (var buff in buffs)
    {
      if (buff is MoveSpeedBuffInstance moveSpeedBuff)
      {
        moveSpeedMultiplier += moveSpeedBuff.moveSpeedMultiplier;
      }
    }
    return moveSpeedMultiplier;
  }

  // 获取攻击范围
  public float GetAttackRangeMultiplier()
  {
    float rangeMultiplier = 0f;
    foreach (var buff in buffs)
    {
      if (buff is AttackRangeBuffInstance attackRangeBuff)
      {
        rangeMultiplier += attackRangeBuff.rangeMultiplier;
      }
    }
    return rangeMultiplier;
  }
}
