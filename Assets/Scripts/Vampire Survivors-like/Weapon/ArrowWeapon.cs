using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowWeapon : MonoBehaviour
{
  [SerializeField] private float speed = 2f;

  private Transform target;

  public void SetTarget(Transform targetTransform, float destoryTime)
  {
    target = targetTransform;
    Destroy(gameObject, destoryTime);
  }

  private void Update()
  {
    if (target != null)
    {
      Vector3 dir = (target.position - transform.position).normalized;
      transform.position += dir * speed * Time.deltaTime;

      float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
      transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    else
    {
      // 没有目标则直线前进，箭头朝向右边，所以是right
      transform.position += transform.right * speed * Time.deltaTime;
    }
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.gameObject.CompareTag("Enemy"))
    {
      EnemyChasing enemyChasing = collision.gameObject.GetComponent<EnemyChasing>();
      DamageController.Instance.ShowDamage(enemyChasing.Damage, transform.position);
      Destroy(gameObject);
      Destroy(collision.gameObject);
    }
  }
}
