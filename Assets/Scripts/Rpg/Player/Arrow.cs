using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Arrow : MonoBehaviour
{
  private Rigidbody2D rb;
  public float speed = 10f;
  public Vector2 direction = Vector2.right;
  public float lifetime = 2f;

  void Start()
  {
    rb = GetComponent<Rigidbody2D>();

    RotateArrow();
    rb.velocity = direction * speed;
    Destroy(gameObject, lifetime);
  }

  private void RotateArrow()
  {
    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    transform.rotation = Quaternion.Euler(0, 0, angle);
  }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    if (collision.gameObject.CompareTag("Enemy"))
    {
      collision.gameObject.GetComponent<RpgEnemyHealth>()
        .ChangeHealth(-StatsManager.Instance.damage);
      collision.gameObject.GetComponent<RpgEnemyKnockBack>()
        .KnockBack(transform, 0.4f, 0.5f);
    }
    // 销毁箭头
    Destroy(gameObject);
  }
}
