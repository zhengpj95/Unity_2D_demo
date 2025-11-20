using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevationEntry : MonoBehaviour
{
  public Collider2D[] colliders;
  public Collider2D[] boundaryColliders;

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Player"))
    {
      foreach (Collider2D collider1 in colliders)
      {
        collider1.enabled = false;
      }

      foreach (Collider2D collider1 in boundaryColliders)
      {
        collider1.enabled = true;
      }

      other.gameObject.GetComponent<SpriteRenderer>().sortingOrder = 20; // 设置为高层级，在高地之上

      Debug.Log("11111 enter2D");
    }

    if (other.gameObject.name == "Enemy_Minotaur")
    {
      other.gameObject.GetComponent<SpriteRenderer>().sortingOrder = 20;
    }
  }
}