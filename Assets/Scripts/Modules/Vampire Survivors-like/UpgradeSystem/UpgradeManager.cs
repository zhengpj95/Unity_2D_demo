using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivorsLike
{
  /// <summary>
  /// 生成升级候选池并过滤不可用配置。没有在 Inspector 配置资源时，会从现有 WeaponManager 和基础玩家升级生成默认候选。
  /// </summary>
  public sealed class UpgradeManager : SingletonMono<UpgradeManager>
  {
    // 可选的资源配置会与运行时自动生成候选合并，便于 Inspector 扩展新升级。
    [Tooltip("可选的自定义升级配置；运行时会与现有武器及基础玩家升级合并。")]
    [SerializeField] private UpgradeConfig[] upgradeConfigs;

    [Header("默认玩家属性升级图标")]
    [Tooltip("移动速度升级卡片的图标。默认候选由本组件在运行时创建，因此图标要在这里配置。")]
    [SerializeField] private Sprite moveSpeedUpgradeIcon;
    [Tooltip("拾取范围升级卡片的图标。")]
    [SerializeField] private Sprite pickupRadiusUpgradeIcon;
    [Tooltip("最大生命升级卡片的图标。")]
    [SerializeField] private Sprite maxHealthUpgradeIcon;

    // 自动候选只创建一次，避免每次打开升级面板重复创建 ScriptableObject。
    private readonly List<UpgradeConfig> runtimeConfigs = new List<UpgradeConfig>();
    private bool runtimeConfigsBuilt;

    /// <summary>使用当前场景中的玩家和武器管理器生成候选升级。</summary>
    public UpgradeConfig[] GetUpgradeOptions(int count)
    {
      GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
      Hero hero = playerObject == null ? null : playerObject.GetComponent<Hero>();
      VSPlayerHealth health = playerObject == null ? null : playerObject.GetComponent<VSPlayerHealth>();
      return GetUpgradeOptions(count, new PlayerUpgradeContext(WeaponManager.Instance, hero, health));
    }

    public UpgradeConfig[] GetUpgradeOptions(int count, PlayerUpgradeContext context)
    {
      // 候选池在每次调用时重新过滤，确保上一次选择已立即影响下一轮结果。
      if (count <= 0 || context == null)
        return new UpgradeConfig[0];

      BuildRuntimeConfigs(context.WeaponManager);

      var candidates = new List<UpgradeConfig>();
      var ids = new HashSet<string>();
      AddAvailableConfigs(upgradeConfigs, context, candidates, ids);
      AddAvailableConfigs(runtimeConfigs, context, candidates, ids);

      for (int i = candidates.Count - 1; i > 0; i--)
      {
        int randomIndex = Random.Range(0, i + 1);
        UpgradeConfig temp = candidates[i];
        candidates[i] = candidates[randomIndex];
        candidates[randomIndex] = temp;
      }

      int resultCount = Mathf.Min(count, candidates.Count);
      var result = new UpgradeConfig[resultCount];
      for (int i = 0; i < resultCount; i++)
        result[i] = candidates[i];
      return result;
    }

    private void AddAvailableConfigs(IEnumerable<UpgradeConfig> configs, PlayerUpgradeContext context, List<UpgradeConfig> candidates, HashSet<string> ids)
    {
      if (configs == null) return;

      foreach (UpgradeConfig config in configs)
      {
        // 同一轮按 Id 去重，避免自定义配置与默认配置重复显示。
        if (config == null || !config.IsAvailable(context)) continue;
        // Id 由 UpgradeId 枚举和目标数据自动生成，不再使用 Inspector 中可随意填写的字符串。
        if (ids.Add(config.Id)) candidates.Add(config);
      }
    }

    private void BuildRuntimeConfigs(WeaponManager weaponManager)
    {
      if (runtimeConfigsBuilt || weaponManager == null) return;

      runtimeConfigsBuilt = true;
      // 复用现有 WeaponSO 作为数据源，不复制或改写武器资源。
      foreach (WeaponSO weapon in weaponManager.GetConfiguredWeapons())
      {
        if (weapon == null) continue;

        NewWeaponUpgradeConfig newWeapon = ScriptableObject.CreateInstance<NewWeaponUpgradeConfig>();
        newWeapon.InitializeRuntime(weapon);
        runtimeConfigs.Add(newWeapon);

        WeaponUpgradeConfig weaponUpgrade = ScriptableObject.CreateInstance<WeaponUpgradeConfig>();
        weaponUpgrade.InitializeRuntime(weapon);
        runtimeConfigs.Add(weaponUpgrade);
      }

      // 这些候选不是 ScriptableObject 资源，而是根据当前局状态动态创建；图标从场景上的 UpgradeManager 读取。
      AddRuntimePlayerUpgrade(PlayerUpgradeStat.MoveSpeed, 0.1f, true, moveSpeedUpgradeIcon);
      AddRuntimePlayerUpgrade(PlayerUpgradeStat.PickupRadius, 0.2f, true, pickupRadiusUpgradeIcon);
      AddRuntimePlayerUpgrade(PlayerUpgradeStat.MaxHealth, 20f, false, maxHealthUpgradeIcon);
    }

    /// <summary>创建一个默认玩家属性候选并加入运行时池。</summary>
    /// <param name="icon">该属性升级在三选一面板上显示的图标。</param>
    private void AddRuntimePlayerUpgrade(PlayerUpgradeStat stat, float value, bool isPercent, Sprite icon)
    {
      PlayerUpgradeConfig config = ScriptableObject.CreateInstance<PlayerUpgradeConfig>();
      config.InitializeRuntime(stat, value, isPercent, icon);
      runtimeConfigs.Add(config);
    }

    /// <summary>销毁运行时创建的 ScriptableObject，避免场景切换留下临时资源。</summary>
    protected override void OnDestroy()
    {
      foreach (UpgradeConfig config in runtimeConfigs)
      {
        if (config != null)
          Destroy(config);
      }
      runtimeConfigs.Clear();
      base.OnDestroy();
    }
  }
}
