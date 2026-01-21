using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DropItem : MonoBehaviour
{
  [SerializeField] private int score = 1;
  [SerializeField] private DropItemType dropItemType;

  void OnTriggerEnter2D(Collider2D other)
  {
    if (other.CompareTag("Player"))
    {
      DropItemManager.Instance.AddScore(score);
      DropItemManager.Instance.AddDropItem(dropItemType, 1);
      Destroy(gameObject);
    }
  }
}
