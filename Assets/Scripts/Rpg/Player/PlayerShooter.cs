using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
  public GameObject bulletPrefab;
  public Transform firePoint;
  public float bulletSpeed = 10f;
  public float moveSpeed = 5f;

  private Rigidbody2D _rb;
  private Vector2 movement;
  private Vector2 mousePos;

  private void Start()
  {
    _rb = GetComponent<Rigidbody2D>();
  }

  void Update()
  {
    movement.x = Input.GetAxisRaw("Horizontal");
    movement.y = Input.GetAxisRaw("Vertical");
    mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

    if (Input.GetButtonDown("Fire1"))
    {
      Shoot();
    }
  }

  private void FixedUpdate()
  {
    _rb.velocity = movement.normalized * moveSpeed;

    // 计算方向向量
    Vector2 lookDir = mousePos - _rb.position;
    // 计算旋转角度，并应用到刚体。+90的偏移，因为默认精灵朝下
    float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg + 90f;
    _rb.rotation = angle;
  }

  private void Shoot()
  {
    GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
    rb.velocity = firePoint.right * bulletSpeed;
  }
}
