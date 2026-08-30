using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rpg
{
  public class UI_GameOver : MonoBehaviour
  {
    public GameObject gameOverPanel;
    public Transform playerPrefab;

    void Start()
    {
      EventBus.On<bool>("Event_GameOver", UpdateActive);
    }

    void OnDestroy()
    {
      EventBus.Off<bool>("Event_GameOver", UpdateActive);
    }

    void UpdateActive(bool active)
    {
      gameOverPanel.SetActive(active);
    }

    // ui Button点击调用
    public void RestartGame()
    {
      Debug.Log("Restart Game UI_GameOver");
      UpdateActive(false);

      StartCoroutine(SpawnPlayerAfterDelay(0.5f));
    }

    private IEnumerator SpawnPlayerAfterDelay(float delay)
    {
      yield return new WaitForSeconds(delay);
      Instantiate(playerPrefab, Vector3.down, Quaternion.identity);
    }
  }
}
