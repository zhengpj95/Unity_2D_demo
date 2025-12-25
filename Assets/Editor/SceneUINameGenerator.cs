using UnityEditor;
using UnityEngine;
using System.IO;


public static class SceneUINameGenerator
{
  private const string FilePath = "Assets/Scripts/def/SceneUIName.cs";

  [MenuItem("Tools/03 SceneUIName Generator")]
  public static void Generate()
  {
    var scenes = EditorBuildSettings.scenes;
    if (scenes.Length == 0)
    {
      Debug.LogWarning("⚠ 没有找到任何场景，请先在 Build Settings 里添加！");
      return;
    }

    string code = "/**\n";
    code += " * 自动生成的 SceneUIName 常量类\n";
    code += " * 生成时间：" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n";
    code += " * 请勿手动修改！使用菜单 Tools > 03 SceneUIName Generator 重新生成\n";
    code += " */\n\n";
    code += "public static class SceneUIName\n";
    code += "{\n";

    foreach (var scene in scenes)
    {
      if (scene.enabled)
      {
        string sceneName = Path.GetFileNameWithoutExtension(scene.path);
        code += $"  public const string {sceneName} = \"{sceneName}\";\n";
      }
    }

    code += "}\n";

    File.WriteAllText(FilePath, code);
    AssetDatabase.Refresh();

    Debug.Log($"✅ SceneNames.cs 已生成，路径：{FilePath}");
  }
}