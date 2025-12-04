using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneEditorUtils
{
  [MenuItem("Tools/Start Game")]
  private static void OpenGameStartScene()
  {
    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
    {
      EditorSceneManager.OpenScene("Assets/Scenes/FrogAdventure/StartGame.unity", OpenSceneMode.Single);
      EditorApplication.isPlaying = true;
    }
  }
}