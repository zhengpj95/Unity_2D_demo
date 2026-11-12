using System;
using UnityEngine;

/// <summary>管理 GuidePanel 预制体，协调引导步骤、目标点击和顶层显示生命周期。</summary>
public sealed class GuidePresenter : BasePresenter<GuideView>
{
  private GuideModule _guideModule;
  private GuideRunner _runner;
  private GuideCursor _activeTarget;

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
      UIManager.Instance.HidePresenter(this);
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

    GuideCursor target = FindTarget(step.TargetId);
    if (target == null)
    {
      ViewT.HideStep();
      return;
    }

    if (!target.HasRaycastableGraphic)
    {
      Debug.LogError($"[GuidePresenter] Target '{step.TargetId}' has no enabled raycastable UI Graphic.", target);
      ViewT.HideStep();
      return;
    }

    if (!ViewT.ShowStep(step, target))
      return;

    _activeTarget = target;
    _activeTarget.Clicked += OnTargetClicked;
  }

  private GuideCursor FindTarget(string targetId)
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

    if (found == null)
      Debug.LogError($"[GuidePresenter] No active GuideCursor matches target ID '{targetId}'.", ViewT);

    return found;
  }

  private void OnTargetClicked()
  {
    if (_runner != null && _runner.State == GuideState.Running && _activeTarget != null)
      _guideModule?.Next();
  }

  private void UnbindTarget()
  {
    if (_activeTarget != null)
      _activeTarget.Clicked -= OnTargetClicked;

    _activeTarget = null;
  }

  private void UnbindRunner()
  {
    UnbindTarget();
    if (_runner != null)
    {
      _runner.OnStepChanged -= OnStepChanged;
      _runner.OnStateChanged -= OnStateChanged;
    }

    _runner = null;
    _guideModule = null;
  }
}
