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
      EventBus.On("Event_GameOver", UpdateActive, this);
    }

    void OnDestroy()
    {
      EventBus.Off("Event_GameOver", UpdateActive, this);
    }

    void UpdateActive(EventContext context)
    {
      if (context.TryGetData(out bool active)) gameOverPanel.SetActive(active);
    }

    // ui Button点击调用
    public void RestartGame()
    {
      Debug.Log("Restart Game UI_GameOver");
      gameOverPanel.SetActive(false);

      StartCoroutine(SpawnPlayerAfterDelay(0.5f));
    }

    private IEnumerator SpawnPlayerAfterDelay(float delay)
    {
      yield return new WaitForSeconds(delay);
      Instantiate(playerPrefab, Vector3.down, Quaternion.identity);
    }
  }
}
