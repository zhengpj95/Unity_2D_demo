using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevationExit : MonoBehaviour
{
  public Collider2D[] colliders;
  public Collider2D[] boundaryColliders;

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Player"))
    {
      foreach (Collider2D collider1 in colliders)
      {
        collider1.enabled = true;
      }

      foreach (Collider2D collider1 in boundaryColliders)
      {
        collider1.enabled = false;
      }

      other.gameObject.GetComponent<SpriteRenderer>().sortingOrder = 5; // 设置为默认层级
      
      Debug.Log("11111 exit2D");
    }
    if (other.gameObject.CompareTag("Enemy"))
    {
      other.gameObject.GetComponent<SpriteRenderer>().sortingOrder = 5;
    }
  }
}