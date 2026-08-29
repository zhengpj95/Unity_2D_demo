using System.IO;

/// <summary>
/// 编辑器代码生成工具的统一输出路径。
/// 调整生成目录时，只需修改此处。
/// </summary>
public static class CodeGenerationPaths
{
  public const string EditorGeneratedScriptsDirectory = "Assets/Scripts/Define/Editor";
  public const string UIGeneratedScriptsDirectory = "Assets/Scripts/Define/UI";
  public const string UIPresenterScriptsDirectory = "Assets/Scripts/Modules/Presenters";

  public static string SceneUINameFilePath => Path.Combine(EditorGeneratedScriptsDirectory, "SceneUIName.cs");
  public static string SortingLayerFilePath => Path.Combine(EditorGeneratedScriptsDirectory, "GameSortingLayers.cs");
  public static string GameTagFilePath => Path.Combine(EditorGeneratedScriptsDirectory, "GameTag.cs");
  public static string GameLayerFilePath => Path.Combine(EditorGeneratedScriptsDirectory, "GameLayer.cs");

  public static void EnsureDirectoryExists(string directoryPath)
  {
    if (!Directory.Exists(directoryPath))
      Directory.CreateDirectory(directoryPath);
  }
}
