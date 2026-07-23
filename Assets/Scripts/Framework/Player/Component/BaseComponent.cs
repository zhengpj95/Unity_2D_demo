/**
 * 组件基类
 */
public interface IComponent
{
  void OnUpdate();
}

public abstract class BaseComponent : IComponent
{
  public virtual void OnUpdate()
  {
    //
  }
}