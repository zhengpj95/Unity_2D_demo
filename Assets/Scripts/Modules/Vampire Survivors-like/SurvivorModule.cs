using UnityEngine;
using VampireSurvivorsLike;

public class SurvivorModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Survivor;

  private const string SurvivorMainPrefabPath = "Prefabs/SurvivorMain";
  private const string SurvivorSkillSelectPanelPrefabPath = "Prefabs/SurvivorSkillSelectPanel";

  private int _currentHealth;
  private int _maxHealth;
  private bool _hasHealthState;

  /// <summary>
  /// 打开幸存者主界面，并将 Presenter 注册、持有在当前模块中。
  /// </summary>
  public SurvivorMainPresenter OpenSurvivorMain()
  {
    SurvivorMainPresenter presenter = GetPresenter<SurvivorMainPresenter>();
    if (presenter != null)
    {
      UIManager.Instance.ShowPresenter(presenter);
    }
    else
    {
      presenter = OpenWindow<SurvivorMainPresenter>(SurvivorMainPrefabPath, UILayerIndex.Window);
    }

    RefreshMainPresenter(presenter);
    return presenter;
  }

  public void UpdateHp(int currentHealth, int maxHealth)
  {
    _currentHealth = currentHealth;
    _maxHealth = maxHealth;
    _hasHealthState = true;

    GetPresenter<SurvivorMainPresenter>()?.UpdateHp(currentHealth, maxHealth);
  }

  public void UpdateExp(int addExp)
  {
    SurvivorMainPresenter presenter = GetPresenter<SurvivorMainPresenter>();
    if (presenter == null)
    {
      Debug.LogWarning("[SurvivorModule] SurvivorMainPresenter is not open.");
      return;
    }

    presenter.UpdateExp(addExp, () => OpenSkillSelectPanel());
  }

  public void UpdateEnemyKillCount()
  {
    GetPresenter<SurvivorMainPresenter>()?.UpdateEnemyKillCount(EnemySpawnManager.Instance.KillEnemyCount);
  }

  public void UpdateInventory()
  {
    GetPresenter<SurvivorMainPresenter>()?.UpdateInventory(DropItemManager.Instance.GemCount, DropItemManager.Instance.CoinCount);
  }

  public SurvivorSkillSelectPanelPresenter OpenSkillSelectPanel()
  {
    SurvivorSkillSelectPanelPresenter presenter = GetPresenter<SurvivorSkillSelectPanelPresenter>();
    if (presenter != null)
    {
      UIManager.Instance.ShowPresenter(presenter);
      return presenter;
    }

    return OpenWindow<SurvivorSkillSelectPanelPresenter>(SurvivorSkillSelectPanelPrefabPath, UILayerIndex.Model);
  }

  protected override void OnUpdate()
  {
    SurvivorSkillSelectPanelPresenter presenter = GetPresenter<SurvivorSkillSelectPanelPresenter>();
    if (presenter?.NeedUpdate == true)
      presenter.Update();
  }

  private void RefreshMainPresenter(SurvivorMainPresenter presenter)
  {
    if (_hasHealthState)
      presenter.UpdateHp(_currentHealth, _maxHealth);

    presenter.UpdateEnemyKillCount(EnemySpawnManager.Instance.KillEnemyCount);
    presenter.UpdateInventory(DropItemManager.Instance.GemCount, DropItemManager.Instance.CoinCount);
  }
}
