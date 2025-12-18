using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
  public static StatsManager Instance { get; private set; }

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    }
    else if (Instance != this)
    {
      Destroy(gameObject);
    }
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }
  }

  [Header("Enemy Stats")]
  public float enemySpeed;
  public int enemyMaxHealth;

  [Header("Player Stats")]
  public float speed;
  public int damage;
  public int health;
  public int MaxHealth;
}