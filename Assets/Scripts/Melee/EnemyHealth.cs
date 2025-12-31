using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
  [SerializeField] private float maxHealth = 100f;
  private float currentHealth;

  private Animator anim;

  void Start()
  {
    anim = GetComponent<Animator>();
    currentHealth = maxHealth;
  }

  public void TakeDamage(float damage)
  {
    if (currentHealth <= 0) return;

    currentHealth -= damage;
    if (currentHealth <= 0)
    {
      anim.SetTrigger("Death");
    }
    else
    {
      anim.SetTrigger("Hit");
    }
  }

  // Animation Event
  private void Death()
  {
    Destroy(gameObject);
  }
}
