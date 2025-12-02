using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneEditorUtils
{
  [MenuItem("Scene/打开游戏启动场景")]
  private static void OpenGameStartScene()
  {
    EditorSceneManager.OpenScene("Assets/Scenes/FrogAdventure/StartGame.unity", OpenSceneMode.Single);
    EditorApplication.isPlaying = true;
  }
}