using UnityEngine.SceneManagement;

public class GameController
{
  private static GameController _instance;
  public static GameController Instance => _instance ??= new GameController();

  // 是否在被击退
  public bool IsKnockBack = false;

  public int Level = 1;
  public void LoadScene(int sceneIndex)
  {
    Level = sceneIndex;
    SceneManager.LoadScene(sceneIndex);
    EventBus.Dispatch("UPDATE_LEVEL");
  }
}