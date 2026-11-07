/// <summary>引导流程的运行状态。</summary>
public enum GuideState
{
  /// <summary>当前没有活动引导。</summary>
  None,
  /// <summary>引导正在执行。</summary>
  Running,
  /// <summary>引导已暂停。</summary>
  Paused,
  /// <summary>整套引导已完成。</summary>
  Completed
}
