using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  /// <summary>
  /// 玩家临时 Buff 的生命周期和叠加处理器。
  /// 它只提供临时 PlayerStatModifier，不保存三选一产生的永久属性升级。
  /// </summary>
  public class BuffHandler : MonoBehaviour
  {
    public List<BuffInstance> buffs = new List<BuffInstance>();

    /// <summary>添加一个临时 Buff，并按配置执行刷新、叠加或替换规则。</summary>
    public void AddBuff(BuffSO data)
    {
      if (data == null)
      {
        Debug.LogWarning("[BuffHandler] Cannot add a null BuffSO.", this);
        return;
      }

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
        case BuffStackType.Refresh:
          exist.RefreshDuration(data.duration);
          break;
        case BuffStackType.Stack:
          if (exist.stack < data.maxStack)
          {
            exist.OnAdd(); // 效果加上去
            exist.stack = Mathf.Min(exist.stack + 1, data.maxStack);
          }
          break;
        case BuffStackType.Replace:
          exist.OnRemove();
          buffs.Remove(exist);
          AddBuff(data);
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

    /// <summary>
    /// 汇总所有活跃 Buff 对指定玩家属性的临时修正。
    /// 该方法只遍历现有列表，不创建临时候选集合。
    /// </summary>
    public PlayerStatModifier GetStatModifier(PlayerStat stat)
    {
      PlayerStatModifier result = default;
      for (int i = 0; i < buffs.Count; i++)
      {
        BuffInstance buff = buffs[i];
        if (buff != null)
          result = result.Combine(buff.GetStatModifier(stat));
      }
      return result;
    }

    /// <summary>兼容旧调用入口：返回移动速度的临时百分比修正。</summary>
    public float GetMoveSpeedMultiplier()
    {
      return GetStatModifier(PlayerStat.MoveSpeed).Percent;
    }

    /// <summary>兼容旧调用入口：当前“攻击范围 Buff”实际映射为武器索敌范围百分比。</summary>
    public float GetAttackRangeMultiplier()
    {
      return GetStatModifier(PlayerStat.TargetingRange).Percent;
    }
  }

}
