using UnityEditor;
using UnityEditor.SceneManagement;

namespace EditorTools
{
  public static class SceneEditorUtils
  {
    [MenuItem("Tools/01 Start Game", false, 0)]
    private static void OpenGameStartScene()
    {
      if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
      {
        EditorSceneManager.OpenScene("Assets/Scenes/FrogAdventure/StartGame.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
      }
    }
  }
}