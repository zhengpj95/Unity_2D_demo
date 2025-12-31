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

  private Rigidbody2D rb;
  private Animator anim;
  private bool isGrounded;
  private bool isAttacking;
  private float attackTimer;
  private EnemyHealth enemyHealth;

  void Start()
  {
    rb = GetComponent<Rigidbody2D>();
    anim = GetComponent<Animator>();
    player = GameObject.FindGameObjectWithTag("Player").transform;
    enemyHealth = GetComponent<EnemyHealth>();
  }

  void Update()
  {
    HandleCheckGround();
    HandleChase();
    HandleAttack();
  }

  private void HandleChase()
  {
    if (player == null || !isGrounded || isAttacking || enemyHealth.IsHit) return;

    float distanceToPlayer = Vector2.Distance(transform.position, player.position);
    if (distanceToPlayer <= stopChaseRange)
    {
      anim.SetFloat("Speed", 0);
      rb.velocity = Vector2.zero;
      return;
    }

    if (distanceToPlayer <= chaseRange)
    {
      Vector2 direction = (player.position - transform.position).normalized;
      rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
      anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
      if (direction.x != 0)
      {
        transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(transform.localScale.x),
          transform.localScale.y, transform.localScale.z);
      }
    }
    else
    {
      anim.SetFloat("Speed", 0);
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
    if (distanceToPlayer <= attackRadius && attackTimer <= 0 && !isAttacking && player != null && hitPlayers.Length > 0 && !enemyHealth.IsHit)
    {
      anim.SetBool("IsAttacking", true);
      isAttacking = true;
      attackTimer = attackCoolDownTime;
    }
  }

  // Animation Event
  private void Attack()
  {
    Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
    foreach (Collider2D player in hitPlayers)
    {
      player.GetComponent<PlayerController>().TakeDamage(10f);
    }
  }

  // Animation Event
  public void ChangeState()
  {
    Debug.Log("Enemy finished attacking.");
    isAttacking = false;
    anim.SetBool("IsAttacking", false);
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
