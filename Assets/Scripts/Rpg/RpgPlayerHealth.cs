using System.Collections;
using System.Collections.Generic;
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
    EventBus.Dispatch("Event_UpdatePlayerHealth");
    if (StatsManager.Instance.health <= 0)
    {
      EventBus.Dispatch("Event_GameOver", true);
      Destroy(gameObject);
    }
  }
}