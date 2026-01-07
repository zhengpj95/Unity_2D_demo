using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropItemManager : SingletonMono<DropItemManager>
{
  [SerializeField] private Transform dropItemContainer;
  [SerializeField] private Transform gemPrefab;
  [SerializeField] private Transform coinPrefab;

  private int totalScore = 0;
  private int speedUpScore = 0; // 速度提升积分
  private int speedUpRateScore = 100; // 每一次提升所需积分

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
    Debug.Log("Total Score: " + totalScore);

    if (speedUpScore > speedUpRateScore)
    {
      speedUpScore = 0;
      speedUpRateScore += 100;
      EnemySpawnManager.Instance.SpeedUpSpawnRate();
    }
  }
}
