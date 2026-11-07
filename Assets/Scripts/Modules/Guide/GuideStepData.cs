using System;
using UnityEngine;

/// <summary>Inspector 中配置的单个引导步骤；StepId 是业务标识而非列表索引。</summary>
[Serializable]
public sealed class GuideStepData
{
  /// <summary>此步骤唯一的业务标识，不要求连续。</summary>
  public int StepId;

  /// <summary>步骤标题。</summary>
  public string Title;

  /// <summary>可多行显示的引导正文。</summary>
  [TextArea(2, 5)]
  public string Content;
}
