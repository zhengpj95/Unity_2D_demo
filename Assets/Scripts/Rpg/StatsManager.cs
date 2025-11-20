using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
  public static StatsManager Instance;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
  }

  [Header("Enemy Stats")]
  public float enemySpeed;
  public int enemyDamage;
  public int enemyHealth;
  public int enemyMaxHealth;

  [Header("Player Stats")]
  public float speed;
  public int damage;
  public float health;
}