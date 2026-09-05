using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VampireSurvivorsLike;

/// <summary>
/// 仅用于验证 Survivor GameOver 闭环的临时测试组件。
/// 挂载在 SurvivorsDemo 的 EnemyDirector 对象上；删除或禁用该组件并重载场景即可恢复正式数值。
/// </summary>
[DisallowMultipleComponent]
public sealed class SurvivorGameOverTestSetup : MonoBehaviour
{
  [Header("GameOver 测试开关")]
  [Tooltip("关闭后不会覆盖任何局内数值；删除本组件同样可以完全移除测试。")]
  [SerializeField] private bool enableTestMode = true;
  [Tooltip("测试局玩家的初始最大生命，设为 1 可在首次碰撞后快速验证结算。")]
  [SerializeField, Min(1)] private int initialHealth = 1;
  [Tooltip("所有已配置武器的伤害倍率。0.25 会使当前 1 点伤害的初始箭矢变为 0。")]
  [SerializeField, Range(0f, 1f)] private float weaponDamageMultiplier = 0.25f;
  [Tooltip("关闭本局已创建武器控制器，避免测试过程中继续发射投射物或生成攻击特效。")]
  [SerializeField] private bool disableWeaponFiring = true;

  private readonly Dictionary<WeaponLevelData, int> _originalDamages = new Dictionary<WeaponLevelData, int>();
  private readonly Dictionary<WeaponController, bool> _weaponEnabledStates = new Dictionary<WeaponController, bool>();
  private bool _damageOverridden;

  private IEnumerator Start()
  {
    if (!enableTestMode)
      yield break;

    // 等待 GameMgr 初始化 SurvivorModule；测试值直接写入 SurvivorModel，不经过 VSPlayerHealth。
    SurvivorModule survivorModule = null;
    const int maxWaitFrames = 10;
    for (int frame = 0; frame < maxWaitFrames; frame++)
    {
      if (ModuleManager.IsCreated)
      {
        survivorModule = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
        if (survivorModule != null)
          break;
      }

      yield return null;
    }

    if (survivorModule == null)
    {
      Debug.LogWarning("[SurvivorGameOverTestSetup] SurvivorModule 尚未初始化；请从 Launcher 场景进入 SurvivorsDemo 后再使用测试模式。", this);
      yield break;
    }

    survivorModule.OverridePlayerHealthForTesting(initialHealth);

    // 此时 Hero.Start 已创建初始武器，再覆盖伤害并禁用发射；不会修改 Project 中的 WeaponSO 资源文件。
    ApplyWeaponDamageOverrides();
    DisableWeaponFiring();
  }

  private void OnDisable()
  {
    RestoreWeaponDamages();
    RestoreWeaponFiring();
  }

  private void ApplyWeaponDamageOverrides()
  {
    WeaponManager weaponManager = WeaponManager.Instance;
    if (weaponManager == null)
    {
      Debug.LogWarning("[SurvivorGameOverTestSetup] WeaponManager 不存在，无法覆盖武器伤害。", this);
      return;
    }

    foreach (WeaponSO weapon in weaponManager.GetConfiguredWeapons())
      OverrideWeaponDamage(weapon);

    _damageOverridden = _originalDamages.Count > 0;
    Debug.Log($"[SurvivorGameOverTestSetup] 测试模式已启用：初始生命={initialHealth}，武器伤害倍率={weaponDamageMultiplier:F2}。禁用或删除本组件后重载场景即可恢复。", this);
  }

  /// <summary>关闭当前局已经创建的武器控制器；不改写 WeaponSO 或正式武器逻辑。</summary>
  private void DisableWeaponFiring()
  {
    if (!disableWeaponFiring)
      return;

    WeaponController[] weaponControllers = FindObjectsOfType<WeaponController>();
    for (int i = 0; i < weaponControllers.Length; i++)
    {
      WeaponController weaponController = weaponControllers[i];
      if (weaponController == null || _weaponEnabledStates.ContainsKey(weaponController))
        continue;

      _weaponEnabledStates.Add(weaponController, weaponController.enabled);
      weaponController.enabled = false;
    }
  }

  private void OverrideWeaponDamage(WeaponSO weapon)
  {
    if (weapon == null || weapon.levels == null)
      return;

    for (int i = 0; i < weapon.levels.Length; i++)
    {
      WeaponLevelData levelData = weapon.levels[i];
      if (levelData == null || _originalDamages.ContainsKey(levelData))
        continue;

      _originalDamages.Add(levelData, levelData.damage);
      levelData.damage = Mathf.FloorToInt(levelData.damage * weaponDamageMultiplier);
    }
  }

  private void RestoreWeaponDamages()
  {
    if (!_damageOverridden)
      return;

    foreach (KeyValuePair<WeaponLevelData, int> pair in _originalDamages)
    {
      if (pair.Key != null)
        pair.Key.damage = pair.Value;
    }

    _originalDamages.Clear();
    _damageOverridden = false;
  }

  /// <summary>恢复测试组件关闭前记录的武器控制器启用状态。</summary>
  private void RestoreWeaponFiring()
  {
    foreach (KeyValuePair<WeaponController, bool> pair in _weaponEnabledStates)
    {
      if (pair.Key != null)
        pair.Key.enabled = pair.Value;
    }

    _weaponEnabledStates.Clear();
  }
}
