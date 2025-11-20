using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RpgEnemyHealth : MonoBehaviour
{
  public void ChangeHealth(int amount)
  {
    StatsManager.Instance.enemyHealth += amount;
    if (StatsManager.Instance.enemyHealth > StatsManager.Instance.enemyMaxHealth)
    {
      StatsManager.Instance.enemyHealth = StatsManager.Instance.enemyMaxHealth;
    }
    if (StatsManager.Instance.enemyHealth <= 0)
    {
      Destroy(gameObject);
    }
  }
}