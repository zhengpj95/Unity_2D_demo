using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 弓箭武器
 */
public class ArrowWeapon : MonoBehaviour
{
  private bool initialized;
  private float speed = 2f;
  private int damage = 1;
  private Transform target;

  public void Init(Transform targetTransform, WeaponLevelData levelData)
  {
    target = targetTransform;
    speed = levelData.speed;
    damage = levelData.damage;
    initialized = true;
  }

  private void Update()
  {
    if (!initialized) return;
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
    if (!initialized) return;
    if (collision.gameObject.CompareTag("Enemy"))
    {
      VSHealth vSHealth = collision.gameObject.GetComponent<VSHealth>();
      vSHealth.TakeDamage(damage);
      Destroy(gameObject); // 销毁弓箭
    }
  }
}
