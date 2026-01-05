using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropItemManager : SingletonMono<DropItemManager>
{
  [SerializeField] private Transform dropItemContainer;
  [SerializeField] private Transform gemPrefab;
  [SerializeField] private Transform coinPrefab;

  private int totalScore = 0;

  public void SpawnDropItem(Vector3 position, DropItemType dropItemType)
  {
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
    Debug.Log("Total Score: " + totalScore);
  }
}
