using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/**
 * Tag和Layer生成
 */
public class TagLayerGenerator : EditorWindow
{
  private const string OutputFolder = "Assets/Scripts/def";

  [MenuItem("Tools/02 TagLayer Generator")]
  public static void Generate()
  {
    if (!Directory.Exists(OutputFolder))
      Directory.CreateDirectory(OutputFolder);

    GenerateTagClass();
    GenerateLayerClass();

    AssetDatabase.Refresh();
    Debug.Log("✅ Tag & Layer 常量类已生成！");
  }

  private static void GenerateTagClass()
  {
    string[] tags = InternalEditorUtility.tags;
    StringBuilder sb = new StringBuilder();
    sb.AppendLine("/**\n * 自动生成的 Tag 常量类");
    sb.AppendLine(" * 请勿手动修改！使用 Scene/TagLayer Generator 重新生成");
    sb.AppendLine("*/");
    sb.AppendLine();
    sb.AppendLine("public static class GameTag");
    sb.AppendLine("{");

    foreach (string tag in tags)
    {
      string fieldName = SanitizeIdentifier(tag);
      sb.AppendLine($"  public const string {fieldName} = \"{tag}\";");
    }

    sb.AppendLine("}");

    File.WriteAllText(Path.Combine(OutputFolder, "GameTag.cs"), sb.ToString(), Encoding.UTF8);
  }

  private static void GenerateLayerClass()
  {
    string[] layers = InternalEditorUtility.layers;
    StringBuilder sb = new StringBuilder();
    sb.AppendLine("/**\n * 自动生成的 Layer 常量类");
    sb.AppendLine(" * 请勿手动修改！使用 Scene/TagLayer Generator 重新生成");
    sb.AppendLine("*/");
    sb.AppendLine();
    sb.AppendLine("public static class GameLayer");
    sb.AppendLine("{");

    foreach (string layer in layers)
    {
      string fieldName = SanitizeIdentifier(layer);
      int layerIndex = LayerMask.NameToLayer(layer);
      sb.AppendLine($"  public const int {fieldName} = {layerIndex};");
    }

    sb.AppendLine("}");

    File.WriteAllText(Path.Combine(OutputFolder, "GameLayer.cs"), sb.ToString(), Encoding.UTF8);
  }

  private static string SanitizeIdentifier(string name)
  {
    // 去掉空格、非法字符，并首字母大写
    string clean = name.Replace(" ", "_").Replace("-", "_");
    if (char.IsDigit(clean[0])) clean = "_" + clean;
    return clean;
  }
}