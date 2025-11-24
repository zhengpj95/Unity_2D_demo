using System;
using UnityEngine;

public class FruitCollect : MonoBehaviour
{
  public int scoreValue = 1;

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.CompareTag("Player"))
    {
      Destroy(gameObject);
      FruitCollectManager.Instance.ChangeScore(scoreValue);
      Debug.Log("Fruit collected: " + FruitCollectManager.Instance.Score);
    }
  }
}