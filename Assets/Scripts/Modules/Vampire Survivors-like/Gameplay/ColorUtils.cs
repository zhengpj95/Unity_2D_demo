using System;
using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>提供品质颜色和显示名称相关的通用转换。</summary>
  public static class ColorUtils
  {
    /// <summary>返回品质对应的颜色；无效值抛出 ArgumentOutOfRangeException。</summary>
    public static Color32 GetQualityColor(this VampireQuality quality)
    {
      switch (quality)
      {
        case VampireQuality.Bronze: return QualityColor.Bronze;
        case VampireQuality.Silver: return QualityColor.Silver;
        case VampireQuality.Green: return QualityColor.Green;
        case VampireQuality.Blue: return QualityColor.Blue;
        case VampireQuality.Purple: return QualityColor.Purple;
        case VampireQuality.OrangeGold: return QualityColor.OrangeGold;
        case VampireQuality.Red: return QualityColor.Red;
        case VampireQuality.DarkRed: return QualityColor.DarkRed;
        case VampireQuality.PaleGold: return QualityColor.PaleGold;
        default: throw new ArgumentOutOfRangeException(nameof(quality), quality, "未知的品质颜色。");
      }
    }

    /// <summary>返回品质的中文颜色名称；无效值抛出 ArgumentOutOfRangeException。</summary>
    public static string GetQualityName(this VampireQuality quality)
    {
      switch (quality)
      {
        case VampireQuality.Bronze: return "铜棕";
        case VampireQuality.Silver: return "银灰";
        case VampireQuality.Green: return "绿色";
        case VampireQuality.Blue: return "蓝色";
        case VampireQuality.Purple: return "紫色";
        case VampireQuality.OrangeGold: return "橙金";
        case VampireQuality.Red: return "亮红";
        case VampireQuality.DarkRed: return "黑红";
        case VampireQuality.PaleGold: return "白金";
        default: throw new ArgumentOutOfRangeException(nameof(quality), quality, "未知的品质颜色。");
      }
    }
  }
}
