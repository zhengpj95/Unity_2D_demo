using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 锯子武器
 */
public class SawWeapon : MonoBehaviour
{
  [Header("Rotation")]
  [SerializeField] private float radius = 1.2f;
  [SerializeField] private float rotateSpeed = 180f; // 度/秒
  [SerializeField] private int damage = 1;

  private float angle;

  void Update()
  {
    transform.Rotate(0, 0, 360 * Time.deltaTime);
    RotateAroundPlayer();
  }

  public void SetDestoryTime(float time)
  {
    Destroy(gameObject, time);
  }

  void RotateAroundPlayer()
  {
    angle += rotateSpeed * Time.deltaTime;

    Vector2 offset = new Vector2(
      Mathf.Cos(angle * Mathf.Deg2Rad),
      Mathf.Sin(angle * Mathf.Deg2Rad)
    ) * radius;

    transform.localPosition = offset;
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.gameObject.CompareTag("Enemy"))
    {
      EnemyChasing enemyChasing = collision.gameObject.GetComponent<EnemyChasing>();
      DamageController.Instance.ShowDamage(damage, enemyChasing.transform.position);
      DropItemManager.Instance.SpawnDropItem(enemyChasing.transform.position, enemyChasing.DropItemType);
      VSHealth vSHealth = collision.gameObject.GetComponent<VSHealth>();
      vSHealth.TakeDamage(damage);
    }
  }
}
