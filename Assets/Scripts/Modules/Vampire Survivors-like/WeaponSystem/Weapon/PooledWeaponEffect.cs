using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// 武器投射物和范围特效的对象池生命周期基类。
  /// 由 WeaponController 登记所有活跃效果；命中、超时或场景重开时统一归还 PoolManager。
  /// </summary>
  public abstract class PooledWeaponEffect : MonoBehaviour, IPoolable
  {
    private WeaponController _owner;
    private float _remainingLifetime;
    private bool _isActiveEffect;
    private Animator[] _animators;

    /// <summary>效果是否已完成初始化且仍处于本次出池生命周期。</summary>
    protected bool IsActiveEffect => _isActiveEffect;

    private void Awake()
    {
      // Animator 仅在实例创建时缓存；出池时 Rebind，避免爆炸/闪电等效果从上次播放进度继续。
      _animators = GetComponentsInChildren<Animator>(true);
    }

    /// <summary>
    /// 开始一次效果生命周期，并登记到创建它的武器控制器。
    /// </summary>
    /// <param name="owner">负责场景重开和销毁时统一回收效果的武器控制器。</param>
    /// <param name="duration">效果存活时间；0 表示下一帧回收，时间受 Time.timeScale 影响。</param>
    protected void BeginEffect(WeaponController owner, float duration)
    {
      _owner = owner;
      _remainingLifetime = Mathf.Max(0f, duration);
      _isActiveEffect = true;
      _owner?.RegisterEffect(this);
    }

    /// <summary>
    /// 推进效果超时计时器；返回 true 表示本帧已回收，调用方应停止后续逻辑。
    /// </summary>
    protected bool TryRecycleWhenExpired()
    {
      if (!_isActiveEffect)
        return true;

      _remainingLifetime -= Time.deltaTime;
      if (_remainingLifetime > 0f)
        return false;

      Recycle();
      return true;
    }

    /// <summary>将当前效果归还通用对象池；重复调用不会重复入池。</summary>
    public void Recycle()
    {
      if (!_isActiveEffect || !gameObject.activeSelf)
        return;

      PoolManager.Instance.Free(gameObject);
    }

    /// <summary>对象出池时清空上一轮效果状态，等待控制器调用具体 Init。</summary>
    public void OnAlloc()
    {
      _owner = null;
      _remainingLifetime = 0f;
      _isActiveEffect = false;
      ResetAnimators();
      ResetEffectState();
    }

    /// <summary>对象入池时注销所属控制器并清空运行时引用，避免下一轮复用旧状态。</summary>
    public void OnFree()
    {
      _owner?.UnregisterEffect(this);
      _owner = null;
      _remainingLifetime = 0f;
      _isActiveEffect = false;
      ResetEffectState();
    }

    /// <summary>由具体效果重置目标、命中列表、方向、计时器等本轮临时状态。</summary>
    protected abstract void ResetEffectState();

    /// <summary>让池化 Animator 从初始状态重新播放，动画事件不会继承上一轮进度。</summary>
    private void ResetAnimators()
    {
      if (_animators == null)
        return;

      for (int i = 0; i < _animators.Length; i++)
      {
        Animator animator = _animators[i];
        if (animator == null)
          continue;

        animator.Rebind();
        animator.Update(0f);
      }
    }
  }
}
