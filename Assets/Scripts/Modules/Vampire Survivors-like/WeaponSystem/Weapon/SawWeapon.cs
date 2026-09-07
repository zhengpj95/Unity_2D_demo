using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike {

  /**
   * 锯子武器
   */
  public class SawWeapon : PooledWeaponEffect
  {
    private bool initialized;
    private float radius = 1.2f;
    private float rotateSpeed = 180f; // 度/秒
    private int damage = 1;
    private float angle;

    /// <summary>启动绕玩家旋转的 Saw，并登记到创建它的武器控制器。</summary>
    public void Init(WeaponController owner, WeaponLevelData weaponLevelData)
    {
      if (weaponLevelData == null)
      {
        Debug.LogWarning("[SawWeapon] Missing weapon level data; effect was skipped.", this);
        PoolManager.Instance.Free(gameObject);
        return;
      }

      radius = weaponLevelData.range;
      damage = weaponLevelData.damage;
      rotateSpeed = weaponLevelData.speed;
      initialized = true;
      BeginEffect(owner, weaponLevelData.duration);
    }

    void Update()
    {
      if (!initialized || TryRecycleWhenExpired()) return;
      transform.Rotate(0, 0, 360 * Time.deltaTime);
      RotateAroundPlayer();
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
      if (!initialized || !IsActiveEffect) return;
      if (collision.gameObject.CompareTag("Enemy"))
      {
        VSEnemyHealth vSHealth = collision.gameObject.GetComponent<VSEnemyHealth>();
        if (vSHealth != null)
          vSHealth.TakeDamage(damage);
      }
    }

    /// <summary>重置环绕角度和变换，保证重复取出的 Saw 从一致位置开始旋转。</summary>
    protected override void ResetEffectState()
    {
      initialized = false;
      radius = 1.2f;
      rotateSpeed = 180f;
      damage = 1;
      angle = 0f;
      transform.localPosition = Vector3.zero;
      transform.localRotation = Quaternion.identity;
    }
  }

}
