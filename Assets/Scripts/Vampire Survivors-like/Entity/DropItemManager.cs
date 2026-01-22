using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropItemManager : SingletonMono<DropItemManager>
{
  [SerializeField] private Transform dropItemContainer;
  [SerializeField] private Transform gemPrefab;
  [SerializeField] private Transform coinPrefab;

  [SerializeField] private int totalScore = 0; // 总积分

  [SerializeField] private int speedUpRateScore = 100; // 每一次提升所需积分
  private int speedUpScore = 0; // 速度提升积分
  [SerializeField] private int skillUpRateScore = 20; // 每一次升级所需积分
  private int skillUpScore = 0; // 技能升级积分

  public int CoinCount { get; set; } = 0;
  public int GemCount { get; set; } = 0;

  public void SpawnDropItem(Vector3 position, DropItemType dropItemType, float dropItemProb)
  {
    if (Random.value > dropItemProb)
    {
      return;
    }
    var prefab = gemPrefab;
    switch (dropItemType)
    {
      case DropItemType.Gem:
        prefab = gemPrefab;
        break;
      case DropItemType.Coin:
        prefab = coinPrefab;
        break;
    }
    if (prefab != null)
    {
      Instantiate(prefab, position, Quaternion.identity, dropItemContainer);
    }
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
      EnemySpawnManager.Instance.SpeedUpSpawnRate();
    }

    if (skillUpScore >= skillUpRateScore)
    {
      skillUpScore = 0;
      // VSUIManager.Instance.ShowSkillSelectPanel(true);
      // BuffManager.Instance.hero.GetComponent<BuffHandler>().AddBuff(BuffManager.Instance.playerAttackRangeSO);
    }

    VSUIManager.Instance.UpdateExp(score);
  }

  public void AddDropItem(DropItemType dropItemType, int count)
  {
    switch (dropItemType)
    {
      case DropItemType.Gem:
        GemCount += count;
        break;
      case DropItemType.Coin:
        CoinCount += count;
        break;
    }
    VSUIManager.Instance.UpdateInventory();
  }
}
