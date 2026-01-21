using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VSPlayerHealth : MonoBehaviour
{
  [SerializeField] private int maxHealth = 5;
  private int currentHealth;

  void Start()
  {
    currentHealth = maxHealth;
    VSUIManager.Instance.UpdateHp(currentHealth, maxHealth);
  }

  public void TakeDamage(int damage)
  {
    currentHealth -= damage;

    VSUIManager.Instance.UpdateHp(currentHealth, maxHealth);
    DamageController.Instance.ShowDamage(damage, transform.position);
  }
}
