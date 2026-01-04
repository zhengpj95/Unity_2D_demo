using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowWeapon : MonoBehaviour
{
  [SerializeField] private float speed = 2f;

  private Transform target;

  void Start()
  {
    Destroy(gameObject, 10f);
  }

  public void SetTarget(Transform targetTransform)
  {
    target = targetTransform;
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
      Destroy(gameObject);
      Destroy(collision.gameObject);
    }
  }
}
