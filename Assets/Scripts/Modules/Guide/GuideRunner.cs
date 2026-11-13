using System;
using System.Collections.Generic;

/// <summary>纯 C# 引导流程执行器；负责状态和步骤，不依赖 Unity UI。</summary>
public sealed class GuideRunner
{
  private GuideConfig _config;
  private int _currentIndex = -1;

  /// <summary>当前引导状态。</summary>
  public GuideState State { get; private set; }

  /// <summary>当前正在执行的配置；无活动步骤时为 null。</summary>
  public GuideConfig Config => _config;

  /// <summary>当前步骤的零起始索引；无活动步骤时为 -1。</summary>
  public int CurrentIndex => _currentIndex;

  /// <summary>当前步骤；配置或索引无效时为 null。</summary>
  public GuideStepConfig CurrentStep =>
    _config != null
    && _config.Steps != null
    && _currentIndex >= 0
    && _currentIndex < _config.Steps.Count
      ? _config.Steps[_currentIndex]
      : null;

  /// <summary>步骤变化通知，参数依次为步骤数据、零起始索引和步骤总数。</summary>
  public event Action<GuideStepConfig, int, int> OnStepChanged;

  /// <summary>状态变化通知。</summary>
  public event Action<GuideState> OnStateChanged;

  /// <summary>整套引导完成时触发；Stop 不会触发此事件。</summary>
  public event Action OnCompleted;

  /// <summary>验证并开始一套引导，从第一个步骤开始执行。</summary>
  /// <param name="config">待执行的完整引导配置。</param>
  /// <returns>配置有效并成功开始时返回 true。</returns>
  public bool Start(GuideConfig config)
  {
    if (!IsValid(config))
      return false;

    if (State == GuideState.Running || State == GuideState.Paused)
      Stop();

    _config = config;
    _currentIndex = 0;
    SetState(GuideState.Running);
    OnStepChanged?.Invoke(CurrentStep, _currentIndex, _config.Steps.Count);
    return true;
  }

  /// <summary>推进到下一步骤；仅 Running 状态有效，越过末尾时完成整套引导。</summary>
  public void Next()
  {
    if (State != GuideState.Running || CurrentStep == null)
      return;

    int nextIndex = _currentIndex + 1;
    if (nextIndex >= _config.Steps.Count)
    {
      Complete();
      return;
    }

    _currentIndex = nextIndex;
    OnStepChanged?.Invoke(CurrentStep, _currentIndex, _config.Steps.Count);
  }

  /// <summary>暂停当前步骤；仅 Running 状态有效。</summary>
  public void Pause()
  {
    if (State == GuideState.Running)
      SetState(GuideState.Paused);
  }

  /// <summary>恢复当前步骤；仅 Paused 状态有效，不重复发送步骤事件。</summary>
  public void Resume()
  {
    if (State == GuideState.Paused)
      SetState(GuideState.Running);
  }

  /// <summary>停止并清除当前进度；不触发完成事件。</summary>
  public void Stop()
  {
    _config = null;
    _currentIndex = -1;
    SetState(GuideState.None);
  }

  private bool IsValid(GuideConfig config)
  {
    if (config == null || config.Steps == null || config.Steps.Count == 0)
      return false;

    HashSet<int> stepIds = new HashSet<int>();
    foreach (GuideStepConfig step in config.Steps)
    {
      if (step == null || !stepIds.Add(step.StepId))
        return false;

      bool hasCursorTarget = !string.IsNullOrWhiteSpace(step.TargetId);
      bool hasPresenterId = !string.IsNullOrWhiteSpace(step.PresenterId);
      bool hasTargetPath = !string.IsNullOrWhiteSpace(step.TargetPath);
      if (hasCursorTarget
            ? hasPresenterId || hasTargetPath
            : !hasPresenterId || !hasTargetPath)
        return false;
    }

    return true;
  }

  private void Complete()
  {
    _config = null;
    _currentIndex = -1;
    SetState(GuideState.Completed);
    OnCompleted?.Invoke();
  }

  private void SetState(GuideState state)
  {
    if (State == state)
      return;

    State = state;
    OnStateChanged?.Invoke(State);
  }
}
