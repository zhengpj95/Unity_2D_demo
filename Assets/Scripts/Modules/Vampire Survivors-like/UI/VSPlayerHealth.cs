using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  public class VSPlayerHealth : MonoBehaviour
  {
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;

    void Start()
    {
      currentHealth = maxHealth;
      ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor).UpdateHp(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
      currentHealth -= damage;

      ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor).UpdateHp(currentHealth, maxHealth);
      DamageController.Instance.ShowDamage(damage, transform.position);
    }

    /// <summary>增加最大生命值并同步当前生命值；升级配置不会修改原始资源。</summary>
    public void ApplyMaxHealthUpgrade(float value, bool isPercent)
    {
      if (value <= 0f) return;

      int increase = isPercent
        ? Mathf.Max(1, Mathf.CeilToInt(maxHealth * value))
        : Mathf.Max(1, Mathf.RoundToInt(value));
      maxHealth += increase;
      currentHealth += increase;
      ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor)?.UpdateHp(currentHealth, maxHealth);
    }
  }

}
