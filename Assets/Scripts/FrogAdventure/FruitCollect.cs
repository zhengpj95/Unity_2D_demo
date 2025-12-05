using System;
using System.Collections;
using UnityEngine;

public class FruitCollect : MonoBehaviour
{
  public int scoreValue = 1;
  public GameObject collectedPrefab;

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.CompareTag("Player"))
    {
      Destroy(gameObject);
      AddCollectedAnimation(gameObject.transform);
      FruitCollectManager.Instance.ChangeScore(scoreValue);
      Debug.Log("Fruit collected: " + FruitCollectManager.Instance.Score);

      var player = other.gameObject.GetComponent<PlayerMovement>();
      player.collectSound.Play();
    }
  }

  private void AddCollectedAnimation(Transform parent)
  {
    Instantiate(collectedPrefab, parent.position, Quaternion.identity);
  }
}