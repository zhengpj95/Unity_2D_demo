using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RpgPlayerArcherBow : MonoBehaviour
{
  public GameObject arrowPrefab;
  public Transform firePoint;

  private Vector2 aimDir = Vector2.right;
  private Animator animator;

  private void Start()
  {
    animator = GetComponent<Animator>();
  }

  void Update()
  {
    HandleAiming();
    if (Input.GetButtonDown("Fire1"))
    {
      Fire();
    }
  }

  private void HandleAiming()
  {
    var x = Input.GetAxisRaw("Horizontal");
    var y = Input.GetAxisRaw("Vertical");
    if (x != 0 || y != 0)
    {
      aimDir = new Vector2(x, y).normalized;
    }
  }

  private void Fire()
  {
    animator.SetBool("isAttacking", true);
  }

  // Animation Event
  private void Shoot()
  {
    animator.SetBool("isAttacking", false);
    GameObject arrow = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
    Arrow arrowScript = arrow.GetComponent<Arrow>();
    arrowScript.direction = aimDir;
  }
}
