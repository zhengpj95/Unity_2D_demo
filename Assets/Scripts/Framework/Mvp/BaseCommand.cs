/// <summary>
/// 模块命令层基类。
/// </summary>
public abstract class BaseCommand
{
  public BaseModule Module { get; private set; }

  internal void Initialize(BaseModule module)
  {
    Module = module;
    OnInit();
  }

  internal void Release()
  {
    OnRelease();
    Module = null;
  }

  /// <summary>
  /// 执行命令。具体命令可按业务参数类型重载或封装此方法。
  /// </summary>
  public virtual void Execute(object args = null)
  {
  }

  protected virtual void OnInit()
  {
  }

  protected virtual void OnRelease()
  {
  }
}