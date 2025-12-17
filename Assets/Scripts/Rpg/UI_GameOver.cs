using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GameOver : MonoBehaviour
{
  public GameObject gameOverPanel;
  public Transform playerPrefab;

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

    StartCoroutine(SpawnPlayerAfterDelay(0.5f));
  }

  IEnumerator SpawnPlayerAfterDelay(float delay)
  {
    yield return new WaitForSeconds(delay);
    Instantiate(playerPrefab, Vector3.down, Quaternion.identity);
  }
}
