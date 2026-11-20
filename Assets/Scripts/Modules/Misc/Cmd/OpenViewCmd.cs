using System;
using UnityEngine;

/// <summary>
/// 通用界面打开参数：按模块名和模块内 ViewType 定位目标界面，并可携带传给 Presenter.OnOpen 的数据。
/// </summary>
public struct OpenViewArgs
{
  /// <summary>目标业务模块。</summary>
  public ModuleName moduleName;
  /// <summary>目标模块内登记的界面类型。</summary>
  public Enum viewType;
  /// <summary>传给目标 Presenter.OnOpen 的运行时参数；由具体界面自行约定类型，可为 null。</summary>
  public object data;

  /// <summary>
  /// 创建不携带额外数据的界面打开参数。
  /// </summary>
  /// <param name="moduleName">目标业务模块。</param>
  /// <param name="viewType">该模块内登记的界面类型枚举值。</param>
  public OpenViewArgs(ModuleName moduleName, Enum viewType) : this(moduleName, viewType, null) { }

  /// <summary>
  /// 创建携带额外数据的界面打开参数。
  /// </summary>
  /// <param name="moduleName">目标业务模块。</param>
  /// <param name="viewType">该模块内登记的界面类型枚举值。</param>
  /// <param name="data">传给 Presenter.OnOpen 的数据；由目标界面约定具体类型。</param>
  public OpenViewArgs(ModuleName moduleName, Enum viewType, object data)
  {
    this.moduleName = moduleName;
    this.viewType = viewType;
    this.data = data;
  }
}

/// <summary>
/// 接收 UI_OPEN_VIEW 事件，按参数中的模块名与 ViewType 打开对应界面。
/// Command 只负责解析参数和查找模块；界面的异步加载、缓存与生命周期由目标模块的 OpenWindow 处理。
/// </summary>
public sealed class OpenViewCmd : BaseCommand
{
  /// <summary>
  /// 打开参数指定的界面。
  /// </summary>
  /// <param name="context">事件上下文，数据类型必须为 OpenViewArgs。</param>
  public override async void Execute(EventContext context)
  {
    if (!context.TryGetData(out OpenViewArgs args))
    {
      Debug.LogError("[OpenViewCmd] 事件数据不是 OpenViewArgs，已忽略本次打开请求。");
      return;
    }

    // None 不是可打开的模块；viewType 为 null 时 BaseModule 也会拒绝，这里提前拦截以减少无效查找。
    if (args.moduleName == ModuleName.None || args.viewType == null)
    {
      Debug.LogError($"[OpenViewCmd] 参数无效：moduleName={args.moduleName}, viewType={args.viewType}。");
      return;
    }

    if (!ModuleManager.Instance.TryGetModule(args.moduleName, out BaseModule targetModule) || targetModule == null)
    {
      Debug.LogError($"[OpenViewCmd] 目标模块不存在：{args.moduleName}。");
      return;
    }

    if (!targetModule.IsInitialized)
    {
      Debug.LogError($"[OpenViewCmd] 目标模块尚未初始化：{args.moduleName}。");
      return;
    }

    try
    {
      // 使用非泛型 OpenWindow：viewType 是运行时参数，无法在编译期确定Presenter类型。
      // data 原样透传给 Presenter.OnOpen，类型契约由目标界面负责，Command 不解析其内容。
      BasePresenter presenter = await targetModule.OpenWindow(args.viewType, args.data);
      if (presenter == null)
        Debug.LogError($"[OpenViewCmd] 打开界面失败：module={args.moduleName}, viewType={args.viewType}。");
    }
    catch (Exception exception)
    {
      Debug.LogError($"[OpenViewCmd] 打开界面异常：module={args.moduleName}, viewType={args.viewType}, error={exception}");
    }
  }
}
