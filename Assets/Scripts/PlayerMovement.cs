using System;
using UnityEngine;

internal enum PlayerState
{
  Idle = 0,
  Run = 1,
  Jump = 2,
  Fall = 3
}

public class PlayerMovement : MonoBehaviour
{
  public int moveSpeed = 5;
  public int jumpSpeed = 5;

  public Transform groundCheck;
  public float checkRadius = 0.2f;
  public LayerMask groundLayer;
  private Animator _animator;

  private float _dirX;

  private Rigidbody2D _rb2d;
  private SpriteRenderer _sprite;

  private void Start()
  {
    _rb2d = GetComponent<Rigidbody2D>();
    _sprite = GetComponent<SpriteRenderer>();
    _animator = GetComponent<Animator>();
  }

  private void Update()
  {
    // 获取左右键的输入
    _dirX = Input.GetAxis("Horizontal");
    _rb2d.velocity = new Vector2(_dirX * moveSpeed, _rb2d.velocity.y);

    if (_dirX > 0) _sprite.flipX = false;
    else if (_dirX < 0) _sprite.flipX = true;

    if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
    {
      // _rb2d.velocity = new Vector2(_rb2d.velocity.x, 0f); // 防止叠力跳
      _rb2d.AddForce(Vector2.up * jumpSpeed, ForceMode2D.Impulse);
    }

    var state = PlayerState.Idle;
    if (_rb2d.velocity.y > 0.1f)
      state = PlayerState.Jump;
    else if (_rb2d.velocity.y < -0.1f)
      state = PlayerState.Fall;
    else if (Math.Abs(_dirX) > 0.1f) state = PlayerState.Run;

    _animator.SetInteger("state", (int)state);
  }

  private bool IsGrounded()
  {
    return Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
  }
}