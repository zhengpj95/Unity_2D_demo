using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RpgPlayerArcherBow : MonoBehaviour
{
  public GameObject arrowPrefab;
  public Transform firePointHorizontal; // 水平
  public Transform firePointVerticalUp; // 垂直上
  public Transform firePointVerticalDown; // 垂直下
  public Transform firePointDiagonalUp; // 斜上
  public Transform firePointDiagonalDown; // 斜下
  public float shootCooldown = 0.5f;

  private float shootTimer = 0f;
  private Vector2 aimDir = Vector2.right;
  private Animator animator;

  private void Start()
  {
    animator = GetComponent<Animator>();
  }

  void Update()
  {
    if (shootTimer > 0)
    {
      shootTimer -= Time.deltaTime;
    }

    HandleAiming();
    if (Input.GetButtonDown("Shoot") && shootTimer <= 0)
    {
      animator.SetBool("isAttacking", true);
    }
  }

  private void HandleAiming()
  {
    var x = Input.GetAxisRaw("Horizontal");
    var y = Input.GetAxisRaw("Vertical");
    if (x != 0 || y != 0)
    {
      aimDir = new Vector2(x, y).normalized;
      Debug.Log(Time.deltaTime + ": " + aimDir);
      animator.SetFloat("aimX", x);
      animator.SetFloat("aimY", y);
    }
  }

  // Animation Event
  private void Fire()
  {
    if (shootTimer <= 0)
    {
      Transform point = GetFirePoint(aimDir);
      GameObject arrow = Instantiate(arrowPrefab, point.position, point.rotation);
      Arrow arrowScript = arrow.GetComponent<Arrow>();
      arrowScript.direction = aimDir;
      shootTimer = shootCooldown;
    }
  }

  /**
   * 获取箭矢发射点。（判断aimDir和左右上下对比，再判断aimDir.y，斜上还是歇下）
   * @param dir 箭矢方向
   * @return 箭矢发射点
  */
  private Transform GetFirePoint(Vector2 dir)
  {
    if (dir.sqrMagnitude < 0.0001f)
    {
      return firePointHorizontal;
    }
    float ax = Mathf.Abs(dir.x);
    float ay = Mathf.Abs(dir.y);
    // how much larger one axis must be to prefer it over the other
    const float dominance = 1.2f; // 判断轴主导性，表示某轴必须大于另一轴的倍数才能优先选择该轴
    if (ax > ay * dominance)
    {
      return firePointHorizontal;
    }
    if (ay > ax * dominance)
    {
      return dir.y > 0 ? firePointVerticalUp : firePointVerticalDown;
    }
    // Otherwise treat as diagonal (use vertical sign to decide up/down)
    return dir.y > 0 ? firePointDiagonalUp : firePointDiagonalDown;
  }

  // Animation Event
  private void Shoot()
  {
    animator.SetBool("isAttacking", false);
  }
}
