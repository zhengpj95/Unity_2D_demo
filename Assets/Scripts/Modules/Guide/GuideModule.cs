using System;
using UnityEngine;

/// <summary>引导模块入口；持有并统一转发引导流程 API，不负责 UI 展示。</summary>
public sealed class GuideModule : BaseModule
{
  public override ModuleName ModuleName => ModuleName.Guide;

  /// <summary>当前模块持有的引导流程执行器；模块释放后为 null。</summary>
  public GuideRunner Runner { get; private set; }

  protected override void OnInit()
  {
    Runner = new GuideRunner();
  }

  protected override void OnRelease()
  {
    Runner?.Stop();
    Runner = null;
  }

  /// <summary>使用配置开始一套引导；配置无效时返回 false 且不会开始流程。</summary>
  /// <param name="config">完整的引导配置。</param>
  /// <returns>配置有效并成功开始时返回 true。</returns>
  public bool StartGuide(GuideConfig config)
  {
    bool started = GetRunner().Start(config);
    if (!started)
      Debug.LogWarning("[GuideModule] StartGuide rejected: config is null, empty, or contains invalid steps.");

    return started;
  }

  /// <summary>推进到下一步骤；仅 Running 状态会生效。</summary>
  public void Next() => GetRunner().Next();

  /// <summary>暂停当前引导；仅 Running 状态会生效。</summary>
  public void Pause() => GetRunner().Pause();

  /// <summary>恢复已暂停的引导；仅 Paused 状态会生效。</summary>
  public void Resume() => GetRunner().Resume();

  /// <summary>停止当前引导并清除进度，不触发完成事件。</summary>
  public void Stop() => GetRunner().Stop();

  private GuideRunner GetRunner()
  {
    return Runner ?? throw new InvalidOperationException("[GuideModule] GuideRunner is unavailable because the module is not initialized or has been released.");
  }
}
