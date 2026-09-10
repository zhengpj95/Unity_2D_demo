using System;
using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// 按品质框颜色标识的可选品质，声明顺序对应图片从左到右。
  /// Poor、Ancient 等只作参考备注，不定义固定等级或必须启用的品质数量。
  /// 保留原六档的序列化数值，新增颜色追加编号；数值不表示强弱或图片位置。
  /// 实际使用的颜色及顺序由业务配置决定，不要通过枚举加减计算下一品质。
  /// </summary>
  public enum VampireQuality
  {
    /// <summary>铜棕品质框；参考名称：Poor / 粗糙。</summary>
    [InspectorName("铜棕 / Bronze（Poor / 粗糙）")]
    Bronze = 0,

    /// <summary>银灰品质框；参考名称：Common / 普通。</summary>
    [InspectorName("银灰 / Silver（Common / 普通）")]
    Silver = 1,

    /// <summary>绿色品质框；参考名称：Uncommon / 优秀。</summary>
    [InspectorName("绿色 / Green（Uncommon / 优秀）")]
    Green = 2,

    /// <summary>蓝色品质框；参考名称：Rare / 稀有。</summary>
    [InspectorName("蓝色 / Blue（Rare / 稀有）")]
    Blue = 3,

    /// <summary>紫色品质框；参考名称：Epic / 史诗。</summary>
    [InspectorName("紫色 / Purple（Epic / 史诗）")]
    Purple = 4,

    /// <summary>橙金品质框；参考名称：Legendary / 传奇。</summary>
    [InspectorName("橙金 / OrangeGold（Legendary / 传奇）")]
    OrangeGold = 5,

    /// <summary>亮红品质框；参考名称：Mythic / 神话。</summary>
    [InspectorName("亮红 / Red（Mythic / 神话）")]
    Red = 6,

    /// <summary>黑红品质框；参考名称：Cursed / 诅咒。</summary>
    [InspectorName("黑红 / DarkRed（Cursed / 诅咒）")]
    DarkRed = 7,

    /// <summary>白金品质框；参考名称：Ancient / 远古。</summary>
    [InspectorName("白金 / PaleGold（Ancient / 远古）")]
    PaleGold = 8,

  }

  /// <summary>按颜色标识解析品质显示，不绑定实际 Sprite 资源。</summary>
  public static class VampireQualityExtensions
  {
    /// <summary>返回 quality 对应的颜色；无效值抛出 ArgumentOutOfRangeException。</summary>
    public static Color32 GetColor(this VampireQuality quality)
    {
      switch (quality)
      {
        case VampireQuality.Bronze: return VampireUIColors.Quality.Bronze;
        case VampireQuality.Silver: return VampireUIColors.Quality.Silver;
        case VampireQuality.Green: return VampireUIColors.Quality.Green;
        case VampireQuality.Blue: return VampireUIColors.Quality.Blue;
        case VampireQuality.Purple: return VampireUIColors.Quality.Purple;
        case VampireQuality.OrangeGold: return VampireUIColors.Quality.OrangeGold;
        case VampireQuality.Red: return VampireUIColors.Quality.Red;
        case VampireQuality.DarkRed: return VampireUIColors.Quality.DarkRed;
        case VampireQuality.PaleGold: return VampireUIColors.Quality.PaleGold;
        default: throw new ArgumentOutOfRangeException(nameof(quality), quality, "未知的品质颜色。");
      }
    }

    /// <summary>返回 quality 的中文颜色名称；无效值抛出 ArgumentOutOfRangeException。</summary>
    public static string GetDisplayName(this VampireQuality quality)
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
