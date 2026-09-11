using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VampireSurvivorsLike;

/// <summary>
/// 编排 Survivor 一局战斗流程；不保存 UI 数据，也不让 Presenter 直接修改玩法。
/// </summary>
public sealed class SurvivorGameplayController
{
  // 场景名必须与 EditorBuildSettings 中启用的场景保持一致。
  private const string LauncherSceneName = "Launcher";
  private const string SurvivorsSceneName = "SurvivorsDemo";

  private readonly SurvivorModule _module;
  private readonly SurvivorProxy _proxy;
  private bool _isSceneTransitioning;

  public SurvivorGameplayController(SurvivorModule module, SurvivorProxy proxy)
  {
    _module = module ?? throw new ArgumentNullException(nameof(module));
    _proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
  }

  /// <summary>
  /// 响应 Home 的开始战斗请求：重置本局状态、隐藏局外界面并异步加载战斗场景。
  /// 重复点击由场景切换标记拦截，Presenter 不直接接触 SceneManager。
  /// </summary>
  public void StartBattle()
  {
    if (_isSceneTransitioning)
      return;

    if (string.Equals(SceneManager.GetActiveScene().name, SurvivorsSceneName, StringComparison.Ordinal))
    {
      Debug.Log("[SurvivorGameplayController] 已处于战斗场景，忽略重复开始请求。");
      return;
    }

    _isSceneTransitioning = true;
    _proxy.ResetRound();
    _module.HideSkillSelectPanel();
    _module.HideSurvivorMain();
    _module.HideSurvivorHome();
    Time.timeScale = 1f;

    LoadSceneAsync(
      SurvivorsSceneName,
      () =>
      {
        Time.timeScale = 1f;
        _module.OpenSurvivorMain();
      },
      () =>
      {
        Time.timeScale = 1f;
        _module.OpenSurvivorHome();
      });
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

    SurvivorGameOverPresenter panel = _module.OpenGameOverPanel(CreateGameOverArgs());
    if (panel == null)
      Debug.LogError("[SurvivorGameplayController] Failed to open game over panel.");
  }

  /// <summary>
  /// 从战斗结算返回 Launcher：先隐藏局内 UI、回收当前场景的池化对象，再加载并显示 Home。
  /// 加载期间保持暂停，避免场景卸载前仍有玩家移动或武器继续触发。
  /// </summary>
  private void ReturnToHome()
  {
    if (_isSceneTransitioning)
      return;

    _isSceneTransitioning = true;
    _module.HideSkillSelectPanel();
    _module.HideSurvivorMain();
    ClearCurrentRoundEntities();
    Time.timeScale = 0f;

    LoadSceneAsync(
      LauncherSceneName,
      () =>
      {
        _proxy.ResetRound();
        Time.timeScale = 1f;
        _module.OpenSurvivorHome();
      },
      () =>
      {
        // 加载失败时恢复 GameOver 界面，让玩家仍能重试或再次返回。
        Time.timeScale = 0f;
        _module.OpenSurvivorMain();
        _module.OpenGameOverPanel(CreateGameOverArgs());
      });
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
  /// 在场景重载前回收本局活跃实体，确保 GameOver 点击重开后不会遗留敌人、掉落物或武器攻击对象。
  /// 使用场景查找避免在异常场景配置下通过单例 getter 意外创建新 Manager。
  /// </summary>
  private static void ClearCurrentRoundEntities()
  {
    EnemyDirector enemyDirector = UnityEngine.Object.FindObjectOfType<EnemyDirector>();
    enemyDirector?.ClearActiveEnemies();

    DropItemManager dropItemManager = UnityEngine.Object.FindObjectOfType<DropItemManager>();
    dropItemManager?.ClearActiveDropItems();

    WeaponManager weaponManager = UnityEngine.Object.FindObjectOfType<WeaponManager>();
    weaponManager?.ClearActiveWeaponEffects();
  }

  /// <summary>创建本次结算窗口所需的数据和流程回调，避免失败恢复时复制参数组装逻辑。</summary>
  private SurvivorGameOverArgs CreateGameOverArgs()
  {
    return new SurvivorGameOverArgs(
      _proxy.Model.Level,
      _proxy.Model.KillCount,
      _proxy.Model.CoinCount,
      RestartRound,
      ReturnToHome);
  }

  /// <summary>
  /// 发起一次带成功/失败收口的单场景异步加载。
  /// 统一复位切换标记，防止场景配置错误后 Home 或 GameOver 按钮永久失效。
  /// </summary>
  /// <param name="sceneName">Build Settings 中注册的目标场景名。</param>
  /// <param name="onLoaded">场景加载并激活后的主线程回调。</param>
  /// <param name="onFailed">无法创建加载操作时的恢复回调。</param>
  private void LoadSceneAsync(string sceneName, Action onLoaded, Action onFailed)
  {
    AsyncOperation loadOperation;
    try
    {
      loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }
    catch (Exception exception)
    {
      _isSceneTransitioning = false;
      Debug.LogError($"[SurvivorGameplayController] 加载场景 '{sceneName}' 时发生异常：{exception}");
      onFailed?.Invoke();
      return;
    }

    if (loadOperation == null)
    {
      _isSceneTransitioning = false;
      Debug.LogError($"[SurvivorGameplayController] 无法创建场景加载操作：{sceneName}");
      onFailed?.Invoke();
      return;
    }

    loadOperation.completed += _ =>
    {
      _isSceneTransitioning = false;
      onLoaded?.Invoke();
    };
  }

  private PlayerUpgradeContext CreateUpgradeContext()
  {
    // Controller 注入当前 Module 和武器管理器，UpgradeConfig 不查找场景对象或直接修改 Hero。
    return new PlayerUpgradeContext(WeaponManager.Instance, _module);
  }
}
