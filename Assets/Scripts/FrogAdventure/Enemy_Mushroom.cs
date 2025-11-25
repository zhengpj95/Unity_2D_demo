using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;

public class Enemy_Mushroom : MonoBehaviour
{
  private static readonly int IsRunning = Animator.StringToHash("isRunning");
  private static readonly int IsHit = Animator.StringToHash("isHit");

  public float jumpForce = 5f;

  [Header("两点巡逻")] public Transform[] targetPoints;
  public float speed = 2f;
  public float stopDuration = 1f;

  [Header("地板检测")] public Transform groundCheck;
  public float checkRadius = 0.2f;
  public LayerMask groundLayer;

  private Rigidbody2D _rb2d;
  private Animator _animator;
  private int _pointIdx;
  private float _stopTimer;
  private bool _isHit;

  private void Start()
  {
    _rb2d = GetComponent<Rigidbody2D>();
    _animator = GetComponent<Animator>();
  }

  private void FixedUpdate()
  {
    if (!IsGrounded() || _isHit) return;

    // 在两点之间巡逻，同时修改动画状态
    var targetPoint = targetPoints[_pointIdx];
    var distanceX = Mathf.Abs(transform.position.x - targetPoint.position.x);

    if (distanceX < 0.5f)
    {
      // 进入 Idle
      _animator.SetBool(IsRunning, false);
      _stopTimer += Time.deltaTime;
      if (_stopTimer >= stopDuration)
      {
        _stopTimer = 0;
        _pointIdx = (_pointIdx + 1) % targetPoints.Length;
      }
    }
    else
    {
      // 进入 Run
      _animator.SetBool(IsRunning, true);
      float dir = Mathf.Sign(transform.position.x - targetPoint.position.x);
      transform.localScale = new Vector3(dir, 1, 1);
      transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, Time.deltaTime * speed);
    }
  }

  private bool IsGrounded()
  {
    return Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
  }

  private void OnCollisionEnter2D(Collision2D other)
  {
    if (other.gameObject.CompareTag("Player") &&
        other.otherCollider.gameObject.layer == LayerMask.NameToLayer("Ground"))
    {
      var playerMovement = other.gameObject.GetComponent<PlayerMovement>();
      playerMovement.JumpUp(jumpForce);
      _rb2d.simulated = false;
      _isHit = true;
      _animator.SetTrigger(IsHit);
    }
  }

  // 销毁父节点
  private void DestroySelf()
  {
    if (gameObject.transform?.parent)
    {
      Destroy(gameObject.transform.parent.gameObject);
    }
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
  }
}