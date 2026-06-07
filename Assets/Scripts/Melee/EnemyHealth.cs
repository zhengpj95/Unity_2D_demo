using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
  [SerializeField] private float maxHealth = 100f;
  [SerializeField][Tooltip("Y 轴低于此值时判定为掉落死亡")]
  private float fallDeathY = -10f;

  private EnemyChase enemyChase;
  private float currentHealth;
  private bool hasFallenToDeath;

  void Start()
  {
    enemyChase = GetComponent<EnemyChase>();
    currentHealth = maxHealth;
  }

  void Update()
  {
    if (!hasFallenToDeath && transform.position.y <= fallDeathY)
    {
      hasFallenToDeath = true;
      currentHealth = 0;
      if (enemyChase != null)
      {
        enemyChase.ChangeState(EntityState.Death);
      }
      else
      {
        Destroy(gameObject);
      }
    }
  }

  public void TakeDamage(float damage)
  {
    if (currentHealth <= 0) return;

    currentHealth -= damage;
    if (currentHealth <= 0)
    {
      enemyChase.ChangeState(EntityState.Death);
    }
    else
    {
      enemyChase.ChangeState(EntityState.Hurt);
    }
  }

  // Animation Event
  private void Death()
  {
    Destroy(gameObject);
  }
}
