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
    RegPresenter<SurvivorGameOverPresenter>(SurvivorViewType.GameOver);
  }

  /// <summary>打开幸存者主界面，并按当前 Model 快照刷新显示。</summary>
  public SurvivorMainPresenter OpenSurvivorMain()
  {
    SurvivorMainPresenter presenter = OpenWindow<SurvivorMainPresenter>(SurvivorViewType.Main);
    RefreshMainPresenter(presenter);
    return presenter;
  }

  /// <summary>结算一次玩家伤害，返回更新后的 Model 供生命组件判断死亡。</summary>
  public SurvivorModel ApplyPlayerDamage(int damage)
  {
    _proxy.ApplyDamage(damage);
    RefreshMainView();
    return _proxy.Model;
  }

  /// <summary>仅供 GameOver 测试配置生命；正式初始生命由 SurvivorModel 提供。</summary>
  public void OverridePlayerHealthForTesting(int maxHealth)
  {
    _proxy.OverrideHealthForTesting(maxHealth);
    RefreshMainView();
  }

  /// <summary>应用最大生命升级；实际数据由 SurvivorProxy 持有和修改。</summary>
  public void ApplyPlayerMaxHealthUpgrade(float value, bool isPercent)
  {
    _proxy.AddMaxHealth(value, isPercent);
    RefreshMainView();
  }

  public void UpdateExp(int addExp)
  {
    _gameplayController.OnExpCollected(addExp);
  }

  /// <summary>接收玩家生命组件上报的死亡事件，并交由 GameplayController 编排结算流程。</summary>
  public void OnPlayerDied()
  {
    _gameplayController.OnPlayerDied();
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

  /// <summary>打开本局结束结算窗口，重开操作由 Presenter 回调给 GameplayController。</summary>
  public SurvivorGameOverPresenter OpenGameOverPanel(SurvivorGameOverArgs args)
  {
    return OpenWindow<SurvivorGameOverPresenter>(SurvivorViewType.GameOver, args);
  }

  /// <summary>玩家死亡时关闭升级选择，防止暂停界面继续倒计时或回调升级逻辑。</summary>
  public void HideSkillSelectPanel()
  {
    SurvivorSkillSelectPanelPresenter presenter = GetPresenter(SurvivorViewType.SkillSelect) as SurvivorSkillSelectPanelPresenter;
    if (presenter != null)
      UIManager.Instance.HidePresenter(presenter);
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

  protected override void OnRelease()
  {
    // 模块在暂停或结算状态被释放时恢复全局时间，避免切场景后仍保持冻结。
    Time.timeScale = 1f;
  }

  private void RefreshMainPresenter(SurvivorMainPresenter presenter)
  {
    if (presenter == null)
      return;

    SurvivorModel model = _proxy.Model;
    presenter.Refresh(model);
  }
}
