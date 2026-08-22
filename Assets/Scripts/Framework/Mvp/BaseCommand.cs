/// <summary>
/// 模块命令基类。
/// 事件订阅由 BaseModule 统一管理；Command 只实现收到事件后的执行逻辑。
/// </summary>
public abstract class BaseCommand
{
  /// <summary>命令所属模块，可用于获取本模块的 Proxy、Presenter 或其他 Command。</summary>
  public BaseModule Module { get; private set; }

  internal void SetModule(BaseModule module)
  {
    Module = module;
  }

  /// <summary>
  /// 执行命令。无参事件的 args 为 null；有参事件会传入对应的事件参数。
  /// </summary>
  public abstract void Execute(object args = null);
}
