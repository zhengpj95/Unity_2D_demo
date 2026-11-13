using System;
using UnityEngine;

/// <summary>Inspector 中配置的单个引导步骤；StepId 是业务标识而非列表索引。</summary>
[Serializable]
public sealed class GuideStepConfig
{
  /// <summary>此步骤唯一的业务标识，不要求连续。</summary>
  public int StepId;

  /// <summary>步骤标题。</summary>
  public string Title;

  /// <summary>可多行显示的引导正文；非空时显示在目标手势附近。</summary>
  [TextArea(2, 5)]
  public string Content;

  /// <summary>旧式目标标识；填写时通过场景中的 GuideCursor 定位目标。</summary>
  public string TargetId;

  /// <summary>新式目标所在 Presenter，格式为四位 ModuleName 编号、下划线和一位或两位 ViewType 编号，例如 1002_2。</summary>
  public string PresenterId;

  /// <summary>目标按钮相对于 Presenter View 根节点的 Transform 路径。</summary>
  public string TargetPath;

  /// <summary>是否显示遮罩并阻挡目标区域以外的点击，与手势显示独立配置。</summary>
  public bool ShowMask = true;

  /// <summary>是否在当前步骤目标旁显示点击手势。</summary>
  public bool ShowCursor = true;
}
