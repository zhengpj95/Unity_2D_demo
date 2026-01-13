using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 锯子武器
 */
public class SawWeapon : MonoBehaviour
{
  private float radius = 1.2f;
  private float rotateSpeed = 180f; // 度/秒
  private int damage = 1;

  private float angle;

  public void SetLevelData(WeaponLevelData weaponLevelData)
  {
    radius = weaponLevelData.range;
    damage = weaponLevelData.damage;
    rotateSpeed = weaponLevelData.speed;
  }

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
      VSHealth vSHealth = collision.gameObject.GetComponent<VSHealth>();
      vSHealth.TakeDamage(damage);
    }
  }
}
