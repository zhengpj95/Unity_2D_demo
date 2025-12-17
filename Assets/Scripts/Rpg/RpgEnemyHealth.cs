using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RpgEnemyHealth : MonoBehaviour
{
  public int health = 10;
  private RpgEnemyMovement _rpgEnemyMovement;

  private void Start()
  {
    _rpgEnemyMovement = GetComponent<RpgEnemyMovement>();
  }

  public void ChangeHealth(int amount)
  {
    health += amount;
    if (health > StatsManager.Instance.enemyMaxHealth)
    {
      health = StatsManager.Instance.enemyMaxHealth;
    }

    if (health <= 0)
    {
      _rpgEnemyMovement.ChangeState(RpgEnemyMovement.EnemyState.Death);
    }
  }

  public void Death()
  {
    Destroy(gameObject);
  }
}