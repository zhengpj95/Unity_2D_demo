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
    [InspectorName("铜棕 | Bronze（Poor | 粗糙）")]
    Bronze = 0,

    /// <summary>银灰品质框；参考名称：Common / 普通。</summary>
    [InspectorName("银灰 | Silver（Common | 普通）")]
    Silver = 1,

    /// <summary>绿色品质框；参考名称：Uncommon / 优秀。</summary>
    [InspectorName("绿色 | Green（Uncommon | 优秀）")]
    Green = 2,

    /// <summary>蓝色品质框；参考名称：Rare / 稀有。</summary>
    [InspectorName("蓝色 | Blue（Rare | 稀有）")]
    Blue = 3,

    /// <summary>紫色品质框；参考名称：Epic / 史诗。</summary>
    [InspectorName("紫色 | Purple（Epic | 史诗）")]
    Purple = 4,

    /// <summary>橙金品质框；参考名称：Legendary / 传奇。</summary>
    [InspectorName("橙金 | OrangeGold（Legendary | 传奇）")]
    OrangeGold = 5,

    /// <summary>亮红品质框；参考名称：Mythic / 神话。</summary>
    [InspectorName("亮红 | Red（Mythic | 神话）")]
    Red = 6,

    /// <summary>黑红品质框；参考名称：Cursed / 诅咒。</summary>
    [InspectorName("黑红 | DarkRed（Cursed | 诅咒）")]
    DarkRed = 7,

    /// <summary>白金品质框；参考名称：Ancient / 远古。</summary>
    [InspectorName("白金 | PaleGold（Ancient | 远古）")]
    PaleGold = 8,

  }
}
