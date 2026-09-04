using System;
using UnityEngine;
using VampireSurvivorsLike;

/// <summary>
/// 编排 Survivor 一局战斗流程；不保存 UI 数据，也不让 Presenter 直接修改玩法。
/// </summary>
public sealed class SurvivorGameplayController
{
  private readonly SurvivorModule _module;
  private readonly SurvivorProxy _proxy;

  public SurvivorGameplayController(SurvivorModule module, SurvivorProxy proxy)
  {
    _module = module ?? throw new ArgumentNullException(nameof(module));
    _proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
  }

  public void OnExpCollected(int value)
  {
    _proxy.AddExp(value);
    _module.RefreshMainView();

    if (_proxy.Model.GameState == SurvivorGameState.Playing && _proxy.HasPendingLevelUp)
      OpenNextLevelUp();
  }

  public void SelectLevelUpOption(UpgradeConfig upgrade)
  {
    if (upgrade == null)
    {
      Debug.LogWarning("[SurvivorGameplayController] Selected upgrade is null.");
      return;
    }

    // 选择时再次校验，防止连续升级或外部状态变化后应用失效候选。
    PlayerUpgradeContext context = CreateUpgradeContext();
    if (!upgrade.IsAvailable(context))
    {
      Debug.LogWarning($"[SurvivorGameplayController] Upgrade is no longer available: {upgrade.Id}");
      if (_proxy.Model.GameState == SurvivorGameState.LevelUp)
        ResumePlaying();
      return;
    }

    upgrade.Apply(context);

    if (_proxy.HasPendingLevelUp)
    {
      OpenNextLevelUp();
      return;
    }

    ResumePlaying();
  }

  private void OpenNextLevelUp()
  {
    if (!_proxy.TryConsumePendingLevelUp())
      return;

    // 每次升级都基于最新的武器等级和槽位重新抽取候选。
    UpgradeConfig[] options = UpgradeManager.Instance.GetUpgradeOptions(3, CreateUpgradeContext());
    if (options.Length == 0)
    {
      Debug.LogWarning("[SurvivorGameplayController] No available upgrades; resuming gameplay.");
      ResumePlaying();
      return;
    }

    _proxy.SetGameState(SurvivorGameState.LevelUp);
    Time.timeScale = 0f;

    SurvivorSkillSelectPanelPresenter panel = _module.OpenSkillSelectPanel(
      new SurvivorSkillSelectArgs(options, SelectLevelUpOption));

    if (panel == null)
    {
      Debug.LogError("[SurvivorGameplayController] Failed to open skill select panel.");
      ResumePlaying();
    }
  }

  private void ResumePlaying()
  {
    _proxy.SetGameState(SurvivorGameState.Playing);
    Time.timeScale = 1f;
    _module.RefreshMainView();
  }

  private PlayerUpgradeContext CreateUpgradeContext()
  {
    // Controller 负责组装上下文，UpgradeConfig 本身不查找场景对象。
    GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
    Hero hero = playerObject == null ? null : playerObject.GetComponent<Hero>();
    VSPlayerHealth health = playerObject == null ? null : playerObject.GetComponent<VSPlayerHealth>();
    return new PlayerUpgradeContext(WeaponManager.Instance, hero, health);
  }
}
