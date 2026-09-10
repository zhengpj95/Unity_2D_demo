using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>Survivor UI 的文本颜色，按界面语义直接选用。</summary>
  public static class TextColor
  {
    /// <summary>主要正文和常规标签。</summary>
    public static readonly Color32 Primary = HexColor.Parse("#F1E7D5");

    /// <summary>说明、辅助信息和低优先级标签。</summary>
    public static readonly Color32 Secondary = HexColor.Parse("#B9B0A6");

    /// <summary>数值、奖励等需要突出显示的文字。</summary>
    public static readonly Color32 Highlight = HexColor.Parse("#FFFFFF");

    /// <summary>不可交互或不可用状态的文字。</summary>
    public static readonly Color32 Disabled = HexColor.Parse("#6D6865");

    /// <summary>升级成功、获得奖励等正向反馈文字。</summary>
    public static readonly Color32 Positive = HexColor.Parse("#4CAF50");

    /// <summary>低血量、危险提示等警告文字。</summary>
    public static readonly Color32 Warning = HexColor.Parse("#FF7043");
  }

  /// <summary>TMP 文本描边颜色；描边宽度由具体 TMP 样式控制。</summary>
  public static class OutlineColor
  {
    /// <summary>常规正文和 HUD 数值的深色描边。</summary>
    public static readonly Color32 Default = HexColor.Parse("#161010");

    /// <summary>标题使用的棕黑描边。</summary>
    public static readonly Color32 Title = HexColor.Parse("#2A0C08");

    /// <summary>按钮文字使用的深红褐描边。</summary>
    public static readonly Color32 Button = HexColor.Parse("#47100C");

    /// <summary>伤害数字使用的深红描边。</summary>
    public static readonly Color32 Damage = HexColor.Parse("#230B08");
  }

  /// <summary>按 vampire02 图片从左到右排列的品质框颜色。</summary>
  public static class QualityColor
  {
    /// <summary>铜棕品质框颜色。</summary>
    public static readonly Color32 Bronze = HexColor.Parse("#A07048");

    /// <summary>银灰品质框颜色。</summary>
    public static readonly Color32 Silver = HexColor.Parse("#B8B8B8");

    /// <summary>绿色品质框颜色。</summary>
    public static readonly Color32 Green = HexColor.Parse("#62C96B");

    /// <summary>蓝色品质框颜色。</summary>
    public static readonly Color32 Blue = HexColor.Parse("#4A9FF5");

    /// <summary>紫色品质框颜色。</summary>
    public static readonly Color32 Purple = HexColor.Parse("#B45CE6");

    /// <summary>橙金品质框颜色。</summary>
    public static readonly Color32 OrangeGold = HexColor.Parse("#F0A92E");

    /// <summary>亮红品质框颜色。</summary>
    public static readonly Color32 Red = HexColor.Parse("#ED4B3B");

    /// <summary>黑红品质框颜色。</summary>
    public static readonly Color32 DarkRed = HexColor.Parse("#A91F2B");

    /// <summary>白金品质框颜色。</summary>
    public static readonly Color32 PaleGold = HexColor.Parse("#F2E2AF");
  }

  /// <summary>为颜色配置提供统一的十六进制字符串转换与格式校验。</summary>
  internal static class HexColor
  {
    /// <summary>将 #RRGGBB 或 #RRGGBBAA 格式的十六进制颜色转换为 Color32。</summary>
    internal static Color32 Parse(string hex)
    {
      if (!ColorUtility.TryParseHtmlString(hex, out Color color))
      {
        throw new System.ArgumentException($"无效的颜色值：{hex}", nameof(hex));
      }

      return (Color32)color;
    }
  }
}
