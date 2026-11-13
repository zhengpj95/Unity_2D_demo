using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>管理 GuidePanel，协调引导步骤、GuideCursor/Presenter 路径目标与显示生命周期。</summary>
public sealed class GuidePresenter : BasePresenter<GuideView>
{
  private GuideModule _guideModule;
  private GuideRunner _runner;
  private GuideCursor _activeTarget;
  private Component _activePathTarget;
  private GuideStepConfig _pendingStep;

  /// <summary>引导界面显示在全局 Guide 层，位于顶层。</summary>
  public override UILayerIndex Layer => UILayerIndex.Guide;

  /// <summary>GuidePanel 预制体对应的稳定资源键。</summary>
  public override string PrefabPath => "Prefabs/Guide/GuidePanel";

  /// <summary>绑定 Prefab View，并验证其子节点配置。</summary>
  /// <param name="view">由 UIManager 实例化的 GuidePanel 视图。</param>
  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    ViewT.InitView();
  }

  /// <summary>显示引导层并订阅当前模块的流程通知。</summary>
  /// <param name="args">必须为已初始化的 GuideModule。</param>
  public override void OnOpen(object args = null)
  {
    if (args is not GuideModule module || module.Runner == null)
      throw new ArgumentException("[GuidePresenter] OnOpen requires an initialized GuideModule.", nameof(args));

    base.OnOpen(args);
    UnbindRunner();
    On(EventDefine.UI_PRESENTER_OPENED, OnPresenterOpened);
    On(EventDefine.UI_PRESENTER_CLOSED, OnPresenterClosed);
    GuideCursor.TargetActivated += OnTargetActivated;
    _guideModule = module;
    _runner = module.Runner;
    _runner.OnStepChanged += OnStepChanged;
    _runner.OnStateChanged += OnStateChanged;
    ViewT.transform.SetAsLastSibling();

    if (_runner.State == GuideState.Running && _runner.CurrentStep != null)
      ShowCurrentStep();
    else
      ViewT.HideStep();
  }

  /// <summary>关闭时解除 Runner 与目标监听，避免缓存 Presenter 保留已结束的引导引用。</summary>
  public override void OnClose()
  {
    UnbindRunner();
    ViewT.HideStep();
    base.OnClose();
  }

  private void OnStepChanged(GuideStepConfig step, int index, int total)
  {
    ShowStep(step);
  }

  private void OnStateChanged(GuideState state)
  {
    if (state == GuideState.Running)
    {
      ShowCurrentStep();
      return;
    }

    UnbindTarget();
    ViewT.HideStep();
    if (state == GuideState.None || state == GuideState.Completed)
    {
      GuideCursor.TargetActivated -= OnTargetActivated;
      UIManager.Instance.HidePresenter(this);
    }
  }

  private void ShowCurrentStep()
  {
    if (_runner != null && _runner.CurrentStep != null)
      ShowStep(_runner.CurrentStep);
  }

  private void ShowStep(GuideStepConfig step)
  {
    UnbindTarget();
    if (step == null)
    {
      Debug.LogError("[GuidePresenter] GuideRunner emitted a null step.", ViewT);
      ViewT.HideStep();
      return;
    }

    _pendingStep = step;
    if (!TryShowPendingStep())
    {
      ViewT.HideStep();
      string targetDescription = !string.IsNullOrWhiteSpace(step.TargetId)
        ? $"GuideCursor '{step.TargetId}'"
        : $"button '{step.TargetPath}' in Presenter '{step.PresenterId}'";
      Debug.Log($"[GuidePresenter] Waiting for {targetDescription}.", ViewT);
    }
  }

  private bool TryShowPendingStep()
  {
    if (_pendingStep == null)
      return false;

    GuideStepConfig step = _pendingStep;
    if (!string.IsNullOrWhiteSpace(step.TargetId))
    {
      GuideCursor target = FindTarget(step.TargetId, false);
      if (target == null)
        return false;

      _pendingStep = null;
      if (!target.HasRaycastableGraphic)
      {
        Debug.LogError($"[GuidePresenter] Target '{step.TargetId}' has no enabled raycastable UI Graphic.", target);
        ViewT.HideStep();
        return true;
      }

      if (!ViewT.ShowStep(step, target))
        return true;

      _activeTarget = target;
      _activeTarget.Clicked += OnTargetClicked;
      return true;
    }

    GuidePathFinder.FindResult result = GuidePathFinder.FindTarget(
      step.PresenterId,
      step.TargetPath,
      out Component pathTarget);
    if (result == GuidePathFinder.FindResult.PresenterUnavailable)
      return false;

    _pendingStep = null;
    if (result != GuidePathFinder.FindResult.Found)
    {
      Debug.LogError(
        $"[GuidePresenter] Cannot find guide target '{step.PresenterId}/{step.TargetPath}': {result}.",
        ViewT);
      return true;
    }

    if (!ViewT.ShowStep(step, (RectTransform)pathTarget.transform))
      return true;

    _activePathTarget = pathTarget;
    BindPathTarget(pathTarget);
    return true;
  }

  private void OnPresenterOpened(EventContext context)
  {
    if (_runner == null
        || _runner.State != GuideState.Running
        || _pendingStep == null
        || string.IsNullOrWhiteSpace(_pendingStep.PresenterId)
        || !context.TryGetData(out PresenterLifecycleEventArgs openedPresenter)
        || !GuidePathFinder.MatchesPresenter(_pendingStep.PresenterId, openedPresenter.ModuleName, openedPresenter.ViewType))
    {
      return;
    }

    TryShowPendingStep();
  }

  private void OnPresenterClosed(EventContext context)
  {
    if (_activePathTarget == null
        || _runner == null
        || _runner.State != GuideState.Running
        || _runner.CurrentStep == null
        || !context.TryGetData(out PresenterLifecycleEventArgs closedPresenter)
        || !GuidePathFinder.MatchesPresenter(_runner.CurrentStep.PresenterId, closedPresenter.ModuleName, closedPresenter.ViewType))
    {
      return;
    }

    GuideStepConfig currentStep = _runner.CurrentStep;
    UnbindTarget();
    _pendingStep = currentStep;
    ViewT.HideStep();
  }

  private void OnTargetActivated(GuideCursor target)
  {
    if (_runner == null
        || _runner.State != GuideState.Running
        || _pendingStep == null
        || target == null
        || !target.isActiveAndEnabled
        || target.TargetId != _pendingStep.TargetId)
    {
      return;
    }

    TryShowPendingStep();
  }

  private GuideCursor FindTarget(string targetId, bool logMissing = true)
  {
    GuideCursor found = null;
    GuideCursor[] targets = UnityEngine.Object.FindObjectsOfType<GuideCursor>(true);
    foreach (GuideCursor target in targets)
    {
      if (!target.isActiveAndEnabled || target.TargetId != targetId)
        continue;

      if (found != null)
      {
        Debug.LogError($"[GuidePresenter] Multiple active GuideCursor components use target ID '{targetId}'.", ViewT);
        return null;
      }

      found = target;
    }

    if (found == null && logMissing)
      Debug.LogError($"[GuidePresenter] No active GuideCursor matches target ID '{targetId}'.", ViewT);

    return found;
  }

  private void OnTargetClicked()
  {
    if (_runner != null && _runner.State == GuideState.Running)
      _guideModule?.Next();
  }

  private void BindPathTarget(Component target)
  {
    if (target is UIButton uiButton)
      uiButton.Clicked += OnTargetClicked;
    else if (target is Button button)
      button.onClick.AddListener(OnTargetClicked);
  }

  private void UnbindTarget()
  {
    _pendingStep = null;
    if (_activeTarget != null)
      _activeTarget.Clicked -= OnTargetClicked;

    if (_activePathTarget is UIButton uiButton)
      uiButton.Clicked -= OnTargetClicked;
    else if (_activePathTarget is Button button)
      button.onClick.RemoveListener(OnTargetClicked);

    _activeTarget = null;
    _activePathTarget = null;
  }

  private void UnbindRunner()
  {
    UnbindTarget();
    GuideCursor.TargetActivated -= OnTargetActivated;
    if (_runner != null)
    {
      _runner.OnStepChanged -= OnStepChanged;
      _runner.OnStateChanged -= OnStateChanged;
    }

    _runner = null;
    _guideModule = null;
  }
}
