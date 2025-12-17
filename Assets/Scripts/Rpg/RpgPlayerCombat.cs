using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class RpgPlayerCombat : MonoBehaviour
{
  [Header("Attack Settings")]
  public float attackCoolDownTime = 0.8f;
  public Transform attackPoint;
  public LayerMask enemyLayer;
  public float weaponRange;

  [Header("KnockBack Settings")]
  public float knockBackForce = 2;
  public float stunTime = 0.5f;

  private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
  private Animator _animator;

  private float _attackTimer;
  private readonly Collider2D[] _enemyColliders = new Collider2D[20];

  private void Start()
  {
    _animator = GetComponent<Animator>();
  }

  private void Update()
  {
    if (_attackTimer > 0)
    {
      _attackTimer -= Time.deltaTime;
    }

    if (_attackTimer <= 0 && Input.GetButtonDown("Jump"))
    {
      Attack();
    }
  }

  // 按下空格，攻击
  public void Attack()
  {
    _attackTimer = attackCoolDownTime;
    _animator.SetBool(IsAttacking, true);
  }

  // 处理伤害
  public void DealDamage()
  {
    var size = Physics2D.OverlapCircleNonAlloc(attackPoint.position, weaponRange, _enemyColliders, enemyLayer);
    if (size > 0 && _enemyColliders.Length > 0)
    {
      for (int i = 0; i < size; i++)
      {
        var enemy = _enemyColliders[i];
        if (enemy == null) continue;
        enemy.GetComponent<RpgEnemyHealth>().ChangeHealth(-StatsManager.Instance.damage);
        enemy.GetComponent<RpgEnemyKnockBack>().KnockBack(transform, knockBackForce, stunTime);
      }
    }
  }

  // 结束攻击
  public void FinishAttack()
  {
    _animator.SetBool(IsAttacking, false);
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireSphere(attackPoint.position, weaponRange);
  }
}