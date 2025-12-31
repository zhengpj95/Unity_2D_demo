using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
  [SerializeField] private float maxHealth = 100f;
  private float currentHealth;

  private Animator anim;

  public bool IsHit { get; private set; }

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
      anim.SetTrigger("Hurt");
      IsHit = true;
    }
  }

  private void HitEnd()
  {
    IsHit = false;
  }

  // Animation Event
  private void Death()
  {
    Destroy(gameObject);
  }
}
