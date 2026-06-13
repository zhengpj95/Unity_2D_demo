using System.Collections.Generic;
using System.Resources;
using UnityEngine;

/**
 * 动画组件
 */
public class AnimationComponent : BaseComponent
{
  private readonly Animator _animator;
  private readonly Dictionary<EntityState, int> _hashes;

  public AnimationComponent(Animator animator)
  {
    _animator = animator;
    _hashes = new Dictionary<EntityState, int>()
    {
      { EntityState.Idle, Animator.StringToHash("CharacterIdle") }, // 要对应模型的统一动作命名，这里暂时命名是如此 TODO
      { EntityState.Run, Animator.StringToHash("CharacterRun") },
      { EntityState.Jump, Animator.StringToHash("Jump") },
      { EntityState.Fall, Animator.StringToHash("Fall") },
      { EntityState.Attack, Animator.StringToHash("CharacterAttack") },
      { EntityState.Hurt, Animator.StringToHash("Hurt") },
      { EntityState.Death, Animator.StringToHash("Death") },
    };
  }

  public void Play(EntityState state, float fadeTime = 0.1f)
  {
    _animator.CrossFade(_hashes[state], fadeTime);
  }

  public bool IsPlaying(EntityState state)
  {
    return _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == _hashes[state];
  }

  public bool IsCurrentAnimFinished()
  {
    AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
    return state.normalizedTime >= 1f && !_animator.IsInTransition(0);
  }
}