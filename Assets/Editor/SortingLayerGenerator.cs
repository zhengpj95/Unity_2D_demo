using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class SortingLayerGenerator
{
  [MenuItem("Tools/04 Generate Sorting Layers")]
  public static void GenerateSortingLayersEnum()
  {
    // 获取所有 Sorting Layer
    string[] sortingLayerNames = GetSortingLayerNames();

    if (sortingLayerNames.Length == 0)
    {
      EditorUtility.DisplayDialog("警告", "没有找到任何 Sorting Layer！", "确定");
      return;
    }

    // 生成代码
    string classCode = GenerateClassCode(sortingLayerNames);

    // 保存到文件
    string filePath = "Assets/Scripts/Def/GameSortingLayers.cs";
    SaveCodeToFile(filePath, classCode);

    // 刷新 Asset 数据库
    AssetDatabase.Refresh();

    EditorUtility.DisplayDialog(
        "成功",
        $"已生成 Sorting Layer 类！\n文件位置：{filePath}\n共 {sortingLayerNames.Length} 个层级",
        "确定"
    );

    // 打开生成的文件
    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(filePath, typeof(TextAsset)));
  }

  /// <summary>
  /// 获取所有 Sorting Layer 名称
  /// </summary>
  private static string[] GetSortingLayerNames()
  {
    return SortingLayer.layers.Length > 0
        ? System.Array.ConvertAll(SortingLayer.layers, layer => layer.name)
        : new string[0];
  }

  /// <summary>
  /// 生成 Static Class 代码
  /// </summary>
  private static string GenerateClassCode(string[] layerNames)
  {
    StringBuilder sb = new StringBuilder();

    sb.AppendLine("/**");
    sb.AppendLine(" * 自动生成的 Sorting Layer 常量类");
    sb.AppendLine(" * 生成时间：" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    sb.AppendLine(" * 请勿手动修改！使用菜单 Tools > 04 Generate Sorting Layers 重新生成");
    sb.AppendLine(" */");
    sb.AppendLine();
    sb.AppendLine("using UnityEngine;");
    sb.AppendLine();
    sb.AppendLine("/// <summary>");
    sb.AppendLine("/// Sorting Layer 常量类");
    sb.AppendLine("/// </summary>");
    sb.AppendLine("public static class GameSortingLayers");
    sb.AppendLine("{");

    // 生成常量
    for (int i = 0; i < layerNames.Length; i++)
    {
      string layerName = layerNames[i];
      string constantName = CleanConstantName(layerName);

      sb.AppendLine($"  /// <summary>");
      sb.AppendLine($"  /// Sorting Layer: {layerName}");
      sb.AppendLine($"  /// </summary>");
      sb.AppendLine($"  public const string {constantName} = \"{layerName}\";");
      sb.AppendLine();
    }

    sb.AppendLine("  // ========================================");
    sb.AppendLine("  // 辅助方法");
    sb.AppendLine("  // ========================================");
    sb.AppendLine();

    // 设置 Sprite Renderer 的 Sorting Layer
    sb.AppendLine("  /// <summary>");
    sb.AppendLine("  /// 设置 Sprite Renderer 的 Sorting Layer");
    sb.AppendLine("  /// </summary>");
    sb.AppendLine("  public static void SetSortingLayer(SpriteRenderer renderer, string layerName)");
    sb.AppendLine("  {");
    sb.AppendLine("    if (renderer != null)");
    sb.AppendLine("    {");
    sb.AppendLine("      renderer.sortingLayerName = layerName;");
    sb.AppendLine("    }");
    sb.AppendLine("  }");
    sb.AppendLine();

    // 设置 Sorting Layer 和 Order
    sb.AppendLine("  /// <summary>");
    sb.AppendLine("  /// 设置 Sprite Renderer 的 Sorting Layer 和 Order");
    sb.AppendLine("  /// </summary>");
    sb.AppendLine("  public static void SetSortingLayer(SpriteRenderer renderer, string layerName, int order)");
    sb.AppendLine("  {");
    sb.AppendLine("    if (renderer != null)");
    sb.AppendLine("    {");
    sb.AppendLine("      renderer.sortingLayerName = layerName;");
    sb.AppendLine("      renderer.sortingOrder = order;");
    sb.AppendLine("    }");
    sb.AppendLine("  }");
    sb.AppendLine();

    // 获取 Sorting Layer ID
    sb.AppendLine("  /// <summary>");
    sb.AppendLine("  /// 获取 Sorting Layer ID");
    sb.AppendLine("  /// </summary>");
    sb.AppendLine("  public static int GetLayerId(string layerName)");
    sb.AppendLine("  {");
    sb.AppendLine("    return SortingLayer.NameToID(layerName);");
    sb.AppendLine("  }");
    sb.AppendLine();

    // 检查 Sorting Layer 是否存在
    sb.AppendLine("  /// <summary>");
    sb.AppendLine("  /// 检查 Sorting Layer 是否存在");
    sb.AppendLine("  /// </summary>");
    sb.AppendLine("  public static bool LayerExists(string layerName)");
    sb.AppendLine("  {");
    sb.AppendLine("    return SortingLayer.NameToID(layerName) != 0 || layerName == \"Default\";");
    sb.AppendLine("  }");
    sb.AppendLine();

    // 获取所有 Sorting Layer 名称
    sb.AppendLine("  /// <summary>");
    sb.AppendLine("  /// 获取所有 Sorting Layer 名称");
    sb.AppendLine("  /// </summary>");
    sb.AppendLine("  public static string[] GetAllLayerNames()");
    sb.AppendLine("  {");
    sb.AppendLine("    return new string[]");
    sb.AppendLine("  {");

    for (int i = 0; i < layerNames.Length; i++)
    {
      string layerName = layerNames[i];
      string constantName = CleanConstantName(layerName);

      sb.Append($"            {constantName}");
      if (i < layerNames.Length - 1)
      {
        sb.AppendLine(",");
      }
      else
      {
        sb.AppendLine();
      }
    }

    sb.AppendLine("        };");
    sb.AppendLine("    }");
    sb.AppendLine("}");

    return sb.ToString();
  }

  /// <summary>
  /// 清理常量名称（移除不合法字符）
  /// </summary>
  private static string CleanConstantName(string name)
  {
    StringBuilder sb = new StringBuilder();

    foreach (char c in name)
    {
      // 允许字母、数字和下划线
      if (char.IsLetterOrDigit(c) || c == '_')
      {
        sb.Append(c);
      }
      else if (c == ' ')
      {
        sb.Append('_');
      }
    }

    string result = sb.ToString();

    // 如果以数字开头，添加下划线前缀
    if (result.Length > 0 && char.IsDigit(result[0]))
    {
      result = "_" + result;
    }

    return result;
  }

  /// <summary>
  /// 保存代码到文件
  /// </summary>
  private static void SaveCodeToFile(string filePath, string code)
  {
    // 确保目录存在
    string directory = Path.GetDirectoryName(filePath);
    if (!Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }

    // 写入文件
    File.WriteAllText(filePath, code, Encoding.UTF8);
  }
}
