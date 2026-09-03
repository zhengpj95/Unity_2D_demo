using System;
using UnityEngine;
using VampireSurvivorsLike;

public class SurvivorModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Survivor;

  private SurvivorProxy _proxy;
  private SurvivorGameplayController _gameplayController;

  protected override void OnInit()
  {
    _proxy = RegProxy<SurvivorProxy>();
    _gameplayController = new SurvivorGameplayController(this, _proxy);

    RegPresenter<SurvivorMainPresenter>(SurvivorViewType.Main);
    RegPresenter<SurvivorSkillSelectPanelPresenter>(SurvivorViewType.SkillSelect);
  }

  /// <summary>打开幸存者主界面，并按当前 Model 快照刷新显示。</summary>
  public SurvivorMainPresenter OpenSurvivorMain()
  {
    SurvivorMainPresenter presenter = OpenWindow<SurvivorMainPresenter>(SurvivorViewType.Main);
    RefreshMainPresenter(presenter);
    return presenter;
  }

  public void UpdateHp(int currentHealth, int maxHealth)
  {
    _proxy.SetHealth(currentHealth, maxHealth);
    RefreshMainView();
  }

  public void UpdateExp(int addExp)
  {
    _gameplayController.OnExpCollected(addExp);
  }

  public void UpdateEnemyKillCount()
  {
    _proxy.SetKillCount(EnemyDirector.Instance.KillEnemyCount);
    RefreshMainView();
  }

  public void AddDropItem(DropItemType dropItemType, int count)
  {
    _proxy.AddDropItem(dropItemType, count);
    RefreshMainView();
  }

  public void UpdateInventory()
  {
    RefreshMainView();
  }

  public SurvivorSkillSelectPanelPresenter OpenSkillSelectPanel(SurvivorSkillSelectArgs args)
  {
    return OpenWindow<SurvivorSkillSelectPanelPresenter>(SurvivorViewType.SkillSelect, args);
  }

  /// <summary>用 SurvivorModel 的当前快照刷新已打开的主界面。</summary>
  public void RefreshMainView()
  {
    RefreshMainPresenter(GetPresenter(SurvivorViewType.Main) as SurvivorMainPresenter);
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
      return;

    SurvivorModel model = _proxy.Model;
    presenter.Refresh(model);
  }
}
