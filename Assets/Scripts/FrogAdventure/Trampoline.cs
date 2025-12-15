using System;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
  private static readonly int IsIsJumping = Animator.StringToHash("isJumping");
  private Animator _animator;

  public float jumpSpeed = 20f;

  private void Start()
  {
    _animator = GetComponent<Animator>();
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Player"))
    {
      var rb = other.gameObject.GetComponent<Rigidbody2D>();
      rb.AddForce(Vector2.up * jumpSpeed, ForceMode2D.Impulse);
      _animator.SetBool(IsIsJumping, true);
    }
  }

  private void StartJump()
  {
  }

  private void JumpEnd()
  {
    _animator.SetBool(IsIsJumping, false);
  }
}