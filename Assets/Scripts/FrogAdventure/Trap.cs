using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
  private void OnCollisionEnter2D(Collision2D other)
  {
    if (other.gameObject.CompareTag("Player") &&
        other.otherCollider.gameObject.layer != LayerMask.NameToLayer("Ground"))
    {
      Debug.Log("Player Hit");
      var knockBack = other.gameObject.GetComponent<PlayerKnockBack>();
      knockBack.KnockBack(transform); // 玩家 knockback
    }
  }
}