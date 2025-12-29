using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RpgEnemyHealth : MonoBehaviour
{
  public int health = 10;
  public int damage = 2;
  private RpgEnemyMovement _rpgEnemyMovement;

  private void Start()
  {
    _rpgEnemyMovement = GetComponent<RpgEnemyMovement>();
  }

  public void ChangeHealth(int amount)
  {
    health += amount;
    DamageController.Instance.ShowDamage(Mathf.Abs(amount), transform.position);
    if (health > StatsManager.Instance.enemyMaxHealth)
    {
      health = StatsManager.Instance.enemyMaxHealth;
    }

    if (health <= 0)
    {
      var rb = GetComponent<Rigidbody2D>();
      rb.simulated = false; // Disable physics interactions
      _rpgEnemyMovement.ChangeState(RpgEnemyMovement.EnemyState.Death);
    }
  }

  public void Death()
  {
    Destroy(gameObject);
  }
}