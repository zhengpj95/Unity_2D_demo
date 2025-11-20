using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RpgEnemyKnockBack : MonoBehaviour
{
  private Rigidbody2D _rb2d;
  private RpgEnemyMovement _rpgEnemyMovement;

  void Start()
  {
    _rb2d = GetComponent<Rigidbody2D>();
    _rpgEnemyMovement = GetComponent<RpgEnemyMovement>();
  }

  public void KnockBack(Transform player, float knockBackForce, float stunTime)
  {
    _rpgEnemyMovement.ChangeState(RpgEnemyMovement.EnemyState.KnockBack);
    StartCoroutine(KnockBackCounter(stunTime));

    Vector2 direction = (transform.position - player.position).normalized;
    _rb2d.velocity = direction * knockBackForce;
  }

  private IEnumerator KnockBackCounter(float stunTime)
  {
    yield return new WaitForSeconds(stunTime);
    _rb2d.velocity = Vector2.zero;
    _rpgEnemyMovement.ChangeState(RpgEnemyMovement.EnemyState.Idle);
  }
}