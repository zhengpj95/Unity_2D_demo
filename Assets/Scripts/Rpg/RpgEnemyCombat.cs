using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 1. 攻击玩家
 * 2. 伤害减少
 */
public class RpgEnemyCombat : MonoBehaviour
{
  [Header("Attack Settings")] 
  public Transform attackPoint;
  public LayerMask playerLayer;
  public float weaponRange;

  [Header("KnockBack Settings")] 
  public float knockBackForce = 2;
  public float stunTime = 0.5f;

  private readonly Collider2D[] _playerColliders = new Collider2D[5];

  private void Attack()
  {
    var size = Physics2D.OverlapCircleNonAlloc(attackPoint.position, weaponRange, _playerColliders, playerLayer);
    if (size > 0 && _playerColliders.Length > 0)
    {
      _playerColliders[0].GetComponent<RpgPlayerHealth>().ChangeHealth(-StatsManager.Instance.enemyDamage);
      _playerColliders[0].GetComponent<RpgPlayerMovement>().KnockBack(transform, knockBackForce, stunTime);
    }
  }
  
  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireSphere(attackPoint.position, weaponRange);
  }
}