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

  private Rigidbody2D rb;
  private Animator anim;

  private bool isGrounded;
  private bool isAttack;

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
    if (isAttack) return;

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
    if (isAttack) return;

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
    if (!isGrounded) return;

    if (Input.GetButtonDown("Fire1"))
    {
      anim.SetTrigger("Attack");
      isAttack = true;
      rb.velocity = Vector2.zero;
    }
  }

  // Animation Event
  private void AttackEnd()
  {
    isAttack = false;
  }
}
