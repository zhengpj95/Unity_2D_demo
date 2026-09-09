using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public abstract class BuffInstance
  {
    protected BuffSO data;
    protected GameObject target;

    public int stack;
    public float timeLeft;
    public BuffSO Data => data;

    protected BuffInstance(BuffSO data)
    {
      this.data = data;
      stack = 1;
      timeLeft = data.duration;
    }

    public virtual void OnAdd() { }
    public virtual void OnRemove() { }
    public virtual void OnTick(float deltaTime) { }

    /// <summary>
    /// 返回当前 Buff 对指定玩家属性提供的临时修正。
    /// 不影响该属性时返回 default，BuffHandler 会统一合并所有活跃 Buff。
    /// </summary>
    public virtual PlayerStatModifier GetStatModifier(PlayerStat stat)
    {
      return default;
    }

    public void Init(GameObject target, int stack = 1)
    {
      this.target = target;
      this.stack = stack;
    }

    public void Update(float deltaTime)
    {
      timeLeft -= deltaTime;
      OnTick(deltaTime);
    }

    /// <summary>刷新 Buff 剩余时间；未传值时使用 BuffSO.duration。</summary>
    public void RefreshDuration(float? duration)
    {
      timeLeft = duration ?? data.duration;
    }

    /// <summary>Buff 是否已经到期。</summary>
    public bool IsExpired => timeLeft <= 0;
  }

}
