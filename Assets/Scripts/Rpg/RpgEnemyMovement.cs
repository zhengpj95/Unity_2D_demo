using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum EnemyType
{
  Normal,
  Goblin,
}

/**
 * 1. 敌人跟随玩家的时候，玩家进入高地会改变层级，敌人也需要调整层级
 * 2. 敌人攻击动画
 * 3. 技能冷却时间
 */
public class RpgEnemyMovement : MonoBehaviour
{
  private static readonly int IsMoving = Animator.StringToHash("isMoving");
  private static readonly int IsIdle = Animator.StringToHash("isIdle");
  private static readonly int IsFighting = Animator.StringToHash("isFighting");
  private static readonly int IsDamage = Animator.StringToHash("isDamage");
  private static readonly int IsDeath = Animator.StringToHash("isDeath");

  [Header("Movement Settings")]
  public float moveRange = 2f;
  public Transform movePoint;
  public LayerMask playerLayer;

  private Transform _player;
  private Rigidbody2D _rb2d;
  private Animator _animator;

  private int _facingRight = 1;
  private float _attackCoolDownTimer;

  public EnemyState enemyState { get; set; }
  public EnemyType enemyType = EnemyType.Normal;

  private readonly Collider2D[] _playerColliders = new Collider2D[5];

  private RpgEnemyCombat _combat;

  public enum EnemyState
  {
    Idle,
    Moving,
    Fighting,
    KnockBack,
    Death,
    Damage
  }

  private void Start()
  {
    _rb2d = GetComponent<Rigidbody2D>();
    _animator = GetComponent<Animator>();
    _combat = GetComponent<RpgEnemyCombat>();
    ChangeState(EnemyState.Idle);
  }

  private void Update()
  {
    if (enemyState == EnemyState.Death || enemyState == EnemyState.Damage)
    {
      return;
    }

    if (enemyState != EnemyState.KnockBack)
    {
      if (_attackCoolDownTimer > 0)
      {
        _attackCoolDownTimer -= Time.deltaTime;
      }
      else
      {
        CheckPlayer();
      }

      if (enemyState == EnemyState.Moving)
      {
        Moving();
      }
      else if (enemyState == EnemyState.Fighting)
      {
        // 进入fight，不动 
        _rb2d.velocity = Vector2.zero;
      }
    }
  }

  private bool CheckDistance()
  {
    return Vector2.Distance(transform.position, _player.transform.position) < _combat.attackRange;
  }

  private void Moving()
  {
    if (transform.position.x > _player.position.x && _facingRight == 1 ||
        transform.position.x < _player.position.x && _facingRight == -1)
    {
      Flip();
    }

    Vector2 direction = (_player.position - transform.position).normalized;
    _rb2d.velocity = direction * StatsManager.Instance.enemySpeed;
  }

  private void Flip()
  {
    _facingRight *= -1;
    transform.localScale = new Vector2(transform.localScale.x * -1, transform.localScale.y);
  }

  private void CheckPlayer()
  {
    var size = Physics2D.OverlapCircleNonAlloc(movePoint.position, moveRange, _playerColliders, playerLayer);
    if (size > 0 && _playerColliders.Length > 0)
    {
      _player = _playerColliders[0].transform;
      if (CheckDistance())
      {
        _attackCoolDownTimer = _combat.attackCoolDownTime;
        ChangeState(EnemyState.Fighting);
      }
      else if (!CheckDistance() && enemyState != EnemyState.Fighting)
      {
        ChangeState(EnemyState.Moving);
      }
    }
    else
    {
      _player = null;
      _rb2d.velocity = Vector2.zero;
      ChangeState(EnemyState.Idle);
    }
  }

  // 改变状态
  public void ChangeState(EnemyState state)
  {
    if (enemyState == EnemyState.Death)
    {
      return;
    }

    if (enemyState == EnemyState.Idle)
    {
      _animator.SetBool(IsIdle, false);
    }
    else if (enemyState == EnemyState.Moving)
    {
      _animator.SetBool(IsMoving, false);
    }
    else if (enemyState == EnemyState.Fighting)
    {
      _animator.SetBool(IsFighting, false);
    }
    else if (enemyState == EnemyState.Damage)
    {
      _animator.SetBool(IsDamage, false);
    }

    enemyState = state;

    if (enemyState == EnemyState.Idle)
    {
      _animator.SetBool(IsIdle, true);
    }
    else if (enemyState == EnemyState.Moving)
    {
      _animator.SetBool(IsMoving, true);
    }
    else if (enemyState == EnemyState.Fighting)
    {
      _animator.SetBool(IsFighting, true);
    }
    else if (enemyState == EnemyState.Death)
    {
      _animator.SetBool(IsDeath, true);
    }
    else if (enemyState == EnemyState.Damage)
    {
      _animator.SetBool(IsDamage, true);
    }
  }

  // 绘制攻击范围和检测范围
  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(movePoint.position, moveRange);
  }
}