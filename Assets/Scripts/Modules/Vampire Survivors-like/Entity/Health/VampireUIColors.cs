using UnityEngine;

/// <summary>
/// Vampire 幸存者模块的品质框颜色表。
/// 仅保留 <see cref="VampireSurvivorsLike.VampireQuality"/> 当前使用的九种品质色，避免维护未接入界面的通用配色。
/// </summary>
public static class VampireUIColors
{
  /// <summary>
  /// 通用文本颜色。优先使用语义名称，避免将颜色值散落在 View、Presenter 或 Prefab 配置中。
  /// </summary>
  public static class Text
  {
    /// <summary>主要正文和常规标签。</summary>
    public static readonly Color32 Primary = FromHex("#F1E7D5");

    /// <summary>说明、辅助信息和低优先级标签。</summary>
    public static readonly Color32 Secondary = FromHex("#B9B0A6");

    /// <summary>数值、奖励等需要突出显示的文字。</summary>
    public static readonly Color32 Highlight = FromHex("#FFFFFF");

    /// <summary>不可交互或不可用状态的文字。</summary>
    public static readonly Color32 Disabled = FromHex("#6D6865");
  }

  /// <summary>
  /// TMP 文本描边颜色。描边宽度由具体 TMP 样式控制，此处只定义颜色。
  /// </summary>
  public static class Outline
  {
    /// <summary>常规正文和 HUD 数值的深色描边。</summary>
    public static readonly Color32 Default = FromHex("#161010");

    /// <summary>标题使用的棕黑描边。</summary>
    public static readonly Color32 Title = FromHex("#2A0C08");

    /// <summary>按钮文字使用的深红褐描边。</summary>
    public static readonly Color32 Button = FromHex("#47100C");

    /// <summary>伤害数字使用的深红描边。</summary>
    public static readonly Color32 Damage = FromHex("#230B08");
  }

  /// <summary>按 vampire02 图片从左到右排列的品质框颜色。</summary>
  public static class Quality
  {
    public static readonly Color32 Bronze = FromHex("#A07048");
    public static readonly Color32 Silver = FromHex("#B8B8B8");
    public static readonly Color32 Green = FromHex("#62C96B");
    public static readonly Color32 Blue = FromHex("#4A9FF5");
    public static readonly Color32 Purple = FromHex("#B45CE6");
    public static readonly Color32 OrangeGold = FromHex("#F0A92E");
    public static readonly Color32 Red = FromHex("#ED4B3B");
    public static readonly Color32 DarkRed = FromHex("#A91F2B");
    public static readonly Color32 PaleGold = FromHex("#F2E2AF");
  }

  /// <summary>将 #RRGGBB 或 #RRGGBBAA 格式的十六进制颜色转换为 Color32。</summary>
  private static Color32 FromHex(string hex)
  {
    if (!ColorUtility.TryParseHtmlString(hex, out Color color))
    {
      throw new System.ArgumentException($"无效的颜色值：{hex}", nameof(hex));
    }

    return (Color32)color;
  }
}
