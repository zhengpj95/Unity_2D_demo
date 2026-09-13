using System;
using UnityEngine;

/// <summary>
/// 一组序列帧动画配置，保存动作名称、帧图片、帧率和循环规则。
/// 数据直接序列化在使用它的组件上，不额外创建 ScriptableObject 资源。
/// </summary>
[Serializable]
public sealed class AnimFrameData
{
  [SerializeField, Tooltip("代码播放动画时使用的名称，例如 Move、Hit、Death。")]
  private string animName = "Default";

  [SerializeField, Tooltip("按播放顺序排列的 Sprite 帧。")]
  private Sprite[] frames = Array.Empty<Sprite>();

  [SerializeField, Min(0.01f), Tooltip("每秒播放的帧数。")]
  private float frameRate = 12f;

  [SerializeField, Tooltip("播放到最后一帧后是否从第一帧继续。")]
  private bool loop = true;

  [SerializeField, Tooltip("每次开始播放时是否随机选择起始帧，适合错开大量循环动画。")]
  private bool randomStartFrame;

  /// <summary>代码查找和播放该动画时使用的名称。</summary>
  public string AnimName => animName;

  /// <summary>动画包含的有效帧数量。</summary>
  public int FrameCount => frames == null ? 0 : frames.Length;

  /// <summary>每秒播放的帧数，配置异常时最小返回 0.01。</summary>
  public float FrameRate => Mathf.Max(0.01f, frameRate);

  /// <summary>播放到最后一帧后是否循环。</summary>
  public bool Loop => loop;

  /// <summary>开始播放时是否随机选择起始帧。</summary>
  public bool RandomStartFrame => randomStartFrame;

  /// <summary>
  /// 获取指定位置的帧图片。
  /// </summary>
  /// <param name="index">从零开始的帧下标。</param>
  /// <returns>对应的 Sprite；下标无效时返回 null。</returns>
  public Sprite GetFrame(int index)
  {
    if (frames == null || index < 0 || index >= frames.Length)
    {
      return null;
    }

    return frames[index];
  }
}

