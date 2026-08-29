/**
 * 自动生成的 Sorting Layer 常量类
 * 生成时间：2025-12-25 09:59:25
 * 请勿手动修改！使用菜单 Tools > 04 Generate Sorting Layers 重新生成
 */

using UnityEngine;

/// <summary>
/// Sorting Layer 常量类
/// </summary>
public static class GameSortingLayers
{
  /// <summary>
  /// Sorting Layer: Background
  /// </summary>
  public const string Background = "Background";

  /// <summary>
  /// Sorting Layer: Default
  /// </summary>
  public const string Default = "Default";

  /// <summary>
  /// Sorting Layer: Enemy
  /// </summary>
  public const string Enemy = "Enemy";

  /// <summary>
  /// Sorting Layer: Player
  /// </summary>
  public const string Player = "Player";

  /// <summary>
  /// Sorting Layer: UI
  /// </summary>
  public const string UI = "UI";

  // ========================================
  // 辅助方法
  // ========================================

  /// <summary>
  /// 设置 Sprite Renderer 的 Sorting Layer
  /// </summary>
  public static void SetSortingLayer(SpriteRenderer renderer, string layerName)
  {
    if (renderer != null)
    {
      renderer.sortingLayerName = layerName;
    }
  }

  /// <summary>
  /// 设置 Sprite Renderer 的 Sorting Layer 和 Order
  /// </summary>
  public static void SetSortingLayer(SpriteRenderer renderer, string layerName, int order)
  {
    if (renderer != null)
    {
      renderer.sortingLayerName = layerName;
      renderer.sortingOrder = order;
    }
  }

  /// <summary>
  /// 获取 Sorting Layer ID
  /// </summary>
  public static int GetLayerId(string layerName)
  {
    return SortingLayer.NameToID(layerName);
  }

  /// <summary>
  /// 检查 Sorting Layer 是否存在
  /// </summary>
  public static bool LayerExists(string layerName)
  {
    return SortingLayer.NameToID(layerName) != 0 || layerName == "Default";
  }

  /// <summary>
  /// 获取所有 Sorting Layer 名称
  /// </summary>
  public static string[] GetAllLayerNames()
  {
    return new string[]
  {
            Background,
            Default,
            Enemy,
            Player,
            UI
        };
    }
}
