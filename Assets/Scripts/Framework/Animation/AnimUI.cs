using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Image 的序列帧动画组件，适用于动态图标、加载动画和界面特效。
/// 使用不受 Time.timeScale 影响的时间，游戏暂停时仍可继续播放。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class AnimUI : AnimFrameBase
{
  [SerializeField, Tooltip("显示序列帧的 UI Image；为空时自动使用当前节点上的组件。")]
  private Image target;

  protected override float DeltaTime => Time.unscaledDeltaTime;

  protected override void ResolveTarget()
  {
    if (target == null)
    {
      target = GetComponent<Image>();
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

