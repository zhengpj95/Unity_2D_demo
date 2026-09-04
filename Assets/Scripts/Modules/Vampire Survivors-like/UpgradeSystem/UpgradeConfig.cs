using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// 一条可出现在升级三选一中的配置。配置是只读资源，运行时效果写入玩家/武器实例。
  /// </summary>
  public abstract class UpgradeConfig : ScriptableObject
  {
    // 面板标题；为空时由具体升级类型生成默认标题。
    [SerializeField] private string title;
    [TextArea]
    // 面板描述，用于向玩家说明本次升级的实际效果。
    [SerializeField] private string description;
    // 面板图标；PlayerUpgrade 可以不配置图标。
    [SerializeField] private Sprite icon;

    /// <summary>返回由枚举和目标数据生成的稳定标识，用于候选去重和日志定位。</summary>
    public string Id => GetUniqueId();
    /// <summary>自定义显示标题。</summary>
    public string Title => title;
    /// <summary>显示在升级卡片上的效果描述。</summary>
    public string Description => description;
    /// <summary>显示在升级卡片上的图标。</summary>
    public Sprite Icon => icon;

    /// <summary>该配置所属的固定升级枚举，禁止通过 Inspector 随意填写字符串。</summary>
    public abstract UpgradeId TypeId { get; }

    /// <summary>
    /// 返回本轮候选去重使用的唯一键。武器类配置会在枚举后追加 weaponId，避免不同武器互相覆盖。
    /// </summary>
    public virtual string GetUniqueId()
    {
      return TypeId.ToString();
    }

    /// <summary>返回显示标题；未配置标题时默认使用资源名。</summary>
    public virtual string GetDisplayTitle()
    {
      return string.IsNullOrWhiteSpace(title) ? name : title;
    }

    /// <summary>根据当前玩家和武器状态判断候选是否仍然可用。</summary>
    public abstract bool IsAvailable(PlayerUpgradeContext context);
    /// <summary>将候选效果应用到运行时对象，不修改配置资源。</summary>
    public abstract void Apply(PlayerUpgradeContext context);

    /// <summary>初始化运行时自动生成候选的显示信息；唯一 ID 由 TypeId 和目标数据自动计算。</summary>
    protected void SetRuntimeInfo(string runtimeTitle, string runtimeDescription, Sprite runtimeIcon)
    {
      title = runtimeTitle;
      description = runtimeDescription;
      icon = runtimeIcon;
    }
  }

  /// <summary>升级配置执行时所需的运行时依赖，避免配置对象自行查找场景对象。</summary>
  public sealed class PlayerUpgradeContext
  {
    /// <summary>当前局使用的武器管理器。</summary>
    public WeaponManager WeaponManager { get; }
    /// <summary>当前局玩家实体及其持久属性。</summary>
    public Hero Hero { get; }
    /// <summary>当前局玩家生命组件。</summary>
    public VSPlayerHealth PlayerHealth { get; }

    /// <summary>创建一次升级应用所需的运行时依赖快照。</summary>
    public PlayerUpgradeContext(WeaponManager weaponManager, Hero hero, VSPlayerHealth playerHealth)
    {
      WeaponManager = weaponManager;
      Hero = hero;
      PlayerHealth = playerHealth;
    }
  }

  public enum PlayerUpgradeStat
  {
    // 百分比或固定值都由 PlayerUpgradeConfig 的 isPercent 决定。
    MoveSpeed,
    PickupRadius,
    MaxHealth,
    AttackRange,
  }

  /// <summary>
  /// 升级候选的固定类型 ID。新增升级类型时先扩展此枚举，再在对应配置中映射，避免手填字符串造成重复。
  /// </summary>
  public enum UpgradeId
  {
    NewWeapon,
    WeaponLevel,
    PlayerMoveSpeed,
    PlayerPickupRadius,
    PlayerMaxHealth,
    PlayerAttackRange,
  }

  [CreateAssetMenu(fileName = "NewWeaponUpgrade", menuName = "Survivor/Upgrade/New Weapon")]
  public sealed class NewWeaponUpgradeConfig : UpgradeConfig
  {
    // 新武器候选的目标武器资源；运行时只读取，不修改资源。
    [SerializeField] private WeaponSO weapon;

    /// <summary>新武器候选对应的 WeaponSO。</summary>
    public WeaponSO Weapon => weapon;

    /// <summary>新武器候选的固定类型 ID。</summary>
    public override UpgradeId TypeId => UpgradeId.NewWeapon;

    /// <summary>按目标武器追加唯一键，避免多个新武器候选互相去重。</summary>
    public override string GetUniqueId()
    {
      return $"{TypeId}:{weapon?.weaponId}";
    }

    public override string GetDisplayTitle()
    {
      return string.IsNullOrWhiteSpace(Title) && weapon != null
        ? $"获得 {weapon.weaponName}"
        : base.GetDisplayTitle();
    }

    public override bool IsAvailable(PlayerUpgradeContext context)
    {
      return context?.WeaponManager != null
        && weapon != null
        && !context.WeaponManager.HasWeapon(weapon)
        && context.WeaponManager.CanAddWeapon(weapon);
    }

    public override void Apply(PlayerUpgradeContext context)
    {
      context?.WeaponManager?.TryAddOrUpgrade(weapon);
    }

    /// <summary>为没有独立资源的默认候选写入运行时显示和目标武器。</summary>
    public void InitializeRuntime(WeaponSO runtimeWeapon)
    {
      weapon = runtimeWeapon;
      SetRuntimeInfo(
        runtimeWeapon == null ? "新武器" : $"获得 {runtimeWeapon.weaponName}",
        runtimeWeapon == null ? string.Empty : "获得一把新的武器。", runtimeWeapon?.icon);
    }
  }

  [CreateAssetMenu(fileName = "WeaponUpgrade", menuName = "Survivor/Upgrade/Weapon Level")]
  public sealed class WeaponUpgradeConfig : UpgradeConfig
  {
    // 武器升级候选的目标 WeaponSO；当前等级由 WeaponManager 的实例保存。
    [SerializeField] private WeaponSO weapon;

    /// <summary>武器升级候选对应的 WeaponSO。</summary>
    public WeaponSO Weapon => weapon;

    /// <summary>武器等级升级候选的固定类型 ID。</summary>
    public override UpgradeId TypeId => UpgradeId.WeaponLevel;

    /// <summary>按目标武器追加唯一键，确保每把武器都有独立的升级候选。</summary>
    public override string GetUniqueId()
    {
      return $"{TypeId}:{weapon?.weaponId}";
    }

    public override string GetDisplayTitle()
    {
      return string.IsNullOrWhiteSpace(Title) && weapon != null
        ? $"{weapon.weaponName} 升级"
        : base.GetDisplayTitle();
    }

    public override bool IsAvailable(PlayerUpgradeContext context)
    {
      return context?.WeaponManager != null
        && weapon != null
        && context.WeaponManager.HasWeapon(weapon)
        && context.WeaponManager.CanUpgrade(weapon);
    }

    public override void Apply(PlayerUpgradeContext context)
    {
      context?.WeaponManager?.TryAddOrUpgrade(weapon);
    }

    /// <summary>为默认武器升级候选写入运行时显示和目标武器。</summary>
    public void InitializeRuntime(WeaponSO runtimeWeapon)
    {
      weapon = runtimeWeapon;
      SetRuntimeInfo(
        runtimeWeapon == null ? "武器升级" : $"{runtimeWeapon.weaponName} 升级",
        runtimeWeapon == null ? string.Empty : "提升该武器的下一等级属性。", runtimeWeapon?.icon);
    }
  }

  [CreateAssetMenu(fileName = "PlayerUpgrade", menuName = "Survivor/Upgrade/Player Stat")]
  public sealed class PlayerUpgradeConfig : UpgradeConfig
  {
    // 要修改的玩家属性。
    [SerializeField] private PlayerUpgradeStat stat;
    // 增量值；isPercent 为 true 时按百分比解释。
    [SerializeField] private float value = 0.1f;
    // 是否按百分比叠加，否则按固定值叠加。
    [SerializeField] private bool isPercent = true;

    /// <summary>根据玩家属性映射固定枚举，保证同一属性不会因手填 ID 而产生重复候选。</summary>
    public override UpgradeId TypeId => GetUpgradeId(stat);

    private static UpgradeId GetUpgradeId(PlayerUpgradeStat playerStat)
    {
      switch (playerStat)
      {
        case PlayerUpgradeStat.MoveSpeed: return UpgradeId.PlayerMoveSpeed;
        case PlayerUpgradeStat.PickupRadius: return UpgradeId.PlayerPickupRadius;
        case PlayerUpgradeStat.MaxHealth: return UpgradeId.PlayerMaxHealth;
        case PlayerUpgradeStat.AttackRange: return UpgradeId.PlayerAttackRange;
        default: return UpgradeId.PlayerMoveSpeed;
      }
    }

    public override string GetDisplayTitle()
    {
      return string.IsNullOrWhiteSpace(Title) ? GetStatTitle() : base.GetDisplayTitle();
    }

    public override bool IsAvailable(PlayerUpgradeContext context)
    {
      return context?.Hero != null || (stat == PlayerUpgradeStat.MaxHealth && context?.PlayerHealth != null);
    }

    public override void Apply(PlayerUpgradeContext context)
    {
      if (context == null) return;
      if (stat == PlayerUpgradeStat.MaxHealth)
      {
        context.PlayerHealth?.ApplyMaxHealthUpgrade(value, isPercent);
        return;
      }

      context.Hero?.ApplyUpgrade(stat, value, isPercent);
    }

    /// <summary>初始化一个运行时生成的玩家属性候选。</summary>
    /// <param name="runtimeIcon">运行时升级卡片使用的图标；由 UpgradeManager 从场景配置传入。</param>
    public void InitializeRuntime(PlayerUpgradeStat runtimeStat, float runtimeValue, bool runtimeIsPercent, Sprite runtimeIcon)
    {
      stat = runtimeStat;
      value = runtimeValue;
      isPercent = runtimeIsPercent;
      // 默认属性升级不是项目资源，必须在创建运行时配置时显式带入图标，否则 View 会隐藏 Image。
      SetRuntimeInfo(GetStatTitle(), GetStatDescription(), runtimeIcon);
    }

    private string GetStatTitle()
    {
      switch (stat)
      {
        case PlayerUpgradeStat.MoveSpeed: return "移动速度提升";
        case PlayerUpgradeStat.PickupRadius: return "拾取范围提升";
        case PlayerUpgradeStat.MaxHealth: return "最大生命提升";
        case PlayerUpgradeStat.AttackRange: return "攻击范围提升";
        default: return "玩家属性提升";
      }
    }

    private string GetStatDescription()
    {
      string amount = isPercent ? $"{value * 100f:F0}%" : $"{value:F0}";
      return $"{GetStatTitle()} +{amount}";
    }
  }
}
