using UnityEngine;

/// <summary>
/// 场景 SpriteRenderer 的序列帧动画组件，适用于敌人、掉落物和世界空间特效。
/// 播放速度受 Time.timeScale 影响，游戏暂停时动画也会暂停。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class AnimSprite : AnimFrameBase
{
  [SerializeField, Tooltip("显示序列帧的 SpriteRenderer；为空时自动使用当前节点上的组件。")]
  private SpriteRenderer target;

  protected override float DeltaTime => Time.deltaTime;

  protected override void ResolveTarget()
  {
    if (target == null)
    {
      target = GetComponent<SpriteRenderer>();
    }
  }

  protected override void SetSprite(Sprite sprite)
  {
    if (target != null)
    {
      target.sprite = sprite;
    }
  }
}

