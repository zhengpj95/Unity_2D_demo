using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class VSHealth : MonoBehaviour
{
  [SerializeField] private int maxHealth;
  private int currentHealth;

  void Start()
  {
    currentHealth = maxHealth;
    UpdateHpBar();
  }

  public void TakeDamage(int damage)
  {
    currentHealth -= damage;
    UpdateHpBar();
    if (currentHealth <= 0)
    {
      Destroy(gameObject);
    }
  }

  private void UpdateHpBar()
  {
    Transform hpBar = transform.Find("HpBar")?.transform;
    if (hpBar)
    {
      HpBarUI hpBarUI = hpBar.GetComponent<HpBarUI>();
      if (hpBarUI)
      {
        hpBarUI.SetPercent(Mathf.Max(0f, currentHealth / (maxHealth * 1f)));
      }
    }
  }
}
