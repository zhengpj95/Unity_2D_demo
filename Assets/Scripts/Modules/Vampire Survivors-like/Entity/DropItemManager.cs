using UnityEngine;

namespace VampireSurvivorsLike
{
  public class DropItemManager : SingletonMono<DropItemManager>
  {
    [SerializeField] private Transform dropItemContainer;
    [SerializeField] private Transform gemPrefab;
    [SerializeField] private Transform coinPrefab;
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

    public void SpawnDropItem(Vector3 position, DropItemType dropItemType, float dropItemProb)
    {
      if (Random.value > dropItemProb)
        return;

      Transform prefab = dropItemType switch
      {
        DropItemType.Gem => gemPrefab,
        DropItemType.Coin => coinPrefab,
        _ => null,
      };

      if (prefab == null)
        return;

      GameObject dropItem = PoolManager.Instance.Alloc(prefab.gameObject, position, Quaternion.identity);
      if (dropItem != null && dropItemContainer != null)
        dropItem.transform.SetParent(dropItemContainer, true);
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
