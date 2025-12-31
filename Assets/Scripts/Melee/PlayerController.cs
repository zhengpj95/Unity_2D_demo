using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

  public EntityState playerState { get; private set; }

  void Start()
  {
    rb = GetComponent<Rigidbody2D>();
    anim = GetComponent<Animator>();
  }

  void Update()
  {
    HandleGroundCheck();
    HandleAttack();
    HandleMove();
    HandleJump();
    UpdateStateFromMovement();
  }

  private void UpdateStateFromMovement()
  {
    // 不在受击或攻击时，基于速度与着地状态决定 Idle/Run/Jump/Fall
    if (playerState == EntityState.Attack || playerState == EntityState.Hurt) return;

    if (!isGrounded)
    {
      if (rb.velocity.y > 0.1f) ChangeState(EntityState.Jump);
      else ChangeState(EntityState.Fall);
    }
    else
    {
      if (Mathf.Abs(rb.velocity.x) > 0.1f) ChangeState(EntityState.Run);
      else ChangeState(EntityState.Idle);
    }

    // 持续更新一些 animator 参数
    anim.SetFloat("VerticalVelocity", rb.velocity.y);
    anim.SetBool("IsGrounded", isGrounded);
    anim.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
  }

  private void ChangeState(EntityState newState)
  {
    if (playerState == newState) return;
    playerState = newState;

    // 根据进入状态触发对应动画或行为
    switch (newState)
    {
      case EntityState.Idle:
        // 无需额外 trigger
        break;
      case EntityState.Run:
        break;
      case EntityState.Jump:
        anim.SetTrigger("Jump");
        break;
      case EntityState.Fall:
        // 使用 VerticalVelocity/IsGrounded 控制过渡
        break;
      case EntityState.Attack:
        anim.SetTrigger("Attack");
        rb.velocity = Vector2.zero; // attack 时停止移动
        break;
      case EntityState.Hurt:
        anim.SetTrigger("Hurt");
        break;
    }
  }

  private void HandleMove()
  {
    if (playerState == EntityState.Attack || playerState == EntityState.Hurt) return;

    float moveX = Input.GetAxisRaw("Horizontal");
    rb.velocity = new Vector2(moveX * moveSpeed, rb.velocity.y);

    if (moveX != 0)
    {
      transform.localScale = new Vector3(Mathf.Sign(moveX), 1, 1);
    }
  }

  private void HandleJump()
  {
    if (playerState == EntityState.Attack || playerState == EntityState.Hurt)
    {
      return;
    }

    // Idle/Run -> Jump
    if (Input.GetButtonDown("Jump") && isGrounded)
    {
      rb.velocity = new Vector2(rb.velocity.x, jumpForce);
      ChangeState(EntityState.Jump);
    }
    // Jump -> Fall
    // 由 UpdateStateFromMovement 更新 VerticalVelocity 与 IsGrounded
  }

  private void HandleGroundCheck()
  {
    isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
  }

  private void HandleAttack()
  {
    if (!isGrounded || playerState == EntityState.Attack || playerState == EntityState.Hurt)
    {
      return;
    }

    if (Input.GetButtonDown("Fire1"))
    {
      ChangeState(EntityState.Attack);
    }
  }

  // Animation Event
  private void AttackEnd()
  {
    ChangeState(EntityState.Idle);
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
    ChangeState(EntityState.Hurt);
  }

  public void TakeDamageEnd()
  {
    ChangeState(EntityState.Idle);
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
  }
}
