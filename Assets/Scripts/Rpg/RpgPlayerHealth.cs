using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RpgPlayerHealth : MonoBehaviour
{

  void Start()
  {
    ChangeHealth(StatsManager.Instance.MaxHealth);
  }

  public void ChangeHealth(int amount)
  {
    StatsManager.Instance.health += amount;
    StartCoroutine(DispatchEvent()); // 延迟更新UI
    if (StatsManager.Instance.health <= 0)
    {
      EventBus.Dispatch("Event_GameOver", true);
      Destroy(gameObject);
    }
  }

  private IEnumerator DispatchEvent()
  {
    yield return new WaitForEndOfFrame();
    EventBus.Dispatch("Event_UpdatePlayerHealth");
  }
}