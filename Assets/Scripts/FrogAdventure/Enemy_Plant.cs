using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Plant : MonoBehaviour
{
  public Transform bullet;
  public Transform firePoint;

  private Animator _animator;
  private bool _isAttack;
  private float _timer;
  private readonly float _duration = 4f;

  private void Start()
  {
    _animator = GetComponent<Animator>();
  }

  private void Update()
  {
    if (!_isAttack)
    {
      _timer += Time.deltaTime;
      if (_timer >= _duration)
      {
        _timer = 0f;
        Attack();
      }
    }
  }

  public void Attack()
  {
    _isAttack = true;
    _animator.SetBool("isAttack", true);
  }

  public void AttackEnd()
  {
    _isAttack = false;
    _animator.SetBool("isAttack", false);
  }

  public void Fire()
  {
    Instantiate(bullet, firePoint.position, Quaternion.identity);
  }
}