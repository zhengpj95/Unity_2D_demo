using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rpg
{
  public class UI_PlayerHealth : MonoBehaviour
  {
    public Slider playerHealthslider;
    public Text healthText;

    private void Start()
    {
      EventBus.On(EventDefine.RPG_PLAYER_HEALTH_CHANGED, OnUpdatePlayerHealth, this);
    }

    void OnDestroy()
    {
      EventBus.Off(EventDefine.RPG_PLAYER_HEALTH_CHANGED, OnUpdatePlayerHealth, this);
    }

    /// <summary>响应玩家生命值事件；事件本身不需要额外数据。</summary>
    private void OnUpdatePlayerHealth(EventContext context)
    {
      UpdatePlayerHealth();
    }

    /// <summary>从当前属性状态刷新生命值显示。</summary>
    public void UpdatePlayerHealth()
    {
      var maxHealth = StatsManager.Instance.MaxHealth;
      var currentHealth = StatsManager.Instance.health;
      playerHealthslider.maxValue = maxHealth;
      playerHealthslider.value = currentHealth;
      healthText.text = Mathf.Max(0, currentHealth) + " / " + maxHealth;
    }
  }
}
