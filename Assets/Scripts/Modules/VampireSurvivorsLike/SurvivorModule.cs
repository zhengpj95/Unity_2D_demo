using System;
using System.Threading.Tasks;
using UnityEngine;
using VampireSurvivorsLike;

public class SurvivorModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Survivor;

  private SurvivorProxy _proxy;
  private SurvivorGameplayController _gameplayController;
  private Vector2 _moveInput;

  /// <summary>当前屏幕摇杆输出的移动向量；由局内 HUD Presenter 写入，Hero 每帧读取。</summary>
  public Vector2 MoveInput => _moveInput;

  /// <summary>设置屏幕摇杆方向；零向量表示摇杆归中。</summary>
  public void SetMoveInput(Vector2 moveInput)
  {
    _moveInput = Vector2.ClampMagnitude(moveInput, 1f);
  }

  protected override void OnInit()
  {
    Debug.Log("[SurvivorModule] Initialized");
    _proxy = RegProxy<SurvivorProxy>();
    _gameplayController = new SurvivorGameplayController(this, _proxy);

    RegPresenter<SurvivorHomePresenter>(SurvivorViewType.Home);
    RegPresenter<SurvivorMainPresenter>(SurvivorViewType.Main);
    RegPresenter<SurvivorSkillSelectPanelPresenter>(SurvivorViewType.SkillSelect);
    RegPresenter<SurvivorGameOverPresenter>(SurvivorViewType.GameOver);
  }

  /// <summary>统一打开 Launcher 场景中的 Survivor 局外主界面。</summary>
  public Task<SurvivorHomePresenter> OpenSurvivorHome()
  {
    return OpenWindow<SurvivorHomePresenter>(
      SurvivorViewType.Home,
      new SurvivorHomeArgs(_gameplayController.StartBattle));
  }

  /// <summary>统一打开幸存者主界面；HUD 显示由 Presenter 从 Proxy 自行读取。</summary>
  public Task<SurvivorMainPresenter> OpenSurvivorMain()
  {
    return OpenWindow<SurvivorMainPresenter>(SurvivorViewType.Main);
  }

  /// <summary>结算一次玩家伤害，返回更新后的 Model 供生命组件判断死亡。</summary>
  public SurvivorModel ApplyPlayerDamage(int damage)
  {
    _proxy.ApplyDamage(damage);
    NotifyModelChanged();
    return _proxy.Model;
  }

  /// <summary>仅供 GameOver 测试配置生命；正式初始生命由 SurvivorModel 提供。</summary>
  public void OverridePlayerHealthForTesting(int maxHealth)
  {
    _proxy.OverrideHealthForTesting(maxHealth);
    NotifyModelChanged();
  }

  /// <summary>兼容旧调用入口：应用最大生命升级。</summary>
  public void ApplyPlayerMaxHealthUpgrade(float value, bool isPercent)
  {
    ApplyPlayerStatUpgrade(PlayerStat.MaxHealth, value, isPercent);
  }

  /// <summary>
  /// 通过 SurvivorProxy 应用一条本局永久玩家属性升级，并刷新依赖 Model 的界面。
  /// </summary>
  public bool ApplyPlayerStatUpgrade(PlayerStat stat, float value, bool isPercent)
  {
    bool applied = _proxy.ApplyPlayerStatUpgrade(stat, value, isPercent);
    if (applied)
      NotifyModelChanged();
    return applied;
  }

  /// <summary>读取指定玩家属性的本局永久修正。</summary>
  public PlayerStatModifier GetPermanentPlayerStatModifier(PlayerStat stat)
  {
    return _proxy.GetPermanentPlayerStatModifier(stat);
  }

  /// <summary>合并基础值、本局永久升级和临时 Buff，返回实体实际使用的最终属性。</summary>
  public float CalculatePlayerStat(PlayerStat stat, float baseValue, PlayerStatModifier temporaryModifier)
  {
    return _proxy.CalculatePlayerStat(stat, baseValue, temporaryModifier);
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
    NotifyModelChanged();
  }

  public void AddDropItem(DropItemType dropItemType, int count)
  {
    _proxy.AddDropItem(dropItemType, count);
    NotifyModelChanged();
  }

  public void UpdateInventory()
  {
    NotifyModelChanged();
  }

  public Task<SurvivorSkillSelectPanelPresenter> OpenSkillSelectPanel(SurvivorSkillSelectArgs args)
  {
    return OpenWindow<SurvivorSkillSelectPanelPresenter>(SurvivorViewType.SkillSelect, args);
  }

  /// <summary>打开本局结束结算窗口，重开和返回主页操作都由 Presenter 回调给 GameplayController。</summary>
  public Task<SurvivorGameOverPresenter> OpenGameOverPanel(SurvivorGameOverArgs args)
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

  /// <summary>战斗场景加载完成后隐藏局外主界面，并保留 Presenter 缓存供返回时复用。</summary>
  public void HideSurvivorHome()
  {
    HidePresenter(SurvivorViewType.Home);
  }

  /// <summary>离开战斗场景前隐藏局内 HUD，并保留 Presenter 缓存供下一局复用。</summary>
  public void HideSurvivorMain()
  {
    HidePresenter(SurvivorViewType.Main);
  }

  /// <summary>广播本局数据变化；HUD Presenter 订阅后自行从 Proxy 读取并刷新，Module 不直接操作 Presenter。</summary>
  public void NotifyModelChanged()
  {
    EventBus.Emit(EventDefine.SURVIVOR_MODEL_CHANGED);
  }

  protected override void OnRelease()
  {
    // 模块在暂停或结算状态被释放时恢复全局时间，避免切场景后仍保持冻结。
    Time.timeScale = 1f;
  }

  /// <summary>按 ViewType 隐藏已创建的 Presenter；未打开时不做处理。</summary>
  private void HidePresenter(SurvivorViewType viewType)
  {
    BasePresenter presenter = GetPresenter(viewType);
    if (presenter != null)
      UIManager.Instance.HidePresenter(presenter);
  }
}
