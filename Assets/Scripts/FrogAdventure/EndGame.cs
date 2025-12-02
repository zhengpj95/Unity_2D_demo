using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGame : MonoBehaviour
{
  public Button restartBtn;

  private void Start()
  {
    restartBtn.onClick.AddListener(RestartGame);
    UIManager.Instance.HideUI("Prefabs/UI/UI_Fruit");
  }

  private void OnDestroy()
  {
    restartBtn.onClick.RemoveAllListeners();
  }

  private static void RestartGame()
  {
    Debug.Log("RestartGame");
    GameController.Instance.LoadScene(1); //第一个关卡序号
    FruitCollectManager.Instance.Score = 0;
  }
}