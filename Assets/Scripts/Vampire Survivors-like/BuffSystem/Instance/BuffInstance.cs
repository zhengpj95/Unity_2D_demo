using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

  // 刷新过期时间
  public void RefreshDuration(float? duration)
  {
    timeLeft = duration ?? data.duration;
  }

  // 过期了否
  public bool IsExpired => timeLeft <= 0;
}
