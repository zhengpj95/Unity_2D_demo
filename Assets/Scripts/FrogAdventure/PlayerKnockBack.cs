using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerKnockBack : MonoBehaviour
{
  private Rigidbody2D _rb2d;
  private Animator _animator;

  [Tooltip("击退距离")]
  public float knockBackForce = 2.5f;
  [Tooltip("击退眩晕时间")]
  public float knockBackStunTime = 0.6f;

  void Start()
  {
    _rb2d = GetComponent<Rigidbody2D>();
    _animator = GetComponent<Animator>();
  }

  public void KnockBack(Transform trap)
  {
    GameController.Instance.IsKnockBack = true;
    _animator.SetTrigger("isHit");
    Vector2 direction = (transform.position - trap.position).normalized;
    _rb2d.velocity = direction * knockBackForce;
  }

  public void StopKnockBack()
  {
    _animator.SetInteger("state", 0);
    StartCoroutine(KnockBackCoroutine());
  }

  private IEnumerator KnockBackCoroutine()
  {
    yield return new WaitForSeconds(knockBackStunTime);
    GameController.Instance.IsKnockBack = false;
  }
}