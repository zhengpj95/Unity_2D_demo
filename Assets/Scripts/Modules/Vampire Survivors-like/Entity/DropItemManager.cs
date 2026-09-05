using UnityEngine;

namespace VampireSurvivorsLike
{
  public class DropItemManager : SingletonMono<DropItemManager>
  {
    [SerializeField] private Transform dropItemContainer;
    [SerializeField] private Transform gemPrefab;
    [SerializeField] private Transform coinPrefab;
    [Tooltip("敌人死亡后 Gem 的掉落权重。Gem 用于获得经验和升级。")]
    [SerializeField, Min(0)] private int gemDropWeight = 90;
    [Tooltip("敌人死亡后 Coin 的掉落权重。Coin 用于局外武器升级，应保持低于 Gem。")]
    [SerializeField, Min(0)] private int coinDropWeight = 10;
    [Tooltip("每种掉落物启动时预热的对象数量，避免玩家拾取时频繁 Instantiate。")]
    [SerializeField, Min(0)] private int preloadCountPerDropItem = 8;

    [SerializeField] private int totalScore;
    [SerializeField] private int speedUpRateScore = 100;
    private int speedUpScore;
    [SerializeField] private int skillUpRateScore = 20;
    private int skillUpScore;

    private void Start()
    {
      PreloadDropItems();
    }

    /// <summary>
    /// 按 Gem/Coin 权重生成一件死亡掉落物。
    /// 两种掉落物都存在时，每次击杀必定掉落一件；金币权重应低于经验宝石。
    /// </summary>
    public void SpawnDropItem(Vector3 position)
    {
      Transform prefab = GetWeightedDropPrefab();
      if (prefab == null)
        return;

      GameObject dropItem = PoolManager.Instance.Alloc(prefab.gameObject, position, Quaternion.identity);
      if (dropItem != null && dropItemContainer != null)
        dropItem.transform.SetParent(dropItemContainer, true);
    }

    /// <summary>根据场景配置的权重选择 Gem 或 Coin，并兼容单个 Prefab 未配置的情况。</summary>
    private Transform GetWeightedDropPrefab()
    {
      if (gemPrefab == null)
        return coinPrefab;
      if (coinPrefab == null)
        return gemPrefab;

      int totalWeight = gemDropWeight + coinDropWeight;
      if (totalWeight <= 0)
      {
        Debug.LogWarning("[DropItemManager] Gem 与 Coin 掉落权重均为 0，已使用 Gem 作为保底掉落。", this);
        return gemPrefab;
      }

      return Random.Range(0, totalWeight) < gemDropWeight ? gemPrefab : coinPrefab;
    }

    private void PreloadDropItems()
    {
      if (preloadCountPerDropItem <= 0)
        return;

      if (gemPrefab != null)
        PoolManager.Instance.Preload(gemPrefab.gameObject, preloadCountPerDropItem);
      if (coinPrefab != null)
        PoolManager.Instance.Preload(coinPrefab.gameObject, preloadCountPerDropItem);
    }

    /// <summary>统一归还已拾取的掉落物，避免拾取路径直接销毁对象。</summary>
    public void RecycleDropItem(GameObject dropItem)
    {
      if (dropItem != null && dropItem.activeSelf)
        PoolManager.Instance.Free(dropItem);
    }

    /// <summary>
    /// 记录掉落物分数及相关进度；经验通过 AddExperience 显式结算，避免 Coin 意外触发升级。
    /// </summary>
    public void AddScore(int score)
    {
      totalScore += score;
      speedUpScore += score;
      skillUpScore += score;

      if (speedUpScore > speedUpRateScore)
      {
        speedUpScore = 0;
        speedUpRateScore += 100;
        EnemyDirector.Instance.SpeedUpSpawnRate();
      }

      if (skillUpScore >= skillUpRateScore)
      {
        skillUpScore = 0;
        // BuffManager.Instance.hero.GetComponent<BuffHandler>().AddBuff(BuffManager.Instance.playerAttackRangeSO);
      }

    }

    /// <summary>
    /// 经验是 Gem 的专属收益，金币只增加金币数量，不应进入升级经验结算。
    /// </summary>
    public void AddExperience(int experience)
    {
      if (experience <= 0)
        return;

      ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor)?.UpdateExp(experience);
    }

    public void AddDropItem(DropItemType dropItemType, int count)
    {
      ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor)?.AddDropItem(dropItemType, count);
    }
  }
}
