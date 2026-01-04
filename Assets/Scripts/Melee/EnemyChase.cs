using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyChase : MonoBehaviour
{
  private Transform player;

  [Header("Chase Settings")]
  [SerializeField] private float moveSpeed = 2f;
  [SerializeField] private float chaseRange = 5f;
  [SerializeField] private float stopChaseRange = 0.5f;

  [Header("Ground Check")]
  [SerializeField] private Transform groundCheck;
  [SerializeField] private LayerMask groundLayer;
  [SerializeField] private float checkRadius = 0.2f;

  [Header("Attack Settings")]
  [SerializeField] private Transform attackPoint;
  [SerializeField] private LayerMask playerLayer;
  [SerializeField] private float attackRadius = 1f;
  [SerializeField] private float attackCoolDownTime = 3f;
  [SerializeField] private float attackDamage = 10f;

  private Rigidbody2D rb;
  private Animator anim;
  private bool isGrounded;
  private bool isAttacking;
  private float attackTimer;
  private EntityState entityState;
  private bool IsHit;

  void Start()
  {
    rb = GetComponent<Rigidbody2D>();
    anim = GetComponent<Animator>();
    player = GameObject.FindGameObjectWithTag("Player").transform;
  }

  void Update()
  {
    HandleCheckGround();
    HandleChase();
    HandleAttack();
  }

  private void HandleChase()
  {
    if (player == null || !isGrounded || isAttacking || IsHit) return;

    float distanceToPlayer = Vector2.Distance(transform.position, player.position);
    if (distanceToPlayer <= stopChaseRange)
    {
      ChangeState(EntityState.Idle);
      return;
    }

    if (distanceToPlayer <= chaseRange)
    {
      Vector2 direction = (player.position - transform.position).normalized;
      rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
      ChangeState(EntityState.Run);
      if (direction.x != 0)
      {
        transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(transform.localScale.x),
          transform.localScale.y, transform.localScale.z);
      }
    }
    else
    {
      ChangeState(EntityState.Idle);
    }
  }

  private void HandleCheckGround()
  {
    isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
  }

  private void HandleAttack()
  {
    if (attackTimer > 0)
    {
      attackTimer -= Time.deltaTime;
    }

    float distanceToPlayer = Vector2.Distance(transform.position, player.position);
    Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
    if (distanceToPlayer <= attackRadius && attackTimer <= 0 && !isAttacking && player != null && hitPlayers.Length > 0 && !IsHit)
    {
      ChangeState(EntityState.Attack);
    }
  }

  // Animation Event
  private void Attack()
  {
    Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
    foreach (Collider2D player in hitPlayers)
    {
      player.GetComponent<PlayerController>().TakeDamage(attackDamage);
    }
  }

  // Animation Event
  public void ChangeState(EntityState newState)
  {
    if (entityState == newState) return;
    var preState = entityState;
    entityState = newState;

    switch (entityState)
    {
      case EntityState.Idle:
        if (preState == EntityState.Attack)
        {
          isAttacking = false;
          anim.SetBool("Attack", false);
        }
        else if (preState == EntityState.Run)
        {
          anim.SetFloat("Speed", 0);
          rb.velocity = Vector2.zero;
        }
        else if (preState == EntityState.Hurt)
        {
          IsHit = false;
        }
        break;
      case EntityState.Run:
        anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        break;
      case EntityState.Attack:
        anim.SetBool("Attack", true);
        isAttacking = true;
        attackTimer = attackCoolDownTime;
        break;
      case EntityState.Hurt:
        anim.SetTrigger("Hurt");
        rb.velocity = Vector2.zero;
        IsHit = true;
        break;
      case EntityState.Death:
        anim.SetTrigger("Death");
        rb.velocity = Vector2.zero;
        break;
    }
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.white;
    Gizmos.DrawWireSphere(transform.position, chaseRange);
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, stopChaseRange);
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
  }
}
