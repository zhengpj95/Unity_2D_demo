using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;

/**
  * top-down movement hero
  * blend tree 处理移动动画，2D simple directional 处理4个方向
  * 下一步要处理：idle朝向，向上run后，idle就要朝上；其他类似
  */
public class Hero : MonoBehaviour
{
  public float runSpeed = 2f;
  private Rigidbody2D _rb;
  private Animator _animator;

  void Start()
  {
    _rb = GetComponent<Rigidbody2D>();
    _animator = GetComponent<Animator>();
  }

  void Update()
  {
    var moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    if (moveInput != Vector2.zero)
    {
      _animator.SetFloat("speed", 1);
      _animator.SetFloat("xVelocity", moveInput.x);
      _animator.SetFloat("yVelocity", moveInput.y);
    }
    else
    {
      _animator.SetFloat("speed", 0);
    }
    // rb.MovePosition(rb.position + moveInput.normalized * runSpeed * Time.deltaTime);
    _rb.velocity = moveInput.normalized * runSpeed;
  }
}
