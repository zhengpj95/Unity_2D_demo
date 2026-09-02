using UnityEngine;

namespace VampireSurvivorsLike {

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
      EnemyChasing enemyChasing = gameObject.GetComponent<EnemyChasing>();
      DamageController.Instance.ShowDamage(damage, transform.position);
      if (currentHealth <= 0)
      {
        EnemySpawnManager.Instance.KillEnemyCount++;
        ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor).UpdateEnemyKillCount();
        DropItemManager.Instance.SpawnDropItem(transform.position, enemyChasing.DropItemType, enemyChasing.DropItemProb);
        EnemySpawnManager.Instance.RecycleEnemy(gameObject);
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
