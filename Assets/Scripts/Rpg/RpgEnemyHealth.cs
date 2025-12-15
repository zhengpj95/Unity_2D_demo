using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RpgEnemyHealth : MonoBehaviour
{
  public int health = 10;

  public void ChangeHealth(int amount)
  {
    health += amount;
    if (health > StatsManager.Instance.enemyMaxHealth)
    {
      health = StatsManager.Instance.enemyMaxHealth;
    }

    if (health <= 0)
    {
      Destroy(gameObject);
    }
  }
}