using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyChase : MonoBehaviour
{
  private Transform player;

  [Header("Chase Settings")]
  [SerializeField] private float chaseRange = 5f;
  [SerializeField] private float stopChaseRange = 0.5f;
  [SerializeField] private float moveSpeed = 2f;

  [Header("Ground Check")]
  [SerializeField] private Transform groundCheck;
  [SerializeField] private float checkRadius = 0.2f;
  [SerializeField] private LayerMask groundLayer;

  private Rigidbody2D rb;
  private Animator anim;
  private bool isGrounded;

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
  }

  private void HandleChase()
  {
    if (player == null || !isGrounded) return;

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

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.white;
    Gizmos.DrawWireSphere(transform.position, chaseRange);
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, stopChaseRange);
  }
}
