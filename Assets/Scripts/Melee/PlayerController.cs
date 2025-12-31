using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MeleePlayerState
{
  Idle,
  Run,
  Jump,
  Fall,
  Attack,
  Hurt,
  Death
}

public class PlayerController : MonoBehaviour
{
  [Header("Movement Settings")]
  [SerializeField] private float moveSpeed = 5f;
  [SerializeField] private float jumpForce = 5f;

  [Header("Ground Check")]
  [SerializeField] private Transform groundCheck;
  [SerializeField] private float checkRadius = 0.2f;
  [SerializeField] private LayerMask groundLayer;

  [Header("Attack Settings")]
  [SerializeField] private Transform attackPoint;
  [SerializeField] private float attackRadius = 0.5f;
  [SerializeField] private LayerMask attackLayer;
  [SerializeField] private float attackDamage = 10f;

  private Rigidbody2D rb;
  private Animator anim;

  private bool isGrounded;

  public MeleePlayerState playerState { get; private set; }

  void Start()
  {
    rb = GetComponent<Rigidbody2D>();
    anim = GetComponent<Animator>();
  }

  void Update()
  {
    HandleAttack();
    HandleMove();
    HandleJump();
    HandleGroundCheck();
  }

  private void HandleMove()
  {
    if (playerState == MeleePlayerState.Attack || playerState == MeleePlayerState.Hurt) return;

    float moveX = Input.GetAxisRaw("Horizontal");
    rb.velocity = new Vector2(moveX * moveSpeed, rb.velocity.y);
    anim.SetFloat("Speed", Mathf.Abs(moveX));

    if (moveX != 0)
    {
      transform.localScale = new Vector3(Mathf.Sign(moveX), 1, 1);
    }
  }

  private void HandleJump()
  {
    if (playerState == MeleePlayerState.Attack || playerState == MeleePlayerState.Hurt)
    {
      return;
    }

    // Idle/Run -> Jump
    if (Input.GetButtonDown("Jump") && isGrounded)
    {
      rb.velocity = new Vector2(rb.velocity.x, jumpForce);
      anim.SetTrigger("Jump");
    }
    // Jump -> Fall
    anim.SetFloat("VerticalVelocity", rb.velocity.y);
    // Fall -> Idle
    anim.SetBool("IsGrounded", isGrounded);
  }

  private void HandleGroundCheck()
  {
    isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
  }

  private void HandleAttack()
  {
    if (!isGrounded || playerState == MeleePlayerState.Attack || playerState == MeleePlayerState.Hurt)
    {
      return;
    }

    if (Input.GetButtonDown("Fire1"))
    {
      anim.SetTrigger("Attack");
      playerState = MeleePlayerState.Attack;
      rb.velocity = Vector2.zero;
    }
  }

  // Animation Event
  private void AttackEnd()
  {
    playerState = MeleePlayerState.Idle;
  }

  // Animation Event
  private void Attack()
  {
    Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, attackLayer);
    foreach (Collider2D enemy in hitEnemies)
    {
      enemy.GetComponent<EnemyHealth>().TakeDamage(attackDamage);
    }
  }

  public void TakeDamage(float damage)
  {
    Debug.Log("Player took " + damage + " damage.");
    anim.SetTrigger("Hurt");
    playerState = MeleePlayerState.Hurt;
  }

  public void TakeDamageEnd()
  {
    playerState = MeleePlayerState.Idle;
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
  }
}
