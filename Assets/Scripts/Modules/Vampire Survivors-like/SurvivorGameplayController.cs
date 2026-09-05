using System;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    if (_proxy.Model.GameState == SurvivorGameState.GameOver)
      return;

    _proxy.AddExp(value);
    _module.RefreshMainView();

    if (_proxy.Model.GameState == SurvivorGameState.Playing && _proxy.HasPendingLevelUp)
      OpenNextLevelUp();
  }

  public void SelectLevelUpOption(UpgradeConfig upgrade)
  {
    if (_proxy.Model.GameState == SurvivorGameState.GameOver)
      return;

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

  /// <summary>
  /// 处理玩家死亡：收起可能仍打开的升级面板，冻结局内时间并展示本局结算。
  /// </summary>
  public void OnPlayerDied()
  {
    if (_proxy.Model.GameState == SurvivorGameState.GameOver)
      return;

    _module.HideSkillSelectPanel();
    _proxy.SetGameState(SurvivorGameState.GameOver);
    Time.timeScale = 0f;
    _module.RefreshMainView();

    SurvivorGameOverPresenter panel = _module.OpenGameOverPanel(
      new SurvivorGameOverArgs(_proxy.Model.Level, _proxy.Model.KillCount, _proxy.Model.CoinCount, RestartRound));
    if (panel == null)
      Debug.LogError("[SurvivorGameplayController] Failed to open game over panel.");
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

  /// <summary>
  /// 重置模块内的局内快照并重载当前场景，让场景组件、对象池对象与 Wave 计时重新初始化。
  /// </summary>
  private void RestartRound()
  {
    if (_proxy.Model.GameState != SurvivorGameState.GameOver)
      return;

    Scene activeScene = SceneManager.GetActiveScene();
    if (activeScene.buildIndex < 0)
    {
      Debug.LogError("[SurvivorGameplayController] Cannot restart an unloaded scene.");
      return;
    }

    ClearCurrentRoundEntities();
    _proxy.ResetRound();
    // UI 根节点跨场景保留，必须在场景重载前立即推送新的 Model，避免继续显示上一局血量。
    _module.RefreshMainView();
    Time.timeScale = 1f;
    SceneManager.LoadScene(activeScene.buildIndex);
  }

  /// <summary>
  /// 在场景重载前回收本局活跃实体，确保 GameOver 点击重开后不会遗留敌人或掉落物。
  /// 使用场景查找避免在异常场景配置下通过单例 getter 意外创建新 Manager。
  /// </summary>
  private static void ClearCurrentRoundEntities()
  {
    EnemyDirector enemyDirector = UnityEngine.Object.FindObjectOfType<EnemyDirector>();
    enemyDirector?.ClearActiveEnemies();

    DropItemManager dropItemManager = UnityEngine.Object.FindObjectOfType<DropItemManager>();
    dropItemManager?.ClearActiveDropItems();
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
