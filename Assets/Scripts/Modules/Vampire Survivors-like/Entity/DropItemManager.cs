using UnityEngine;

namespace VampireSurvivorsLike
{
  public class DropItemManager : SingletonMono<DropItemManager>
  {
    [SerializeField] private Transform dropItemContainer;
    [SerializeField] private Transform gemPrefab;
    [SerializeField] private Transform coinPrefab;

    [SerializeField] private int totalScore;
    [SerializeField] private int speedUpRateScore = 100;
    private int speedUpScore;
    [SerializeField] private int skillUpRateScore = 20;
    private int skillUpScore;

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

      if (prefab != null)
        Instantiate(prefab, position, Quaternion.identity, dropItemContainer);
    }

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

      ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor)?.UpdateExp(score);
    }

    public void AddDropItem(DropItemType dropItemType, int count)
    {
      ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor)?.AddDropItem(dropItemType, count);
    }
  }
}
