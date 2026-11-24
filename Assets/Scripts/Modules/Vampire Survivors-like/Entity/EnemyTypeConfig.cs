using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// 敌人类型配置资源：集中保存一种敌人的动画、战斗数值、碰撞体和显示偏移。
  /// 当前仅作为配置数据载体，供编辑器工具创建与修改；把配置应用到实例的逻辑由后续生成链路实现。
  /// </summary>
  [CreateAssetMenu(fileName = "NewEnemyTypeConfig", menuName = "Game Data/Survivor/Enemies/Enemy Type Config")]
  public sealed class EnemyTypeConfig : ScriptableObject
  {
    [Header("基础信息")]
    [SerializeField, Tooltip("敌人类型标识，用于 Inspector 识别与日志输出。")]
    private string typeId = "enemy";

    [Header("动画")]
    [SerializeField, Tooltip("该类型使用的序列帧动画配置资源。")]
    private AnimFrameConfig animConfig;

    [Header("战斗数值")]
    [SerializeField, Min(0f), Tooltip("追击速度，单位为世界单位/秒。")]
    private float chaseSpeed = 0.8f;
    [SerializeField, Min(0), Tooltip("碰撞玩家时造成的伤害。")]
    private int damage = 2;
    [SerializeField, Min(1), Tooltip("最大生命值；从对象池取出时应按该值重置。")]
    private int maxHealth = 6;

    [Header("碰撞体")]
    [SerializeField, Tooltip("CapsuleCollider2D 相对根节点的中心偏移。")]
    private Vector2 colliderOffset;
    [SerializeField, Tooltip("CapsuleCollider2D 尺寸：x 为横截面直径，y 为高度。")]
    private Vector2 colliderSize = new Vector2(0.28f, 0.34f);
    [SerializeField, Tooltip("胶囊碰撞体方向：0 为 Vertical，1 为 Horizontal。")]
    private CapsuleDirection2D colliderDirection;

    [Header("显示（可选）")]
    [SerializeField, Tooltip("SpriteRenderer 绘制尺寸；两个分量均为 0 时沿用预制体默认值。")]
    private Vector2 spriteSize;
    [SerializeField, Tooltip("血条相对根节点的局部位置；Vector3.zero 表示沿用预制体默认值。")]
    private Vector3 hpBarLocalPosition;
    [SerializeField, Tooltip("血条局部缩放；Vector3.one 表示沿用预制体默认值。")]
    private Vector3 hpBarLocalScale = Vector3.one;

    /// <summary>敌人类型标识。</summary>
    public string TypeId => typeId;
    /// <summary>该类型使用的序列帧动画配置。</summary>
    public AnimFrameConfig AnimConfig => animConfig;
    /// <summary>追击速度（世界单位/秒）。</summary>
    public float ChaseSpeed => chaseSpeed;
    /// <summary>碰撞伤害。</summary>
    public int Damage => damage;
    /// <summary>最大生命值。</summary>
    public int MaxHealth => maxHealth;
    /// <summary>胶囊碰撞体中心偏移。</summary>
    public Vector2 ColliderOffset => colliderOffset;
    /// <summary>胶囊碰撞体尺寸。</summary>
    public Vector2 ColliderSize => colliderSize;
    /// <summary>胶囊碰撞体方向。</summary>
    public CapsuleDirection2D ColliderDirection => colliderDirection;
    /// <summary>SpriteRenderer 绘制尺寸；为零表示沿用预制体默认值。</summary>
    public Vector2 SpriteSize => spriteSize;
    /// <summary>血条局部位置。</summary>
    public Vector3 HpBarLocalPosition => hpBarLocalPosition;
    /// <summary>血条局部缩放。</summary>
    public Vector3 HpBarLocalScale => hpBarLocalScale;
  }
}
