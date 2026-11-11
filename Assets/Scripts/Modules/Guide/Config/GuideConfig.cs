using System.Collections.Generic;
using UnityEngine;

/// <summary>以 ScriptableObject 保存的一套完整引导配置。</summary>
[CreateAssetMenu(fileName = "GuideConfig", menuName = "Guide/Guide Config")]
public sealed class GuideConfig : ScriptableObject
{
  /// <summary>此套引导的业务标识。</summary>
  public int GuideId;

  /// <summary>便于 Inspector 识别的引导名称。</summary>
  public string GuideName;

  /// <summary>按 Inspector 顺序执行的步骤列表。</summary>
  public List<GuideStepConfig> Steps = new();
}
