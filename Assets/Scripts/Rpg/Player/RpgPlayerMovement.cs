using System.Collections;
using UnityEngine;

public class RpgPlayerMovement : MonoBehaviour
{
  private static readonly int VelocityHash = Animator.StringToHash("velocity");

  private Rigidbody2D _rb2d;
  private Animator _animator;

  private Vector2 _movement;
  private bool _isKnockBack;
  private int _facingRight = 1;

  private void Start()
  {
    _rb2d = GetComponent<Rigidbody2D>();
    _animator = GetComponent<Animator>();
  }

  private void FixedUpdate()
  {
    if (!_isKnockBack)
    {
      _movement.x = Input.GetAxis("Horizontal");
      _movement.y = Input.GetAxis("Vertical");
      _rb2d.velocity = _movement * StatsManager.Instance.speed;

      if ((_movement.x > 0f && _facingRight == -1) || (_movement.x < 0f && _facingRight == 1))
      {
        Flip();
      }

      _animator.SetFloat(VelocityHash, _movement.sqrMagnitude);
    }
  }

  private void Flip()
  {
    _facingRight *= -1;
    transform.localScale = new Vector2(transform.localScale.x * -1, transform.localScale.y);
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