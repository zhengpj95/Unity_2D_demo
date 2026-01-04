using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowWeapon : MonoBehaviour
{
  [SerializeField] private float speed = 2f;

  private Vector3 targetPosition;

  void Start()
  {
    Destroy(gameObject, 10f);
  }

  public void SetTarget(Transform targetTransform)
  {
    targetPosition = targetTransform.position.normalized * 100;
  }

  private void Update()
  {
    if (targetPosition != null)
    {
      Vector3 dir = (targetPosition - transform.position).normalized;
      transform.position += dir * speed * Time.deltaTime;

      float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
      transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
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
