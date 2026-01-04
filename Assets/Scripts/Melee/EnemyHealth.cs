using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
  [SerializeField] private float maxHealth = 100f;

  private EnemyChase enemyChase;

  private float currentHealth;

  void Start()
  {
    enemyChase = GetComponent<EnemyChase>();
    currentHealth = maxHealth;
  }

  public void TakeDamage(float damage)
  {
    if (currentHealth <= 0) return;

    currentHealth -= damage;
    if (currentHealth <= 0)
    {
      enemyChase.ChangeState(EntityState.Death);
    }
    else
    {
      enemyChase.ChangeState(EntityState.Hurt);
    }
  }

  // Animation Event
  private void Death()
  {
    Destroy(gameObject);
  }
}
