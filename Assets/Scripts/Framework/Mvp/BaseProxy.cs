/// <summary>
/// 模块数据访问层基类。
/// </summary>
public abstract class BaseProxy
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

  protected virtual void OnInit()
  {
  }

  protected virtual void OnRelease()
  {
  }
}