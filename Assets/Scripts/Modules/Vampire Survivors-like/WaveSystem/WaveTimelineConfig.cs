using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VampireSurvivorsLike
{
  /// <summary>描述一个可复用 Wave 在关卡时间轴中的持续时间。</summary>
  [Serializable]
  public sealed class WaveTimelineEntry
  {
    [Tooltip("本时间段使用的可复用 Wave 配置。")]
    [FormerlySerializedAs("wave")]
    [SerializeField] private WaveSpawnConfig waveSpawnConfig;
    [Tooltip("该时间段持续的秒数；无限持续时忽略此值。")]
    [SerializeField, Min(0.1f)] private float duration = 60f;
    [Tooltip("是否持续到本局结束；勾选后必须作为时间轴最后一项。")]
    [SerializeField] private bool isInfinite;

    /// <summary>本时间段使用的 Wave 配置。</summary>
    public WaveSpawnConfig WaveSpawnConfig => waveSpawnConfig;
    /// <summary>本时间段的持续秒数；无限持续时不使用。</summary>
    public float Duration => duration;
    /// <summary>是否持续到本局结束。</summary>
    public bool IsInfinite => isInfinite;
    /// <summary>时间段是否包含可执行的 Wave 和有效时长。</summary>
    public bool IsValid => waveSpawnConfig != null && (isInfinite || duration > 0f);
  }

  /// <summary>按列表顺序组织一局战斗的 Wave 时间轴，起止时间由各段 Duration 自动累加。</summary>
  [CreateAssetMenu(fileName = "WaveTimeline", menuName = "Survivor/Wave/Wave Timeline")]
  public sealed class WaveTimelineConfig : ScriptableObject
  {
    [Tooltip("按执行顺序配置 Wave；无限持续项必须放在最后。")]
    [FormerlySerializedAs("waves")]
    [SerializeField] private List<WaveTimelineEntry> waveEntries = new List<WaveTimelineEntry>();

    /// <summary>按执行顺序排列的 Wave 时间段。</summary>
    public IReadOnlyList<WaveTimelineEntry> WaveEntries => waveEntries;

    private void OnValidate()
    {
      if (waveEntries == null || waveEntries.Count == 0)
      {
        Debug.LogWarning($"[WaveTimelineConfig] '{name}' 至少需要一个 Wave。", this);
        return;
      }

      for (int i = 0; i < waveEntries.Count; i++)
      {
        WaveTimelineEntry entry = waveEntries[i];
        if (entry == null || !entry.IsValid)
          Debug.LogWarning($"[WaveTimelineConfig] '{name}' 的 Wave[{i}] 配置无效。", this);

        if (entry != null && entry.IsInfinite && i != waveEntries.Count - 1)
          Debug.LogWarning($"[WaveTimelineConfig] '{name}' 的无限 Wave 必须放在最后。", this);
      }
    }
  }
}
