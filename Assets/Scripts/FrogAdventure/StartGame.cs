using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{
  public Button startBtn;

  private void Start()
  {
    startBtn.onClick.AddListener(ClickStartGame);
  }

  private void OnDestroy()
  {
    startBtn.onClick.RemoveAllListeners();
  }

  private static void ClickStartGame()
  {
    Debug.Log("StartGame");
    GameController.Instance.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
  }
}