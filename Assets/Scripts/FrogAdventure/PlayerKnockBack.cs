using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Android.LowLevel;
using UnityEngine.SceneManagement;

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

  // 受击
  public void KnockBack(Transform trap)
  {
    GameController.Instance.IsKnockBack = true;

    GameController.Instance.UpdateHp(-1);
    if (GameController.Instance.MaxHp == 0)
    {
      _animator.SetTrigger("isDeath");
      return;
    }

    _animator.SetTrigger("isHit");
    Vector2 direction = (transform.position - trap.position).normalized;
    _rb2d.velocity = direction * knockBackForce;
  }

  // 受击结束
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

  // 死亡
  private void Death()
  {
    Debug.Log("Death");
    GameController.Instance.ResetHp();
    GameController.Instance.IsKnockBack = false;
    EventBus.Dispatch("PLAYER_REVIVE");
    Destroy(gameObject);
  }
}