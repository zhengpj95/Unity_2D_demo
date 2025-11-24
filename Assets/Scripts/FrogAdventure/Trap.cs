using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
  private void OnCollisionEnter2D(Collision2D other)
  {
    if (other.gameObject.CompareTag("Player"))
    {
      Debug.Log("Player Hit");
      var knockBack = other.gameObject.GetComponent<PlayerKnockBack>();
      knockBack.KnockBack(transform);
    }
  }
}