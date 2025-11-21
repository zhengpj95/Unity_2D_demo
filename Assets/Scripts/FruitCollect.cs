using System;
using UnityEngine;

public class FruitCollect : MonoBehaviour
{
  public int scoreValue = 1;
  private FruitCollectManager collectManager;

  private void Start()
  {
    collectManager = FindAnyObjectByType<FruitCollectManager>();
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.CompareTag("Player"))
    {
      Destroy(gameObject);
      collectManager.ChangeScore(scoreValue);
      Debug.Log("Fruit collected: " + collectManager.score);
    }
  }
}