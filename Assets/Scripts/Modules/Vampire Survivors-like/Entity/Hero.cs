using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;

namespace VampireSurvivorsLike
{

  /**
    * top-down movement hero
    * blend tree 处理walk动画，4个walk动画
    * blend tree 处理idle动画，4个idle动画
    */
  public class Hero : MonoBehaviour
  {
    public WeaponSO baseWeapon;
    public float baseMoveSpeed = 2f;
    public float baseAttackRange = 4f;
    // 基础拾取半径；运行时会由触发器同步到 PickupRadius。
    [SerializeField, Min(0.1f)] private float basePickupRadius = 0.05f;

    public float debugSpeed;// todo test
    public float debugRange;// todo test
    // 以下字段保存本局升级结果，避免直接改写 Hero Prefab 或其他配置资源。
    private float moveSpeedBonusPercent;
    private float moveSpeedBonusFlat;
    private float attackRangeBonusPercent;
    private float attackRangeBonusFlat;
    private float pickupRadiusBonusPercent;
    private float pickupRadiusBonusFlat;
    private CircleCollider2D pickupCollider;

    /// <summary>基础速度、玩家升级和临时 Buff 合并后的移动速度。</summary>
    public float MoveSpeed
    {
      get => (baseMoveSpeed + moveSpeedBonusFlat) * (1 + moveSpeedBonusPercent + (_buffHandler?.GetMoveSpeedMultiplier() ?? 0));
    }
    /// <summary>基础攻击范围与玩家升级、临时 Buff 合并后的范围。</summary>
    public float AttackRange
    {
      get => (baseAttackRange + attackRangeBonusFlat) * (1 + attackRangeBonusPercent) + (_buffHandler?.GetAttackRangeMultiplier() ?? 0);
    }
    /// <summary>当前拾取范围，DropItem 通过玩家触发器使用该值。</summary>
    public float PickupRadius => Mathf.Max(0.1f, (basePickupRadius + pickupRadiusBonusFlat) * (1 + pickupRadiusBonusPercent));

    private Rigidbody2D _rb;
    private Animator _animator;
    private Vector2 _lastFacing = Vector2.down; // 初始朝向，默认向下
    private BuffHandler _buffHandler;

    void Start()
    {
      _rb = GetComponent<Rigidbody2D>();
      _animator = GetComponent<Animator>();
      WeaponManager.Instance.AddOrUpgrade(baseWeapon);
      _buffHandler = GetComponent<BuffHandler>();
      // 使用独立触发器扩大拾取范围，不改变玩家原有碰撞体。
      pickupCollider = GetComponent<CircleCollider2D>();
      if (pickupCollider == null)
        pickupCollider = gameObject.AddComponent<CircleCollider2D>();
      pickupCollider.isTrigger = true;
      pickupCollider.radius = PickupRadius;
    }

    void Update()
    {
      var moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
      if (moveInput != Vector2.zero)
      {
        var face = GetCardinal(moveInput);
        _lastFacing = face;
        _animator.SetFloat("speed", 1);
        _animator.SetFloat("xVelocity", face.x);
        _animator.SetFloat("yVelocity", face.y);
      }
      else
      {
        _animator.SetFloat("speed", 0);
        _animator.SetFloat("moveX", _lastFacing.x);
        _animator.SetFloat("moveY", _lastFacing.y);
      }
      // _rb.MovePosition(rb.position + moveInput.normalized * MoveSpeed * Time.deltaTime);
      debugSpeed = MoveSpeed;
      debugRange = AttackRange;
      // 属性升级后同步触发器半径，使拾取范围立即生效。
      if (pickupCollider != null && !Mathf.Approximately(pickupCollider.radius, PickupRadius))
        pickupCollider.radius = PickupRadius;
      _rb.velocity = moveInput.normalized * MoveSpeed;
    }

    /// <summary>应用一条持久的玩家属性升级，不修改任何 ScriptableObject 配置。</summary>
    public void ApplyUpgrade(PlayerUpgradeStat stat, float value, bool isPercent)
    {
      if (value <= 0f) return;

      switch (stat)
      {
        case PlayerUpgradeStat.MoveSpeed:
          if (isPercent) moveSpeedBonusPercent += value;
          else moveSpeedBonusFlat += value;
          break;
        case PlayerUpgradeStat.PickupRadius:
          if (isPercent) pickupRadiusBonusPercent += value;
          else pickupRadiusBonusFlat += value;
          break;
        case PlayerUpgradeStat.AttackRange:
          if (isPercent) attackRangeBonusPercent += value;
          else attackRangeBonusFlat += value;
          break;
      }
    }

    private Vector2 GetCardinal(Vector2 v)
    {
      const float dead = 0.1f;
      // 横向分量 > 纵向分量，在横向移动
      if (Mathf.Abs(v.x) > Mathf.Abs(v.y) && Mathf.Abs(v.x) > dead)
        return new Vector2(Mathf.Sign(v.x), 0);
      // 判断有没有纵向输入，有则纵向移动
      if (Mathf.Abs(v.y) > dead)
        return new Vector2(0, Mathf.Sign(v.y));
      return _lastFacing;
    }

    void OnDrawGizmosSelected()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
  }

}
