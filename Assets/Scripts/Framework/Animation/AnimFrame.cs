using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 序列帧动画显示组件，通过 Inspector 拖入的渲染目标同时支持场景与 UI。
/// 场景目标受 Time.timeScale 影响；UI 目标使用 unscaledDeltaTime，游戏暂停时仍可播放。
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimFrame : AnimFrameBase
{
  [SerializeField, Tooltip("场景序列帧的显示目标；适用于敌人、掉落物与特效，留空时自动取当前节点的 SpriteRenderer。")]
  private SpriteRenderer target;

  [SerializeField, Tooltip("UI 序列帧的显示目标；适用于界面图标与引导光标，留空时自动取当前节点的 Image。")]
  private Image image;

  // 两个字段二选一：拖入 Image 时按 UI 时间推进，否则按场景时间推进。
  protected override float DeltaTime => image != null ? Time.unscaledDeltaTime : Time.deltaTime;

  /// <summary>优先保留 Inspector 拖入的目标，均为空时才按组件类型自动解析。</summary>
  protected override void ResolveTarget()
  {
    if (target != null || image != null)
    {
      return;
    }

    target = GetComponent<SpriteRenderer>();
    image = GetComponent<Image>();
  }

  /// <summary>把当前帧写入实际显示目标；UI 目标优先，避免两个字段同时有值时行为含糊。</summary>
  protected override void SetSprite(Sprite sprite)
  {
    if (image != null)
    {
      image.sprite = sprite;
      return;
    }

    if (target != null)
    {
      target.sprite = sprite;
    }
  }
}
