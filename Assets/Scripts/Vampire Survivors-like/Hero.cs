using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;

/**
  * top-down movement hero
  * blend tree 处理walk动画，4个walk动画
  * blend tree 处理idle动画，4个idle动画
  */
public class Hero : MonoBehaviour
{
  public WeaponSO baseWeapon;
  public float baseMoveSpeed = 2f;
  public float attackRange = 4f;

  public float debugSpeed;// todo test
  public float moveSpeed
  {
    get => baseMoveSpeed * (1 + _buffHandler.GetMoveSpeedMultiplier());
  }

  private Rigidbody2D _rb;
  private Animator _animator;
  private Vector2 _lastFacing = Vector2.down; // 初始朝向，默认向下
  private BuffHandler _buffHandler;

  void Start()
  {
    _rb = GetComponent<Rigidbody2D>();
    _animator = GetComponent<Animator>();
    WeaponManager.Instance.AddOrUpgrade(baseWeapon);
    _buffHandler = GetComponent<BuffHandler>();
  }

  void Update()
  {
    var moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    if (moveInput != Vector2.zero)
    {
      var face = GetCardinal(moveInput);
      _lastFacing = face;
      _animator.SetFloat("speed", 1);
      _animator.SetFloat("xVelocity", face.x);
      _animator.SetFloat("yVelocity", face.y);
    }
    else
    {
      _animator.SetFloat("speed", 0);
      _animator.SetFloat("moveX", _lastFacing.x);
      _animator.SetFloat("moveY", _lastFacing.y);
    }
    // _rb.MovePosition(rb.position + moveInput.normalized * moveSpeed * Time.deltaTime);
    debugSpeed = moveSpeed;
    _rb.velocity = moveInput.normalized * moveSpeed;
  }

  private Vector2 GetCardinal(Vector2 v)
  {
    const float dead = 0.1f;
    // 横向分量 > 纵向分量，在横向移动
    if (Mathf.Abs(v.x) > Mathf.Abs(v.y) && Mathf.Abs(v.x) > dead)
      return new Vector2(Mathf.Sign(v.x), 0);
    // 判断有没有纵向输入，有则纵向移动
    if (Mathf.Abs(v.y) > dead)
      return new Vector2(0, Mathf.Sign(v.y));
    return _lastFacing;
  }

  void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, attackRange);
  }
}
