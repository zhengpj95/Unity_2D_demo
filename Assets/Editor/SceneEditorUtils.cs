using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneEditorUtils
{
  [MenuItem("Tools/01 Start Game")]
  private static void OpenGameStartScene()
  {
    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
    {
      EditorSceneManager.OpenScene("Assets/Scenes/FrogAdventure/StartGame.unity", OpenSceneMode.Single);
      EditorApplication.isPlaying = true;
    }
  }
}