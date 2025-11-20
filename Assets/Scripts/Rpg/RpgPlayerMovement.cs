using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class RpgPlayerMovement : MonoBehaviour
{
  private static readonly int VelocityHash = Animator.StringToHash("velocity");

  private RpgPlayerCombat _combat;

  private Rigidbody2D _rb2d;
  private Animator _animator;
  private SpriteRenderer _renderer;

  private Vector2 _movement;
  private bool _isKnockBack;

  private void Start()
  {
    _rb2d = GetComponent<Rigidbody2D>();
    _animator = GetComponent<Animator>();
    _renderer = GetComponent<SpriteRenderer>();
    _combat = GetComponent<RpgPlayerCombat>();
  }

  private void Update()
  {
    if (Input.GetButtonDown("Jump"))
    {
      _combat.Attack();
    }
  }

  private void FixedUpdate()
  {
    if (!_isKnockBack)
    {
      _movement.x = Input.GetAxis("Horizontal");
      _movement.y = Input.GetAxis("Vertical");
      _rb2d.velocity = _movement * StatsManager.Instance.speed;

      if (_movement.x > 0f) _renderer.flipX = false;
      else if (_movement.x < 0f) _renderer.flipX = true;

      _animator.SetFloat(VelocityHash, _movement.sqrMagnitude);
    }
  }

  // 击退
  public void KnockBack(Transform enemy, float knockBackForce, float stunTime)
  {
    _isKnockBack = true;
    Vector2 direction = (transform.position - enemy.position).normalized;
    _rb2d.velocity = direction * knockBackForce;
    StartCoroutine(KnockBackCounter(stunTime));
  }

  private IEnumerator KnockBackCounter(float stunTime)
  {
    yield return new WaitForSeconds(stunTime);
    _isKnockBack = false;
    _rb2d.velocity = Vector2.zero;
  }
}