using System;
using UnityEngine;

/// <summary>
/// 单个实体的序列帧动画配置资源。
/// 一个资源对应一个敌人、玩家或掉落物，其中包含该实体可播放的多个动作 Clip。
/// </summary>
[CreateAssetMenu(fileName = "NewAnimFrameConfig", menuName = "Game/Animation/Anim FrameConfig")]
public sealed class AnimFrameConfig : ScriptableObject
{
  [SerializeField, Tooltip("该实体的配置 ID，用于查找对应的动画配置。")]
  private int configId;
  [SerializeField, Tooltip("该实体的配置名称，用于查找对应的动画配置。")]
  private string configName;

  [SerializeField, Tooltip("未指定动作时播放的 Clip 名称；留空时由播放器选择第一组有效 Clip。")]
  private string defaultClipName;

  [SerializeField, Tooltip("该实体拥有的全部序列帧动作 Clip，例如 Idle、Run、Hit。")]
  private AnimFrameClip[] clips = Array.Empty<AnimFrameClip>();

  /// <summary>未指定动作时使用的默认 Clip 名称。</summary>
  public string DefaultClipName => defaultClipName;

  /// <summary>该实体配置的全部动作 Clip。</summary>
  public AnimFrameClip[] Clips => clips;
}
