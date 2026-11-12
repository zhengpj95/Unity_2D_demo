using System;
using System.Threading.Tasks;
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
    RegPresenter<GuidePresenter>(GuideViewType.Panel);
  }

  protected override void OnRelease()
  {
    Runner?.Stop();
    Runner = null;
  }

  /// <summary>启动引导并异步打开其顶层 Presenter；配置或资源无效时返回 false。</summary>
  /// <param name="config">完整的引导配置。</param>
  /// <returns>配置有效且 GuidePanel 成功打开时返回 true。</returns>
  public async Task<bool> StartGuide(GuideConfig config)
  {
    GuideRunner runner = GetRunner();
    if (!runner.Start(config))
    {
      Debug.LogWarning("[GuideModule] StartGuide rejected: config is null, empty, or contains invalid steps.");
      return false;
    }

    bool presenterOpened = false;
    try
    {
      GuidePresenter presenter = await OpenWindow<GuidePresenter>(GuideViewType.Panel, this);
      if (presenter == null)
      {
        Debug.LogError("[GuideModule] Failed to open GuidePanel; the guide has been stopped.");
        return false;
      }

      presenterOpened = true;
      return true;
    }
    finally
    {
      if (!presenterOpened && ReferenceEquals(Runner, runner))
        runner.Stop();
    }
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
