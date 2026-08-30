using UnityEngine;
using VampireSurvivorsLike;

public class SurvivorModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Survivor;

  private int _currentHealth;
  private int _maxHealth;
  private bool _hasHealthState;

  protected override void OnInit()
  {
    RegPresenter<SurvivorMainPresenter>(SurvivorViewType.Main, "Prefabs/SurvivorMain", UILayerIndex.Window);
    RegPresenter<SurvivorSkillSelectPanelPresenter>(SurvivorViewType.SkillSelect, "Prefabs/SurvivorSkillSelectPanel", UILayerIndex.Model);
  }

  /// <summary>
  /// 打开幸存者主界面，并将 Presenter 注册、持有在当前模块中。
  /// </summary>
  public SurvivorMainPresenter OpenSurvivorMain()
  {
    SurvivorMainPresenter presenter = OpenWindow<SurvivorMainPresenter>(SurvivorViewType.Main);

    RefreshMainPresenter(presenter);
    return presenter;
  }

  public void UpdateHp(int currentHealth, int maxHealth)
  {
    _currentHealth = currentHealth;
    _maxHealth = maxHealth;
    _hasHealthState = true;

    (GetPresenter(SurvivorViewType.Main) as SurvivorMainPresenter)?.UpdateHp(currentHealth, maxHealth);
  }

  public void UpdateExp(int addExp)
  {
    SurvivorMainPresenter presenter = GetPresenter(SurvivorViewType.Main) as SurvivorMainPresenter;
    if (presenter == null)
    {
      Debug.LogWarning("[SurvivorModule] SurvivorMainPresenter is not open.");
      return;
    }

    presenter.UpdateExp(addExp, () => OpenSkillSelectPanel());
  }

  public void UpdateEnemyKillCount()
  {
    (GetPresenter(SurvivorViewType.Main) as SurvivorMainPresenter)?.UpdateEnemyKillCount(EnemySpawnManager.Instance.KillEnemyCount);
  }

  public void UpdateInventory()
  {
    (GetPresenter(SurvivorViewType.Main) as SurvivorMainPresenter)?.UpdateInventory(DropItemManager.Instance.GemCount, DropItemManager.Instance.CoinCount);
  }

  public SurvivorSkillSelectPanelPresenter OpenSkillSelectPanel()
  {
    return OpenWindow<SurvivorSkillSelectPanelPresenter>(SurvivorViewType.SkillSelect);
  }

  protected override void OnUpdate()
  {
    SurvivorSkillSelectPanelPresenter presenter = GetPresenter(SurvivorViewType.SkillSelect) as SurvivorSkillSelectPanelPresenter;
    if (presenter?.NeedUpdate == true)
      presenter.Update();
  }

  private void RefreshMainPresenter(SurvivorMainPresenter presenter)
  {
    if (presenter == null)
    {
      Debug.LogWarning("[SurvivorModule] Failed to open SurvivorMainPresenter.");
      return;
    }

    if (_hasHealthState)
      presenter.UpdateHp(_currentHealth, _maxHealth);

    presenter.UpdateEnemyKillCount(EnemySpawnManager.Instance.KillEnemyCount);
    presenter.UpdateInventory(DropItemManager.Instance.GemCount, DropItemManager.Instance.CoinCount);
  }
}
