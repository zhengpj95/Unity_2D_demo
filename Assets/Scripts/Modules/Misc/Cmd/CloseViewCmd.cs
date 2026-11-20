using System;
using UnityEngine;

/// <summary>
/// 通用界面关闭参数：按模块名和模块内 ViewType 定位目标界面。
/// </summary>
public struct CloseViewArgs
{
  /// <summary>目标业务模块。</summary>
  public ModuleName moduleName;
  /// <summary>目标模块内登记的界面类型。</summary>
  public Enum viewType;

  /// <summary>
  /// 创建界面关闭参数。
  /// </summary>
  /// <param name="moduleName">目标业务模块。</param>
  /// <param name="viewType">该模块内登记的界面类型枚举值。</param>
  public CloseViewArgs(ModuleName moduleName, Enum viewType)
  {
    this.moduleName = moduleName;
    this.viewType = viewType;
  }
}

/// <summary>
/// 接收 UI_CLOSE_VIEW 事件，按参数中的模块名与 ViewType 关闭对应界面。
/// 关闭会销毁 Presenter 实例并释放资源；下次打开时重新加载。
/// 若只需临时收起界面并保留缓存，应调用 UIManager.HidePresenter，而不是本命令。
/// </summary>
public sealed class CloseViewCmd : BaseCommand
{
  /// <summary>
  /// 关闭参数指定的界面。
  /// </summary>
  /// <param name="context">事件上下文，数据类型必须为 CloseViewArgs。</param>
  public override void Execute(EventContext context)
  {
    if (!context.TryGetData(out CloseViewArgs args))
    {
      Debug.LogError("[CloseViewCmd] 事件数据不是 CloseViewArgs，已忽略本次关闭请求。");
      return;
    }

    // None 不是有效模块；viewType 为 null 时 BaseModule 也会拒绝，这里提前拦截。
    if (args.moduleName == ModuleName.None || args.viewType == null)
    {
      Debug.LogError($"[CloseViewCmd] 参数无效：moduleName={args.moduleName}, viewType={args.viewType}。");
      return;
    }

    if (!ModuleManager.Instance.TryGetModule(args.moduleName, out BaseModule targetModule) || targetModule == null)
    {
      Debug.LogError($"[CloseViewCmd] 目标模块不存在：{args.moduleName}。");
      return;
    }

    if (!targetModule.IsInitialized)
    {
      Debug.LogError($"[CloseViewCmd] 目标模块尚未初始化：{args.moduleName}。");
      return;
    }

    // 先取消尚未完成的异步打开，避免关闭后加载完成又把界面显示出来。
    targetModule.CancelPendingOpen(args.viewType);

    BasePresenter presenter = targetModule.GetPresenter(args.viewType);
    if (presenter == null)
    {
      Debug.LogWarning($"[CloseViewCmd] 界面未打开，无需关闭：module={args.moduleName}, viewType={args.viewType}。");
      return;
    }

    UIManager.Instance.CloseWindow(presenter);
  }
}
