using UnityEngine;

namespace VampireSurvivorsLike {

  /// <summary>管理敌人的运行时生命、死亡结算、场景统一掉落与对象池回收。</summary>
  public class VSEnemyHealth : MonoBehaviour, IPoolable
  {
    [SerializeField] private int maxHealth;
    private int currentHealth;

    public void OnAlloc()
    {
      currentHealth = maxHealth;
      UpdateHpBar();
    }

    public void OnFree()
    {
      currentHealth = 0;
    }

    public void TakeDamage(int damage)
    {
      currentHealth -= damage;
      UpdateHpBar();
      DamageController.Instance.ShowDamage(damage, transform.position);
      if (currentHealth <= 0)
      {
        EnemyDirector.Instance.KillEnemyCount++;
        ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor).UpdateEnemyKillCount();
        // 掉落由场景级权重统一决定，避免不同敌人 Prefab 固定产出同一种资源。
        DropItemManager.Instance.SpawnDropItem(transform.position);
        EnemyDirector.Instance.RecycleEnemy(gameObject);
      }
    }

    private void UpdateHpBar()
    {
      UI_HpBar hpBarUI = gameObject.GetComponent<UI_HpBar>();
      if (hpBarUI)
      {
        hpBarUI.SetPercent(Mathf.Max(0f, currentHealth / (maxHealth * 1f)));
      }
    }
  }

}
