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
    EnemyChasing enemyChasing = gameObject.GetComponent<EnemyChasing>();
    DamageController.Instance.ShowDamage(damage, transform.position);
    if (currentHealth <= 0)
    {
      EnemySpawnManager.Instance.KillEnemyCount++;
      VSUIManager.Instance.UpdateEnemyKillCount();
      Destroy(gameObject);
      DropItemManager.Instance.SpawnDropItem(transform.position, enemyChasing.DropItemType, enemyChasing.DropItemProb);
    }
  }

  private void UpdateHpBar()
  {
    HpBarUI hpBarUI = gameObject.GetComponent<HpBarUI>();
    if (hpBarUI)
    {
      hpBarUI.SetPercent(Mathf.Max(0f, currentHealth / (maxHealth * 1f)));
    }
  }
}
