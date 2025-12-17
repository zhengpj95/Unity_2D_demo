using System.Collections;
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
    if (_rpgEnemyMovement.enemyState == RpgEnemyMovement.EnemyState.Death)
    {
      return;
    }

    _rpgEnemyMovement.ChangeState(RpgEnemyMovement.EnemyState.Damage);
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