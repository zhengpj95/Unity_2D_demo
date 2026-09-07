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
    // 为兼容现有场景序列化继续保留字段名；运行时语义是武器索敌距离，不是武器碰撞体大小。
    public float baseAttackRange = 4f;
    // 基础拾取半径；运行时会由触发器同步到 PickupRadius。
    [SerializeField, Min(0.1f)] private float basePickupRadius = 0.1f;

    public float debugSpeed;// todo test
    public float debugRange;// todo test
    private CircleCollider2D pickupCollider;

    /// <summary>基础速度、SurvivorModel 永久升级和临时 Buff 合并后的移动速度。</summary>
    public float MoveSpeed => Mathf.Max(0f, CalculateStat(PlayerStat.MoveSpeed, baseMoveSpeed));

    /// <summary>武器寻找目标的最终距离；与未来用于缩放武器碰撞体的 WeaponArea 分离。</summary>
    public float TargetingRange => Mathf.Max(0.1f, CalculateStat(PlayerStat.TargetingRange, baseAttackRange));

    /// <summary>兼容现有武器调用方的旧名称；新代码应使用 TargetingRange。</summary>
    public float AttackRange => TargetingRange;

    /// <summary>当前拾取范围，DropItem 通过玩家触发器使用该值。</summary>
    public float PickupRadius => Mathf.Max(0.1f, CalculateStat(PlayerStat.PickupRadius, basePickupRadius));

    private Rigidbody2D _rb;
    private Animator _animator;
    private Vector2 _lastFacing = Vector2.down; // 初始朝向，默认向下
    private BuffHandler _buffHandler;
    private SurvivorModule _survivorModule;

    private void Awake()
    {
      // Unity 组件引用在 Awake 缓存；BuffHandler 缺失时补充，保证所有玩家都有临时属性入口。
      _rb = GetComponent<Rigidbody2D>();
      _animator = GetComponent<Animator>();
      _buffHandler = GetComponent<BuffHandler>();
      if (_buffHandler == null)
        _buffHandler = gameObject.AddComponent<BuffHandler>();

      _survivorModule = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);

      // 使用独立触发器扩大拾取范围，不改变玩家原有碰撞体。
      pickupCollider = GetComponent<CircleCollider2D>();
      if (pickupCollider == null)
        pickupCollider = gameObject.AddComponent<CircleCollider2D>();
      pickupCollider.isTrigger = true;
      pickupCollider.radius = PickupRadius;
    }

    void Start()
    {
      WeaponManager.Instance.AddOrUpgrade(baseWeapon);
      // 直接从玩法场景启动时，Module 初始化顺序可能晚于 Hero.Awake，因此在 Start 再解析一次。
      if (_survivorModule == null)
        _survivorModule = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
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
      debugRange = TargetingRange;
      // 属性升级后同步触发器半径，使拾取范围立即生效。
      if (pickupCollider != null && !Mathf.Approximately(pickupCollider.radius, PickupRadius))
        pickupCollider.radius = PickupRadius;
      _rb.velocity = moveInput.normalized * MoveSpeed;
    }

    /// <summary>
    /// 根据统一玩家属性规则计算最终值。永久修正来自 SurvivorModel，临时修正来自 BuffHandler；
    /// Module 不可用时仍保留基础值和临时 Buff，便于单独预览 Hero Prefab。
    /// </summary>
    private float CalculateStat(PlayerStat stat, float baseValue)
    {
      PlayerStatModifier temporaryModifier = _buffHandler == null
        ? default
        : _buffHandler.GetStatModifier(stat);

      return _survivorModule == null
        ? temporaryModifier.ApplyTo(baseValue)
        : _survivorModule.CalculatePlayerStat(stat, baseValue, temporaryModifier);
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
      // 红色圆表示武器索敌范围，便于确认 TargetingRange 的最终计算结果。
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, TargetingRange);

      // 青色圆表示运行时 PickupRadius 触发器范围，中心与玩家根节点保持一致。
      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(transform.position, PickupRadius);
    }
  }

}
