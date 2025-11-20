using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RpgPlayerHealth : MonoBehaviour
{
  public void ChangeHealth(int amount)
  {
    StatsManager.Instance.health += amount;
    if (StatsManager.Instance.health <= 0)
    {
      Destroy(gameObject);
    }
  }
}