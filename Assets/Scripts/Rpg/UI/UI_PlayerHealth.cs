using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerHealth : MonoBehaviour
{
  public Slider playerHealthslider;
  public Text healthText;

  private void Start()
  {
    EventBus.AddListener("Event_UpdatePlayerHealth", UpdatePlayerHealth);
  }

  void OnDestroy()
  {
    EventBus.RemoveListener("Event_UpdatePlayerHealth", UpdatePlayerHealth);
  }

  public void UpdatePlayerHealth()
  {
    var maxHealth = StatsManager.Instance.MaxHealth;
    var currentHealth = StatsManager.Instance.health;
    playerHealthslider.maxValue = maxHealth;
    playerHealthslider.value = currentHealth;
    healthText.text = Mathf.Max(0, currentHealth) + " / " + maxHealth;
  }
}
