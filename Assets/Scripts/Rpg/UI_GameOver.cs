using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GameOver : MonoBehaviour
{
  public GameObject gameOverPanel;

  void Start()
  {
    EventBus.AddListener<bool>("Event_GameOver", UpdateActive);
  }

  void OnDestroy()
  {
    EventBus.RemoveListener<bool>("Event_GameOver", UpdateActive);
  }

  void UpdateActive(bool active)
  {
    gameOverPanel.SetActive(active);
  }

  // ui Button点击调用
  void RestartGame()
  {
    Debug.Log("Restart Game UI_GameOver");
    UpdateActive(false);
  }
}
