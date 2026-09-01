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

  public void SelectLevelUpOption(WeaponSO weapon)
  {
    if (weapon == null)
    {
      Debug.LogWarning("[SurvivorGameplayController] Selected weapon is null.");
      return;
    }

    WeaponManager.Instance.AddOrUpgrade(weapon);

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

    _proxy.SetGameState(SurvivorGameState.LevelUp);
    Time.timeScale = 0f;

    SurvivorSkillSelectPanelPresenter panel = _module.OpenSkillSelectPanel(
      new SurvivorSkillSelectArgs(SelectLevelUpOption));

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
}
