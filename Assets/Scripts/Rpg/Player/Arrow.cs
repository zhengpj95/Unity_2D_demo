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
      // 伤害和击退
      collision.gameObject.GetComponent<RpgEnemyHealth>()
        .ChangeHealth(-StatsManager.Instance.damage);
      collision.gameObject.GetComponent<RpgEnemyKnockBack>()
        .KnockBack(transform, 0.4f, 0.5f);

      // 箭插入敌人
      // PinToEnemy(collision.gameObject);
      Destroy(gameObject);
    }
    else
    {
      Destroy(gameObject);
    }
  }

  private void PinToEnemy(GameObject enemy)
  {
    // 记录箭相对于敌人的本地位置（这样敌人移动时箭会跟随）
    transform.SetParent(enemy.transform);

    // 停止物理移动
    rb.velocity = Vector2.zero;
    rb.angularVelocity = 0f;
    rb.isKinematic = true;  // 改为动画刚体，不再受物理影响

    // 禁用碰撞，避免重复触发或与其他物体碰撞
    GetComponent<Collider2D>().enabled = false;

    // 几秒后销毁（或者由敌人销毁时一起销毁）
    Destroy(gameObject, 3f);
  }
}
