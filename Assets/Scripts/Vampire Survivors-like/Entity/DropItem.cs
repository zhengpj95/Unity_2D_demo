using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DropItem : MonoBehaviour
{
  [SerializeField] private int score = 1;

  void OnTriggerEnter2D(Collider2D other)
  {
    if (other.CompareTag("Player"))
    {
      Destroy(gameObject);
      DropItemManager.Instance.AddScore(score);
    }
  }
}
