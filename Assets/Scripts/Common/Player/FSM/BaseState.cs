/**
 * FSM
 */
public interface IState
{
  void OnEnter();
  void OnExit();
  void OnUpdate();
}

public abstract class BaseState : IState
{
  protected Character owner;

  protected BaseState(Character owner)
  {
    this.owner = owner;
  }

  public virtual void OnEnter()
  {
    // 
  }

  public virtual void OnExit()
  {
    //
  }

  public virtual void OnUpdate()
  {
    //
  }
}